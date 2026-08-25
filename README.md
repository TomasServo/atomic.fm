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
atomic.fm.volume=1.0
```

5. Stand near the block. The station starts automatically.

Any terminal block with Custom Data can be a radio source. Planters, lights, LCDs, cockpits, cargo containers, refineries, assemblers, and most functional blocks work. Plain armor blocks do not.

## Controls

- `Ctrl+Alt+J`: toggle playback
- `atomic.fm.range`: hearing range in meters
- `atomic.fm.volume`: block volume from `0.0` to `1.0`

## Notes

- Audio is client-side; each player chooses whether to install and enable the plugin.
- Marked blocks provide distance fade and left/right stereo panning.
- The default stream is `http://3.140.179.166:8000/atomic-radio`.
