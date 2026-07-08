import pathlib

p = pathlib.Path(__file__).resolve().parents[1] / "web/src/navigation/sidebarMenu.ts"
text = p.read_text(encoding="utf-8")

mapping = {
    "dashboard": "management",
    "atendimento": "clinical",
    "pacientes": "clinical",
    "pep": "clinical",
    "agenda": "clinical",
    "enfermagem": "clinical",
    "cme": "clinical",
    "farmacia": "diagnostic",
    "almoxarifado": "administrative",
    "laboratorio": "diagnostic",
    "imagem": "diagnostic",
    "sangue": "diagnostic",
    "nutricao": "clinical",
    "faturamento": "administrative",
    "financeiro": "administrative",
    "compras": "administrative",
    "rh": "administrative",
    "ccih": "clinical",
    "seguranca-lgpd": "security",
    "qualidade": "management",
    "regulacao": "management",
    "eng-clinica": "administrative",
    "bi": "management",
    "relatorios": "management",
    "operacional": "clinical",
    "acesso-fisico": "security",
    "integracoes-gov": "security",
    "integracoes": "security",
    "automacao": "management",
    "configuracoes": "management",
}

for sid, mg in mapping.items():
    old = f"id: '{sid}',\n      title:"
    new = f"id: '{sid}',\n      megaGroup: '{mg}',\n      title:"
    if old in text and f"id: '{sid}',\n      megaGroup:" not in text:
        text = text.replace(old, new, 1)

p.write_text(text, encoding="utf-8")
print("megaGroup fields added")
