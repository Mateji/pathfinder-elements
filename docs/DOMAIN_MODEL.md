# DOMAIN MODEL — Fachmodell (konzeptioneller Entwurf)

**Version:** 0.2 · **Status:** Architekturvorschlag mit festgelegten fachlichen Invarianten; keine fertige Datenbankschema-Definition.

## Begriffstrennung

| Objekt | Verantwortlichkeit | Quelle/Status |
| --- | --- | --- |
| `User` | Eigentümer der privaten Daten; zunächst ein Konto, später mehrere | FESTGELEGT |
| `CharacterTemplate` (PC) | Wiederverwendbare PC-Kampfdaten, ohne komplettes Character Sheet | FESTGELEGT |
| `MonsterTemplate` | Wiederverwendbarer NPC/Monster-Statblock und Angriffe | FESTGELEGT |
| `EncounterTemplate` | Planung eines Kampfes; referenziert Bibliothekseinträge | FESTGELEGT |
| `EncounterEntry` | Ein geplanter Teilnehmer, ggf. einer von mehreren identischen Monstern | FESTGELEGT |
| `Combat` | Gestarteter, pausierter oder abgeschlossener Kampf mit stabilem Snapshot | FESTGELEGT |
| `Combatant` | Individuelle Instanz innerhalb eines Combat mit eigenen HP/Initiative/Zuständen | FESTGELEGT |
| `AttackDefinition` | Werte/Typen/Sequenzen eines Angriffs; ggf. Natur-/Fern-/Nahkampf | FESTGELEGT |
| `ConditionDefinition` | PF1E-Regeln für einen Zustand inkl. Bonusarten und Duration-Semantik | VORSCHLAG |
| `AppliedCondition` | Konkreter Zustand und Ablaufzeitpunkt auf einem Combatant | FESTGELEGT |
| `CombatAction` | Bestätigter Befehl mit Ergebnis, Zeitpunkt und ausführender Figur | FESTGELEGT |
| `CombatEvent` | Unveränderbarer Protokolleintrag einer bestätigten Zustandsänderung | VORSCHLAG |
| `InitiativeEntry` | Rolle innerhalb der aktuellen Initiativefolge, nicht nur statischer Index | VORSCHLAG |

### Wichtig: Vorlage vs. Instanz vs. Snapshot

- **Bibliotheksvorlage:** kanonische kampfrelevante Daten. Anpassungen sind in noch nicht gestarteten Encountern sichtbar.
- **EncounterEntry:** referenziert Bibliotheksvorlage über stabile ID, ist einzeln identifizierbar (auch bei mehrfachen gleichen Monstern) und speichert nur geplante Abweichungen; Anzahl im Editor standardmäßig 1.
- **Combatant:** bei Kampfstart aus Vorlage/EncounterEntry erzeugter Snapshot, damit laufende und abgeschlossene Kämpfe durch spätere Bibliotheksänderungen nicht nachträglich mutieren.
- **Overrides:** brauchen eine nachvollziehbare Regel, welche Felder aus der Bibliothek aktualisiert und welche nur in der Vorbereitung lokal überschrieben werden; Details `OPEN_QUESTIONS.md`.

## Bibliotheksvorlagen in M2

`CharacterTemplate` und `MonsterTemplate` sind eigenständige Bibliothekseinträge mit stabiler UUID, verpflichtendem `OwnerUserId` und einem Namen. Gleichlautende Namen sind möglich, weil die UUID den Eintrag identifiziert. Die Vorlage speichert den eingegebenen Namen. Die Lokalisierung kuratierter Inhalte bleibt in OQ-14 offen.

Das Backend speichert die Vorlagen in `character_templates` und `monster_templates`. Beide Tabellen referenzieren ein vorhandenes Konto und indizieren den Besitzer für spätere Abfragen. Der Fremdschlüssel blockiert das Löschen eines Kontos mit vorhandenen Vorlagen.

Jede Vorlage speichert den Archivstatus und eine Version für konkurrierende Schreibzugriffe. Archivierte Vorlagen bleiben über ihre UUID abrufbar und fehlen in der Standardauswahl. Vorbereitete Encounter behalten ihre Referenz. Beim Modellieren dieser Referenzen in M3 verhindert ein restriktiver Fremdschlüssel das Löschen verwendeter Vorlagen. [D-028](DECISIONS.md#d-028--archivieren-und-löschen-von-bibliotheksvorlagen--25112026--f)

| Pflichtfelder des gemeinsamen `CombatStatistics`-Werts | Bedeutung |
| --- | --- |
| `MaxHitPoints` | Maximale TP/HP der Vorlage, mindestens 1 |
| `InitiativeModifier` | Gesamter Initiative-Modifikator für den späteren NPC-Wurf und den Gleichstand bei PCs und NPCs |
| `ArmorClass`, `TouchArmorClass`, `FlatFootedArmorClass` | Normale RK, Berührungs-RK und RK auf dem falschen Fuß als eigenständige Werte |
| `FortitudeSave`, `ReflexSave`, `WillSave` | Gesamtmodifikatoren für Zähigkeit, Reflex und Wille |

Initiative und Rettungswürfe können auch negative Modifikatoren haben. Die drei RK-Werte werden einzeln aus dem Charakterbogen oder Statblock übernommen. [PF1E-Bestiary zur Struktur von Statblocks](https://legacy.aonprd.com/bestiary/introduction.html)

Ausgewürfelte Initiative, aktuelle und temporäre HP, Zustände und Zugfolge gehören zur Kampf-Instanz. Die Angular-Demo enthält bereits ausgewürfelte Initiative und keine Max-HP. Ihre Daten sind ein Beispiel für die Tracker-Anzeige. Welche Attributswerte, Bonustypen und situativen Modifikatoren die Zustandsautomatik braucht, bleibt in OQ-05 für M6 offen.

### Angriffssequenzen von Monstervorlagen

Eine `AttackSequence` benennt eine Auswahl aus einem oder mehreren geordneten `AttackDefinition`-Schritten. Ein Einzelangriff hat einen Schritt, ein voller Angriff kann mehrere Schritte mit unterschiedlichen Boni enthalten. Jeder Schritt speichert Nah- oder Fernkampf, das Merkmal natürlicher Angriff, den Gesamt-Angriffsbonus, die Ziel-RK (normal oder Berührung), die untere Grenze des Kritbereichs und den Kritmultiplikator.

Jeder Schadensanteil speichert Würfelanzahl, Seitenzahl, festen Modifikator, die Schadensart als eingegebenen Text und die Angabe, ob der Anteil bei einem Krit multipliziert wird. Damit bleiben Grundschaden und zusätzliche Anteile wie Feuerschaden getrennt. Hinweise des GM können Sonderfälle festhalten. Die Angriffsberechnung und die unterstützten Sonderfälle folgen in M5 (OQ-03). [PF1E-Regeln zu Angriff und kritischem Schaden](https://legacy.aonprd.com/coreRulebook/combat.html)

Die Sequenzen gehören zum Aggregat der Monstervorlage. PostgreSQL speichert sie geordnet in `monster_templates.attack_sequences`. Eine Änderung der Sequenzen ändert die Vorlagenversion. Beim Kampfstart gehören die Angriffsdefinitionen zum Snapshot der Kampf-Instanz.

### Bibliotheksimport

Ein gültiges Importdokument legt eigene PC- und Monstervorlagen an. Die API ermittelt den Besitzer aus der Sitzung und vergibt für jeden Eintrag eine neue UUID. Der Importbeleg in `library_import_receipts` ist pro Besitzer und Fingerabdruck des versionierten Dokuments eindeutig. Er wird zusammen mit den Vorlagen gespeichert und bleibt beim späteren Bearbeiten oder Löschen einzelner Vorlagen erhalten. [D-030](DECISIONS.md#d-030--versionierter-bibliotheksimport--25112026--f)

## Combatant-Kerndaten

- `id`, `combatId`, `sourceTemplateId?`, `sourceEncounterEntryId?`, `displayName`, `side` (PC/NPC), `color`, `shortLabel`.
- `maxHitPoints`, `currentHitPoints`, `temporaryHitPoints` getrennt; negative aktuelle HP zulässig.
- `initiativeRoll`, `initiativeModifier`, `currentInitiativePosition` bzw. explizite Initiative-Sortierung; `hasActedThisRound`, `readiedAction?`, `delayState?` (genaue Modellierung noch zu designen).
- Verteidigung: RK (normal/touch/flat-footed), Saves (Fort/Ref/Will), für Zustandsberechnung notwendige Einzelwerte/Bonusarten.
- Angriffe: eine oder mehrere Definitionen; volle Sequenzen mit individuellen Angriffsschritten, Schadenstypen, Critical Threat/Multiplier.
- Aktive Zustände mit Anfangs-/Ablaufpunkt und ggf. manuelle Overrides.
- `location`/Positionsnotiz optional reservieren, semantische Ausgestaltung **OFFEN**.

## Domain-Invarianten

1. Jede `Combatant.id` ist innerhalb/über Kämpfe eindeutig; gleiche Monster teilen **keinen** Kampfzustand.
2. Ein begonnenes/abgeschlossenes Combat ändert sich nicht durch spätere Bibliotheksänderungen.
3. Max-HP ist Basiswert, Current-HP und Temp-HP sind unabhängig; Temp-HP werden nach Regelreihenfolge verbraucht.
4. Eine geplante/gewürfelte Angriffsaktion ist zunächst **nicht bestätigt**. Solange nicht bestätigt, hat sie keinen dauerhaften HP-/Status-Effekt.
5. Nur eine bestätigte zustandsändernde Aktion darf in Combat-Log/Undo-Historie aufgenommen werden.
6. Schreibaktionen erfolgen transaktional; parallele Updates erkennen Versionskonflikte oder verwenden geeignete Sperr-/Versionsstrategien.
7. Initiative ist **dynamisch**: Ready/Delay können Zugfolge und künftige Runden verändern; Liste nicht allein aus ursprünglichem Wurf rekonstruieren.
8. Modifikatorberechnung muss PF1E-Bonustypen und Stack-Regeln respektieren; fehlende Attribute => kein behaupteter automatischer Effekt.
9. Konditionsabläufe definieren einen präzisen Runden-/Zug-Zeitpunkt und sind testbar.
10. Alle privaten Aggregate gehören einem Benutzer; Besitzprüfung erfolgt serverseitig.

## Rechnen vs. Persistieren

- **Backend-Domäne:** autoritative PF1E-Regeln und Zustandsübergänge, Würfelwürfe, HP-Änderungen, Undo.
- **Frontend:** UI-Signals, Eingabeentwürfe, Anzeige und lokale Validierung; keine zweite konkurrierende PF1E-Regelengine.
- **Backend** speichert bestätigte Änderungen; ein Request sollte eine eindeutige `actionId`/Idempotency-ID tragen.

## Offene fachliche Grenzfälle

Mehrere Ziele für volle Angriffe; Angriff auf sich selbst; AOOs und freie Aktionen außerhalb regulärer Züge; Zustandsbeginn zum Zeitpunkt der Aktion statt Rundenende; Stack-Regeln für gleiche Bonusarten; zwei gleichzeitige Tabs; Undo nach nachfolgenden abhängigen Aktionen; Feldkonflikte bei Vorlagenupdates. Diese Themen vor dem jeweiligen Implementierungspaket entscheiden und mit Tests dokumentieren.
