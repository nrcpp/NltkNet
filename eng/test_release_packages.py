import json
import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest.mock import patch

from select_release_packages import select


class ReleaseSelectionTests(unittest.TestCase):
    def setUp(self) -> None:
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.source = self.root / "source"
        self.output = self.root / "release"
        for rid in ("osx-arm64", "win-x64", "linux-x64"):
            directory = self.source / ("packages-" + rid)
            directory.mkdir(parents=True)
            for identity in ("NltkNet", "NltkNet.Runtime." + rid):
                self.write_package(directory / (identity + ".nupkg"), identity)
        verification = patch("select_release_packages.verify")
        self.verify = verification.start()
        self.addCleanup(verification.stop)

    def write_package(self, path: Path, identity: str, version: str = "2.0.0-preview.1", commit: str = "abc123") -> None:
        with zipfile.ZipFile(path, "w") as archive:
            archive.writestr(
                identity + ".nuspec",
                '<package xmlns="http://schemas.microsoft.com/packaging/2011/10/nuspec.xsd">'
                f"<metadata><id>{identity}</id><version>{version}</version>"
                f'<repository commit="{commit}" /></metadata></package>',
            )

    def test_selects_four_original_packages_and_records_hashes(self) -> None:
        select(self.source, self.output, "2.0.0-preview.1", "abc123")
        self.assertEqual(self.verify.call_count, 3)
        self.assertEqual(len(json.loads((self.output / "SHA256.json").read_text())), 4)
        self.assertEqual(
            (self.output / "NltkNet.2.0.0-preview.1.nupkg").read_bytes(),
            (self.source / "packages-osx-arm64/NltkNet.nupkg").read_bytes(),
        )

    def test_rejects_wrong_commit(self) -> None:
        with self.assertRaisesRegex(ValueError, "source commit differs"):
            select(self.source, self.output, "2.0.0-preview.1", "different")
        self.assertFalse(self.output.exists())

    def test_rejects_wrong_version(self) -> None:
        with self.assertRaisesRegex(ValueError, "unexpected version"):
            select(self.source, self.output, "2.0.0", "abc123")

    def test_rejects_missing_platform(self) -> None:
        (self.source / "packages-win-x64/NltkNet.Runtime.win-x64.nupkg").unlink()
        with self.assertRaisesRegex(ValueError, "expected .*found"):
            select(self.source, self.output, "2.0.0-preview.1", "abc123")

    def test_rejects_extra_package(self) -> None:
        self.write_package(self.source / "packages-osx-arm64/other.nupkg", "Other")
        with self.assertRaisesRegex(ValueError, "unexpected or duplicate package"):
            select(self.source, self.output, "2.0.0-preview.1", "abc123")

    def test_refuses_to_overwrite_a_release(self) -> None:
        self.output.mkdir()
        with self.assertRaisesRegex(ValueError, "must not exist"):
            select(self.source, self.output, "2.0.0-preview.1", "abc123")


if __name__ == "__main__":
    unittest.main()
