#!/usr/bin/env python3
"""Generate a GitHub Wiki Home.md from the repository README."""

from pathlib import Path
import os
import re

repository = os.environ["GITHUB_REPOSITORY"]
readme = Path("README.md").read_text(encoding="utf-8")


def rewrite_link(match: re.Match[str]) -> str:
    prefix, target, suffix = match.groups()
    path, separator, fragment = target.partition("#")
    encoded_fragment = f"#{fragment}" if separator else ""

    if path == "../../releases":
        destination = f"https://github.com/{repository}/releases"
    elif path == "LICENSE":
        destination = f"https://github.com/{repository}/blob/main/LICENSE"
    elif path.startswith("docs/"):
        if Path(path).suffix.lower() in {".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg"}:
            destination = f"https://raw.githubusercontent.com/{repository}/main/{path}"
        else:
            destination = f"https://github.com/{repository}/blob/main/{path}"
    else:
        return match.group(0)

    return f"{prefix}{destination}{encoded_fragment}{suffix}"


# Rewrite relative Markdown links so they work from the separate wiki repository.
readme = re.sub(r"(!?\[[^\]]*\]\()([^)]+)(\))", rewrite_link, readme)
Path("Home.md").write_text(readme.rstrip() + "\n", encoding="utf-8")
