"""Utilitários para espelho HTTrack do Consulta Remédios (bulas offline)."""

from __future__ import annotations

import re
from dataclasses import dataclass
from pathlib import Path

from cr_bula_scraper import LEAFLET_START_RE

DEFAULT_MIRROR_DIR = Path(r"C:\Meus Sites\BulasdeAaZ")
SITE_ROOT_NAME = "consultaremedios.com.br"
BULA_FILE_NAMES = ("bula.html", "bula.html.tmp")
PRODUCT_FILE_NAMES = ("p.html", "p.html.tmp", "pa.html.tmp")

HTTRACK_LOCK = "hts-in_progress.lock"
HTTRACK_LOG = "hts-log.txt"
HTTRACK_CACHE = "hts-cache/new.txt"

BULA_URL_RE = re.compile(
    r"https://consultaremedios\.com\.br/([a-z0-9-]+)/bula\t",
    re.IGNORECASE,
)


@dataclass(frozen=True)
class BulaMirrorFile:
    slug: str
    bula_path: Path
    product_path: Path | None
    pending: bool


@dataclass
class MirrorStatus:
    mirror_dir: Path
    in_progress: bool
    lock_pid: int | None
    bula_final: int
    bula_pending: int
    bula_with_content: int
    bula_empty: int
    product_files: int
    cache_bula_urls: int
    total_html_tmp: int
    ready_for_import: bool

    @property
    def bula_total(self) -> int:
        return self.bula_final + self.bula_pending


def resolve_site_root(mirror_dir: Path) -> Path:
    direct = mirror_dir / SITE_ROOT_NAME
    if direct.is_dir():
        return direct
    raise FileNotFoundError(
        f"Pasta do site não encontrada em {mirror_dir / SITE_ROOT_NAME}"
    )


def read_lock_pid(mirror_dir: Path) -> int | None:
    lock_path = mirror_dir / HTTRACK_LOCK
    if not lock_path.exists():
        return None
    text = lock_path.read_text(encoding="utf-8", errors="replace")
    match = re.search(r"PID=(\d+)", text)
    return int(match.group(1)) if match else None


def is_mirror_in_progress(mirror_dir: Path) -> bool:
    return (mirror_dir / HTTRACK_LOCK).exists()


def count_cache_bula_urls(mirror_dir: Path) -> int:
    cache_path = mirror_dir / HTTRACK_CACHE
    if not cache_path.exists():
        return 0
    count = 0
    with cache_path.open("r", encoding="utf-8", errors="replace") as handle:
        for line in handle:
            if BULA_URL_RE.search(line):
                count += 1
    return count


def find_product_html(slug_dir: Path) -> Path | None:
    for name in PRODUCT_FILE_NAMES:
        candidate = slug_dir / name
        if candidate.is_file() and candidate.stat().st_size > 0:
            return candidate
    return None


def discover_bula_files(
    mirror_dir: Path,
    *,
    include_pending: bool = True,
) -> list[BulaMirrorFile]:
    site_root = resolve_site_root(mirror_dir)
    discovered: dict[str, BulaMirrorFile] = {}

    names = list(BULA_FILE_NAMES) if include_pending else ["bula.html"]
    for name in names:
        pending = name.endswith(".tmp")
        for path in site_root.glob(f"*/{name}"):
            slug = path.parent.name
            if slug in ("bulas", "_frsh") or slug.startswith("."):
                continue
            current = discovered.get(slug)
            if current and not pending and current.pending:
                pass
            elif current and pending and not current.pending:
                continue
            discovered[slug] = BulaMirrorFile(
                slug=slug,
                bula_path=path,
                product_path=find_product_html(path.parent),
                pending=pending,
            )

    return sorted(discovered.values(), key=lambda item: item.slug)


def html_has_bula_content(path: Path) -> bool:
    try:
        sample = path.read_text(encoding="utf-8", errors="replace")[:500_000]
    except OSError:
        return False
    return LEAFLET_START_RE.search(sample) is not None


def inspect_mirror(mirror_dir: Path) -> MirrorStatus:
    mirror_dir = mirror_dir.resolve()
    site_root = resolve_site_root(mirror_dir)
    files = discover_bula_files(mirror_dir, include_pending=True)

    bula_final = sum(1 for item in files if not item.pending)
    bula_pending = sum(1 for item in files if item.pending)
    bula_with_content = sum(1 for item in files if html_has_bula_content(item.bula_path))
    bula_empty = len(files) - bula_with_content
    product_files = sum(1 for item in files if item.product_path is not None)
    total_html_tmp = len(list(site_root.rglob("*.html.tmp")))

    in_progress = is_mirror_in_progress(mirror_dir)
    return MirrorStatus(
        mirror_dir=mirror_dir,
        in_progress=in_progress,
        lock_pid=read_lock_pid(mirror_dir),
        bula_final=bula_final,
        bula_pending=bula_pending,
        bula_with_content=bula_with_content,
        bula_empty=bula_empty,
        product_files=product_files,
        cache_bula_urls=count_cache_bula_urls(mirror_dir),
        total_html_tmp=total_html_tmp,
        ready_for_import=not in_progress and bula_with_content > 0,
    )


def format_status_report(status: MirrorStatus) -> str:
    lines = [
        f"Espelho: {status.mirror_dir}",
        f"HTTrack: {'EM ANDAMENTO' if status.in_progress else 'FINALIZADO'}",
    ]
    if status.lock_pid:
        lines.append(f"PID HTTrack: {status.lock_pid}")
    lines.extend(
        [
            f"Bulas baixadas: {status.bula_total} ({status.bula_final} final, {status.bula_pending} pendentes .tmp)",
            f"Bulas com texto clínico: {status.bula_with_content}",
            f"Bulas sem conteúdo: {status.bula_empty}",
            f"Páginas de produto (/p): {status.product_files}",
            f"URLs /bula no cache HTTrack: {status.cache_bula_urls}",
            f"Arquivos .html.tmp no espelho: {status.total_html_tmp}",
        ]
    )
    if status.in_progress:
        lines.append(
            "Importação offline: pode usar --include-pending enquanto o espelho não termina."
        )
    elif status.ready_for_import:
        lines.append("Pronto para importar: python scripts/import-offline-cr-bulas.py")
    return "\n".join(lines)
