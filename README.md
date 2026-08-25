# atomic.fm

Internet radio for Space Engineers through Pulsar.

## Use

1. Install and enable `atomic.fm` in Pulsar.
2. Start Space Engineers.
3. Open any terminal block's **Custom Data**.
4. Add:

```text
atomic.fm=true
atomic.fm.range=35
atomic.fm.volume=0.3
```

5. Press `Ctrl+Alt+M` or use **Start atomic.fm** in plugin settings. Stand near the block to hear it.

Any terminal block with Custom Data can be a radio source. Planters, lights, LCDs, cockpits, cargo containers, refineries, assemblers, and most functional blocks work. Plain armor blocks do not.

## Controls

- `Ctrl+Alt+M`: toggle playback
- `atomic.fm.range`: hearing range in meters
- `atomic.fm.volume`: block volume from `0.0` to `1.0`; start around `0.3`
- atomic.fm is off when the game starts.
- The opening menu is muted.
- Default playback is intentionally quiet. Raise `Volume` or `atomic.fm.volume` if needed.

## Notes

- Audio is client-side; each player chooses whether to install and enable the plugin.
- Marked blocks provide distance fade and left/right stereo panning.
- The stream URL can be changed from the Pulsar plugin settings.
