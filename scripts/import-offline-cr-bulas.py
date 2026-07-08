#!/usr/bin/env python3
"""Importa bulas a partir de HTML offline (espelho HTTrack do Consulta Remédios).

Procura arquivos em:
  {mirror}/consultaremedios.com.br/{slug}/bula.html
  {mirror}/consultaremedios.com.br/{slug}/bula.html.tmp  (--include-pending)

Uso:
  python scripts/check-offline-cr-mirror.py
  python scripts/check-offline-cr-mirror.py --watch
  python scripts/import-offline-cr-bulas.py --include-pending
  python scripts/import-offline-cr-bulas.py --mirror-dir "C:\\Meus Sites\\BulasdeAaZ" --resume
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from cr_bula_scraper import scrape_bula_from_html
from medication_metadata import format_display_name
from offline_cr_mirror import DEFAULT_MIRROR_DIR, discover_bula_files, format_status_report, inspect_mirror


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


def read_html(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="replace")


def main() -> int:
    parser = argparse.ArgumentParser(description="Importa bulas HTML offline para JSONL")
    parser.add_argument(
        "--mirror-dir",
        type=Path,
        default=DEFAULT_MIRROR_DIR,
        help="Pasta raiz do espelho HTTrack",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=Path(__file__).resolve().parents[1] / "data" / "offline-cr-bulas.jsonl",
    )
    parser.add_argument(
        "--include-pending",
        action="store_true",
        help="Lê bula.html.tmp (HTTrack ainda em andamento)",
    )
    parser.add_argument("--resume", action="store_true", help="Ignora slugs já no JSONL")
    parser.add_argument("--limit", type=int, default=0)
    args = parser.parse_args()

    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    if hasattr(sys.stderr, "reconfigure"):
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")

    mirror_dir = args.mirror_dir.resolve()
    if not mirror_dir.is_dir():
        print(f"Pasta não encontrada: {mirror_dir}", file=sys.stderr)
        return 1

    status = inspect_mirror(mirror_dir)
    include_pending = args.include_pending or status.in_progress
    files = discover_bula_files(mirror_dir, include_pending=include_pending)

    if not files:
        print(
            "Nenhuma bula encontrada. Aguarde o HTTrack baixar /slug/bula ou use --include-pending.",
            file=sys.stderr,
        )
        print(format_status_report(status), file=sys.stderr)
        return 1

    output_path = args.output
    output_path.parent.mkdir(parents=True, exist_ok=True)
    existing = load_existing_slugs(output_path) if args.resume else set()

    imported = 0
    skipped = 0
    empty = 0
    errors = 0

    with output_path.open("a", encoding="utf-8") as out:
        for item in files:
            if item.slug in existing:
                skipped += 1
                continue

            try:
                bula_html = read_html(item.bula_path)
                product_html = read_html(item.product_path) if item.product_path else ""
                record = scrape_bula_from_html(
                    bula_html=bula_html,
                    slug=item.slug,
                    product_html=product_html,
                )
            except Exception as exc:
                errors += 1
                print(f"ERRO {item.slug}: {exc}", file=sys.stderr)
                continue

            if not record["packageInsert"]:
                empty += 1
                print(f"AVISO sem conteúdo: {item.slug}", file=sys.stderr)
                continue

            formatted = format_display_name(record["name"], record.get("strength"))
            if formatted:
                record["name"] = formatted

            record["source"] = "offline-httrack"
            record["offlinePath"] = str(item.bula_path)
            if item.pending:
                record["offlinePending"] = True

            out.write(json.dumps(record, ensure_ascii=False) + "\n")
            out.flush()
            existing.add(item.slug)
            imported += 1
            suffix = " (tmp)" if item.pending else ""
            print(f"+ {record['name']}{suffix}")

            if args.limit and imported >= args.limit:
                break

    print(
        f"Concluído: {imported} importadas, {skipped} ignoradas, "
        f"{empty} sem texto, {errors} erros -> {output_path}",
        file=sys.stderr,
    )
    if status.in_progress:
        print(
            "Espelho HTTrack ainda em andamento. Reexecute após finalizar para consolidar .tmp -> .html.",
            file=sys.stderr,
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
