import hashlib
import json
import re
import sys
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path


runtime, docx = Path(sys.argv[1]), Path(sys.argv[2])
expected_hash = "41a207b10c8ab91d0d2f10a854715f73dca54509581692d2fe179aa3ffcb8540"
if hashlib.sha256(docx.read_bytes()).hexdigest() != expected_hash:
    raise ValueError("Microsoft license download has changed; review before updating its hash")
with zipfile.ZipFile(docx) as archive:
    document = ET.fromstring(archive.read("word/document.xml"))
namespace = {"w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main"}
paragraphs = [
    "".join(part.itertext()) for part in document.findall(".//w:p", namespace)
]
terms = "\n\n".join(paragraphs) + "\n"
if "MICROSOFT SOFTWARE LICENSE TERMS" not in terms or "DISTRIBUTABLE CODE" not in terms:
    raise ValueError("Microsoft license text extraction is incomplete")
(runtime / "MICROSOFT-VS2022-COMMUNITY-LICENSE.txt").write_text(terms, encoding="utf-8")
files = []
for path in sorted(runtime.rglob("*.dll")):
    if re.search(r"(vcruntime|msvcp)\d", path.name, re.IGNORECASE):
        data = path.read_bytes()
        versions = re.findall(r"14\.\d+\.\d+(?:\.\d+)?", data.decode("utf-16le", errors="ignore"))
        if not versions:
            raise ValueError(f"Cannot identify Microsoft runtime DLL version: {path}")
        files.append({
            "path": path.relative_to(runtime).as_posix(),
            "version": versions[-1],
            "sha256": hashlib.sha256(data).hexdigest(),
        })
if len(files) != 3:
    raise ValueError(f"Expected the three reviewed Microsoft runtime DLLs, found {len(files)}")
(runtime / "microsoft-redistribution.json").write_text(
    json.dumps({
        "license_source": "https://visualstudio.microsoft.com/wp-content/uploads/2021/11/Visual-Studio-2022-Community-License-EN.docx",
        "license_source_sha256": expected_hash,
        "redistributable_list": "https://learn.microsoft.com/en-us/visualstudio/releases/2022/redistribution",
        "redistribution_guidance": "https://learn.microsoft.com/en-us/cpp/windows/redistributing-visual-cpp-files?view=msvc-170",
        "files": files,
        "publisher_entitlement_confirmed": False,
    }, indent=2) + "\n", encoding="utf-8"
)
