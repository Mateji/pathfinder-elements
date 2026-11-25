# OPEN QUESTIONS — Vor fachlicher Umsetzung zu klären

**Stand:** 19.11.2026. Diese Liste ist **kein Auftrag, alles vor dem ersten Commit zu entscheiden**. Fragen werden vor dem jeweils betroffenen Meilenstein geklärt; bis dahin keine vermeintlich endgültige Implementierung erfinden.

| ID | Priorität | Frage / Entscheidung | Betroffener Meilenstein |
| --- | --- | --- | --- |
| OQ-01 | hoch | Wie wirken lokale Feld-Overrides in vorbereiteten Encountern bei späteren Bibliotheksänderungen? Feldweise Vererbung? Warnung bei Konflikt? | M2/M3 |
| OQ-02 | entschieden | Archivierte Vorlagen bleiben in vorbereiteten Encountern nutzbar. Endgültiges Löschen setzt voraus, dass keine Verweise aus vorbereiteten Encountern bestehen. [D-028](DECISIONS.md#d-028--archivieren-und-löschen-von-bibliotheksvorlagen--25112026--f) | M2/M3 |
| OQ-03 | hoch | Welche Angriffstypen/Volle Angriffe müssen für v1 mit welchen PF1E-Ausnahmen automatisiert werden (z. B. Angriffe mehrerer Ziele, Iterative, Secondary Natural Attacks, Touch, AOOs, Concealment, Resistenz)? | M5 |
| OQ-04 | hoch | Welche Zustände bilden den ersten Katalog? Was sind konkrete Start-/Ablaufzeitpunkte und erlaubte Sondermodifikatoren? | M6 |
| OQ-05 | hoch | Welche Basiseinzelwerte und Bonustypen braucht die Zustandsautomatik über die bereits erfassten [Gesamtwerte](DOMAIN_MODEL.md#bibliotheksvorlagen-in-m2) hinaus? | M2/M6 |
| OQ-06 | hoch | Welche Undo-Semantik: nur letzter Schritt oder beliebige frühere Aktion? Wie mit abhängigen Folgeschritten umgehen? | M4/M5 |
| OQ-07 | hoch | Ready/Delay: Welche Bedienung/Trigger müssen automatisiert sein? Was bleibt manueller GM-Trigger? | M4 |
| OQ-08 | mittel | Was ist eine „Position“: Freitext, Raum/Zonen oder nur farbliche Battlemap-Markerzuordnung? | M3/M7 |
| OQ-09 | mittel | Wie wird Initiativgleichstand bei manueller Reihenfolgenänderung dargestellt, ohne Rollwerte zu verfälschen? | M4 |
| OQ-10 | mittel | Soll NPC-Initiative bei erneutem Start einer Vorlage neu gewürfelt werden? Welche Reroll-Bedienung? | M4 |
| OQ-11 | mittel | Wie verhält sich Kampfende/Abbruch/Neustart bei demselben vorbereiteten Encounter? | M3/M4 |
| OQ-12 | mittel | Reihung und Farbvergabe: automatische Farbpalette vs. individuelle Farben; persistente Kurzlabels? | M3/M7 |
| OQ-13 | entschieden | ASP.NET Core Identity, zwölf Stunden absolute Sitzungsdauer, Kontoanlage und Passwort-Reset per Server-CLI. [D-026](DECISIONS.md#d-026--login-und-wiederherstellung-für-release-1--24112026--f) | M1 |
| OQ-14 | mittel | M1 nutzt typisierte Laufzeitkataloge für UI-Texte ([D-027](DECISIONS.md#d-027--ui-sprache-zur-laufzeit--25112026--v)). Wie werden kuratierte Bibliotheksinhalte und weitere PF1E-Regelbegriffe lokalisiert? | M2 |
| OQ-15 | mittel | API-Betrieb: Caddy bereits auf Host? Welche Domain/Netzwerke/Containergrenzen/Backup-Retention? | M1 |
| OQ-16 | hoch | Konfliktverhalten bei zwei Browser-Tabs oder wiederholten Mobilfunk-Requests: Fehlermeldung, Neu laden, Konfliktlösung? | M1/M4 |
| OQ-17 | mittel | Import-JSON-Schema, Versionierung, Validierung, Dublettenhandling und Export/Backup via UI? | M2 |
| OQ-18 | mittel | PC-Schadenseingabe: nur Schaden/Zustand oder auch Heilung/mehrere Ziele im selben Dialog? | M5 |
| OQ-19 | mittel | Welche GM-Overrides erscheinen im Protokoll und wie schützt man vertrauliche Abweichungen vor späteren Spieleransichten? | M5 |
| OQ-20 | mittel | Wie detailliert sollen Todes-/Stabilisierungszustände angezeigt werden und welche manuelle Bestätigung ist nötig? | M5/M6 |

## Vorgehensweise

Wenn eine Frage ein Feature blockiert: 2–3 Optionen mit Konsequenzen formulieren, Entscheidung vom Auftraggeber einholen, in `DECISIONS.md` dokumentieren und erst dann umsetzen. Andere unabhängige Aufgaben können parallel weitergehen.
