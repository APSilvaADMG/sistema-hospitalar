"""Utilitários compartilhados para normalização de texto de bulas."""

from __future__ import annotations

import re

SECTION_MARKERS: list[tuple[re.Pattern[str], str]] = [
    (re.compile(r"(?:,\s*)?para o que [eé] indicado e para o que serve\?", re.I), "INDICAÇÕES"),
    (re.compile(r"Como (?:devo usar|usar|tomar)[^?]*\?", re.I), "POSOLOGIA"),
    (re.compile(r"Quais as contraindica(?:ç|c)[oõ]es[^?]*\?", re.I), "CONTRAINDICAÇÕES"),
    (
        re.compile(
            r"Quais (?:os )?efeitos colaterais[^?]*\?|Quais as rea(?:ç|c)[oõ]es adversas[^?]*\?",
            re.I,
        ),
        "EFEITOS ADVERSOS",
    ),
    (re.compile(r"Intera(?:ç|c)[oõ]es medicamentosas[^?]*\?", re.I), "INTERAÇÕES"),
    (
        re.compile(
            r"(?:Este medicamento )?pode ser utilizado durante a gravidez[^?]*\?|Gravidez e lacta(?:ç|c)[aã]o[^?]*\?",
            re.I,
        ),
        "GRAVIDEZ E LACTAÇÃO",
    ),
    (re.compile(r"Como devo armazenar[^?]*\?|Conserva(?:ç|c)[aã]o[^?]*\?", re.I), "CUIDADOS NA ADMINISTRAÇÃO"),
    (re.compile(r"O que fazer se (?:eu )?tomar[^?]*\?|Superdosagem[^?]*\?", re.I), "SUPERDOSAGEM"),
]

CANONICAL_ORDER = [
    "INDICAÇÕES",
    "POSOLOGIA",
    "CONTRAINDICAÇÕES",
    "EFEITOS ADVERSOS",
    "INTERAÇÕES",
    "CUIDADOS NA ADMINISTRAÇÃO",
]

STRUCTURED = re.compile(
    r"^INDICAÇÕES$|^POSOLOGIA$|^CONTRAINDICAÇÕES$|^EFEITOS ADVERSOS$|^INTERAÇÕES$|^CUIDADOS NA ADMINISTRAÇÃO$",
    re.I | re.M,
)
LEADING = re.compile(r"^\s*>\s*", re.I)
HOW_IT_WORKS = re.compile(r"Como o .+? funciona\?\s*", re.I)
SENTENCE_SPLIT = re.compile(r"(?<=[.!?])\s+")


def _summarize_body(body: str, max_chars: int = 320) -> str:
    text = HOW_IT_WORKS.sub("", body.strip())
    text = re.sub(r"\s+", " ", text).strip()
    if not text:
        return body.strip()[:max_chars]

    sentences = [s.strip() for s in SENTENCE_SPLIT.split(text) if len(s.strip()) > 15]
    if not sentences:
        return text[:max_chars]

    parts: list[str] = []
    total = 0
    for sentence in sentences:
        if total + len(sentence) > max_chars and parts:
            break
        parts.append(sentence)
        total += len(sentence) + 1

    return " ".join(parts)


def _parse_structured_blocks(text: str) -> dict[str, str]:
    blocks: dict[str, str] = {}
    current_title = ""
    current_lines: list[str] = []

    for line in text.splitlines():
        stripped = line.strip()
        upper = stripped.upper()
        if upper in CANONICAL_ORDER or upper in {"GRAVIDEZ E LACTAÇÃO", "CONSERVAÇÃO", "SUPERDOSAGEM"}:
            if current_title:
                blocks[current_title] = "\n".join(current_lines).strip()
            current_title = (
                "CUIDADOS NA ADMINISTRAÇÃO"
                if upper == "CONSERVAÇÃO"
                else upper
            )
            current_lines = []
            continue
        if current_title:
            current_lines.append(line)

    if current_title:
        blocks[current_title] = "\n".join(current_lines).strip()

    return blocks


def normalize_bula_text(raw: str | None) -> str:
    if not raw or not raw.strip():
        return ""

    text = LEADING.sub("", raw.strip())
    if STRUCTURED.search(text):
        blocks = _parse_structured_blocks(text)
    else:
        blocks = {}
        matches: list[tuple[int, str, re.Pattern[str]]] = []
        for pattern, title in SECTION_MARKERS:
            for match in pattern.finditer(text):
                mapped = (
                    "CUIDADOS NA ADMINISTRAÇÃO"
                    if title == "CUIDADOS NA ADMINISTRAÇÃO"
                    else title
                )
                matches.append((match.start(), mapped, pattern))

        if not matches:
            return text

        matches.sort(key=lambda item: item[0])
        for index, (start, title, pattern) in enumerate(matches):
            end = matches[index + 1][0] if index + 1 < len(matches) else len(text)
            chunk = text[start:end].strip()
            body = pattern.sub("", chunk, count=1).strip() or chunk
            existing = blocks.get(title, "")
            blocks[title] = f"{existing}\n{body}".strip() if existing else body

    if "CONSERVAÇÃO" in blocks:
        conservacao = blocks.pop("CONSERVAÇÃO")
        blocks["CUIDADOS NA ADMINISTRAÇÃO"] = "\n".join(
            part for part in [blocks.get("CUIDADOS NA ADMINISTRAÇÃO", ""), conservacao] if part
        ).strip()

    sections: list[str] = []
    for title in CANONICAL_ORDER:
        body = blocks.get(title, "").strip()
        if not body:
            continue
        summary = _summarize_body(body, 320 if title != "POSOLOGIA" else 220)
        sections.append(f"{title}\n{summary}")

    if not sections:
        return text

    return "\n\n".join(sections)


def extract_posologia(raw: str | None) -> str | None:
    normalized = normalize_bula_text(raw)
    for block in normalized.split("\n\n"):
        lines = block.split("\n", 1)
        if len(lines) == 2 and lines[0].strip().upper() == "POSOLOGIA":
            value = lines[1].strip()
            return value or None
    return None
