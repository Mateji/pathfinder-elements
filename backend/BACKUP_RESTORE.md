# PostgreSQL sichern und wiederherstellen

Das Runbook gilt für PostgreSQL 17 im Compose-Stack. Alle Befehle laufen im Repository-Root in Bash. Das Archiv enthält die Anwendungsdaten, die EF-Migrationshistorie und die Identity-Konten samt Passwort-Hashes. Bewahre es zugriffsgeschützt und verschlüsselt außerhalb des Servers auf. Backup-Zeitplan und Aufbewahrung sind in [OQ-15](../docs/OPEN_QUESTIONS.md) offen.

## Backup erstellen

Der Datenbankdienst muss laufen. `pg_dump` erzeugt einen konsistenten Stand, während die Anwendung weiterläuft. Für eine geplante Migration mit anschließender Umschaltung zuerst die schreibende API stoppen und dann den endgültigen Dump erstellen. [PostgreSQL-Dokumentation zu pg_dump](https://www.postgresql.org/docs/17/app-pgdump.html)

```bash
(
    set -euo pipefail
    umask 077
    backup_root="$HOME/pathfinder-backups"
    mkdir -p -- "$backup_root"
    backup_dir=$(mktemp -d "$backup_root/pathfinder-$(date -u +%Y%m%dT%H%M%SZ)-XXXXXXXX")
    partial_dump="$backup_dir/pathfinder.dump.partial"
    trap 'rm -f -- "$partial_dump"' EXIT

    docker compose exec -T db pg_dump -U pathfinder -d pathfinder --format=custom > "$partial_dump"
    docker compose exec -T db pg_restore --list < "$partial_dump" >/dev/null
    mv -- "$partial_dump" "$backup_dir/pathfinder.dump"
    (cd "$backup_dir" && sha256sum pathfinder.dump > SHA256SUMS)
    printf 'Backup unter %s\n' "$backup_dir"
)
```

Übertrage `pathfinder.dump` und `SHA256SUMS` gemeinsam. Sichere die Serverkonfiguration und Secrets über den dafür vorgesehenen geschützten Kanal. Das Volume `auth_keys` ist separat vom Datenbankarchiv. Eine Wiederherstellung mit neuem Schlüsselvolume erfordert eine erneute Anmeldung. Bei einem Sicherheitsvorfall betroffene Zugangsdaten zurücksetzen und neue Sitzungsschlüssel verwenden.

## In ein frisches Compose-Projekt wiederherstellen

Verwende ein Archiv aus einer vertrauenswürdigen Quelle. Wähle einen bisher unbenutzten Compose-Projektnamen, damit das Ziel ein eigenes frisches Datenbank-Volume erhält. `docker compose -p "$restore_project" ps -a` zeigt vorhandene Container dieses Projekts. Starte zuerst nur die Datenbank, damit die API vor dem Restore keine Migrationen anwendet. [PostgreSQL-Dokumentation zu pg_restore](https://www.postgresql.org/docs/17/app-pgrestore.html)

```bash
(
    set -euo pipefail
    backup_dir="/pfad/zur/backup-kopie"
    restore_project="pathfinder-restore-01"

    (cd "$backup_dir" && sha256sum -c SHA256SUMS)
    docker compose -p "$restore_project" up -d --wait db
    docker compose -p "$restore_project" exec -T db pg_restore --list < "$backup_dir/pathfinder.dump" >/dev/null
    docker compose -p "$restore_project" exec -T db pg_restore -U pathfinder -d pathfinder \
        --single-transaction --exit-on-error --no-owner --no-privileges < "$backup_dir/pathfinder.dump"
    API_PORT=0 docker compose -p "$restore_project" up --build -d api
    restore_api=$(docker compose -p "$restore_project" port api 8080)
    curl --fail --retry 10 --retry-connrefused "http://$restore_api/health/ready"
)
```

Der Restore läuft in einer Transaktion und bricht beim ersten Fehler ab. Bei einem Fehler zunächst Archiv, Prüfsumme und Zielprojekt prüfen. Für einen weiteren Versuch ein neues Zielprojekt wählen. Verwende für die API denselben Anwendungsstand wie zum Backup-Zeitpunkt oder ein vorgesehenes Upgrade, das offene EF-Migrationen anwenden kann.

Vor einer Umschaltung zusätzlich die erwarteten Daten und das vorhandene Konto am Restore-Ziel prüfen. Für den Login-Test HTTPS und die Proxy-Quelladresse des Zielprojekts nach der [Caddy-Anleitung](README.md#https-mit-caddy-auf-dem-linux-host) konfigurieren. Bei einer produktiven Umschaltung die alte API stoppen, den Ziel-API-Port für Caddy festlegen, Caddy umstellen und Readiness sowie Login erneut prüfen. Das ursprüngliche Projekt und sein Volume für eine Rückkehr bereithalten. Wenn bereits neue Schreibvorgänge am Ziel erfolgt sind, diese vor einer Rückkehr berücksichtigen.

## Automatischer Wiederherstellungstest

`backend/scripts/test-persistence.sh` erstellt Quell- und Restore-Projekte mit synthetischen Daten. Er prüft Readiness beim Datenbankausfall, das Fortbestehen der Daten nach einem Containerwechsel und die Wiederherstellung eines Testdatensatzes und des Identity-Kontos aus dem Archiv. Danach räumt der Test seine Container, Volumes und den temporären Dump auf. [Testvoraussetzungen](README.md#automatischer-persistenztest)
