#!/usr/bin/env bash
set -euo pipefail

repository_root=$(cd "$(dirname "$0")/../.." && pwd)
test_directory=$(mktemp -d -t pathfinder-persistence-XXXXXXXX)
project_name=$(basename "$test_directory" | tr '[:upper:]' '[:lower:]')
export POSTGRES_PASSWORD
POSTGRES_PASSWORD=$(openssl rand -hex 32)
export API_PORT=0
compose=(docker compose -f "$repository_root/compose.yaml" --project-name "$project_name")
restore_compose=(docker compose -f "$repository_root/compose.yaml" --project-name "$project_name-restore")
backup_file="$test_directory/pathfinder.dump"
umask 077

cleanup() {
    if [[ $? -ne 0 ]]; then
        "${compose[@]}" logs --tail 30 db api >&2
        "${restore_compose[@]}" logs --tail 30 db api >&2
    fi
    "${restore_compose[@]}" down --volumes --rmi local
    "${compose[@]}" down --volumes --rmi local
    rm -f -- "$backup_file"
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
    local sql=$1
    shift
    "$@" exec -T db psql -X -v ON_ERROR_STOP=1 -U pathfinder -d pathfinder -Atc "$sql"
}

library_snapshot() {
    local fields='template.id, template.name, owner."NormalizedUserName", template.max_hit_points,
                  template.initiative_modifier, template.armor_class, template.touch_armor_class,
                  template.flat_footed_armor_class, template.fortitude_save, template.reflex_save, template.will_save,
                  template.is_archived, template.version'
    query "
        SELECT $fields FROM character_templates AS template
        JOIN \"AspNetUsers\" AS owner ON owner.\"Id\" = template.owner_user_id;
        SELECT $fields FROM monster_templates AS template
        JOIN \"AspNetUsers\" AS owner ON owner.\"Id\" = template.owner_user_id;
    " "$@"
}

"${compose[@]}" up --build -d
api_url="http://$("${compose[@]}" port api 8080)"
wait_for_status live 200
wait_for_status ready 200
query "INSERT INTO application_metadata (key, value) VALUES ('compose-test', 'volume-ok')" "${compose[@]}"
printf '%s\n' 'Synthetic-Backup-Test123!' | "${compose[@]}" exec -T api dotnet PathfinderElements.Api.dll account create backup-test
"${compose[@]}" exec -T db createdb -U pathfinder pathfinder_api_tests
"${compose[@]}" exec -T db createdb -U pathfinder pathfinder_upgrade_tests
docker run --rm --user "$(id -u):$(id -g)" --network "${project_name}_default" \
    --env DOTNET_CLI_HOME=/tmp --env PATHFINDER_TEST_POSTGRES_HOST=db --env POSTGRES_PASSWORD \
    --volume "$repository_root/backend:/source:ro" --workdir /source mcr.microsoft.com/dotnet/sdk:10.0 \
    dotnet test PathfinderElements.slnx --configuration Release --artifacts-path /tmp/pathfinder-artifacts \
    --filter 'FullyQualifiedName~LibraryTemplateTests.RoundTrips|FullyQualifiedName~LibraryTemplateTests.Existing|FullyQualifiedName~LibraryApiTests'
expected_library_snapshot=$(library_snapshot "${compose[@]}")
[[ $(wc -l <<< "$expected_library_snapshot") -eq 2 ]]
original_container=$("${compose[@]}" ps -q db)
"${compose[@]}" stop db
wait_for_status ready 503
wait_for_status live 200

"${compose[@]}" up -d --no-deps --force-recreate --wait db
[[ "$original_container" != "$("${compose[@]}" ps -q db)" ]]
wait_for_status ready 200
[[ "$(query "SELECT value FROM application_metadata WHERE key = 'compose-test'" "${compose[@]}")" == 'volume-ok' ]]
[[ "$(library_snapshot "${compose[@]}")" == "$expected_library_snapshot" ]]
echo "OK: Migration, Readiness, Liveness und Persistenz der Bibliotheksvorlagen nach Containerwechsel."

"${compose[@]}" exec -T db pg_dump -U pathfinder -d pathfinder --format=custom > "$backup_file"
"${restore_compose[@]}" up -d --wait db
"${restore_compose[@]}" exec -T db pg_restore --list < "$backup_file" >/dev/null
"${restore_compose[@]}" exec -T db pg_restore -U pathfinder -d pathfinder \
    --single-transaction --exit-on-error --no-owner --no-privileges < "$backup_file"
"${restore_compose[@]}" up --build -d api
api_url="http://$("${restore_compose[@]}" port api 8080)"
wait_for_status live 200
wait_for_status ready 200
[[ "$(query "SELECT value FROM application_metadata WHERE key = 'compose-test'" "${restore_compose[@]}")" == 'volume-ok' ]]
[[ "$(query 'SELECT "NormalizedUserName" FROM "AspNetUsers" WHERE "PasswordHash" IS NOT NULL' "${restore_compose[@]}")" == 'BACKUP-TEST' ]]
[[ "$(library_snapshot "${restore_compose[@]}")" == "$expected_library_snapshot" ]]
echo "OK: Restore in frischem Volume mit Bibliotheksvorlagen, Datensatz und Identity-Konto."
