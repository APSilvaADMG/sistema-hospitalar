#!/usr/bin/env python3
"""Atualiza o campo name do JSONL para incluir concentração (ex.: Amaryl 2mg)."""

from __future__ import annotations

import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from medication_metadata import format_display_name


def main() -> int:
    path = Path(__file__).resolve().parents[1] / "data" / "cr-index-bulas.jsonl"
    temp = path.with_suffix(path.suffix + ".tmp")
    changed = 0
    total = 0

    with path.open("r", encoding="utf-8") as source, temp.open("w", encoding="utf-8") as target:
        for line in source:
            line = line.strip()
            if not line:
                continue
            total += 1
            record = json.loads(line)
            formatted = format_display_name(record.get("name", ""), record.get("strength"))
            if formatted != record.get("name"):
                changed += 1
            record["name"] = formatted
            target.write(json.dumps(record, ensure_ascii=False) + "\n")

    try:
        temp.replace(path)
    except OSError:
        import shutil

        shutil.copyfile(temp, path)
        temp.unlink(missing_ok=True)

    print(f"Nomes atualizados: {changed}/{total}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
