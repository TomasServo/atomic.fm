#!/usr/bin/env python3
"""
Prepare atomic.fm for public Pulsar PluginHub submission.

This replaces the original client-plugin-template setup flow. It does not rename
projects or regenerate GUIDs. It updates and validates the PluginHub XML
descriptor for the current public repository.
"""

from __future__ import annotations

import argparse
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


PLUGIN_ID = "TomasServo/atomic.fm"
REPO_ID = "TomasServo/atomic.fm"
FRIENDLY_NAME = "atomic.fm"
AUTHOR = "TomasServo"
TOOLTIP = "Streams atomic.fm from Icecast into the Space Engineers client."
SOURCE_DIRECTORY = "ClientPlugin"
NAUDIO_VERSION = "2.2.1"
DESCRIPTION_LIMIT = 1000

DESCRIPTION = """All Ultralounge all the time.

atomic.fm brings live internet radio, music, and ambient station audio to Space Engineers through Pulsar. Tune into an Icecast stream in the client, or turn almost any terminal block into a spatial radio source by adding atomic.fm=true to Custom Data.

Use it for lounges, bars, ships, stations, hangars, stores, events, faction bases, and server communities that want music without adding a server mod. Optional per-block settings include atomic.fm.range=35 and atomic.fm.volume=5.5 on a 0-11 scale. Ctrl+Alt+M toggles playback manually. Audio is client-side, so each player chooses whether to install, enable, and hear atomic.fm."""


def run_git(args: list[str], repo_root: Path) -> str:
    result = subprocess.run(
        ["git", *args],
        cwd=repo_root,
        check=True,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    return result.stdout.strip()


def require_clean_worktree(repo_root: Path) -> None:
    status = run_git(["status", "--short"], repo_root)
    if status:
        raise SystemExit(
            "Working tree has uncommitted changes. Commit or stash them before "
            "preparing a public PluginHub descriptor."
        )


def current_commit(repo_root: Path) -> str:
    commit = run_git(["rev-parse", "HEAD"], repo_root)
    if len(commit) != 40:
        raise SystemExit(f"Unexpected git commit hash: {commit}")
    return commit


def text(node: ET.Element, name: str, value: str) -> ET.Element:
    child = node.find(name)
    if child is None:
        child = ET.SubElement(node, name)
    child.text = value
    return child


def ensure_source_directory(root: ET.Element) -> None:
    source_directories = root.find("SourceDirectories")
    if source_directories is None:
        source_directories = ET.SubElement(root, "SourceDirectories")

    directories = source_directories.findall("Directory")
    if not directories:
        directories.append(ET.SubElement(source_directories, "Directory"))

    directories[0].text = SOURCE_DIRECTORY
    for extra in directories[1:]:
        source_directories.remove(extra)


def ensure_nuget_reference(root: ET.Element) -> None:
    nuget = root.find("NuGetReferences")
    if nuget is None:
        nuget = ET.SubElement(root, "NuGetReferences")

    for child in list(nuget):
        nuget.remove(child)

    package = ET.SubElement(nuget, "PackageReference")
    package.set("Include", "NAudio")
    package.set("Version", NAUDIO_VERSION)


def update_descriptor(path: Path, commit: str) -> None:
    ET.register_namespace("xsd", "http://www.w3.org/2001/XMLSchema")
    ET.register_namespace("xsi", "http://www.w3.org/2001/XMLSchema-instance")

    tree = ET.parse(path)
    root = tree.getroot()

    xsi_type = "{http://www.w3.org/2001/XMLSchema-instance}type"
    root.set(xsi_type, "GitHubPlugin")

    text(root, "Id", PLUGIN_ID)
    text(root, "RepoId", REPO_ID)
    text(root, "FriendlyName", FRIENDLY_NAME)
    text(root, "Author", AUTHOR)
    text(root, "Tooltip", TOOLTIP)
    text(root, "Description", DESCRIPTION)
    ensure_source_directory(root)
    text(root, "Runtimes", "CLR;Mono")
    text(root, "Platforms", "Windows")
    ensure_nuget_reference(root)
    text(root, "Hidden", "false")
    text(root, "Commit", commit)

    tree.write(path, encoding="utf-8", xml_declaration=True)


def validate_descriptor(path: Path, commit: str) -> None:
    root = ET.parse(path).getroot()

    required = {
        "Id": PLUGIN_ID,
        "RepoId": REPO_ID,
        "FriendlyName": FRIENDLY_NAME,
        "Author": AUTHOR,
        "Hidden": "false",
        "Commit": commit,
    }
    for name, expected in required.items():
        actual = (root.findtext(name) or "").strip()
        if actual != expected:
            raise SystemExit(f"{path}: expected {name}={expected!r}, got {actual!r}")

    description = root.findtext("Description") or ""
    if len(description) > DESCRIPTION_LIMIT:
        raise SystemExit(
            f"{path}: description is {len(description)} characters; "
            f"PluginHub limit is {DESCRIPTION_LIMIT}."
        )

    source_directories = [node.text for node in root.findall("SourceDirectories/Directory")]
    if SOURCE_DIRECTORY not in source_directories:
        raise SystemExit(f"{path}: missing SourceDirectories/Directory={SOURCE_DIRECTORY}")

    packages = root.findall("NuGetReferences/PackageReference")
    has_naudio = any(
        package.get("Include") == "NAudio" and package.get("Version") == NAUDIO_VERSION
        for package in packages
    )
    if not has_naudio:
        raise SystemExit(f"{path}: missing NAudio {NAUDIO_VERSION} NuGet reference")


def copy_submission_file(source: Path, destination: Path) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(source.read_bytes())


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Prepare atomic.fm XML for public Pulsar PluginHub submission."
    )
    parser.add_argument(
        "--repo-root",
        type=Path,
        default=Path(__file__).resolve().parents[1],
        help="Path to the atomic.fm repository root.",
    )
    parser.add_argument(
        "--pluginhub",
        type=Path,
        help="Optional path to a local StarCpt/PluginHub fork.",
    )
    parser.add_argument(
        "--allow-dirty",
        action="store_true",
        help="Allow preparing the descriptor with uncommitted local changes.",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    repo_root = args.repo_root.resolve()
    descriptor = repo_root / "AtomicRadio.xml"

    if not descriptor.is_file():
        raise SystemExit(f"Could not find descriptor: {descriptor}")

    if not args.allow_dirty:
        require_clean_worktree(repo_root)

    commit = current_commit(repo_root)
    update_descriptor(descriptor, commit)
    validate_descriptor(descriptor, commit)

    print(f"Prepared {descriptor}")
    print(f"PluginHub commit: {commit}")
    print(f"Description length: {len(DESCRIPTION)}")

    if args.pluginhub:
        destination = args.pluginhub.resolve() / "Plugins" / "atomic.fm.xml"
        copy_submission_file(descriptor, destination)
        print(f"Copied submission XML to {destination}")
    else:
        print("Submit AtomicRadio.xml as Plugins/atomic.fm.xml in StarCpt/PluginHub.")


if __name__ == "__main__":
    try:
        main()
    except subprocess.CalledProcessError as exc:
        sys.exit(exc.stderr or str(exc))
