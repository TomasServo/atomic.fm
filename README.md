# Atomic Radio for Space Engineers

Client-side Space Engineers plugin prototype for Pulsar/Plugin Loader style clients. It streams an HTTP/HTTPS internet radio URL through the local Windows audio device.

This is intentionally a first-stage proof of concept. It does not hook into Sound Blocks, Jukeboxes, antennas, or Space Engineers positional audio yet.

## Features

- Configurable stream URL.
- Volume slider.
- Optional autoplay when the plugin loads.
- Start/stop buttons in the plugin config dialog.
- Toggle keybind, default `Ctrl+Alt+R`.
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
2. Enable `Internet Radio` from the local plugin list.
3. Restart if the loader asks you to.
4. Open the plugin config dialog.
5. Press `Start Radio`, or use `Ctrl+Alt+R`.

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

- Audio is global desktop audio, not in-world positional audio.
- Each player must install and enable the plugin locally.
- Some AAC/playlist/redirect streams may fail depending on Windows Media Foundation codecs and station headers.
- This should be treated as a trusted-source plugin only. Client plugins can access network and local system resources.
