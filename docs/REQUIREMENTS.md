# REQUIREMENTS — Pathfinder Elements Combat Manager

**Version:** 0.2 · **Stand:** 19.11.2026 · **Priorität:** alle `MUSS` für Release 1, soweit in der Roadmap phasenweise geplant.

**Konventionen:** `FESTGELEGT` = vom Auftraggeber beschlossen; `OFFEN` = in `OPEN_QUESTIONS.md` zu klären. Vor einem Feature müssen die zugehörigen offenen Fragen geklärt oder eine bewusst akzeptierte manuelle Fallback-Lösung vereinbart sein.

## FR-01: Bibliothek von PCs und Monstern

- **FR-01.1 MUSS:** PCs und Monster/NPC-Vorlagen erstellen, lesen, bearbeiten und löschen/archivieren können.
- **FR-01.2 MUSS:** PCs benötigen kampfrelevante Werte, insbesondere RK/Touch-RK/auf-dem-falschen-Fuß-RK, Rettungswürfe, Max-HP und für Initiative nötige Werte. Keine vollständige PC-Charakterverwaltung mit Talenten erforderlich.
- **FR-01.3 MUSS:** Monster-Vorlagen enthalten die für ihre automatisierten Angriffe, Verteidigung, HP, Zustände und Initiative nötigen Daten. Verschiedene Angriffsarten (Nah-/Fernkampf, natürliche Angriffe) sowie Sequenzen sind modellierbar.
- **FR-01.4 MUSS:** Manuelle Dateneingabe und JSON-Import für Bibliotheksobjekte; Formatversion und Validierung dokumentieren.
- **FR-01.5 MUSS:** Änderungen an PC **und** Monster-Vorlagen gelten auch für bereits **vorbereitete, noch nicht gestartete** Encounter. Bewusste lokale Encounter-Overrides dürfen nicht ungeprüft überschrieben werden (Konfliktregel noch offen).
- **FR-01.6 MUSS:** Bereits laufende/abgeschlossene Kampfaufzeichnungen werden durch Bibliotheksänderungen nicht still verändert.

**Akzeptanztests:** Ein Monster ändern; ein vorbereiteter Encounter zeigt aktualisierte Daten. Bereits gespielte Runden behalten alte Werte. Ungültiger JSON-Import erzeugt verständliche Fehlermeldung ohne Teildaten.

**Akzeptanz für das Grundmodell in M2.** Je eine synthetische PC- und Monstervorlage wird mit eigener ID, Besitzer und allen Kampfwerten gespeichert und wieder geladen. Unterschiedliche RK-Werte, negative Modifikatoren und Modifikatoren von null bleiben unverändert. Das Modell lehnt leere Namen, leere Besitzer und maximale HP unter 1 ab. PostgreSQL verlangt einen Verweis auf ein vorhandenes Benutzerkonto. [Felder und Abgrenzung der Vorlagen](DOMAIN_MODEL.md#bibliotheksvorlagen-in-m2)

**Akzeptanz für die Bibliotheks-API in M2.** Der Besitzer kann beide Vorlagentypen anlegen, lesen, ändern und archivieren. Eine archivierte Vorlage fehlt in der Standardliste, bleibt über ihre ID abrufbar und erscheint nach Wiederherstellung wieder in der Standardliste. Eine Vorlage mit einem Verweis aus einem vorbereiteten Encounter kann erst nach Auflösung des Verweises endgültig gelöscht werden. M2 prüft die Löschsperre mit einem PostgreSQL-Testverweis. M3 prüft die Regel zusätzlich mit dem Encounter-Modell. Fremde Konten erhalten weder Listeneinträge noch Lese- oder Schreibzugriff über eine bekannte ID. Schreibzugriffe verlangen CSRF-Schutz, vollständige Vorlagendaten beim Anlegen oder Bearbeiten und die aktuelle Version bei Änderungen an einem bestehenden Eintrag. [HTTP-Vertrag](../backend/README.md#http-vertrag-der-bibliothek)

## FR-02: Encounter-Vorbereitung und Wiederverwendung

- **FR-02.1 MUSS:** Neuen Encounter anlegen, benennen, bearbeiten, speichern und später starten.
- **FR-02.2 MUSS:** PCs und Monster aus Bibliotheken auswählen. Anzahl je ausgewählter Monster-Vorlage einstellbar, **Standard 1**.
- **FR-02.3 MUSS:** Jede Kopie hat eine eindeutige Encounter-Teilnehmer-ID und individuelle spätere Kampfwerte.
- **FR-02.4 MUSS:** Vorbereitete Encounter separat von laufenden Kämpfen speichern; abgeschlossene Encounter als Vorlage kopieren/wiederverwenden.
- **FR-02.5 MUSS:** Beim Kampfstart Start-HP je Teilnehmer unabhängig setzen können, **Default = maximale HP**. Temp-HP separat erfassen.
- **FR-02.6 MUSS:** Verschiedene Instanzen gleicher Monster mit individuellen, konfigurierbaren Farben markieren können. Farben müssen im Kampf sichtbar sein; ausreichend unterscheidbare Zusatzkennzeichnung vorsehen (nicht ausschließlich Farbe).
- **FR-02.7 OFFEN:** Die endgültige Spezifikation für Positionen/Locations (Freitext/Zonen) ist noch nicht entschieden; Platz in Domäne und UI nicht verbauen.

**Akzeptanztests:** Vier Kopien eines Goblins und ein Ork erzeugen fünf unabhängig identifizierbare Instanzen; eine HP-Änderung an Goblin 2 betrifft nicht Goblin 1. Vorlage lässt sich ohne alte Kampf-HP neu starten.

## FR-03: Initiative, Runden und Zugfolge

- **FR-03.1 MUSS:** Beim Start NPC-Initiative automatisiert aus d20 + jeweiligem Gesamt-Initiativemodifikator würfeln; PC-Initiativergebnisse manuell erfassen.
- **FR-03.2 MUSS:** Bei gleichen Initiativergebnissen entscheidet zunächst der **gesamte Initiative-Modifikator**. Bei erneutem Gleichstand ein entsprechender Entscheidungswurf. Der GM kann im Ausnahmefall die Reihenfolge manuell ändern.
- **FR-03.3 MUSS:** Einen aktiven Teilnehmer hervorheben; Rundenzähler führen; Nächster/Vorheriger, gezielt Teilnehmer anwählen.
- **FR-03.4 MUSS:** `Delay`/Abwarten und `Ready`/Bereithalten nach PF1E unterstützen. Diese Aktionen verändern die langfristige Reihenfolge; Auslösen bereiter Aktionen unterbricht ggf. andere Züge.
- **FR-03.5 MUSS:** Überraschungsrunde mit teilnahmeberechtigten Figuren und anschließendem Übergang in normale Runde 1 unterstützen.
- **FR-03.6 MUSS:** Reihenfolge nicht bloß als `activeIndex` in einer jederzeit neu sortierten Liste abbilden. Stabile Teilnehmer-IDs und explizites Modell der aktuellen Zugfolge sind erforderlich.
- **FR-03.7 MUSS:** Kampfunfähige Teilnehmer bleiben sichtbar und markiert; Option zum Ausblenden, ohne sie aus dem Kampf zu löschen.
- **FR-03.8 MUSS:** Manuelle Overrides werden protokolliert; normale Züge funktionieren ohne manuelle Reihenfolgenpflege.

**Akzeptanztests:** Gleiche Würfe mit unterschiedlichen Initiative-Modifikatoren sortieren korrekt. Bei gleich hohen Modifikatoren gibt es einen eindeutigen Tiebreak. Ready/Delay führen nicht zu doppelten regulären Zügen. Runde steigt beim Wechsel gemäß tatsächlicher Sequenz korrekt. Überraschungsrunde erlaubt nur berechtigte Teilnehmer.

**Regelreferenz:** https://pathfinder.d20srd.org/coreRulebook/combat.html (Abschnitte Initiative, Surprise, Special Initiative Actions).

## FR-04: HP, Heilung und Kampfunfähigkeit

- **FR-04.1 MUSS:** Aktuelle und maximale HP, temporäre HP und negative HP von PCs und NPCs führen.
- **FR-04.2 MUSS:** Per Klick auf HP Schaden, Heilung und temporäre HP manuell hinzufügen/abziehen können.
- **FR-04.3 MUSS:** Schadensanwendung berücksichtigt vorhandene temporäre HP nach PF1E-Regeln. HP-Berechnung ist deterministisch testbar.
- **FR-04.4 MUSS:** Bei kritischen HP-Schwellen relevante Zustände/Hinweise anzeigen (Disabled/Dying/Dead, soweit die notwendigen Daten vorhanden sind).
- **FR-04.5 MUSS:** Keine eigenmächtige Entscheidung über Stabilisierung, sterbende Figuren oder endgültigen Tod; GM bestimmt und kann Werte korrigieren.
- **FR-04.6 MUSS:** HP bei Kampfstart unabhängig von früheren Encountern konfigurieren, Standard Max-HP.

**Akzeptanztests:** Temp-HP werden bei Schaden zuerst verbraucht; Heilung verändert aktuelle HP nach festgelegtem Regelsatz; HP dürfen negativ werden; die UI zeigt problematische Schwellen, ohne automatisch einen PC zu löschen.

## FR-05: Angriff / Schaden / Würfelengine

- **FR-05.1 MUSS (PC):** PC-Angriffsaktion starten (Icon), Ziel auswählen, Schaden und/oder Zustand über Dialog erfassen. Der Spieler würfelt selbst; GM erfasst das mitgeteilte Ergebnis.
- **FR-05.2 MUSS (NPC):** Angriff auswählen (z. B. Nah-/Fernkampf, natürliche Angriffe), Ziel bestimmen und Angriffswurf anhand gespeicherter Werte und der relevanten Ziel-RK automatisch berechnen.
- **FR-05.3 MUSS:** Natürliche 1/20, kritische Bedrohung/Bestätigung, kritischer Multiplikator und mehrere Angriffe/volle Angriffssequenzen für modellierte Angriffe nach PF1E korrekt behandeln.
- **FR-05.4 MUSS:** Bei Treffer Schaden entsprechend dem Angriff automatisch würfeln. Vor **jeder** endgültigen Anwendung Ergebnisse und Auswirkungen im **GM-Bestätigungsdialog** anzeigen. GM kann Würfe/Schaden/Modifier überschreiben oder abbrechen.
- **FR-05.5 MUSS:** Ein bestätigter NPC-Angriff beeinflusst HP des ausgewählten Ziels. Ein abgebrochener Angriff ändert keinen dauerhaften Kampfzustand.
- **FR-05.6 MUSS:** Manuelle HP-Änderungen bleiben möglich, auch wenn der Effekt nicht durch einen modellierten Angriff abbildbar ist (Zauber, Sonderfälle etc.).
- **FR-05.7 MUSS:** Alle automatischen Ergebnisse müssen im GM-Interface sichtbar sein, bevor sie bestätigt werden. Vertrauliche GM-Overrides dürfen nicht in einen ungeschützten Spieler-Ausgabekanal gelangen.
- **FR-05.8 OFFEN:** Vollständiger Funktionskatalog für Sonderangriffe, Zauber, Flächeneffekte, RK-Bonusarten, Schadensreduktion und Resistenz ist noch abzugrenzen. Bis dahin manuell übersteuerbare Fallbacks statt falscher Automatik.

**Akzeptanztests:** Verfehlt gegen RK -> kein Schaden. Natürliche 1 trifft nicht, natürliche 20 trifft; kritischer Treffer benötigt passende Bestätigung. Ein abgebrochener Angriff verändert HP nicht. Bei mehreren identischen NPCs wird der richtige Angreifer und das richtige Ziel verbucht.

## FR-06: Zustände und zeitliche Effekte

- **FR-06.1 MUSS:** Gängige PF1E-Zustände anwenden, entfernen und anzeigen. Startkatalog muss vor Implementierung konkret festgelegt werden.
- **FR-06.2 MUSS:** Für modellierte Zustände korrekte, automatische Auswirkungen auf relevante Werte (z. B. RK, Angriffe, Rettungswürfe) berechnen, **sofern dafür die notwendigen Basiswerte/Bonusarten verfügbar sind**.
- **FR-06.3 MUSS:** Effektdauer als Runden/Zugbezug verwalten und bei richtigem Zeitpunkt automatisch auslaufen lassen.
- **FR-06.4 MUSS:** GM kann Zustände und relevante Einzelwerte manuell übersteuern. Manuelle Abweichungen werden sichtbar dokumentiert.
- **FR-06.5 MUSS:** Nicht jede denkbare PF1E-Fähigkeit vollständig automatisieren; nicht unterstützte Effekte über explizite manuelle Modifikatoren behandeln.

**Akzeptanztests:** Zustand mit Dauer 2 Runden läuft zum definierten Zeitpunkt aus; daraus abgeleitete Werte aktualisieren sich. Überschneidende Boni werden nicht blind addiert; unzureichende Daten führen zu sichtbarem manuellem Modus.

## FR-07: Kampf speichern, Protokoll, Undo

- **FR-07.1 MUSS:** Kampf vor dem Start speichern, pausieren, fortsetzen und abschließen; abgeschlossene Kämpfe historisch anzeigen und als neue Vorlage kopieren.
- **FR-07.2 MUSS:** Jede bestätigte Kampfaktion unmittelbar und **atomar** im Backend speichern: HP, Initiative, Zustände, Runde, Sequenz, bestätigte Angriffe.
- **FR-07.3 MUSS:** Nach Refresh, versehentlichem Wegnavigieren oder mobilem Tab-Neustart letzten bestätigten Zustand zuverlässig wieder laden.
- **FR-07.4 MUSS:** Chronologisches Kampfprotokoll mit Runde, handelnder Figur, Aktion und relevanten Ergebnissen.
- **FR-07.5 MUSS:** Undo für bestätigte, zustandsändernde Aktionen; Folgewirkungen (HP, Zustände, Initiative) konsistent zurückrollen. Umfang/Grenzen von Undo sind in `OPEN_QUESTIONS.md` zu klären.
- **FR-07.6 MUSS:** Duplizierte Netzwerk-Requests dürfen nicht doppelt Schaden oder andere Aktionen ausführen; Mehrfachklicks und konkurrierende Tabs müssen sicher behandelt werden.

**Akzeptanztests:** Schaden bestätigen -> Backend schreibt Aktion/HP/Protokoll zusammen; Browser neu laden -> gleicher Stand; denselben Request wiederholen -> Schaden nur einmal; Undo -> exakt wiederhergestellter Zustand samt nachvollziehbarem Log-Eintrag.

## FR-08: UI / Bedienung / Mehrsprachigkeit

- **FR-08.1 MUSS:** Tablet als primäres Zielgerät; Smartphone voll bedienbar, kompaktes Listenlayout mit aufklappbaren Details; Laptop unterstützt.
- **FR-08.2 MUSS:** Initiative, aktive Figur, Identifikation gleicher Gegner, RK und relevante Defensivwerte schnell erkennbar. NPC-Angriffe im aktiven Kontext verfügbar.
- **FR-08.3 MUSS:** Zwei Sprachen **Deutsch und Englisch**. Sichtbare UI-Texte, Formularvalidierungen und Statusanzeigen müssen übersetzbar sein; Sprachwahl ohne Datenverlust.
- **FR-08.4 MUSS:** Farbe allein darf kein Identifizierungsmerkmal sein (Kurzlabel/Nummer ergänzen); Touch-Ziele ausreichend groß.
- **FR-08.5 MUSS:** Fehler beim Speichern/Laden deutlich anzeigen; bei nicht bestätigter Serverpersistenz keinen falschen Erfolg vorspiegeln.
- **FR-08.6 FESTGELEGT:** Keine garantierte Offline-Funktionalität für Release 1.

**Akzeptanztests:** Kernablauf auf einem Smartphone ohne horizontales Pflichtscrollen bedienbar. Der Benutzer kann UI-Texte in DE/EN sehen; Einträge behalten beim Sprachwechsel ihre Daten. Temporärer Serverfehler wird klar kommuniziert.

## FR-09: Login und spätere Mandantenfähigkeit

- **FR-09.1 MUSS:** Das System ist öffentlich über HTTPS erreichbar, private Inhalte ausschließlich nach erfolgreicher Anmeldung.
- **FR-09.2 MUSS:** Release 1 enthält genau ein administrativ eingerichtetes Konto; keine offene Selbstregistrierung.
- **FR-09.3 MUSS:** Daten von PCs, Monstern, Encountern und Kämpfen sind einem Besitzer/Konto zugeordnet; API setzt Autorisierung serverseitig durch.
- **FR-09.4 MUSS:** Struktur darf spätere Mehrbenutzerfähigkeit/SaaS nicht durch globale ungeschützte Datensätze verhindern.
- **FR-09.5 MUSS:** Standard-Schutzmaßnahmen: HTTPS, sichere Cookie-/Session-Einstellungen, CSRF-Schutz für zustandsändernde Anfragen, sichere Passworthashes, Eingabevalidierung, Rate-Limiting/Login-Schutz und keine Secrets im Repo.

**Akzeptanztests:** Nicht angemeldete Anfrage erhält keine privaten Daten. Benutzer kann fremde Ressourcen selbst mit erratener ID nicht lesen/bearbeiten (für späteren Mehrbenutzertest). Deployment veröffentlicht keinen Datenbankport.

## NFR: Nichtfunktionale Anforderungen

- **NFR-01 Datenintegrität:** Aktionen werden transaktional, idempotent/duplikatsicher und nachvollziehbar persistiert.
- **NFR-02 Testbarkeit:** Die regeltragende Kampf-Domäne ist unabhängig von Angular/HTTP testbar; Unit-/Integrationstests für Turn Order, PF1E-Angriff, HP, Zustände, Undo.
- **NFR-03 Sicherheit:** OWASP-nahe Web-Basics, regelmäßige Updates, Backup/Wiederherstellungstest; sensible GM-Daten nicht öffentlich.
- **NFR-04 Deployment:** Docker Compose auf Linux, persistente Datenbank-Volumes, dokumentierte Migrationen, Healthchecks und verständlicher Start-/Backup-Prozess.
- **NFR-05 Bedienbarkeit:** Wenige Klicks während des Spiels, guter Kontrast, Tastatur-/Touchbedienung; keine unbeabsichtigten Doppelaktionen.
- **NFR-06 Sprache:** DE/EN von Beginn an, keine Übersetzung von regelrelevanten stabilen IDs; UI-Lokalisierung lösungsneutral vor Implementierung konkretisieren.
- **NFR-07 Erweiterbarkeit:** Weitere GM-Tools und SaaS später möglich, aber **keine Microservice-Architektur** und keine spekulativen Module in Release 1.

## Nicht Teil von Release 1

Automatische Encounter-Generierung, digitale Battlemaps und Markertracking auf Maps, allgemeiner Pathfinder-Regelinterpreter, öffentliche Registrierung, Zahlungsabwicklung, garantierte Offline-Nutzung. Einfache Positionsnotizen bleiben als mögliche Erweiterung offen.
