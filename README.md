# atomic.fm for Space Engineers

Client-side Space Engineers plugin prototype for Pulsar/Plugin Loader style clients. It streams an HTTP/HTTPS internet radio URL through the local Windows audio device.

This is intentionally a first-stage proof of concept. It does not hook into Sound Blocks, Jukeboxes, antennas, or Space Engineers positional audio yet.

## Features

- Configurable stream URL.
- Volume slider.
- Optional autoplay when the plugin loads.
- Start/stop buttons in the plugin config dialog.
- Fixed playback toggle hotkey: `Ctrl+Alt+F`.
- Optional Sound Block speaker mode. Name Sound Blocks with `[atomic.fm]` to make them local radio speaker anchors.
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
7. Press `Start atomic.fm`, or use `Ctrl+Alt+F`.

### Sound Block speaker mode

The plugin is still client-side. It does not replace vanilla Sound Block audio or broadcast audio through the server. Instead, tagged Sound Blocks act as local speaker anchors for the player's own client.

1. Place one or more Sound Blocks.
2. Add `[atomic.fm]` to each Sound Block name.
3. Make sure the Sound Blocks are enabled, functional, and powered.
4. Set each Sound Block's volume and range.
5. Start atomic.fm with `Ctrl+Alt+F`.

When Sound Block speaker mode is enabled, the stream stays synchronized locally while its volume follows the nearest tagged Sound Block. Players can opt in, change the stream URL, or disable speaker mode from their own plugin settings.

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
