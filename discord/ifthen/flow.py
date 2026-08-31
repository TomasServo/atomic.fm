"""Evaluate JSON if-then scripts and pick the next Discord button view."""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Mapping


class FlowError(ValueError):
    """Raised when a flow script is invalid or a click cannot be resolved."""


@dataclass(frozen=True)
class UserContext:
    """Facts about the clicker used by `if` conditions on buttons."""

    user_id: str = ""
    role_ids: frozenset[str] = field(default_factory=frozenset)
    role_names: frozenset[str] = field(default_factory=frozenset)

    def has_role_id(self, role_id: str) -> bool:
        return str(role_id) in self.role_ids

    def has_role_name(self, name: str) -> bool:
        return name.casefold() in {n.casefold() for n in self.role_names}


@dataclass
class ButtonSpec:
    id: str
    label: str
    then: str
    style: str = "secondary"
    condition: Mapping[str, Any] | None = None
    ephemeral: bool = False

    def visible_for(self, user: UserContext) -> bool:
        return matches_condition(self.condition, user)


@dataclass
class Node:
    id: str
    content: str
    buttons: list[ButtonSpec]
    title: str | None = None

    def visible_buttons(self, user: UserContext) -> list[ButtonSpec]:
        return [button for button in self.buttons if button.visible_for(user)]


@dataclass
class FlowScript:
    name: str
    start: str
    nodes: dict[str, Node]
    description: str = ""

    @classmethod
    def from_dict(cls, data: Mapping[str, Any]) -> FlowScript:
        if not isinstance(data, Mapping):
            raise FlowError("Flow script must be a JSON object.")
        name = _require_str(data, "name")
        start = _require_str(data, "start")
        raw_nodes = data.get("nodes")
        if not isinstance(raw_nodes, Mapping) or not raw_nodes:
            raise FlowError("Flow script needs a non-empty `nodes` object.")

        nodes: dict[str, Node] = {}
        for node_id, raw in raw_nodes.items():
            nodes[str(node_id)] = _parse_node(str(node_id), raw)

        script = cls(
            name=name,
            start=start,
            nodes=nodes,
            description=str(data.get("description") or ""),
        )
        script.validate()
        return script

    @classmethod
    def from_path(cls, path: str | Path) -> FlowScript:
        text = Path(path).read_text(encoding="utf-8")
        try:
            data = json.loads(text)
        except json.JSONDecodeError as exc:
            raise FlowError(f"Invalid JSON in {path}: {exc}") from exc
        return cls.from_dict(data)

    def validate(self) -> None:
        if self.start not in self.nodes:
            raise FlowError(f"Start node `{self.start}` is missing.")
        for node in self.nodes.values():
            for button in node.buttons:
                if button.then not in self.nodes:
                    raise FlowError(
                        f"Button `{button.id}` on `{node.id}` points at missing node `{button.then}`."
                    )

    def node(self, node_id: str) -> Node:
        try:
            return self.nodes[node_id]
        except KeyError as exc:
            raise FlowError(f"Unknown node `{node_id}`.") from exc


class FlowEngine:
    """Walk a script: show a node, click a visible button, land on `then`."""

    def __init__(self, script: FlowScript):
        self.script = script

    def start(self, user: UserContext | None = None) -> Node:
        return self.render(self.script.start, user or UserContext())

    def render(self, node_id: str, user: UserContext | None = None) -> Node:
        user = user or UserContext()
        node = self.script.node(node_id)
        return Node(
            id=node.id,
            content=node.content,
            title=node.title,
            buttons=node.visible_buttons(user),
        )

    def click(self, node_id: str, button_id: str, user: UserContext | None = None) -> Node:
        user = user or UserContext()
        node = self.script.node(node_id)
        match = next((button for button in node.buttons if button.id == button_id), None)
        if match is None:
            raise FlowError(f"No button `{button_id}` on node `{node_id}`.")
        if not match.visible_for(user):
            raise FlowError(f"Button `{button_id}` is hidden for this user.")
        return self.render(match.then, user)


def matches_condition(condition: Mapping[str, Any] | None, user: UserContext) -> bool:
    """True when the button should be shown.

    Supported keys (all optional; combined with AND):
    - has_role / has_any_role: Discord role name or snowflake id
    - missing_role: hide the button if the user has this role
    - user_id / user_ids: only these Discord user ids
    """
    if not condition:
        return True
    if not isinstance(condition, Mapping):
        raise FlowError("`if` must be an object.")

    if "user_id" in condition and str(condition["user_id"]) != user.user_id:
        return False
    if "user_ids" in condition:
        allowed = {str(item) for item in _as_list(condition["user_ids"])}
        if user.user_id not in allowed:
            return False

    if "has_role" in condition and not _has_any_role(user, [condition["has_role"]]):
        return False
    if "has_any_role" in condition and not _has_any_role(user, _as_list(condition["has_any_role"])):
        return False
    if "missing_role" in condition and _has_any_role(user, [condition["missing_role"]]):
        return False
    return True


def _has_any_role(user: UserContext, roles: list[Any]) -> bool:
    for role in roles:
        token = str(role)
        if token.isdigit():
            if user.has_role_id(token):
                return True
        elif user.has_role_name(token):
            return True
    return False


def _parse_node(node_id: str, raw: Any) -> Node:
    if not isinstance(raw, Mapping):
        raise FlowError(f"Node `{node_id}` must be an object.")
    content = _require_str(raw, "content")
    title = raw.get("title")
    raw_buttons = raw.get("buttons") or []
    if not isinstance(raw_buttons, list):
        raise FlowError(f"Node `{node_id}` `buttons` must be a list.")
    buttons = [_parse_button(node_id, item) for item in raw_buttons]
    ids = [button.id for button in buttons]
    if len(ids) != len(set(ids)):
        raise FlowError(f"Node `{node_id}` has duplicate button ids.")
    if len(buttons) > 25:
        raise FlowError(f"Node `{node_id}` has more than 25 buttons (Discord limit).")
    return Node(
        id=node_id,
        content=content,
        title=str(title) if title else None,
        buttons=buttons,
    )


def _parse_button(node_id: str, raw: Any) -> ButtonSpec:
    if not isinstance(raw, Mapping):
        raise FlowError(f"Buttons on `{node_id}` must be objects.")
    button_id = _require_str(raw, "id")
    if "|" in button_id or len(button_id) > 80:
        raise FlowError(f"Button id `{button_id}` is invalid.")
    return ButtonSpec(
        id=button_id,
        label=_require_str(raw, "label")[:80],
        then=_require_str(raw, "then"),
        style=str(raw.get("style") or "secondary"),
        condition=raw.get("if"),
        ephemeral=bool(raw.get("ephemeral", False)),
    )


def _require_str(data: Mapping[str, Any], key: str) -> str:
    value = data.get(key)
    if not isinstance(value, str) or not value.strip():
        raise FlowError(f"Missing string field `{key}`.")
    return value.strip()


def _as_list(value: Any) -> list[Any]:
    if isinstance(value, list):
        return value
    return [value]
