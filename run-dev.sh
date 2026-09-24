#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOG_DIR="$ROOT_DIR/.dev-logs"
mkdir -p "$LOG_DIR"

cleanup_port() {
  local port="$1"
  local pid
  pid="$(lsof -ti "tcp:${port}" || true)"
  if [ -n "$pid" ]; then
    echo "Port ${port} đang được PID ${pid} chiếm dụng. Đang giải phóng..."
    kill -9 "$pid" || true
  fi
}

cleanup_port 5000
cleanup_port 8080

echo "[1/2] Khởi động backend API trên cổng 5000..."
nohup dotnet run --project "$ROOT_DIR/ExamSchedule.Api/ExamSchedule.Api.csproj" --urls http://0.0.0.0:5000 > "$LOG_DIR/api.log" 2>&1 &
echo $! > "$LOG_DIR/api.pid"

echo "[2/2] Khởi động frontend web trên cổng 8080..."
nohup bash -lc "cd '$ROOT_DIR/ExamSchedule.Web' && PORT=8080 ./serve.sh" > "$LOG_DIR/web.log" 2>&1 &
echo $! > "$LOG_DIR/web.pid"

echo
printf "Backend: http://<forwarded-url>:5000\n"
printf "Frontend: http://<forwarded-url>:8080\n"
printf "Logs: %s/api.log và %s/web.log\n" "$LOG_DIR" "$LOG_DIR"
printf "Để lấy URL thật, mở tab Ports trong VS Code. Không dùng localhost trong môi trường này.\n"
