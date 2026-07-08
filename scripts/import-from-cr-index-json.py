#!/usr/bin/env python3
"""Importa bulas a partir do índice JSON do Consulta Remédios (Diversos/*.json)."""

from __future__ import annotations

import argparse
import json
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from cr_bula_scraper import scrape_bula_url


def default_index_path() -> Path:
    root = Path(__file__).resolve().parents[1]
    return root / "Diversos" / "Bulário de A a Z_ veja todas as bulas de remédios _ CR.json"


def parse_index_entries(index_path: Path) -> list[tuple[str, str]]:
    data = json.loads(index_path.read_text(encoding="utf-8"))
    entries: list[tuple[str, str]] = []
    seen: set[str] = set()

    for row in data:
        if not isinstance(row, dict):
            continue
        for key, url in row.items():
            if not key.startswith("borderb_URL") or not isinstance(url, str):
                continue
            url = url.strip()
            if not url.startswith("http"):
                continue

            suffix = key.removeprefix("borderb_URL")
            name_key = "borderb" if suffix == "" else f"borderb{int(suffix) + 1}"
            label = str(row.get(name_key, "")).strip()
            if url in seen:
                continue
            seen.add(url)
            entries.append((url, label))

    return entries


def load_existing_slugs(output_path: Path) -> set[str]:
    if not output_path.exists():
        return set()
    slugs: set[str] = set()
    with output_path.open("r", encoding="utf-8") as handle:
        for line in handle:
            line = line.strip()
            if not line:
                continue
            try:
                slugs.add(json.loads(line)["slug"])
            except (json.JSONDecodeError, KeyError):
                continue
    return slugs


def main() -> int:
    parser = argparse.ArgumentParser(description="Importa bulas do índice JSON do Consulta Remédios")
    parser.add_argument("--input", type=Path, default=default_index_path())
    parser.add_argument(
        "--output",
        type=Path,
        default=Path(__file__).resolve().parents[1] / "data" / "cr-index-bulas.jsonl",
    )
    parser.add_argument("--limit", type=int, default=0, help="Limite de bulas novas")
    parser.add_argument("--delay", type=float, default=0.35)
    parser.add_argument("--resume", action="store_true")
    args = parser.parse_args()

    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    if hasattr(sys.stderr, "reconfigure"):
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")

    if not args.input.exists():
        print(f"Arquivo não encontrado: {args.input}", file=sys.stderr)
        return 1

    entries = parse_index_entries(args.input)
    output_path = args.output
    output_path.parent.mkdir(parents=True, exist_ok=True)
    existing = load_existing_slugs(output_path) if args.resume else set()

    imported = 0
    skipped = 0
    errors = 0

    with output_path.open("a", encoding="utf-8") as out:
        for url, label in entries:
            slug_hint = url.rstrip("/").split("/")[-2]
            if slug_hint in existing:
                skipped += 1
                continue

            try:
                record = scrape_bula_url(url, label or None)
            except Exception as exc:
                errors += 1
                print(f"ERRO {slug_hint}: {exc}", file=sys.stderr)
                continue

            if not record["packageInsert"]:
                errors += 1
                print(f"AVISO sem conteúdo: {record['name']}", file=sys.stderr)
                continue

            out.write(json.dumps(record, ensure_ascii=False) + "\n")
            out.flush()
            existing.add(record["slug"])
            imported += 1
            print(f"+ {record['name']}")

            if args.limit and imported >= args.limit:
                break

            time.sleep(args.delay)

    print(
        f"Concluído: {imported} importadas, {skipped} ignoradas, {errors} erros -> {output_path}",
        file=sys.stderr,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
