# Atomic.FM — developer notes

Player-facing docs live in [README.md](../README.md). Below is the longer Pulsar / build / PluginHub reference.
# atomic.fm

Internet radio for Space Engineers through [Pulsar](https://github.com/SpaceGT/Pulsar).

Built from the [CometWorks client plugin template](https://github.com/CometWorks/client-plugin-template).

## Use

1. Install and enable **atomic.fm** in Pulsar (Local plugin after a build, or from the repo).
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
- Plugin config dialog: stream URL, volume, autoplay, anchor options
- `atomic.fm.range`: hearing range in meters
- `atomic.fm.volume`: block volume from `0.0` to `1.0`

## Development

### Prerequisites

- Space Engineers
- Python 3.12+
- [Pulsar](https://github.com/SpaceGT/Pulsar)
- .NET Framework 4.8.1 Developer Pack and .NET 10 SDK

### First-time setup

1. Clone this repository.
2. Run `setup.py` if you still need local path detection (or create `Directory.Build.props.user` yourself).
3. Open `atomic.fm.sln` in Visual Studio or Rider.

`Directory.Build.props.user` is gitignored. Example:

```xml
<Project>
  <PropertyGroup>
    <Bin64>C:\Path\To\SpaceEngineers\Bin64</Bin64>
    <Pulsar></Pulsar>
  </PropertyGroup>
</Project>
```

Leave `Bin64` / `Pulsar` empty to use Steam / AppData auto-detection (`%AppData%\Pulsar` on Windows).

### Pulsar paths

| Item | Path |
|------|------|
| Main installation folder | `%AppData%\Pulsar` |
| SE1 executable | `%AppData%\Pulsar\Legacy.exe` |
| SE1 data files | `%AppData%\Pulsar\Legacy\` |
| SE1 loader log | `%AppData%\Pulsar\Legacy\info.log` |
| Separate .NET 10 data folder (optional; missing by default) | `%AppData%\Pulsar\Interim` |

Create `Interim` next to `Legacy` when you want separate profiles and settings for .NET 10 (`Interim.exe`).

### Pulsar options

Suggested launch args for development:

```text
-skipintro -nosplash -sources
```

| Option | Purpose |
|--------|---------|
| `-skipintro` | Passed to the game for a faster start |
| `-nosplash` | Passed to the game to skip the splash window |
| `-sources` | Enables the Sources dialog (developers only) |

### Profiles

Use Pulsar **Profiles** to keep separate plugin lists:

| Profile | Loads plugins from | When to use |
|---------|--------------------|-------------|
| **Development** | DLL files (e.g. `Legacy\Local\atomic.fm\`) | Day-to-day IDE builds |
| **Test** | “dev” folders (Sources) | Before each release — verify Pulsar can build the plugin |
| **Production** | Public PluginHub registration | What players see after the hub PR merges |

You can include your usual third-party plugins in every profile.

Back up profiles regularly. Keep a copy of each with a `Backup` suffix so you do not overwrite the wrong one by mistake.

### Useful plugins for development

- **Instant Exit** — stops the game faster and cleaner (kills the process) and helps avoid stuck background instances.

### Release plugin to PluginHub

**atomic.fm** is a GitHub client **plugin** (not a Steam Workshop mod). Pulsar lists plugins from [StarCpt/PluginHub](https://github.com/StarCpt/PluginHub).

1. **Publish this code** to the public GitHub repo [`TomasServo/atomicfm`](https://github.com/TomasServo/atomicfm) (required for review and for Pulsar to clone).
2. **Fill `atomic.fm.xml`** in this folder (already prepared). Set `<Commit>` to the GitHub commit SHA you want players to run:

```bash
git rev-parse HEAD
```

   Update that same SHA in `PluginHub/Plugins/atomic.fm.xml` (copy of the descriptor for the hub PR).
3. **Fork** [StarCpt/PluginHub](https://github.com/StarCpt/PluginHub).
4. **Add** `Plugins/atomic.fm.xml` (from this repo’s `PluginHub/Plugins/atomic.fm.xml`) and open a pull request.
5. **Wait** for a human review of the plugin source. Be patient.
6. **Updates** later: bump `<Commit>` (and optionally description) in the PluginHub XML via another PR.

Do not change `<Id>` after the first publish (`TomasServo/atomic.fm`). `<RepoId>` must stay `TomasServo/atomicfm` (the real GitHub path).

### Registering a client-side mod to Pulsar

Client-only mods (no matching server-side mod required) can be listed in Pulsar via PluginHub. This does **not** apply to atomic.fm; use it if you also maintain a Workshop mod.

1. Check whether the mod is already on Pulsar.
2. Fork [StarCpt/PluginHub](https://github.com/StarCpt/PluginHub) and clone your fork.
3. Search `Plugins/Mods` for the mod’s Steam Workshop ID so you do not duplicate an entry.
4. Create a new branch from `main`.
5. Copy `SampleMod.xml` from the PluginHub repo root into `Plugins/Mods` and rename it to match the mod.
6. Fill in the XML fields (`xsi:type="ModPlugin"`, Workshop `<Id>`, name, author, tooltip/description). Use neighboring files as examples.
7. Commit, push, and open a PR to PluginHub.
8. Wait for review and merge if the mod is accepted.

The game updates Workshop mods themselves, so the XML usually does not need further changes unless you fix metadata fields.

### Build, run and debug locally

There are two ways to build and debug this client plugin locally.

#### 1. Build from the IDE (DLL deploy) — Development profile

A successful build runs the project's `DeployPlugin` target, which copies `plugin.dll`, `plugin.pdb`, `plugin.xml`, and NAudio dependencies into Pulsar's Local folder:

| Build     | Deployed to                              |
|-----------|------------------------------------------|
| `net48`   | `%AppData%\Pulsar\Legacy\Local\atomic.fm\` |
| `net10.0` | `%AppData%\Pulsar\Interim\Local\atomic.fm\` |

Then:

1. Enable **atomic.fm** in your **Development** profile.
2. Set up an IDE run configuration that starts `%AppData%\Pulsar\Legacy.exe` (or `Interim.exe` for .NET 10) with the debugger attached. That lets you debug plugin code and most of the game's code.
3. Use a **Debug** build if you plan to set breakpoints.
4. Pass `-skipintro -nosplash` (and `-sources` when needed). Enable **Instant Exit** for a faster, cleaner shutdown.

#### 2. Pulsar Sources / "dev" folder — Test profile

This path is essential before each release: Pulsar builds the plugin itself, so you catch cases where the IDE succeeds but Pulsar fails.

1. Start Pulsar with `-sources` so the Sources dialog is available.
2. Add this repository as a **dev folder** in that dialog.
3. Inside the dev folder you can make **Debug** or **Release** builds:
   - **Debug** — attach your IDE debugger to `Legacy.exe` (or `Interim.exe`).
   - **Release** — same build players get when they install the plugin.
4. After the dev folder is added, enable it in the regular plugin list and save it in your **Test** profile.
5. Double-click the dev folder in the Plugins list and assign `atomic.fm.xml` as the plugin info file.

> **Bug:** That XML association is currently not saved across restarts. Re-assign it if Pulsar forgets it (a PR exists to fix this).

#### Separating .NET Framework 4.8 and .NET 10

Create an `Interim` folder next to `Legacy` under `%AppData%\Pulsar`. Pulsar then keeps separate trees for profiles and settings so you can test both runtimes.

- Most useful for DLL-based local deployment (`Legacy\Local` vs `Interim\Local`).
- Still helpful with a Sources/dev-folder setup because you are not forced to rebuild from scratch as often when switching runtimes.

On Windows this project builds `net48` and `net10.0` by default; each lands in the matching Pulsar edition folder above.

### FAQ

**Can I develop plugins on Linux?**  
Prefer the Native Pulsar for Linux build. It supports direct plugin development without Proton and full debugging, same as on Windows.  
Note: **atomic.fm** itself is Windows-only today (`Platforms=Windows`) because playback uses NAudio Media Foundation / WinMM.

**Which C# versions are supported?**  
Up to C# 14 is known to work for plugins; set the language version to the latest minor in the project (this template uses C# 14). Mods are limited to C# 7.3 and programmable-block scripts to C# 6.0 — those are compiled by the game sandbox.

**Can I use NuGet packages?**  
Yes. Packages must support **.NET Standard 2.0** or **.NET Framework 4.8**. For Interim.exe (.NET 10) prefer **.NET Standard 2.0**. List runtime packages in `atomic.fm.xml` under `<NuGetReferences>` (this plugin declares NAudio 2.2.1).

**Can I use additional data files?**  
Ship them as asset files and implement `LoadAssets` on the `Plugin` class.

**Where can I find example source code?**  
Other Pulsar plugins are open source on GitHub — read their client plugin projects for patterns.

**How can I track game code changes between versions?**  
Clone [viktor-ferenczi/se-dev-skills](https://github.com/viktor-ferenczi/se-dev-skills), then run `Prepare.bat` (Windows) or `prepare.sh` (Linux) inside each skill to decompile the client or dedicated server. See each skill’s `SKILL.md`. Skills can re-prepare after game updates and help agents (and humans) track diffs.  
**Never publish decompiled game or server code.**

**Where are best practices and AI coding instructions?**  
See `AGENTS.md` in this repo (from the plugin template) and [se-dev-skills](https://github.com/viktor-ferenczi/se-dev-skills). Related: [CometWorks/skills](https://github.com/CometWorks/skills).

**My older plugin’s project file is not “SDK format”**  
Rebuild on the latest [client plugin template](https://github.com/CometWorks/client-plugin-template) and port the logic over (this repo already did that for atomic.fm).

### Notes

- Audio is client-side; each player chooses whether to install and enable the plugin.
- Marked blocks provide distance fade and left/right stereo panning via `RadioSampleProvider`.
- Default stream: `http://radio.atomic.fm:8000/atomic-radio`
- NAudio Media Foundation requires Windows.
