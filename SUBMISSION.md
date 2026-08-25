# Pulsar submission

This checklist is for preparing `atomic.fm` for the Space Engineers 1 Pulsar PluginHub.

## Pulsar paths

- Main install: `%AppData%\Pulsar`
- SE1 launcher: `%AppData%\Pulsar\Legacy.exe`
- SE1 data: `%AppData%\Pulsar\Legacy\`
- SE1 loader log: `%AppData%\Pulsar\Legacy\info.log`
- Optional separate .NET 10 data folder: `%AppData%\Pulsar\Interim`

## Launch options

Use this while testing plugin sources:

```powershell
%AppData%\Pulsar\Legacy.exe -skipintro -nosplash -sources
```

- `-skipintro`: starts Space Engineers faster
- `-nosplash`: skips the splash window
- `-sources`: enables the developer Sources dialog

## Recommended profiles

Create separate Pulsar profiles:

- `Development`: local DLL deployment
- `Test`: dev-folder build from this repository
- `Production`: public PluginHub install after the PR is merged

Back up profiles regularly and keep backup copies with a `Backup` suffix.

## Pre-release dev-folder test

Before submitting or updating PluginHub, test from Pulsar's dev-folder flow. This confirms Pulsar can build the plugin the same way players will receive it.

1. Launch Pulsar with `-skipintro -nosplash -sources`.
2. Open the Sources dialog.
3. Add this repository folder as a dev source.
4. Add the dev source to the `Test` plugin profile.
5. Double-click the dev-folder plugin entry and assign:

```text
AtomicRadio.xml
```

6. Build the dev source as `Release`.
7. Enable the plugin and launch Space Engineers.

Expected behavior:

- atomic.fm is off when the game starts.
- The opening menu is muted.
- `Ctrl+Alt+M` toggles playback.
- **Start atomic.fm** and **Stop atomic.fm** work from plugin settings.
- A terminal block with Custom Data can act as a radio source:

```text
atomic.fm=true
atomic.fm.range=35
atomic.fm.volume=0.3
```

## PluginHub submission

1. Fork `https://github.com/StarCpt/PluginHub`.
2. Copy `AtomicRadio.xml` into the fork as:

```text
Plugins/atomic.fm.xml
```

3. Commit the new XML file.
4. Open a PR to `StarCpt/PluginHub`.
5. Wait for human review and merge.

Updates use the same workflow: commit source changes, regenerate `AtomicRadio.xml` so its `<Commit>` points to the source commit, then PR the updated `Plugins/atomic.fm.xml` file to PluginHub.
