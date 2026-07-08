#!/usr/bin/env python3
"""Verifica progresso do espelho HTTrack de bulas do Consulta Remédios."""

from __future__ import annotations

import argparse
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from offline_cr_mirror import DEFAULT_MIRROR_DIR, format_status_report, inspect_mirror


def main() -> int:
    parser = argparse.ArgumentParser(description="Status do espelho offline de bulas")
    parser.add_argument(
        "--mirror-dir",
        type=Path,
        default=DEFAULT_MIRROR_DIR,
        help="Pasta raiz do espelho HTTrack",
    )
    parser.add_argument(
        "--watch",
        action="store_true",
        help="Aguarda até o HTTrack finalizar (remove hts-in_progress.lock)",
    )
    parser.add_argument(
        "--interval",
        type=float,
        default=30.0,
        help="Intervalo em segundos entre verificações com --watch",
    )
    args = parser.parse_args()

    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    mirror_dir = args.mirror_dir.resolve()
    if not mirror_dir.is_dir():
        print(f"Pasta não encontrada: {mirror_dir}", file=sys.stderr)
        return 1

    while True:
        status = inspect_mirror(mirror_dir)
        print(format_status_report(status))
        if not args.watch or not status.in_progress:
            break
        print(f"\nAguardando {args.interval:.0f}s...\n")
        time.sleep(args.interval)

    if args.watch and not status.in_progress:
        print("\nEspelho finalizado. Próximo passo:")
        print("  python scripts/import-offline-cr-bulas.py --resume")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
