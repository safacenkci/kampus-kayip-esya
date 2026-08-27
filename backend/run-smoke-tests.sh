#!/usr/bin/env bash
# Start PostgreSQL + the API, then run HTTP smoke tests against http://localhost:5080.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BACKEND="$ROOT/backend"
API_URL="${API_BASE_URL:-http://localhost:5080}"
DB_NAME="kampus_kayip_esya"
DB_USER="postgres"
DB_PASSWORD="postgres"
API_LOG="${API_LOG:-/tmp/kampus-kayip-esya-api.log}"
API_PID_FILE="${API_PID_FILE:-/tmp/kampus-kayip-esya-api.pid}"

log() { printf '%s\n' "$*"; }

wait_for_port() {
  local host="$1" port="$2" seconds="${3:-30}"
  for _ in $(seq 1 "$seconds"); do
    if (echo >"/dev/tcp/${host}/${port}") >/dev/null 2>&1; then
      return 0
    fi
    sleep 1
  done
  return 1
}

start_postgres() {
  if command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
    log "Starting PostgreSQL with docker compose..."
    (cd "$ROOT" && docker compose down -v >/dev/null 2>&1 || true)
    (cd "$ROOT" && docker compose up -d postgres)
    for _ in $(seq 1 40); do
      if docker exec kampus-kayip-esya-db pg_isready -U postgres -d "$DB_NAME" >/dev/null 2>&1; then
        log "PostgreSQL (docker) is ready."
        return 0
      fi
      sleep 1
    done
    log "ERROR: docker postgres did not become ready."
    return 1
  fi

  log "Docker is not available; starting local PostgreSQL 16..."
  if command -v pg_ctlcluster >/dev/null 2>&1; then
    sudo pg_ctlcluster 16 main start || true
  elif command -v service >/dev/null 2>&1; then
    sudo service postgresql start || true
  fi

  if ! wait_for_port 127.0.0.1 5432 20; then
    log "ERROR: local PostgreSQL did not listen on 5432."
    return 1
  fi

  sudo -u postgres psql -v ON_ERROR_STOP=1 <<'SQL'
ALTER USER postgres WITH PASSWORD 'postgres';
SELECT 'CREATE DATABASE kampus_kayip_esya'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'kampus_kayip_esya')\gexec
SQL
  sudo -u postgres psql -d "$DB_NAME" -v ON_ERROR_STOP=1 -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public; GRANT ALL ON SCHEMA public TO postgres; GRANT ALL ON SCHEMA public TO public;"
  log "PostgreSQL (local) is ready with a fresh public schema."
}

stop_api() {
  if [[ -f "$API_PID_FILE" ]]; then
    local pid
    pid="$(cat "$API_PID_FILE" || true)"
    if [[ -n "${pid:-}" ]] && kill -0 "$pid" 2>/dev/null; then
      kill "$pid" 2>/dev/null || true
      wait "$pid" 2>/dev/null || true
    fi
    rm -f "$API_PID_FILE"
  fi
  if command -v fuser >/dev/null 2>&1; then
    fuser -k 5080/tcp >/dev/null 2>&1 || true
  fi
}

start_api() {
  stop_api
  log "Starting API: cd backend && dotnet run (http://localhost:5080)"
  (
    cd "$BACKEND"
    ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5080 \
      nohup dotnet run --urls http://localhost:5080 >"$API_LOG" 2>&1 &
    echo $! >"$API_PID_FILE"
  )
  for _ in $(seq 1 60); do
    if curl -sf "$API_URL/api/categories" >/dev/null 2>&1; then
      log "API is ready at $API_URL"
      return 0
    fi
    sleep 1
  done
  log "ERROR: API did not become ready. Last log lines:"
  tail -n 80 "$API_LOG" || true
  return 1
}

start_postgres
start_api

export API_BASE_URL="$API_URL"
export API_PROJECT_DIR="$BACKEND"

log "Running smoke tests..."
dotnet test "$BACKEND/KampusKayipEsya.Api.Tests/KampusKayipEsya.Api.Tests.csproj" --logger "console;verbosity=detailed"
