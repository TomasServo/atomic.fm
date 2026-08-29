#!/usr/bin/env bash
set -euo pipefail

if [[ "${EUID}" -ne 0 ]]; then
  echo "Run with sudo: sudo ./install-icecast.sh" >&2
  exit 1
fi

apt-get update
DEBIAN_FRONTEND=noninteractive apt-get install -y icecast2 ezstream

if [[ -f /etc/default/icecast2 ]]; then
  sed -i 's/^ENABLE=.*/ENABLE=true/' /etc/default/icecast2
fi

systemctl enable icecast2
systemctl restart icecast2
systemctl --no-pager status icecast2 || true

cat <<'MSG'

Icecast is installed.

Next:
1. Edit /etc/icecast2/icecast.xml and set strong source/admin passwords.
2. Restart with: sudo systemctl restart icecast2
3. Open: http://localhost:8000

MSG

