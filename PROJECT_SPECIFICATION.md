# Pathfinder Elements — Fachliche Projektspezifikation v0.2

**Stand: 19. November 2026** · **Ziel: Combat Manager für Pathfinder 1E**

> Diese Zusammenfassung beschreibt die vereinbarten Ziele und Entscheidungen. Die vollständigen, prüfbaren Anforderungen einschließlich Akzeptanztests stehen in `docs/REQUIREMENTS.md` des begleitenden Dokumentationspakets. Offene Fragen sind ausdrücklich nicht vom Coding-Agenten frei zu entscheiden.

## Vision

Ein browserbasierter Combat Manager, der den GM von Papierlisten für Initiative, HP, Positionen und Gegnerverwaltung entlastet. Geeignet für Smartphone, Tablet und Laptop; Tablet ist das angestrebte Hauptgerät. Langfristig Teil einer Sammlung von Pathfinder-GM-Tools.

## Festgelegter Tech-Stack

- Angular 21, standalone components, Signals und modernes Template-Control-Flow.
- ASP.NET Core 10 Web API (modularer Monolith), Entity Framework Core, PostgreSQL.
- Docker Compose auf Linux, öffentlich erreichbar über HTTPS und Login; Reverse Proxy vorzugsweise Caddy.
- Release 1 ein Konto ohne offene Registrierung, Datenmodell schon mit Besitz-/Owner-Bezug für spätere Benutzer/SaaS.
- Zweisprachige UI: Deutsch und Englisch. Offline-Betrieb ist keine Pflicht.

## Festgelegte Funktionen

1. **Bibliothek:** PCs und Monster mit kampfrelevanten Werten verwalten und aus JSON importieren. Änderungen an Vorlagen wirken auf noch nicht gestartete vorbereitete Encounter.
2. **Encounter vorbereiten:** Teilnehmer wählen, Anzahl mehrfacher Monster vorgeben (Default 1), einzelne Gegnerinstanzen farblich und zusätzlich textuell identifizieren. Vorbereitete Kämpfe speichern, kopieren und wiederverwenden.
3. **Kampfbeginn:** PCs melden Initiativewürfe, NPCs würfeln automatisch. Bei Gleichstand zählt erst der Gesamt-Initiativemodifikator, danach ein Entscheidungswurf. Start-HP unabhängig setzen, Default Max-HP.
4. **Initiative und Runden:** Aktiver Zug, Rundenzähler, Next/Previous/Jump, dynamische Zugfolge einschließlich Ready, Delay und Überraschungsrunde; manuelle Ausnahme-Overrides.
5. **HP:** PC- und NPC-HP, Temp-HP, Heilung, negative HP, manuelle Eingabe; kampfunfähige Gegner bleiben markiert in der Liste und können optional ausgeblendet werden. Hinweise auf kritische Zustände, GM entscheidet über Tod/Stabilisierung.
6. **Angriffe:** PC-Angriffe vom Spieler gewürfelt, Schaden/Zustand vom GM eingegeben. NPCs würfeln ausgewählten Angriff samt PF1E-Treffer/Krit-/Schadensergebnis automatisch. Alle Ergebnisse können vom GM im Bestätigungsdialog diskret angepasst oder abgebrochen werden. Mehrfachangriffe/volle Angriffsaktionen für modellierte Fälle.
7. **Zustände:** Relevante Einzelwerte/Bonustypen für automatische Auswirkungen speichern, gängige Zustände abbilden und zu ihrem korrekten Zeitpunkt automatisch auslaufen lassen; manuelle Sonderfälle möglich.
8. **Persistenz:** Laufenden Kampf nach Browser-/Tab-Neustart fortsetzen, jede bestätigte Änderung atomar speichern, einfaches Aktionsprotokoll, Undo, Schutz vor doppelten Schreibrequests.
9. **UI & Schutz:** Mobile-first Bedienung, Sprachwechsel DE/EN, sicheres HTTPS-Login, serverseitige Zugriffskontrolle.

## Wichtige Domänentrennung

**Bibliotheksvorlage** → **vorbereiteter Encounter-Eintrag mit Referenz** → **eigenständiger Combatant-Snapshot im laufenden Kampf**. Vorlagenänderungen wirken auf Vorbereitungen, aber niemals unkontrolliert rückwirkend auf laufende oder abgeschlossene Kämpfe. Vier Goblins sind vier eigenständige Combatants.

## Anweisung für Codex

Vor Codeänderungen Repository prüfen, `AGENTS.md` und sämtliche `docs/*.md` lesen, einen begrenzten Implementierungsplan erstellen und blockierende offene Fragen benennen. In kleinen, getesteten Meilensteinen arbeiten. Keine Entscheidungen zu unklaren PF1E-Regeln erfinden. Code-Stil: Block-Statements und vollständig ausgeschriebene Variablennamen.

## Offene Punkte (Auszug)

Konkreter Zustandskatalog, Grenzen komplexer Angriffsfälle und Schadensresistenzen, Mechanik lokaler Encounter-Overrides, Undo nach Folgeaktionen, exakte Ready-Trigger-Bedienung, Positionsnotizen, UI-i18n-Methode, Session-/Backupdetails. Diese stehen in `docs/OPEN_QUESTIONS.md` mit Zuordnung zu Meilensteinen.

## Roadmap

M0 Bestandsaufnahme → M1 Infrastruktur/Login/Persistenz → M2 Bibliothek → M3 Encounter-Builder → M4 Initiative/Wiederaufnahme → M5 Angriffe/HP/Undo → M6 Zustände → M7 Mobil-UX/Release-Abnahme.

**Nicht Release 1:** Battlemaps, automatische Encounter-Erstellung, öffentliche Registrierung, Bezahlung, universeller PF1E-Regelinterpreter.
