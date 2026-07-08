import pathlib
import re

root = pathlib.Path(__file__).resolve().parents[1] / "src/SistemaHospitalar.Api/Controllers"

for path in sorted(root.glob("*.cs")):
    text = path.read_text(encoding="utf-8")
    orig = text

    text = re.sub(r";\n\nusing SistemaHospitalar", r";\nusing SistemaHospitalar", text)
    text = re.sub(
        r"(\n    )\[Authorize\]\n\[(Require(?:Any)?Permission)",
        r"\1[\2",
        text,
    )
    text = re.sub(
        r"\n\[(Require(?:Any)?Permission)",
        r"\n    [\1",
        text,
    )
    # Class-level attributes should not be indented.
    text = re.sub(
        r"namespace [^\n]+;\n\n    \[(Require(?:Any)?Permission)",
        lambda m: m.group(0).replace("\n    [", "\n[", 1),
        text,
    )
    text = re.sub(
        r"(namespace [^\n]+;\n\n)\[Authorize\]\n    \[(Require(?:Any)?Permission)",
        r"\1[Authorize]\n[\2",
        text,
    )

    if text != orig:
        path.write_text(text, encoding="utf-8")
        print(f"fixed {path.name}")
