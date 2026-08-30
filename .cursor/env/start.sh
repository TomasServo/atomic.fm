#!/usr/bin/env bash
# Cloud Agent start phase: bring up the local atomic.fm test stream.
#
# Starts Icecast (background) and, once it is accepting connections, an ezstream
# source that loops the test tone onto http://localhost:8000/atomic-radio.mp3 —
# the same kind of HTTP MP3 mount the plugin's RadioPlayer consumes in-game.
# The script stays attached (tailing logs) so the Cloud Agent surfaces it as a
# running service. Idempotent: it will not start a second copy of either process.
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
RUNTIME_DIR="${ATOMICFM_STREAM_DIR:-$HOME/.atomicfm-stream}"

# Self-heal if the install phase's assets are missing.
if [[ ! -f "$RUNTIME_DIR/icecast.xml" || ! -f "$RUNTIME_DIR/ezstream.xml" ]]; then
    bash "$SCRIPT_DIR/prepare-stream.sh"
fi

icecast_up() { curl -fsS -o /dev/null --max-time 2 http://localhost:8000/ 2>/dev/null; }

if icecast_up; then
    echo "Icecast already listening on :8000"
else
    echo "Starting Icecast on :8000"
    nohup icecast2 -c "$RUNTIME_DIR/icecast.xml" \
        > "$RUNTIME_DIR/logs/icecast-console.log" 2>&1 &
fi

echo "Waiting for Icecast to accept connections..."
for _ in $(seq 1 30); do
    if icecast_up; then break; fi
    sleep 1
done
icecast_up || { echo "ERROR: Icecast did not come up on :8000" >&2; exit 1; }

if pgrep -f "ezstream -c $RUNTIME_DIR/ezstream.xml" >/dev/null 2>&1; then
    echo "ezstream source already running"
else
    echo "Starting ezstream source -> /atomic-radio.mp3"
    nohup ezstream -c "$RUNTIME_DIR/ezstream.xml" \
        > "$RUNTIME_DIR/logs/ezstream-console.log" 2>&1 &
    sleep 3
fi

echo "atomic.fm test stream live at http://localhost:8000/atomic-radio.mp3"
echo "Tailing logs (Ctrl+C to detach; the stream keeps running)."
exec tail -n +1 -F "$RUNTIME_DIR/logs/icecast-console.log" "$RUNTIME_DIR/logs/ezstream-console.log"
