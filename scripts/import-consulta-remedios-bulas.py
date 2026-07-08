#!/usr/bin/env python3
"""Extrai bulas do Consulta Remédios (consultaremedios.com.br/bulas) para JSONL.

Uso:
  python scripts/import-consulta-remedios-bulas.py --batch 1 --resume
  python scripts/import-consulta-remedios-bulas.py --letter a --limit 20
  python scripts/import-consulta-remedios-bulas.py --all --resume

Lotes (26 letras a-z + grupo 0-9 no lote 5):
  1: a b c d e
  2: f g h i j
  3: k l m n o
  4: p q r s t
  5: u v w x y z 0-9

Saída padrão: data/consulta-remedios-bulas.jsonl
"""

from __future__ import annotations

import argparse
import html
import json
import re
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from bula_text import normalize_bula_text

BASE_URL = "https://consultaremedios.com.br"
LETTERS = list("abcdefghijklmnopqrstuvwxyz") + ["0-9"]
BATCHES: dict[int, list[str]] = {
    1: list("abcde"),
    2: list("fghij"),
    3: list("klmno"),
    4: list("pqrst"),
    5: list("uvwxyz") + ["0-9"],
}
USER_AGENT = "SistemaHospitalar-BulaImport/1.0 (+uso interno hospitalar)"
NAME_PREFIX = "Bula do "

LISTING_LINK_RE = re.compile(
    r'href="(/[^"]+/bula)"[^>]*>\s*(Bula do [^<]+)\s*</a>',
    re.IGNORECASE,
)
ACTIVE_INGREDIENT_RE = re.compile(
    r'Princ[ií]pio\s+Ativo[^:]*:\s*</[^>]+>\s*<[^>]+>([^<]+)',
    re.IGNORECASE,
)
LEAFLET_START_RE = re.compile(r'class="leaflet-article"', re.IGNORECASE)
TAG_RE = re.compile(r"<[^>]+>")


def configure_utf8_output() -> None:
    """Evita mojibake (Laborat¾rio) no console/log do Windows."""
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    if hasattr(sys.stderr, "reconfigure"):
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")


def make_logger(log_file: Path | None):
    handle = None
    if log_file is not None:
        log_file.parent.mkdir(parents=True, exist_ok=True)
        handle = log_file.open("a", encoding="utf-8")

    def log(message: str) -> None:
        print(message, file=sys.stderr)
        if handle is not None:
            handle.write(message + "\n")
            handle.flush()

    def close() -> None:
        if handle is not None:
            handle.close()

    return log, close


def clean_name(raw: str) -> str:
    name = html.unescape(raw.strip())
    if name.lower().startswith(NAME_PREFIX.lower()):
        name = name[len(NAME_PREFIX) :].strip()
    return name


def html_to_text(fragment: str) -> str:
    text = TAG_RE.sub(" ", fragment)
    text = html.unescape(text)
    text = re.sub(r"\s+", " ", text).strip()
    return text


def fetch(url: str, retries: int = 3) -> str:
    last_error: Exception | None = None
    for attempt in range(retries):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
            with urllib.request.urlopen(req, timeout=60) as resp:
                charset = resp.headers.get_content_charset() or "utf-8"
                return resp.read().decode(charset, errors="replace")
        except (urllib.error.URLError, TimeoutError) as exc:
            last_error = exc
            time.sleep(1.5 * (attempt + 1))
    raise RuntimeError(f"Falha ao baixar {url}: {last_error}") from last_error


def extract_leaflet(html_content: str) -> str:
    match = LEAFLET_START_RE.search(html_content)
    if not match:
        return ""
    start = match.end()
    end = html_content.find("</article>", start)
    chunk = html_content[start:end if end != -1 else start + 50000]
    return html_to_text(chunk)


def extract_active_ingredient(html_content: str) -> str | None:
    match = ACTIVE_INGREDIENT_RE.search(html_content)
    if not match:
        return None
    value = html.unescape(match.group(1)).strip()
    return value or None


def listing_pages(letter: str):
    page = 1
    while True:
        suffix = "" if page == 1 else f"?pagina={page}"
        url = f"{BASE_URL}/bulas/{letter}{suffix}"
        content = fetch(url)
        links = LISTING_LINK_RE.findall(content)
        if not links:
            break
        yield url, links
        if 'rel="next"' not in content:
            break
        page += 1


def scrape_bula(path: str, display_name: str) -> dict:
    url = f"{BASE_URL}{path}"
    html_content = fetch(url)
    slug = path.strip("/").split("/")[0]
    name = clean_name(display_name)
    package_insert = normalize_bula_text(extract_leaflet(html_content))
    active_ingredient = extract_active_ingredient(html_content)
    return {
        "name": name,
        "slug": slug,
        "url": url,
        "activeIngredient": active_ingredient,
        "packageInsert": package_insert,
    }


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
    parser = argparse.ArgumentParser(description="Importa bulas do Consulta Remédios")
    parser.add_argument("--letter", action="append", help="Letra(s) do bulário (a-z ou 0-9)")
    parser.add_argument(
        "--batch",
        type=int,
        choices=sorted(BATCHES),
        help="Lote 1-5 (5 letras cada; lote 5 com u-z + 0-9)",
    )
    parser.add_argument("--all", action="store_true", help="Processa todas as letras")
    parser.add_argument("--limit", type=int, default=0, help="Limite de bulas novas")
    parser.add_argument("--delay", type=float, default=0.35, help="Intervalo entre requisições (s)")
    parser.add_argument("--resume", action="store_true", help="Pula slugs já presentes no JSONL")
    parser.add_argument(
        "--output",
        type=Path,
        default=Path(__file__).resolve().parents[1] / "data" / "consulta-remedios-bulas.jsonl",
    )
    parser.add_argument(
        "--log-file",
        type=Path,
        help="Arquivo de log UTF-8 (evita erro de acentuação no PowerShell)",
    )
    args = parser.parse_args()

    configure_utf8_output()
    log, close_log = make_logger(args.log_file)

    if args.batch:
        letters = BATCHES[args.batch]
        log(f"Lote {args.batch}/5: {', '.join(letters)}")
    elif args.all:
        letters = LETTERS
    else:
        letters = args.letter or ["a"]
    output_path: Path = args.output
    output_path.parent.mkdir(parents=True, exist_ok=True)

    existing = load_existing_slugs(output_path) if args.resume else set()
    imported = 0
    skipped = 0

    try:
        with output_path.open("a", encoding="utf-8") as out:
            for letter in letters:
                log(f"Letra {letter}...")
                for _page_url, links in listing_pages(letter):
                    for path, label in links:
                        slug = path.strip("/").split("/")[0]
                        if slug in existing:
                            skipped += 1
                            continue

                        try:
                            record = scrape_bula(path, label)
                        except Exception as exc:
                            log(f"  ERRO {slug}: {exc}")
                            continue

                        if not record["packageInsert"]:
                            log(f"  AVISO sem conteúdo: {record['name']}")
                            continue

                        out.write(json.dumps(record, ensure_ascii=False) + "\n")
                        out.flush()
                        existing.add(slug)
                        imported += 1
                        log(f"  + {record['name']}")

                        if args.limit and imported >= args.limit:
                            log(
                                f"Concluído: {imported} importadas, {skipped} ignoradas -> {output_path}"
                            )
                            return 0

                        time.sleep(args.delay)

        log(f"Concluído: {imported} importadas, {skipped} ignoradas -> {output_path}")
        return 0
    finally:
        close_log()


if __name__ == "__main__":
    raise SystemExit(main())
