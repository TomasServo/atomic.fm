# Discord if-then button scripts

Post a JSON **if → then** tree in Discord. Each button is a branch: *if the user clicks this, then show that node*. Optional `if` on a button hides it unless the clicker has a role or user id.

This is a small bot next to the Atomic.FM plugin. It does not talk to Space Engineers.

## Write a script

Add a file under `flows/`. `atomic.fm.json` is the example help menu.

```json
{
  "name": "atomic-fm",
  "start": "root",
  "nodes": {
    "root": {
      "title": "Atomic.FM",
      "content": "What do you need?",
      "buttons": [
        { "id": "listen", "label": "Listen in-game", "style": "primary", "then": "listen" },
        {
          "id": "dj",
          "label": "DJ tools",
          "style": "success",
          "then": "dj",
          "if": { "has_any_role": ["DJ", "Admin"] }
        }
      ]
    },
    "listen": {
      "content": "Install the plugin, then paste Custom Data on a block.",
      "buttons": [{ "id": "home", "label": "Back", "then": "root" }]
    },
    "dj": {
      "content": "Only people with DJ or Admin see the button that leads here.",
      "buttons": [{ "id": "home", "label": "Back", "then": "root" }]
    }
  }
}
```

Button `style`: `primary`, `secondary`, `success`, `danger`.

Button `if` (all keys optional, combined with AND):

| Key | Meaning |
| --- | --- |
| `has_role` | Role name or snowflake id |
| `has_any_role` | List of names or ids |
| `missing_role` | Hide if they have this role |
| `user_id` / `user_ids` | Only these Discord users |

Discord allows at most 25 buttons per message (5 rows).

## Preview without Discord

```bash
cd discord
PYTHONPATH=. python3 preview.py --flow flows/atomic.fm.json listen sound
PYTHONPATH=. python3 preview.py --roles DJ listen
```

Omit button ids for an interactive prompt.

## Run the bot

1. [Discord Developer Portal](https://discord.com/developers/applications) → New Application → Bot.
2. Invite the bot with `bot` and `applications.commands`. Role `if` checks use the clicker's roles from the interaction (no privileged intents).
3. Copy the bot token.

```bash
cd discord
python3 -m pip install -r requirements.txt
export DISCORD_TOKEN='your-bot-token'
PYTHONPATH=. python3 bot.py
```

In a server channel: `/ifthen` or `/ifthen script:atomic-fm`.

Clicking a button **edits the same message** to the next node, so one post stays a single menu instead of a stack of replies.

## Tests

```bash
cd discord
PYTHONPATH=. python3 tests/test_flow.py
```
