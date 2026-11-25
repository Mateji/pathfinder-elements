# DECISIONS — Entscheidungsprotokoll

**Stand:** 19.11.2026 · `F` = vom Auftraggeber festgelegt; `V` = vorgeschlagene, noch zu validierende technische Umsetzung.

| ID | Status | Entscheidung | Begründung / Konsequenz |
| --- | --- | --- | --- |
| D-001 | F | PF1E als einziges Regelwerk im ersten Modul | Keine Vermischung mit PF2E/D&D |
| D-002 | F | Zielgerät Tablet; Smartphone voll nutzbar | Kompakte aufklappbare mobile Liste |
| D-003 | F | PCs und Monster aus Bibliothek in Encounter ziehen, Monsteranzahl Standard 1 | Schnelle Vorbereitung, unabhängige Gegnerinstanzen |
| D-004 | F | Änderungen an Bibliothekseinträgen wirken auf noch nicht gestartete Encounter, **auch Monster** | Weniger Doppelpflege; lokale Overrides brauchen Konfliktregeln |
| D-005 | F | Vorbereitete, pausierte und abgeschlossene Kämpfe sowie Vorlagen unterstützen | Wiederaufnahme und Wiederverwendung |
| D-006 | F | PC-Initiative manuell, NPC-Initiative automatisch; PF1E-Tiebreak, Ausnahme-Override | GM-Kontrolle und Regeln |
| D-007 | F | Rundenzähler, aktiver Zug, Ready, Delay, Überraschungsrunde | PF1E-Kampfmanagement |
| D-008 | F | HP bei Kampfstart unabhängig, Default Max-HP | Keine automatische HP-Übertragung zwischen Kämpfen |
| D-009 | F | PC- und NPC-HP inkl. negativ/temp/heal; Tod-/Stabilitätsentscheid beim GM | Unterstützende Automatisierung, keine Entmündigung |
| D-010 | F | NPCs würfeln Angriff und Schaden automatisch, Ergebnis vor Anwenden korrigierbar | GM kann diskret eingreifen |
| D-011 | F | Vollständige Angriffsfälle für modellierte Angriffe (u. a. mehrere Angriffe und Krits), manuelle Sonderfälle | PF1E-Nutzen ohne unerfüllbare Vollautomatik |
| D-012 | F | Zustände automatisch berechnen, relevante Einzelwerte speichern, Laufzeiten automatisch beenden | Regelkorrekte Auswirkungen soweit modelliert |
| D-013 | F | Undo und einfaches chronologisches Kampfprotokoll | Korrigierbarkeit |
| D-014 | F | Manuelle Eingabe und JSON-Import zunächst ausreichend | Parser beliebiger Statblocks später |
| D-015 | F | Öffentlich erreichbare Anwendung per HTTPS mit Login | Gerätewechsel über Internet |
| D-016 | F | Zunächst genau ein Benutzerkonto, Mehrbenutzer-/SaaS-Fähigkeit für später vorsehen | Schlanke v1 ohne spätere Sackgasse |
| D-017 | F | Deutsch und Englisch | Zwei Sprachen schon beim UI-Design |
| D-018 | F | Keine Pflicht zur Offline-Verfügbarkeit | Zentraler Server genügt |
| D-019 | F | Docker Compose auf eigenem Linuxserver | Einfacher Betrieb |
| D-020 | F | Angular 21 Frontend als bestehende Basis | Modernes Angular/Signals |
| D-021 | F/V | ASP.NET Core 10 Web API + EF Core + PostgreSQL als bevorzugte Architektur | Gemeinsam besprochener und empfohlener Stack; Repository-Audit vor Umsetzung |
| D-022 | V | ASP.NET Core Identity / Cookies auf gleicher Origin; Caddy als HTTPS-Reverse-Proxy | Geringe Browser-/Deployment-Komplexität; Security-Konzept vor Code prüfen |
| D-023 | V | Modulares Monolith-Backend statt Microservices | Verhältnismäßig für 1 Nutzer und später erweiterbar |
| D-024 | V | Append-only-Aktionslog + atomare Combat-Snapshots + gezieltes Undo | Zuverlässige Recovery und Nachvollziehbarkeit; genaue Technik validieren |

### D-025 · Monorepo-Struktur · 23.11.2026 · F

Der Angular-Workspace und das künftige Backend benötigen getrennte Arbeitsverzeichnisse. Die Alternative, Angular im Repository-Root zu belassen und nur das Backend zu gruppieren, würde frontendbezogene und projektweite Konfiguration am selben Ort bündeln. Der freigegebene Aufbau legt Angular unter `frontend/` und ASP.NET Core unter `backend/` ab. npm-Befehle laufen dadurch unter `frontend/`, Compose-Befehle im Root.

### D-026 · Login und Wiederherstellung für Release 1 · 24.11.2026 · F

ASP.NET Core Identity speichert das einzelne Konto in PostgreSQL. Eine sichere Cookie-Sitzung läuft spätestens zwölf Stunden nach der Anmeldung ab. Der Serveradministrator legt das Konto an und setzt das Passwort bei Bedarf über die CLI zurück. Ein Passwort-Reset macht bestehende Sitzungen ungültig. Ein längerer Login wurde zugunsten einer kürzeren Gültigkeit auf gemeinsam genutzten Geräten verworfen. Passwort-Reset per E-Mail würde für das Einzelkonto zusätzlichen Mailbetrieb erfordern. Die UI und die API nutzen im Betrieb dieselbe Origin.

### D-027 · UI-Sprache zur Laufzeit · 25.11.2026 · V

Der Angular-Client bündelt typisierte DE/EN-Textkataloge in einem Build. Ein Angular-Signal hält die UI-Sprache, und Komponenten lesen den aktiven Katalog reaktiv. Damit bleibt der Zustand des laufenden Trackers beim Umschalten im Speicher. Angulars Buildzeit-Lokalisierung erzeugt separate Ausgaben je Sprache. Auch ein Laufzeitkatalog mit Angulars `loadTranslations()` würde für bereits angezeigte Texte einen Neuladevorgang erfordern. [Angular-Dokumentation zu den Locale-Builds](https://angular.dev/guide/i18n/deploy) · [Angular-Dokumentation zu `loadTranslations()`](https://angular.dev/api/localize/loadTranslations)

Die Auswahl gilt pro Browser und Origin. Beim Start hat eine gespeicherte Auswahl Vorrang, danach folgt die erste unterstützte Browsersprache und schließlich Englisch. Das Dokument übernimmt die gewählte Sprache. Formatierungen für künftige Datums- und Dezimalfelder verwenden die aktive Sprache explizit. Stabile IDs, Werte und eingegebene Inhalte bleiben beim Sprachwechsel erhalten. Wie kuratierte Bibliotheksinhalte und weitere PF1E-Regelbegriffe lokalisiert werden, bleibt in OQ-14 für M2 offen.

### D-028 · Archivieren und Löschen von Bibliotheksvorlagen · 25.11.2026 · F

Eine archivierte PC- oder Monstervorlage verschwindet aus der Neuauswahl. Bereits vorbereitete Encounter behalten ihren Verweis und können die Vorlage weiter nutzen. Archivieren ist umkehrbar. Endgültiges Löschen ist möglich, solange kein vorbereiteter Encounter die Vorlage referenziert. Laufende und abgeschlossene Kämpfe verwenden ihre Snapshots.

Der Auftraggeber hat sich für den Erhalt bestehender Verweise entschieden. Ausschließliches Archivieren würde auch bei ungenutzten Vorlagen keinen endgültigen Löschweg bieten. Eine Pflicht zum vorherigen Auflösen aller Referenzen würde auch die reversible Archivierung blockieren. In M3 müssen die Vorlagenreferenzen vorbereiteter Encounter mit einem restriktiven Datenbank-Fremdschlüssel angelegt werden, damit auch paralleles Löschen und Referenzieren konsistent bleibt.

### D-029 · Pflichtfelder für Bibliotheksvorlagen · 25.11.2026 · F

Für die Bibliothek in M2 genügen die Gesamtwerte des Grundmodells. Weitere Basiseinzelwerte und Bonustypen werden in M6 aus dem ersten Zustandskatalog abgeleitet. Wenn für eine Zustandswirkung nötige Angaben fehlen, greift der vereinbarte manuelle Modus. [Grundmodell](DOMAIN_MODEL.md#bibliotheksvorlagen-in-m2) · [OQ-04 und OQ-05](OPEN_QUESTIONS.md)

## Regeln für neue Entscheidungen

Neue Architektur-/Fachentscheidungen erhalten fortlaufende IDs mit Datum, Problem, Alternativen, Begründung und Auswirkungen. `V` wird erst nach ausdrücklicher Prüfung in `F` überführt. Kein stillschweigendes Umschreiben früherer Entscheidungen.
