"""Inventory or materialize a portable Unity project, including Windows junction assets.

Read-only by default. --stage must name a new directory outside the source.
Excludes Library, builds, profiles, Git history and credentials by allowlisting roots.
"""
import argparse
import hashlib
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

ROOTS = ("Assets", "Packages", "ProjectSettings", "tools/apple", "docs/apple",
         "docs/licenses", "docs/ASSET_PROVENANCE.md")


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def inventory(source):
    entries, links = [], []

    def walk(path, relative, ancestors):
        resolved = path.resolve(strict=True)
        if path.is_symlink() or (hasattr(path, "is_junction") and path.is_junction()):
            links.append({"path": relative.as_posix(), "target": str(resolved)})
        if path.is_dir():
            if resolved in ancestors:
                raise ValueError(f"Directory link cycle: {relative}")
            for child in sorted(path.iterdir()):
                walk(child, relative / child.name, ancestors | {resolved})
        else:
            entries.append({"path": relative.as_posix(), "bytes": path.stat().st_size})

    for name in ROOTS:
        walk(source / name, Path(name), set())
    return entries, links


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--stage", type=Path)
    args = parser.parse_args()
    source = args.source.resolve(strict=True)
    if not (source / "ProjectSettings/ProjectVersion.txt").is_file():
        parser.error("Source is not a Unity project")
    entries, links = inventory(source)
    result = {"created_utc": datetime.now(timezone.utc).isoformat(), "source": str(source),
              "files": len(entries), "bytes": sum(x["bytes"] for x in entries),
              "materialized_links": links, "entries": entries, "staged": False}
    if args.stage:
        stage = args.stage.resolve()
        if stage.exists() or stage == source or source in stage.parents or stage in source.parents:
            parser.error("Stage must be a NEW directory outside the source tree")
        probe = stage.parent
        while not probe.exists():
            probe = probe.parent
        if shutil.disk_usage(probe).free < result["bytes"] + 2 * 1024**3:
            parser.error("Insufficient space: keep at least 2 GiB extra for copy; Unity import needs more")
        stage.mkdir(parents=True)
        for item in entries:
            src, dst = source / item["path"], stage / item["path"]
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src, dst)
            original = sha256(src)
            digest = sha256(dst)
            if digest != original:
                raise RuntimeError(f"Copy mismatch: {item['path']}")
            item["sha256"] = digest
        result["staged"] = True
        result["stage"] = str(stage)
        (stage / ".testflight-staging.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({k: v for k, v in result.items() if k != "entries"}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
