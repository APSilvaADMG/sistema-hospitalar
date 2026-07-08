#!/usr/bin/env python3
"""Gera log legível (UTF-8) a partir de data/consulta-remedios-bulas.jsonl."""

from __future__ import annotations

import argparse
import json
import sys
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_JSONL = ROOT / "data" / "consulta-remedios-bulas.jsonl"
DEFAULT_LOG_DIR = ROOT / "data" / "import-logs"
BATCH_LETTERS = {
    1: set("abcde"),
    2: set("fghij"),
    3: set("klmno"),
    4: set("pqrst"),
    5: set("uvwxyz") | {"0"},
}


def first_letter(name: str, slug: str) -> str:
    source = (slug or name).strip().lower()
    if not source:
        return "?"
    if source[0].isdigit():
        return "0-9"
    return source[0]


def load_records(jsonl_path: Path) -> list[dict]:
    records: list[dict] = []
    with jsonl_path.open("r", encoding="utf-8") as handle:
        for line_no, line in enumerate(handle, start=1):
            line = line.strip()
            if not line:
                continue
            try:
                record = json.loads(line)
            except json.JSONDecodeError as exc:
                print(f"Linha {line_no} inválida: {exc}", file=sys.stderr)
                continue
            if record.get("name") and record.get("slug"):
                records.append(record)
    return records


def filter_batch(records: list[dict], batch: int | None) -> list[dict]:
    if batch is None:
        return records
    letters = BATCH_LETTERS.get(batch)
    if letters is None:
        raise ValueError(f"Lote inválido: {batch}")
    filtered = []
    for record in records:
        letter = first_letter(record["name"], record["slug"])
        if letter == "0-9" and "0" in letters:
            filtered.append(record)
        elif letter in letters:
            filtered.append(record)
    return filtered


def write_summary(
    records: list[dict],
    output_path: Path,
    *,
    title: str,
) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    by_letter: dict[str, list[dict]] = defaultdict(list)
    for record in sorted(records, key=lambda r: r["name"].casefold()):
        by_letter[first_letter(record["name"], record["slug"])].append(record)

    now = datetime.now(timezone.utc).astimezone()
    lines = [
        title,
        f"Gerado em: {now.strftime('%d/%m/%Y %H:%M:%S %z')}",
        f"Total de bulas: {len(records)}",
        "",
    ]

    for letter in sorted(by_letter, key=lambda x: (x == "0-9", x)):
        group = by_letter[letter]
        lines.append(f"## Letra {letter} ({len(group)})")
        for record in group:
            ingredient = record.get("activeIngredient")
            suffix = f" — {ingredient}" if ingredient else ""
            lines.append(f"  + {record['name']}{suffix}")
        lines.append("")

    output_path.write_text("\n".join(lines).rstrip() + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Gera resumo UTF-8 das bulas importadas")
    parser.add_argument("--jsonl", type=Path, default=DEFAULT_JSONL)
    parser.add_argument("--batch", type=int, choices=sorted(BATCH_LETTERS))
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()

    if not args.jsonl.exists():
        print(f"Arquivo não encontrado: {args.jsonl}", file=sys.stderr)
        return 1

    records = load_records(args.jsonl)
    records = filter_batch(records, args.batch)

    if args.output:
        output = args.output
    elif args.batch:
        output = DEFAULT_LOG_DIR / f"batch-{args.batch}-resumo.log"
    else:
        output = DEFAULT_LOG_DIR / "bulas-resumo-completo.log"

    title = (
        f"# Resumo do Lote {args.batch}/5 (letras: {', '.join(sorted(BATCH_LETTERS[args.batch]))})"
        if args.batch
        else "# Resumo completo do bulário importado"
    )
    write_summary(records, output, title=title)
    print(f"Resumo gerado: {output} ({len(records)} bulas)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
