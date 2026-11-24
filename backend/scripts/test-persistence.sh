#!/usr/bin/env bash
set -euo pipefail

repository_root=$(cd "$(dirname "$0")/../.." && pwd)
test_directory=$(mktemp -d -t pathfinder-persistence-XXXXXXXX)
project_name=$(basename "$test_directory" | tr '[:upper:]' '[:lower:]')
export POSTGRES_PASSWORD
POSTGRES_PASSWORD=$(openssl rand -hex 32)
export API_PORT=0
compose=(docker compose -f "$repository_root/compose.yaml" --project-name "$project_name")

cleanup() {
    if [[ $? -ne 0 ]]; then
        "${compose[@]}" logs --tail 30 db api >&2
    fi
    "${compose[@]}" down --volumes --rmi local
    rmdir "$test_directory"
}
trap cleanup EXIT

wait_for_status() {
    local endpoint=$1 expected=$2 actual
    for ((attempt = 1; attempt <= 45; attempt++)); do
        actual=$(curl --silent --max-time 5 --output /dev/null --write-out '%{http_code}' "$api_url/health/$endpoint") || true
        if [[ "$actual" == "$expected" ]]; then
            return 0
        fi
        sleep 1
    done
    echo "$endpoint: HTTP $expected erwartet, zuletzt $actual" >&2
    return 1
}

query() {
    "${compose[@]}" exec -T db psql -X -v ON_ERROR_STOP=1 -U pathfinder -d pathfinder -Atc "$1"
}

"${compose[@]}" up --build -d
api_url="http://$("${compose[@]}" port api 8080)"
wait_for_status live 200
wait_for_status ready 200
query "INSERT INTO application_metadata (key, value) VALUES ('compose-test', 'volume-ok')"
original_container=$("${compose[@]}" ps -q db)
"${compose[@]}" stop db
wait_for_status ready 503
wait_for_status live 200

"${compose[@]}" up -d --no-deps --force-recreate --wait db
[[ "$original_container" != "$("${compose[@]}" ps -q db)" ]]
wait_for_status ready 200
[[ "$(query "SELECT value FROM application_metadata WHERE key = 'compose-test'")" == 'volume-ok' ]]
echo "OK: Migration, Readiness, Liveness und Persistenz nach Containerwechsel."
