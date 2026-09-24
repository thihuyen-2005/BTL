#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"
if ss -ltn "sport = :${PORT:-8080}" 2>/dev/null | grep -q LISTEN; then
	echo "Web server đã chạy trên cổng ${PORT:-8080}."
	exit 0
fi
exec python3 -m http.server "${PORT:-8080}" --bind 0.0.0.0