# atomic.fm for Space Engineers

Client-side Space Engineers plugin for Pulsar/Plugin Loader style clients. It streams an HTTP/HTTPS internet radio URL through the local Windows audio device.

## Features

- Configurable stream URL.
- Volume slider. The default is `0.15` because internet radio streams are usually mastered louder than Space Engineers ambience.
- Optional autoplay when the plugin loads.
- Start/stop buttons in the plugin config dialog.
- Fixed playback toggle hotkey: `Ctrl+Alt+J`.
- Optional block anchor mode. Any terminal block with `atomic.fm=true` in Custom Data can act as an ambient local atomic.fm radio source with distance fade and left/right panning.
- NAudio-backed playback using Windows Media Foundation.

## Requirements

- Space Engineers 1 on Windows.
- Pulsar or another compatible client plugin loader.
- .NET Framework 4.8.1 Developer Pack for building.
- A local Space Engineers install path containing `Bin64`.

## Build

Create `Directory.Build.props.user` if the default Steam registry lookup does not find your Space Engineers install:

```xml
<Project>
  <PropertyGroup>
    <Bin64>C:\Path\To\Steam\steamapps\common\SpaceEngineers\Bin64</Bin64>
  </PropertyGroup>
</Project>
```

Then build `InternetRadio.sln` in Visual Studio/Rider, or run:

```powershell
dotnet restore .\InternetRadio.sln
dotnet build .\InternetRadio.sln -c Release
```

The post-build script copies `InternetRadio.dll` and dependency DLLs to:

```text
<Space Engineers>\Bin64\Plugins\Local
```

## Test

1. Start Space Engineers through Pulsar.
2. If the plugin is not published yet, start Pulsar with `-sources`, open **Sources**, and add this repository as a development folder.
3. Select `AtomicRadio.xml` as the plugin registration file when Pulsar asks for plugin metadata.
4. Enable `atomic.fm` from the plugin list.
5. Restart if the loader asks you to.
6. Open the plugin config dialog.
7. Press `Start atomic.fm`, or use `Ctrl+Alt+J`.

### Any Block As An atomic.fm Sound Source

The plugin is still client-side. It does not replace vanilla Sound Block audio or broadcast audio through the server. Instead, tagged blocks act as local speaker anchors for the player's own client.

You do not need to place a vanilla Sound Block. Any terminal block with Custom Data can act as an atomic.fm sound source, including planters, lights, antennas, LCDs, doors, cockpits, cargo containers, refineries, assemblers, and most functional blocks. Plain armor blocks cannot be used because they do not expose terminal Custom Data.

To make a block emit atomic.fm:

1. Aim at the block and open the terminal/control panel for that block.
2. Select the exact block in the terminal list.
3. Find the block's **Custom Data** field.
4. Add the required marker on its own line:
   ```text
   atomic.fm=true
   ```
5. Optionally add a range line. This controls how far away the block can be heard:
   ```text
   atomic.fm.range=35
   ```
6. Optionally add a volume line. This controls this block's loudness before distance fade:
   ```text
   atomic.fm.volume=1.0
   ```
7. Close the terminal.
8. Make sure the block is enabled, functional, and powered when that block type supports those states.
9. Stand near the marked block. atomic.fm scans for marked blocks and starts automatically when one is found.
10. Move away from the block to hear distance fade. Turn around the block to hear left/right stereo panning.

Complete Custom Data example:

```text
atomic.fm=true
atomic.fm.range=35
atomic.fm.volume=1.0
```

When block speaker mode is enabled, the plugin periodically scans for marked blocks and starts atomic.fm automatically when one is found. The stream stays synchronized locally while its volume and stereo pan follow the strongest nearby tagged anchor block. If no tagged blocks are found, atomic.fm plays at normal plugin volume only when manually started, so a missing block does not look like a broken stream. If tagged blocks exist and the player is outside their range, the stream fades out. Players can opt in, change the stream URL, or disable speaker mode from their own plugin settings.

Custom Data reference:

```text
atomic.fm=true       Required. Enables this block as an atomic.fm radio anchor.
atomic.fm.range=35   Optional. Audible range in meters. Default fallback is 50.
atomic.fm.volume=1.0 Optional. Anchor loudness from 0.0 to 1.0. Default is 1.0.
```

The default stream URL is your Icecast mount fed by SAM Broadcaster:

```text
http://3.140.179.166:8000/atomic-radio
```

Before launching the plugin, confirm the stream is live in a browser or media player.

From PowerShell, you can also run:

```powershell
.\tools\Test-AtomicRadioStream.ps1
```

## SAM Broadcaster to Icecast

SAM Broadcaster is the source client. Icecast is the transmitter. Use these SAM server settings:

```text
Server Type: IceCast
IceCast 2: selected
Server IP: 3.140.179.166
Server Port: 8000
Username: source
Password: <your Icecast source-password>
Mount: /atomic-radio
```

On the Icecast server, verify the mount is connected:

```bash
curl http://localhost:8000/status-json.xsl
```

The JSON should contain a source entry for `/atomic-radio`.

## Known Limitations

- Audio is rendered by the client through NAudio. Block anchors provide distance fade and stereo panning, not native Space Engineers server-side sound propagation.
- Each player must install and enable the plugin locally.
- Some AAC/playlist/redirect streams may fail depending on Windows Media Foundation codecs and station headers.
- This should be treated as a trusted-source plugin only. Client plugins can access network and local system resources.
