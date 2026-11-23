# Erster Arbeitsauftrag an Codex (VS Code)

Diesen Prompt in Codex einfügen. Er soll **zunächst nur analysieren und planen**, nicht das Backend in einem einzigen Auftrag implementieren.

```text
Arbeite am Repository Pathfinder Elements.
Lies zuerst AGENTS.md und anschließend alle Markdown-Dateien unter docs/ in der empfohlenen Reihenfolge (README.md).

Aufgabe: Meilenstein M0 — Ist-Zustand und belastbare Planung.

1. Untersuche die tatsächliche Angular-Projektstruktur inklusive vorhandener InitiativeTracker-Komponente, EncounterStore, Data-Access-Services, Typen, JSON-Seeds, Tests und Build-Skripte.
2. Vergleiche den Ist-Zustand mit docs/REQUIREMENTS.md, docs/DOMAIN_MODEL.md und docs/ARCHITECTURE.md.
3. Gib eine kompakte Bestandsaufnahme: was funktioniert bereits, was ist veraltet oder fehlt, wo liegen technische Schulden?
4. Identifiziere fachliche Blocker aus docs/OPEN_QUESTIONS.md für die ersten beiden Meilensteine.
5. Formuliere eine sichere, inkrementelle Implementierungsplanung für M1. Benenne vorgeschlagene Verzeichnis-/Projektstruktur für ASP.NET Core 10, PostgreSQL, EF Core und Docker Compose, ohne vorhandenen Code unnötig umzubauen.
6. Prüfe verfügbare Frontend-Builds und Tests; melde Ergebnisse und Probleme.

WICHTIG:
- NOCH KEINE Dateien ändern und KEINE Dependencies installieren.
- Keine unbegründeten Architekturänderungen; offene Produktfragen ausdrücklich markieren.
- Pathfinder 1E, nicht 2E.
- Block statements und ausgeschriebene Variablennamen verwenden.
- Antworte auf Deutsch mit: Ist-Zustand, Risiken/Blocker, vorgeschlagener Plan, Tests, benötigte Entscheidungen.
```

## Auftrag nach Freigabe des M0-Plans

```text
Setze nur den ersten abgestimmten, kleinen Teilschritt aus Meilenstein M1 um.
Halte dich an AGENTS.md und die Projektspezifikation.
Erstelle/ändere nur benötigte Dateien, ergänze Tests und dokumentiere überprüfte Ergebnisse.
Keine Architekturwechsel, keine ungefragten Feature-Erweiterungen und keine Änderungen an anderen Meilensteinen.
```
