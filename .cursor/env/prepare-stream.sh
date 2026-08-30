#!/usr/bin/env bash
# Idempotently render the local atomic.fm test-stream assets (Icecast + ezstream
# config, a looping test tone, and the playlist) into the runtime directory.
# Safe to run repeatedly; only regenerates the disposable test tone if missing.
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
RUNTIME_DIR="${ATOMICFM_STREAM_DIR:-$HOME/.atomicfm-stream}"

mkdir -p "$RUNTIME_DIR/logs"

# Render config templates, pinning the runtime directory for absolute paths.
sed "s#__RUNTIME__#${RUNTIME_DIR}#g" "$SCRIPT_DIR/icecast.xml" > "$RUNTIME_DIR/icecast.xml"
sed "s#__RUNTIME__#${RUNTIME_DIR}#g" "$SCRIPT_DIR/ezstream.xml" > "$RUNTIME_DIR/ezstream.xml"
# ezstream refuses to hide, but warns, on a world-readable config holding a password.
chmod 600 "$RUNTIME_DIR/ezstream.xml"

# A short looping test tone stands in for the real radio.atomic.fm feed so the
# environment has a self-contained, network-independent stream to serve.
TONE="$RUNTIME_DIR/atomic-test.mp3"
if [[ ! -s "$TONE" ]]; then
    ffmpeg -y -loglevel error \
        -f lavfi -i "sine=frequency=440:sample_rate=44100:duration=30" \
        -c:a libmp3lame -b:a 128k -ac 2 "$TONE"
fi

printf '%s\n' "$TONE" > "$RUNTIME_DIR/playlist.m3u"

echo "atomic.fm test-stream assets ready in $RUNTIME_DIR"
