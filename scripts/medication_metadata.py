"""Extrai metadados clínicos (PA, forma, concentração, via) do Consulta Remédios."""

from __future__ import annotations

import html
import json
import re
from typing import Any

ACTIVE_INGREDIENT_PATTERNS = [
    re.compile(
        r"Princ[ií]pio\s+Ativo[^:>]*:?\s*(?:</[^>]+>\s*)*(?:<a[^>]*>)?([^<]+)",
        re.IGNORECASE,
    ),
]

STRENGTH_PATTERN = re.compile(
    r"(\d+(?:[.,]\d+)?\s*(?:mg|mcg|µg|g|UI|U|%|mL|mg/mL|mcg/mL|UI/mL)"
    r"(?:\s*/\s*\d+(?:[.,]\d+)?\s*(?:mg|mcg|g|UI|mL|mg/mL|mcg/mL|UI/mL))*)",
    re.IGNORECASE,
)

ROUTE_HINTS: list[tuple[re.Pattern[str], str]] = [
    (re.compile(r"\bvia oral\b|\bpor via oral\b|\badministr(?:a|á)ção oral\b", re.I), "VO"),
    (re.compile(r"\bintravenos[a]?\b|\bvia intravenos[a]?\b|\bIV\b", re.I), "IV"),
    (re.compile(r"\bintramuscular\b|\bvia intramuscular\b|\bIM\b", re.I), "IM"),
    (re.compile(r"\bsubcut[aâ]ne[a]?\b|\bvia subcut[aâ]ne[a]?\b|\bSC\b", re.I), "SC"),
    (re.compile(r"\bt[oó]pic[oa]?\b|\bpele\b|\bdermatol[oó]gic", re.I), "Tópica"),
    (re.compile(r"\boft[aá]lmic[oa]?\b|\bcol[ií]rio\b", re.I), "Oftálmica"),
    (re.compile(r"\bnasal\b|\bspray nasal\b", re.I), "Nasal"),
    (re.compile(r"\bretal\b|\bsuposit[oó]rio\b", re.I), "Retal"),
    (re.compile(r"\binhalat[oó]ri[oa]?\b|\binala[cç][aã]o\b", re.I), "Inalatória"),
]

FORM_ROUTE: dict[str, str] = {
    "comprimido": "VO",
    "capsula": "VO",
    "cápsula": "VO",
    "dragea": "VO",
    "pastilha": "VO",
    "solução oral": "VO",
    "suspensão oral": "VO",
    "suspensao oral": "VO",
    "xarope": "VO",
    "gotas": "VO",
    "pó para solução oral": "VO",
    "comprimido efervescente": "VO",
    "solução injetável": "IV",
    "injetável": "IV",
    "injetavel": "IV",
    "pó liofilizado": "IV",
    "creme": "Tópica",
    "pomada": "Tópica",
    "gel": "Tópica",
    "spray nasal": "Nasal",
    "solução oftálmica": "Oftálmica",
    "solução oftalmica": "Oftálmica",
    "colírio": "Oftálmica",
    "colirio": "Oftálmica",
    "adesivo": "Transdérmica",
    "supositório": "Retal",
    "supositorio": "Retal",
}


def _clean(value: str | None) -> str | None:
    if not value:
        return None
    text = html.unescape(value).strip()
    text = re.sub(r"\s+", " ", text)
    return text or None


def _normalize_form(value: str | None) -> str | None:
    text = _clean(value)
    if not text:
        return None
    lower = text.lower()
    if "comprim" in lower:
        return "Comprimido"
    if "caps" in lower or "cáps" in lower:
        return "Cápsula"
    if "gotas" in lower and "oft" not in lower:
        return "Gotas"
    if "xarope" in lower or "suspens" in lower:
        return "Suspensão oral"
    if "solu" in lower and "oral" in lower:
        return "Solução oral"
    if "injet" in lower or "infus" in lower:
        return "Solução injetável"
    if "creme" in lower:
        return "Creme"
    if "pomada" in lower:
        return "Pomada"
    if "gel" in lower:
        return "Gel"
    if "spray" in lower and "nasal" in lower:
        return "Spray nasal"
    if "oftalm" in lower or "colirio" in lower or "colírio" in lower:
        return "Solução oftálmica"
    if "suposit" in lower:
        return "Supositório"
    return text[:1].upper() + text[1:]


def extract_drug_jsonld(page_html: str) -> dict[str, Any] | None:
    for block in re.findall(
        r'<script type="application/ld\+json"[^>]*>(.*?)</script>',
        page_html,
        re.S,
    ):
        try:
            data = json.loads(block)
        except json.JSONDecodeError:
            continue
        if isinstance(data, dict) and data.get("@type") == "Drug":
            return data
    return None


def extract_active_ingredient(page_html: str, drug: dict[str, Any] | None = None) -> str | None:
    if drug:
        ingredients = drug.get("activeIngredient") or []
        if isinstance(ingredients, list) and ingredients:
            names = [_clean(item.get("name")) for item in ingredients if isinstance(item, dict)]
            names = [name for name in names if name]
            if names:
                return " + ".join(names)
        non_proprietary = _clean(drug.get("nonProprietaryName"))
        if non_proprietary:
            return non_proprietary

    for pattern in ACTIVE_INGREDIENT_PATTERNS:
        match = pattern.search(page_html)
        if match:
            value = _clean(match.group(1))
            if value:
                return value
    return None


def _strength_from_text(text: str | None) -> str | None:
    if not text:
        return None
    match = STRENGTH_PATTERN.search(text)
    return _clean(match.group(1)) if match else None


def extract_strength(name: str, product_html: str, drug: dict[str, Any] | None = None) -> str | None:
    from_name = _strength_from_text(name)
    if from_name:
        return from_name

    drug_name = _clean(drug.get("name") if drug else None) or name
    escaped = re.escape(drug_name)
    branded = re.search(rf"{escaped}\s+(\d+(?:[.,]\d+)?\s*(?:mg|mcg|g|UI|%))", product_html, re.I)
    if branded:
        return _clean(branded.group(1))

    for match in re.finditer(r'"name"\s*:\s*"([^"]+)"', product_html):
        candidate = match.group(1)
        if drug_name.lower() not in candidate.lower():
            continue
        strength = _strength_from_text(candidate)
        if strength:
            return strength

    return _strength_from_text(product_html[:120000])


def infer_route(
    pharmaceutical_form: str | None,
    product_html: str = "",
    package_insert: str = "",
) -> str | None:
    corpus = f"{product_html}\n{package_insert}".lower()
    for pattern, route in ROUTE_HINTS:
        if pattern.search(corpus):
            return route

    if pharmaceutical_form:
        normalized = pharmaceutical_form.lower()
        for key, route in FORM_ROUTE.items():
            if key in normalized:
                return route

    if pharmaceutical_form and any(
        token in pharmaceutical_form.lower()
        for token in ("comprim", "caps", "cáps", "gotas", "xarope", "suspens", "solução oral")
    ):
        return "VO"

    return None


def format_display_name(name: str, strength: str | None) -> str:
    trimmed = (name or "").strip()
    if not strength or not strength.strip():
        return trimmed
    normalized_strength = strength.strip()
    if normalized_strength.lower() in trimmed.lower():
        return trimmed
    compact_name = trimmed.replace(" ", "").replace("/", "").lower()
    compact_strength = normalized_strength.replace(" ", "").replace("/", "").lower()
    if compact_strength in compact_name:
        return trimmed
    return f"{trimmed} {normalized_strength}"


def infer_form_from_text(text: str | None) -> str | None:
    if not text:
        return None
    lower = text.lower()
    checks = [
        ("comprim", "Comprimido"),
        ("cáps", "Cápsula"),
        ("caps", "Cápsula"),
        ("gotas", "Gotas"),
        ("xarope", "Suspensão oral"),
        ("suspens", "Suspensão oral"),
        ("solução oral", "Solução oral"),
        ("solucao oral", "Solução oral"),
        ("injet", "Solução injetável"),
        ("creme", "Creme"),
        ("pomada", "Pomada"),
        ("gel", "Gel"),
        ("colírio", "Solução oftálmica"),
        ("colirio", "Solução oftálmica"),
        ("suposit", "Supositório"),
    ]
    for token, label in checks:
        if token in lower:
            return label
    return None


def extract_metadata(
    *,
    name: str,
    bula_html: str,
    product_html: str | None = None,
    package_insert: str | None = None,
) -> dict[str, str | None]:
    drug = extract_drug_jsonld(product_html or bula_html)
    active_ingredient = extract_active_ingredient(bula_html, drug)
    if not active_ingredient and product_html:
        active_ingredient = extract_active_ingredient(product_html, drug)

    pharmaceutical_form = _normalize_form(_clean(drug.get("dosageForm") if drug else None))
    if not pharmaceutical_form and product_html:
        match = re.search(r"Forma Farmac[^:]*:?\s*([^<\n]{3,80})", product_html, re.I)
        pharmaceutical_form = _normalize_form(_clean(match.group(1) if match else None))
    if not pharmaceutical_form:
        pharmaceutical_form = infer_form_from_text(f"{name}\n{package_insert or ''}")

    strength = extract_strength(name, product_html or bula_html, drug)
    if not strength:
        strength = _strength_from_text(package_insert)
    route = infer_route(pharmaceutical_form, product_html or "", package_insert or "")

    return {
        "activeIngredient": active_ingredient,
        "pharmaceuticalForm": pharmaceutical_form,
        "strength": strength,
        "route": route,
    }
