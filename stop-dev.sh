#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
for pidfile in "$ROOT_DIR/.dev-logs/api.pid" "$ROOT_DIR/.dev-logs/web.pid"; do
  if [ -f "$pidfile" ]; then
    pid="$(cat "$pidfile" 2>/dev/null || true)"
    if [ -n "$pid" ]; then
      echo "Dừng process PID $pid từ $pidfile"
      kill "$pid" 2>/dev/null || true
    fi
    rm -f "$pidfile"
  fi
done

for port in 5000 8080; do
  pids="$(lsof -ti "tcp:${port}" || true)"
  if [ -n "$pids" ]; then
    echo "Giải phóng port $port: $pids"
    echo "$pids" | xargs -r kill -9
  fi
done

echo "Đã dừng backend và frontend."
