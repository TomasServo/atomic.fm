"""Walk an if-then flow in the terminal (no Discord token required)."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

from ifthen.flow import FlowEngine, FlowError, FlowScript, UserContext

ROOT = Path(__file__).resolve().parent
DEFAULT_FLOW = ROOT / "flows" / "atomic.fm.json"


def parse_roles(raw: str) -> frozenset[str]:
    return frozenset(part.strip() for part in raw.split(",") if part.strip())


def print_node(node) -> None:
    title = node.title or node.id
    print()
    print(f"== {title} ({node.id}) ==")
    print(node.content)
    print()
    if not node.buttons:
        print("(no buttons)")
        return
    for index, button in enumerate(node.buttons, start=1):
        extra = f"  [if {dict(button.condition)}]" if button.condition else ""
        print(f"  {index}. {button.label}  -> {button.then}{extra}")


def interactive(engine: FlowEngine, user: UserContext) -> int:
    node = engine.start(user)
    while True:
        print_node(node)
        if not node.buttons:
            return 0
        raw = input("Click button # (q to quit): ").strip()
        if raw.lower() in {"q", "quit", "exit"}:
            return 0
        try:
            index = int(raw)
            button = node.buttons[index - 1]
        except (ValueError, IndexError):
            print("Pick a listed number.")
            continue
        try:
            node = engine.click(node.id, button.id, user)
        except FlowError as exc:
            print(exc)
    return 0


def scripted(engine: FlowEngine, user: UserContext, clicks: list[str]) -> int:
    node = engine.start(user)
    print_node(node)
    for click in clicks:
        node = engine.click(node.id, click, user)
        print(f"\n-- clicked {click} --")
        print_node(node)
    return 0


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Preview a Discord if-then button script.")
    parser.add_argument("--flow", type=Path, default=DEFAULT_FLOW, help="Path to flow JSON")
    parser.add_argument("--roles", default="", help="Comma-separated role names for `if` checks")
    parser.add_argument("--user-id", default="preview", help="Fake Discord user id")
    parser.add_argument(
        "clicks",
        nargs="*",
        help="Button ids to click in order. Omit for an interactive prompt.",
    )
    args = parser.parse_args(argv)

    try:
        script = FlowScript.from_path(args.flow)
        engine = FlowEngine(script)
        user = UserContext(user_id=args.user_id, role_names=parse_roles(args.roles))
        if args.clicks:
            return scripted(engine, user, args.clicks)
        if not sys.stdin.isatty():
            print_node(engine.start(user))
            return 0
        return interactive(engine, user)
    except FlowError as exc:
        print(exc, file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
