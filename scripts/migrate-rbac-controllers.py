import pathlib

root = pathlib.Path(__file__).resolve().parents[1] / "src/SistemaHospitalar.Api/Controllers"
imports = (
    "using SistemaHospitalar.Domain.Security;\n"
    "using SistemaHospitalar.Infrastructure.Security;\n"
)
staff = (
    "[RequireAnyPermission(PermissionCodes.PatientsRead, PermissionCodes.PepRead, "
    "PermissionCodes.ReportsRead, PermissionCodes.HospitalizationManage)]"
)
admin_ops = (
    "[RequireAnyPermission(PermissionCodes.ReportsRead, PermissionCodes.PatientsCreate)]"
)
clinical = "[RequireAnyPermission(PermissionCodes.PepRead, PermissionCodes.PepWrite)]"

class_replacements = [
    ('[Authorize(Roles = "Admin,Reception,Doctor")]', f"[Authorize]\n{staff}"),
    ('[Authorize(Roles = "Admin,Doctor,Reception")]', f"[Authorize]\n{staff}"),
    ('[Authorize(Roles = "Admin,Reception")]', f"[Authorize]\n{admin_ops}"),
]

method_replacements = [
    ('[Authorize(Roles = "Admin,Reception,Doctor")]', staff),
    ('[Authorize(Roles = "Admin,Doctor,Reception")]', staff),
    ('[Authorize(Roles = "Admin,Reception")]', admin_ops),
    ('[Authorize(Roles = "Admin,Doctor")]', clinical),
]

skip = {"PatientPortalController.cs"}

for path in sorted(root.glob("*.cs")):
    if path.name in skip:
        continue

    text = path.read_text(encoding="utf-8")
    if "Authorize(Roles" not in text:
        continue

    orig = text
    for old, new in class_replacements:
        text = text.replace(old, new)
    for old, new in method_replacements:
        text = text.replace(old, new)

    if "PermissionCodes." in text and "using SistemaHospitalar.Domain.Security" not in text:
        text = text.replace(
            "using Microsoft.AspNetCore.Mvc;",
            f"using Microsoft.AspNetCore.Mvc;\n{imports}",
        )

    if text != orig:
        path.write_text(text, encoding="utf-8")
        print(f"updated {path.name}")
