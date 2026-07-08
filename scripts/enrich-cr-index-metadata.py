#!/usr/bin/env python3
"""Enriquece JSONL existente com princípio ativo, forma, concentração e via."""

from __future__ import annotations

import argparse
import json
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from cr_bula_scraper import fetch, fetch_product_html
from medication_metadata import extract_metadata, format_display_name

BASE_URL = "https://consultaremedios.com.br"


def main() -> int:
    parser = argparse.ArgumentParser(description="Enriquece metadados do JSONL de bulas")
    parser.add_argument(
        "--input",
        type=Path,
        default=Path(__file__).resolve().parents[1] / "data" / "cr-index-bulas.jsonl",
    )
    parser.add_argument("--delay", type=float, default=0.25)
    args = parser.parse_args()

    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    input_path = args.input
    temp_path = input_path.with_suffix(input_path.suffix + ".tmp")
    updated = 0
    total = 0

    with input_path.open("r", encoding="utf-8") as source, temp_path.open("w", encoding="utf-8") as target:
        for line in source:
            line = line.strip()
            if not line:
                continue
            total += 1
            record = json.loads(line)
            slug = record.get("slug") or ""
            name = record.get("name") or slug
            package_insert = record.get("packageInsert") or ""
            bula_url = record.get("url") or f"{BASE_URL}/{slug}/bula"

            try:
                bula_html = fetch(bula_url)
            except Exception:
                bula_html = ""

            try:
                product_html = fetch_product_html(slug)
            except Exception as exc:
                print(f"AVISO {slug}: {exc}", file=sys.stderr)
                product_html = bula_html

            metadata = extract_metadata(
                name=name,
                bula_html=bula_html or product_html,
                product_html=product_html,
                package_insert=package_insert,
            )

            changed = False
            for key in ("activeIngredient", "pharmaceuticalForm", "strength", "route"):
                value = metadata.get(key)
                if value and record.get(key) != value:
                    record[key] = value
                    changed = True

            formatted_name = format_display_name(name, record.get("strength"))
            if formatted_name and record.get("name") != formatted_name:
                record["name"] = formatted_name
                changed = True

            if changed:
                updated += 1
                print(f"+ {name}")

            target.write(json.dumps(record, ensure_ascii=False) + "\n")
            time.sleep(args.delay)

    try:
        temp_path.replace(input_path)
    except OSError:
        import shutil

        shutil.copyfile(temp_path, input_path)
        temp_path.unlink(missing_ok=True)
    print(f"Concluído: {updated}/{total} enriquecidos -> {input_path}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
