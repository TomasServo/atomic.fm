# Pulsar submission

This checklist is for preparing `atomic.fm` for the Space Engineers 1 Pulsar PluginHub.

`atomic.fm` is a Pulsar client plugin, not a Steam Workshop mod. Submit it to the `Plugins` folder in PluginHub. Do not use `Plugins/Mods`, `SampleMod.xml`, or a Workshop ID for this plugin.

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

## Build and debug locally

There are two useful local workflows.

### DLL deployment

Build from the IDE or MSBuild. The project build runs `Deploy.bat`, which copies the plugin DLL into:

```text
%AppData%\Pulsar\Legacy\Local
```

Use a `Debug` build when attaching a debugger to the running `Legacy.exe` process. `Instant Exit` is useful during development because it closes the game faster and avoids stuck background instances.

### Dev-folder build

Use Pulsar's dev-folder flow before every public release. This confirms Pulsar can build the repository the same way players will receive it from PluginHub.

Launch:

```powershell
%AppData%\Pulsar\Legacy.exe -skipintro -nosplash -sources
```

Then add this repository in the Sources dialog, add that source to a test profile, and assign `AtomicRadio.xml` as the plugin info file. Use `Release` for final pre-submission testing.

## Recommended profiles

Create separate Pulsar profiles:

- `Development`: local DLL deployment
- `Test`: dev-folder build from this repository
- `Production`: public PluginHub install after the PR is merged

Back up profiles regularly and keep backup copies with a `Backup` suffix.

## Pre-release dev-folder test

Before submitting or updating PluginHub, run this exact pre-release test:

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

## XML file

The repository submission descriptor is:

```text
AtomicRadio.xml
```

For PluginHub, submit the same file under this path in your PluginHub fork:

```text
Plugins/atomic.fm.xml
```

Before submitting, confirm these fields are correct:

- `Id`: `TomasServo/atomic.fm`
- `RepoId`: `TomasServo/atomic.fm`
- `FriendlyName`: `atomic.fm`
- `Author`: `TomasServo`
- `Hidden`: `false`
- `Commit`: a public commit hash from the `TomasServo/atomic.fm` repository
- `SourceDirectories`: `ClientPlugin`
- `NuGetReferences`: `NAudio` `2.2.1`

## PluginHub submission

1. Fork `https://github.com/StarCpt/PluginHub`.
2. Clone your fork with Git.
3. Check whether `atomic.fm` is already present in the `Plugins` folder.
4. Create a branch from `main`.
5. Copy `AtomicRadio.xml` into the fork as:

```text
Plugins/atomic.fm.xml
```

6. Commit the new XML file.
7. Push the branch.
8. Open a PR to `StarCpt/PluginHub`.
9. Wait for human review and merge.

Updates use the same workflow: commit source changes, regenerate `AtomicRadio.xml` so its `<Commit>` points to the source commit, then PR the updated `Plugins/atomic.fm.xml` file to PluginHub.

## Not the mod-registration path

The `Plugins/Mods` instructions are only for client-side Steam Workshop mods. They involve `SampleMod.xml` and Workshop IDs. `atomic.fm` is a compiled Pulsar client plugin, so its PR belongs in `Plugins/atomic.fm.xml`.
