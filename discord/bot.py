"""Post and walk JSON if-then scripts as Discord button messages."""

from __future__ import annotations

import os
from pathlib import Path

import discord
from discord import app_commands
from discord.ext import commands

from ifthen.flow import FlowEngine, FlowError, FlowScript, UserContext

ROOT = Path(__file__).resolve().parent
FLOWS_DIR = ROOT / "flows"

STYLES = {
    "primary": discord.ButtonStyle.primary,
    "secondary": discord.ButtonStyle.secondary,
    "success": discord.ButtonStyle.success,
    "danger": discord.ButtonStyle.danger,
}


def load_scripts() -> dict[str, FlowScript]:
    scripts: dict[str, FlowScript] = {}
    for path in sorted(FLOWS_DIR.glob("*.json")):
        script = FlowScript.from_path(path)
        scripts[script.name] = script
    if not scripts:
        raise SystemExit(f"No flow JSON files in {FLOWS_DIR}")
    return scripts


def user_context(interaction: discord.Interaction) -> UserContext:
    role_ids: set[str] = set()
    role_names: set[str] = set()
    member = interaction.user
    roles = getattr(member, "roles", None)
    if roles:
        for role in roles:
            role_ids.add(str(role.id))
            role_names.add(role.name)
    return UserContext(
        user_id=str(interaction.user.id),
        role_ids=frozenset(role_ids),
        role_names=frozenset(role_names),
    )


class FlowButton(discord.ui.Button):
    def __init__(self, engine: FlowEngine, node_id: str, button_id: str, label: str, style: str):
        super().__init__(
            custom_id=f"ifthen:{engine.script.name}:{node_id}:{button_id}"[:100],
            label=label,
            style=STYLES.get(style, discord.ButtonStyle.secondary),
        )
        self.engine = engine
        self.node_id = node_id
        self.button_id = button_id

    async def callback(self, interaction: discord.Interaction) -> None:
        try:
            nxt = self.engine.click(self.node_id, self.button_id, user_context(interaction))
        except FlowError as exc:
            await interaction.response.send_message(str(exc), ephemeral=True)
            return
        await interaction.response.edit_message(
            embed=node_embed(self.engine.script.name, nxt),
            view=FlowView(self.engine, nxt.id, user_context(interaction)),
        )


class FlowView(discord.ui.View):
    def __init__(self, engine: FlowEngine, node_id: str, user: UserContext):
        super().__init__(timeout=None)
        node = engine.render(node_id, user)
        for button in node.buttons:
            self.add_item(FlowButton(engine, node.id, button.id, button.label, button.style))


def node_embed(script_name: str, node) -> discord.Embed:
    embed = discord.Embed(
        title=node.title or script_name,
        description=node.content,
        color=discord.Color.blurple(),
    )
    embed.set_footer(text=f"if-then · {script_name} · {node.id}")
    return embed


def make_bot(scripts: dict[str, FlowScript]) -> commands.Bot:
    bot = commands.Bot(command_prefix="!", intents=discord.Intents.default())
    engines = {name: FlowEngine(script) for name, script in scripts.items()}
    names = sorted(engines)

    @bot.event
    async def on_ready() -> None:
        await bot.tree.sync()
        print(f"Logged in as {bot.user} with flows: {', '.join(names)}")

    @bot.tree.command(name="ifthen", description="Post an if-then button script")
    @app_commands.describe(script="JSON flow name from discord/flows")
    async def ifthen(interaction: discord.Interaction, script: str | None = None) -> None:
        name = script or next(iter(names))
        engine = engines.get(name)
        if engine is None:
            await interaction.response.send_message(
                f"Unknown script `{name}`. Try: {', '.join(names)}",
                ephemeral=True,
            )
            return
        user = user_context(interaction)
        node = engine.start(user)
        await interaction.response.send_message(
            embed=node_embed(engine.script.name, node),
            view=FlowView(engine, node.id, user),
        )

    @ifthen.autocomplete("script")
    async def script_autocomplete(
        interaction: discord.Interaction, current: str
    ) -> list[app_commands.Choice[str]]:
        needle = current.casefold()
        return [
            app_commands.Choice(name=item, value=item)
            for item in names
            if needle in item.casefold()
        ][:25]

    return bot


def main() -> None:
    token = os.environ.get("DISCORD_TOKEN", "").strip()
    if not token:
        raise SystemExit("Set DISCORD_TOKEN to your bot token.")
    bot = make_bot(load_scripts())
    bot.run(token)


if __name__ == "__main__":
    main()
