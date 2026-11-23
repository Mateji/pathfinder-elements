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

## Regeln für neue Entscheidungen

Neue Architektur-/Fachentscheidungen erhalten `D-025` aufwärts mit Datum, Problem, Alternativen, Begründung und Auswirkungen. `V` wird erst nach ausdrücklicher Prüfung in `F` überführt. Kein stillschweigendes Umschreiben früherer Entscheidungen.
