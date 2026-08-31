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
atomic.fm.volume=5.5
```

Or use section format:

```text
[atomic.fm]
enabled=true
range=35
volume=5.5
```

5. Press `Ctrl+Alt+M` or use **Start atomic.fm** in plugin settings. Stand near the block to hear it.

Any terminal block with Custom Data can be a radio source. Planters, lights, LCDs, cockpits, cargo containers, refineries, assemblers, and most functional blocks work. Plain armor blocks do not.

## Controls

- `Ctrl+Alt+M`: toggle playback
- `atomic.fm.range`: hearing range in meters
- `atomic.fm.volume`: block volume from `0` to `11`; decimals such as `1.5` and `5.5` work
- `[atomic.fm]` section keys can use `enabled`, `range`, and `volume`
- atomic.fm is off when the game starts.
- Default plugin volume is `11` on the `0-11` scale.
- The stream uses the configured volume immediately after startup.
- The opening menu stays silent.

## Notes

- Audio is client-side; each player chooses whether to install and enable the plugin.
- Marked blocks provide distance fade and left/right stereo panning.
- The stream URL can be changed from the Pulsar plugin settings.
- Pulsar release testing and PluginHub submission steps are in [SUBMISSION.md](SUBMISSION.md).
