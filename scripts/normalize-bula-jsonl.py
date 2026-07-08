#!/usr/bin/env python3
"""Reorganiza packageInsert do JSONL em seções (INDICAÇÕES, POSOLOGIA, etc.)."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from bula_text import normalize_bula_text


def main() -> int:
    parser = argparse.ArgumentParser(description="Normaliza seções das bulas no JSONL")
    parser.add_argument(
        "--input",
        type=Path,
        default=Path(__file__).resolve().parents[1] / "data" / "consulta-remedios-bulas.jsonl",
    )
    parser.add_argument(
        "--output",
        type=Path,
        help="Saída (padrão: sobrescreve --input)",
    )
    args = parser.parse_args()

    input_path = args.input
    output_path = args.output or input_path
    temp_path = output_path.with_suffix(output_path.suffix + ".tmp")

    changed = 0
    total = 0
    with input_path.open("r", encoding="utf-8") as source, temp_path.open("w", encoding="utf-8") as target:
        for line in source:
            line = line.strip()
            if not line:
                continue
            total += 1
            record = json.loads(line)
            normalized = normalize_bula_text(record.get("packageInsert"))
            if normalized != record.get("packageInsert"):
                changed += 1
            record["packageInsert"] = normalized
            target.write(json.dumps(record, ensure_ascii=False) + "\n")

    temp_path.replace(output_path)
    print(f"Normalizado: {changed}/{total} registros -> {output_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
