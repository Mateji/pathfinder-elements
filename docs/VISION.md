# VISION — Pathfinder Elements

**Version:** 0.2 · **Stand:** 19.11.2026

## Problem

Der GM notiert Initiative, Namen, HP und Positionen bisher auf Papier. Gerade das Sortieren, Unterscheiden gleicher Gegner, Nachhalten von Schäden, Zuständen und Aktionen ist umständlich und fehleranfällig. Während des Spiels soll die Aufmerksamkeit bei der Gruppe liegen, nicht auf Verwaltungszetteln.

## Produktvision

**Pathfinder Elements** ist eine wachsende Sammlung digitaler Werkzeuge für die Leitung von Pathfinder-1E-Runden. Das erste große Modul ist ein **Combat Manager**, der Encounter vorbereiten, Kämpfe leiten und zuverlässig fortsetzen lässt.

Die App funktioniert im Browser auf Smartphone, Tablet und Laptop; das Zielgerät für die Spielsitzung ist ein Tablet. Bis dahin muss die Bedienung auf einem Smartphone vollständig möglich sein. Eine dauerhafte Internetverbindung ist für Release 1 zulässig (Offline/PWA ist keine Pflicht).

## Fachliche Grundsätze

1. **GM in control:** Automatisierung ist ein Vorschlag oder ein berechenbares Ergebnis, nie ein Ersatz für die GM-Entscheidung.
2. **PF1E zuerst:** Initiative, Angriff und Zustände folgen Pathfinder First Edition; kein Mix mit D&D 5E oder PF2E.
3. **Schnelle Bedienung:** Wenige Eingaben während einer Runde; aktive Figur und relevante Defensivwerte auf einen Blick.
4. **Zuverlässigkeit:** Bestätigte Aktionen gehen auch bei Tab-Schließung oder mobilem Browser-Neustart nicht verloren.
5. **Wiederverwendbarkeit:** Zentrale PC-/Monsterbibliotheken und vorbereitete Encounter ersparen wiederholte Eingaben.
6. **Transparenz:** Würfelwerte, angewandte Modifikatoren, HP-Änderungen und Undo sind nachvollziehbar.
7. **Internationalisierung:** Deutsch und Englisch sind Produktanforderung, keine nachträgliche Übersetzung.

## Zielgruppe und Reichweite

- **Release 1:** ein GM mit einem eigenen, passwortgeschützten Konto auf einem öffentlich über HTTPS erreichbaren Linuxserver.
- **Später:** Mehrbenutzer-/SaaS-Version mit getrennten Daten pro GM, eventuell zusätzlichen Werkzeugen für Encounter-Generierung und Battlemaps.

## Release-1-Umfang

PC-/Monsterverwaltung; Encounter-Bibliothek und Vorlagen; Starten/Speichern/Fortsetzen; Initiative samt Ready/Delay/Überraschungsrunde; Angriffswürfe inkl. vollerer Sequenzen und GM-Freigabe; HP/temporäre HP/Heilung; ausgewählte Zustände und zeitgesteuerte Effekte; Undo/Protokoll; verlässliches Speichern; sichere Anmeldung; responsive DE/EN-Oberfläche.

## Ausdrücklich nicht Release 1

Automatisch generierte Encounter, interaktive Dungeon-/Battlemaps, öffentliche Registrierung, Abos/Bezahlung, vollautomatische Regeln für sämtliche Zauber/Talente/Bestiarium-Sonderfähigkeiten, garantierte Offline-Nutzung, Spielerportale. Diese Punkte dürfen die Erweiterbarkeit beeinflussen, aber keinen Vorab-Bau großer Subsysteme auslösen.
