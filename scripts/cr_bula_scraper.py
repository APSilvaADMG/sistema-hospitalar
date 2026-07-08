"""Funções compartilhadas para extrair bulas do Consulta Remédios."""

from __future__ import annotations

import html
import re
import time
import urllib.error
import urllib.request

from bula_text import normalize_bula_text
from medication_metadata import extract_metadata

BASE_URL = "https://consultaremedios.com.br"
USER_AGENT = "SistemaHospitalar-BulaImport/1.0 (+uso interno hospitalar)"
NAME_PREFIX = "Bula do "

LEAFLET_START_RE = re.compile(r'class="leaflet-article"', re.IGNORECASE)
TAG_RE = re.compile(r"<[^>]+>")


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
    chunk = html_content[start : end if end != -1 else start + 50000]
    return html_to_text(chunk)


def slug_from_bula_url(url: str) -> str:
    parts = [part for part in url.rstrip("/").split("/") if part]
    if len(parts) >= 2 and parts[-1].lower() == "bula":
        return parts[-2]
    return parts[-1] if parts else ""


def extract_active_ingredient(html_content: str) -> str | None:
    metadata = extract_metadata(name="", bula_html=html_content)
    return metadata.get("activeIngredient")


def fetch_product_html(slug: str) -> str:
    candidates = [f"{BASE_URL}/{slug}/p", f"{BASE_URL}/{slug}"]
    last_error: Exception | None = None
    for candidate in candidates:
        try:
            return fetch(candidate)
        except Exception as exc:
            last_error = exc
    raise RuntimeError(f"Falha ao baixar produto {slug}: {last_error}") from last_error


def scrape_bula_from_html(
    bula_html: str,
    slug: str,
    display_name: str | None = None,
    product_html: str = "",
    url: str | None = None,
) -> dict:
    name = clean_name(display_name) if display_name else extract_title_name(bula_html) or slug.replace("-", " ").title()
    package_insert = normalize_bula_text(extract_leaflet(bula_html))
    metadata = extract_metadata(
        name=name,
        bula_html=bula_html,
        product_html=product_html,
        package_insert=package_insert,
    )

    return {
        "name": name,
        "slug": slug,
        "url": url or f"{BASE_URL}/{slug}/bula",
        "activeIngredient": metadata.get("activeIngredient"),
        "pharmaceuticalForm": metadata.get("pharmaceuticalForm"),
        "strength": metadata.get("strength"),
        "route": metadata.get("route"),
        "packageInsert": package_insert,
    }


def extract_title_name(html_content: str) -> str | None:
    match = re.search(r"<title[^>]*>([^<]+)</title>", html_content, re.IGNORECASE)
    if not match:
        return None
    title = html.unescape(match.group(1).strip())
    for suffix in (" | CR", " - Consulta Remédios", " | Consulta Remédios"):
        if title.endswith(suffix):
            title = title[: -len(suffix)].strip()
    if title.lower().startswith(NAME_PREFIX.lower()):
        return clean_name(title)
    bula_split = re.split(r":\s*bula\b", title, maxsplit=1, flags=re.IGNORECASE)
    if len(bula_split) > 1 and bula_split[0].strip():
        return bula_split[0].strip()
    return clean_name(title) if title else None


def scrape_bula_url(url: str, display_name: str | None = None) -> dict:
    bula_html = fetch(url)
    slug = slug_from_bula_url(url)
    name = clean_name(display_name) if display_name else None

    try:
        product_html = fetch_product_html(slug)
    except Exception:
        product_html = ""

    return scrape_bula_from_html(
        bula_html=bula_html,
        slug=slug,
        display_name=name,
        product_html=product_html,
        url=url,
    )
