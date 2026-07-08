import pathlib
import re

root = pathlib.Path(__file__).resolve().parents[1] / "web/src"

replacements = [
    (
        "hasRole('Admin', 'Reception', 'Doctor')",
        "hasPermission('patients.read', 'pep.read', 'reports.read', 'hospitalization.manage')",
    ),
    (
        "hasRole('Admin', 'Doctor', 'Reception')",
        "hasPermission('patients.read', 'pep.read', 'reports.read', 'hospitalization.manage')",
    ),
    (
        "hasRole('Admin', 'Reception')",
        "hasPermission('patients.create', 'reports.read')",
    ),
    (
        "hasRole('Admin', 'Doctor')",
        "hasPermission('pep.read', 'pep.write')",
    ),
]

skip_files = {"AuthContext.tsx", "Layout.tsx", "PatientPortalPage.tsx", "AuditPage.tsx"}

for path in sorted(root.rglob("*.tsx")):
    if path.name in skip_files:
        continue

    text = path.read_text(encoding="utf-8")
    if "hasRole(" not in text:
        continue

    orig = text
    for old, new in replacements:
        text = text.replace(old, new)

    if "hasPermission(" in text and "hasRole(" not in text.replace("hasRole('Patient')", ""):
        text = re.sub(
            r"const \{ hasRole \} = useAuth\(\)",
            "const { hasPermission } = useAuth()",
            text,
        )
        text = re.sub(
            r"const \{ ([^}]*?)hasRole([^}]*?) \} = useAuth\(\)",
            lambda m: f"const {{ {m.group(1)}hasPermission{m.group(2)} }} = useAuth()".replace(
                "hasRole", "hasPermission"
            )
            if "hasRole" in m.group(0) and "hasPermission" not in m.group(0)
            else m.group(0),
            text,
        )

    if text != orig:
        path.write_text(text, encoding="utf-8")
        print(f"updated {path.relative_to(root)}")
