# Atomic.FM

All Ultralounge all the time.

## How to use

1. Install and enable **Atomic.FM** in Pulsar.
2. In Space Engineers, open any block that has **Custom Data**.
3. Paste this:

```text
atomic.fm=true
atomic.fm.range=35
atomic.fm.volume=1.5
```

4. Stand near the block. The station starts automatically.

Plain armor blocks do not work — they have no Custom Data.

## Controls

- `Ctrl+Alt+M` — toggle playback
- `atomic.fm.range` — hearing range in meters
- `atomic.fm.volume` — block volume (`0.0`–`11.0`, decimals allowed)

Default stream: `http://radio.atomic.fm:8000/atomic-radio`

## Discord if-then menu

A JSON button script for community help lives in `discord/`. `/ifthen` posts the tree; each button is *if you click this, then show that node*. See `discord/README.md`.
