# Cloud Agent environment

Scripts used by the Cursor Cloud Agent environment for **atomic.fm**.

The base image is a saved snapshot that already contains the toolchain:

- **.NET SDK 10** (`~/.dotnet`, on `PATH` via `/etc/profile.d/dotnet.sh`) — restore/edit the plugin
- **Python 3.12** — `setup.py`, `verify_props.sh`
- **Icecast 2.4.4 + ezstream 1.0.2 + ffmpeg** — the local radio test stream
- **PowerShell 7** — `tools/Test-AtomicRadioStream.ps1`

## Phases

| Phase | Script | What it does |
|-------|--------|--------------|
| `install` | `install.sh` | `dotnet restore` the plugin, then render the test-stream assets (`prepare-stream.sh`) |
| `start` | `start.sh` | Launch Icecast + an ezstream source and stay attached, serving `http://localhost:8000/atomic-radio.mp3` |

Runtime assets (rendered configs, a looping test tone, logs) live in
`~/.atomicfm-stream` (override with `ATOMICFM_STREAM_DIR`), outside the repo tree.

## Scope / limitation

The `ClientPlugin` is a **Windows-only** Space Engineers client plugin. A full
`dotnet build` needs the proprietary Space Engineers assemblies from the local
`Bin64` folder (see `Directory.Build.props`), which are not present here and must
never be shipped or decompiled. `dotnet build` therefore stops at the
missing-`Bin64` guard by design. This environment supports plugin editing +
restore and runs the Linux-side streaming backend that the plugin consumes.
