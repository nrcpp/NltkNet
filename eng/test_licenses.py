import hashlib
import io
import json
import tarfile
import tempfile
import unittest
import zipfile
from pathlib import Path

import collect_licenses as collector
from configure_linux_runtime import configure
from verify_licenses import verify


class LicenseCollectionTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.runtime = self.root / "python"
        self.upstream = self.root / "upstream"
        self.repository = self.root / "repository"
        for directory in (self.runtime, self.upstream / "licenses", self.repository / "eng"):
            directory.mkdir(parents=True)
        (self.repository / "LICENSE").write_text("MIT test license\n", encoding="utf-8")
        (self.repository / "eng/THIRD-PARTY-NOTICES.txt").write_text("Test notices\n", encoding="utf-8")
        (self.runtime / "PYTHON-BUILD-STANDALONE-LICENSE").write_text("MPL test license\n", encoding="utf-8")
        (self.runtime / "wheel-install-report.json").write_text("{}", encoding="utf-8")
        (self.upstream / "provenance.json").write_text("{}", encoding="utf-8")
        (self.upstream / "licenses/LICENSE.cpython.txt").write_text("PSF test license\n", encoding="utf-8")
        self.metadata = {
            "target_triple": "aarch64-apple-darwin",
            "licenses": ["Python-2.0"],
            "license_path": "licenses/LICENSE.cpython.txt",
            "build_info": {"extensions": {}},
        }
        self.site = self.runtime / "lib/python3.12/site-packages"
        self.distribution = self.site / "example-1.0.dist-info"
        self.distribution.mkdir(parents=True)
        (self.distribution / "METADATA").write_text(
            "Name: example\nVersion: 1.0\nLicense-Expression: MIT\nLicense-File: LICENSE.txt\n",
            encoding="utf-8",
        )
        self.license_file = self.distribution / "licenses/LICENSE.txt"
        self.license_file.parent.mkdir()
        self.license_file.write_text("Copyright: Example\nMIT terms\n", encoding="utf-8")

    def collect(self) -> Path:
        (self.upstream / "PYTHON.json").write_text(json.dumps(self.metadata), encoding="utf-8")
        return collector.collect(self.runtime, self.upstream, self.repository, "osx-arm64")

    def package(self, destination: Path, omit: str = "") -> Path:
        payload = io.BytesIO()
        with tarfile.open(fileobj=payload, mode="w:gz") as runtime:
            runtime.add(self.runtime, arcname="python")
        package = self.root / "example.nupkg"
        with zipfile.ZipFile(package, "w") as archive:
            archive.writestr(
                "example.nuspec",
                '<package xmlns="http://schemas.microsoft.com/packaging/2011/10/nuspec.xsd">'
                '<metadata><license type="file">licenses/LICENSE.txt</license></metadata></package>',
            )
            for path in destination.rglob("*"):
                if path.is_file() and path.name != omit:
                    archive.write(path, "licenses/" + path.relative_to(destination).as_posix())
            archive.writestr("runtimes/osx-arm64/native/python.tar.gz", payload.getvalue())
            archive.writestr(
                "runtimes/osx-arm64/native/python.tar.gz.sha256",
                hashlib.sha256(payload.getvalue()).hexdigest(),
            )
        return package

    def test_preserves_originals_hashes_and_full_combined_text(self) -> None:
        vendor = self.site / "example/vendor/NOTICE.txt"
        vendor.parent.mkdir(parents=True)
        vendor.write_text("Vendored copyright\n", encoding="utf-8")
        destination = self.collect()
        inventory = json.loads((destination / "inventory.json").read_text())
        self.assertEqual(len(inventory["python_packages"]), 1)
        for name, checksum in inventory["documents"].items():
            path = destination / name
            self.assertEqual(collector.digest(path), checksum)
            self.assertIn(path.read_text(), (destination / "LICENSE.txt").read_text())
        self.assertIn("python-packages/example/vendor/NOTICE.txt", inventory["documents"])
        self.assertFalse(inventory["legal_clearance"])

    def test_missing_declared_license_is_an_error(self) -> None:
        self.license_file.unlink()
        with self.assertRaisesRegex(ValueError, "declared License-File is missing"):
            self.collect()

    def test_crlf_license_text_is_preserved_in_combined_document(self) -> None:
        self.license_file.write_bytes(b"Copyright: Example\r\nMIT terms\r\n")
        destination = self.collect()
        self.assertIn(self.license_file.read_bytes(), (destination / "LICENSE.txt").read_bytes())
        verify(self.package(destination))

    def test_missing_extensionless_notice_is_an_error(self) -> None:
        vendor = self.site / "example/vendor/LICENSE"
        vendor.parent.mkdir(parents=True)
        vendor.write_text("Vendor terms", encoding="utf-8")
        destination = self.collect()
        with self.assertRaises(KeyError):
            verify(self.package(destination, omit="LICENSE"))

    def test_packaged_licenses_do_not_imply_publication_clearance(self) -> None:
        destination = self.collect()
        with self.assertRaisesRegex(ValueError, "publication is not cleared"):
            verify(self.package(destination), require_publishable=True)

    def test_changed_consolidated_license_is_an_error(self) -> None:
        destination = self.collect()
        (destination / "LICENSE.txt").write_text("Incomplete license\n", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "consolidated license omits"):
            verify(self.package(destination))

    def test_missing_native_license_is_an_error(self) -> None:
        self.metadata["build_info"]["extensions"] = {
            "_example": [{"license_paths": ["licenses/missing.txt"]}]
        }
        with self.assertRaisesRegex(ValueError, "Missing native dependency license"):
            self.collect()

    def test_wrong_platform_is_an_error(self) -> None:
        self.metadata["target_triple"] = "x86_64-pc-windows-msvc"
        with self.assertRaisesRegex(ValueError, "does not match"):
            self.collect()

    def test_explicitly_removed_backend_is_not_an_active_component(self) -> None:
        self.metadata["build_info"]["extensions"] = {
            "_dbm": [{"licenses": ["Sleepycat"], "license_paths": []}]
        }
        modifications = {"excluded_extensions": [{
            "extension": "_dbm", "path": "lib/python3.12/lib-dynload/_dbm.example.so"
        }]}
        (self.runtime / "runtime-modifications.json").write_text(json.dumps(modifications))
        destination = self.collect()
        inventory = json.loads((destination / "inventory.json").read_bytes())
        self.assertFalse(inventory["publication_blockers"])
        self.assertNotIn("_dbm", [c["name"] for c in inventory["cpython_components"]])
        verify(self.package(destination))

    def test_backend_cannot_be_excluded_only_on_paper(self) -> None:
        self.metadata["build_info"]["extensions"] = {"_dbm": [{}]}
        backend = self.runtime / "lib/python3.12/lib-dynload/_dbm.example.so"
        backend.parent.mkdir(parents=True)
        backend.write_bytes(b"backend")
        (self.runtime / "runtime-modifications.json").write_text(json.dumps({
            "excluded_extensions": [{"extension": "_dbm", "path": backend.relative_to(self.runtime).as_posix()}]
        }))
        with self.assertRaisesRegex(ValueError, "Excluded extension is still present"):
            self.collect()

    def test_linux_configuration_removes_only_dbm_and_records_original_hash(self) -> None:
        backend = self.runtime / "lib/python3.12/lib-dynload/_dbm.example.so"
        backend.parent.mkdir(parents=True)
        backend.write_bytes(b"backend")
        retained = backend.with_name("_sqlite3.example.so")
        retained.write_bytes(b"retain")
        build = self.root / "numpy-build"
        (build / "wheels").mkdir(parents=True)
        wheel = build / "wheels/numpy.whl"
        wheel.write_bytes(b"wheel")
        checksum = collector.digest(wheel)
        (build / "provenance.json").write_text(json.dumps({
            "wheel": wheel.name, "wheel_sha256": checksum,
            "meson_options": {"blas": "none", "lapack": "none"},
        }))
        (build / "requirements.lock").write_text(f"numpy==2.5.4 --hash=sha256:{checksum}\n")
        (self.runtime / "wheel-install-report.json").write_text('{"install":[]}')
        (self.runtime / "numpy-wheel-install-report.json").write_text(
            '{"install":[{"metadata":{"name":"numpy"}}]}'
        )
        configure(self.runtime, build)
        self.assertFalse(backend.exists())
        self.assertEqual(retained.read_bytes(), b"retain")
        modifications = json.loads((self.runtime / "runtime-modifications.json").read_bytes())
        self.assertEqual(modifications["excluded_extensions"][0]["sha256"], hashlib.sha256(b"backend").hexdigest())

    def test_publication_obligations_are_not_silently_cleared(self) -> None:
        self.metadata["build_info"]["extensions"] = {
            "_dbm": [{"licenses": ["Sleepycat"], "license_paths": []}]
        }
        (self.runtime / "libquadmath.so.0").write_bytes(b"native test data")
        (self.runtime / "vcruntime140.dll").write_bytes(b"native test data")
        destination = self.collect()
        inventory = json.loads((destination / "inventory.json").read_text())
        self.assertEqual(len(inventory["publication_blockers"]), 3)
        self.assertEqual(len(inventory["native_files"]), 2)


if __name__ == "__main__":
    unittest.main()
