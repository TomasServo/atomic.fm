# Icecast Test Server

This folder contains a minimal Ubuntu setup path for hosting a test stream that the Space Engineers plugin can play.

## Install Icecast

Run on Ubuntu, Debian, WSL Ubuntu, or a Linux VPS:

```bash
sudo ./install-icecast.sh
```

The script installs `icecast2` and `ezstream`.

## Configure Icecast

Edit `/etc/icecast2/icecast.xml` and set strong passwords for:

- `source-password`
- `relay-password`
- `admin-password`

Then enable the daemon in `/etc/default/icecast2`:

```text
ENABLE=true
```

Restart:

```bash
sudo systemctl restart icecast2
```

Icecast status page:

```text
http://localhost:8000
```

## Send a Test File Stream

Put MP3 files in a folder, then run:

```bash
ezstream -c ezstream-playlist.xml
```

The default mount in `ezstream-playlist.xml` is:

```text
http://localhost:8000/atomic-radio.mp3
```

Use that URL in the plugin while testing locally.

## Network Notes

For other players to hear the stream directly, the Icecast host must be reachable from their PCs. That usually means:

- open TCP port `8000` on the host firewall
- port-forward TCP `8000` on the router, if hosting from home
- use a VPS or public tunnel for easier multiplayer testing

