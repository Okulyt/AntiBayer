# AntiBayer: Reparatur und Modernisierung (Design)

Stand: 2026-11-06. Dieses Dokument soll ohne Rückfragen planbar sein. Punkte mit
„(Entscheidung Entwurf)“ hat der Autor des Entwurfs festgelegt, nicht der Nutzer; sie sind
im Review bewusst anfechtbar.

## Zweck und Hintergrund

AntiBayer ist ein Windows-Werkzeug für Astrofotografie. Es macht aus Rohaufnahmen des Farbsensors
QHY5III462C (IMX462) Bilder, die sich als **Monochrom-Aufnahmen** auswerten lassen. Bei solchen
Aufnahmen zeichnet sich in den ungedebayerten Rohbildern das Bayer-Muster ab und stört das Stacking,
weil die vier Filterpositionen (R, G1, G2, B) unterschiedlich stark durchlassen.

Einsatzfälle laut Nutzer:

- **Sonne im Weißlicht** (Weißlichtfolie): Hier ist das Muster aufgefallen.
- **NIR mit 680-nm- und 742-nm-Filtern** (in den Logs u. a. `IR740`): Die Bayer-Matrix lässt noch
  unterschiedlich durch. Der manuelle Abgleich der Color Gains in der Kamera ist möglich, aber nie
  zu 100 % perfekt. Das Tool korrigiert den Rest.
- **Nicht nötig ab ca. 800 nm** (800-nm-Kantenfilter, 850-nm-Methanbandfilter): Dort verhält sich der
  Sensor wie ein SW-Sensor.
- **Venus mit Bandpassfiltern im und am Rand des UV** (z. B. CaK): ein Versuch ohne Erfolgsversprechen.
  Die Durchlassunterschiede könnten zu groß sein, um sie zu korrigieren.

**Normale Farbaufnahmen sind nicht das Ziel.** Dort sind die Kanalunterschiede echte Motivfarbe, und
eine Korrektur würde sie zerstören. Die Monokamera QHY5III678M braucht das Tool nicht. Die Dateien
tragen den Filter im Namen und im Log (`Filter=`), das ist aber nicht immer richtig, weil es in
FireCapture falsch eingestellt sein kann. Das Tool zeigt den Filter deshalb an und weist bei
`Filter=RGB` darauf hin, dass es sich um eine Farbaufnahme handeln könnte. Es blockiert nichts
(Entscheidung Entwurf).

Das Tool gleicht das aus: Der Nutzer legt pro Filterposition eine stückweise lineare
Tonwertkurve über Stützpunkte fest, prüft sie an einem vergrößerten Ausschnitt und wendet sie
auf eine ganze Serie an. Nachkommastellen der Kurve werden bei der Serie zufällig auf- oder
abgerundet, sodass sie sich beim Stacken zum richtigen Grauwert aufsummieren.

Die erste Fassung (2023) belegt durch Sichtprüfung, dass das Muster vorhanden ist, enthält aber
Fehler, die das Ergebnis verfälschen, und kann 16 Bit sowie SER nicht verarbeiten.

## Ziele

1. Die Korrektur rechnet nachweislich richtig (siehe Fehlerliste).
2. SER (Normalfall), TIFF, PNG und FITS (selten) lassen sich lesen und schreiben, in 8 und 16 Bit.
3. Eine Serie bleibt in Format und Bittiefe erhalten. Innerhalb einer Serie kommt nur ein
   Format vor; gemischte Serien müssen nicht unterstützt werden.
4. Umstieg auf .NET 10 (WinForms bleibt). Läuft zuerst auf dem Rechner des Nutzers; eine
   Weitergabe an andere Astrofotografen soll später ohne Umbau möglich sein.
5. Schreibweise überall korrekt: „Bayer“ statt „Beyer“.

**Erfolgskriterium:** Eine echte SER-Aufnahme der 462C mit sichtbarem Muster (Weißlicht-Sonne oder
NIR 680/742 nm) geht durch das Tool, das Muster ist in den Ausgabebildern sichtbar schwächer, und
der Stacker des Nutzers (Ordner deuten auf AutoStakkert hin) kommt mit den Ausgabedateien mindestens
so gut zurecht wie mit den Originalen. Die Abnahme macht der Nutzer. UV-Aufnahmen gehören nicht zum
Erfolgskriterium.

## Nicht-Ziele

- Automatische Bestimmung der Kurven (der manuelle Ablauf bleibt).
- Erkennung oder Korrektur von Zeilen-/Spaltenrauschen: siehe
  `docs/superpowers/handoffs/2026-11-06-zeilen-spaltenrauschen.md`.
- Verstärkerglühen (für Planetenfotos laut Nutzer egal).
- Debayering, Stacking, Installer, UI-Neugestaltung.

## Darf nicht passieren

- Originaldateien werden überschrieben oder verändert.
- Bittiefe wird stillschweigend reduziert (16 → 8 Bit), Frames gehen verloren oder ihre
  SER-Zeitstempel und Kopfdaten verändern sich.
- Eine Korrektur läuft mit nicht bestätigten Vorschau-Stützpunkten über eine Serie.
- Eine mehrere GB große SER-Datei muss komplett in den Arbeitsspeicher.

## Technische Rahmenbedingungen

- .NET 10 (`net10.0-windows`), WinForms. SDK 10.0.402 ist installiert. Der Designer-Code wird
  so weit wie möglich übernommen.
- Lizenz bleibt AGPL (`LICENSE.txt`). Keine Abhängigkeiten, die dazu nicht passen.
- Bevorzugt ohne Fremdbibliotheken (Entscheidung Entwurf): SER und FITS mit eigenem Code,
  TIFF/PNG über WIC (`System.Windows.Media.Imaging`, per `UseWPF`), das 16-Bit-Graustufen liest
  und schreibt. Das Testprojekt nutzt xUnit oder Vergleichbares.
- `System.Drawing` dient nur noch der Anzeige, nie als Pixeldatenquelle.

## Datenmodell der Bilder

- Eine geladene Bildquelle liefert Breite, Höhe, Bittiefe (8 oder 16), Frame-Anzahl und einen
  Frame als `ushort`-/`byte`-Daten pro Pixel. Einzeldateien haben einen Frame.
- **Nur einkanalige Rohdaten werden verarbeitet** (Entscheidung Entwurf). Ausnahme: Dateien im
  RGB-Format mit R=G=B in jedem Pixel (die alte Fassung hat solche ausgegeben) gelten als
  einkanalig und werden im selben Format zurückgeschrieben. Echte Farbbilder und SER-ColorIDs
  100/101 (RGB/BGR) werden mit klarer Meldung abgelehnt. Debayerte Bilder enthalten kein
  korrigierbares Rohmuster.
- 16-Bit-Daten mit geringerer Sensor-Tiefe (Entscheidung Entwurf): Es gibt einen **wirksamen
  Maximalwert**. Beleg: Alle 7 Mars-SER (QHY5III462C) haben im Kopf Tiefe 16, aber im ersten Frame
  sind die unteren 4 Bit durchgehend 0, die Daten sind also 12 Bit linksbündig, der wahre Weißwert
  ist 65520. Regel: Sind im ersten Frame die unteren `n` Bit überall 0, gilt `65536 − 2^n`. Sagt der
  Kopf eine Tiefe `t` unter 16 und liegen alle Werte unter `2^t`, gilt `2^t − 1`. Sonst gilt 65535.
  Die Kurve endet immer bei Maximalwert→Maximalwert und die Ausgabe wird darauf begrenzt, damit der
  Sättigungswert je Filterposition gleich bleibt und in den Lichtern kein neues Muster entsteht.
  Die Ausgabe darf die unteren Bits belegen. Tabellen dürfen den ganzen Container abdecken; der
  Kopf-Wert bleibt erhalten.

### SER

- Kopf 178 Byte, „LUCAM-RECORDER“; `ColorID` (0 = Mono, 8 RGGB, 9 GRBG, 10 GBRG, 11 BGGR);
  Zeitstempel-Anhang mit 8 Byte pro Frame, falls vorhanden.
- Ausgabe: Kopf und Zeitstempel-Anhang werden byte-genau übernommen. Nur die Pixeldaten ändern
  sich. Keine Änderung der Byte-Reihenfolge der Ausgabe gegenüber der Eingabe.
- Das Feld `LittleEndian` ist nicht verlässlich. Beleg: Die 16-Bit-Dateien von FireCapture 2.7.14
  tragen den Wert 0, die Daten sind aber Little Endian (nur so sind die unteren 4 Bit überall 0).
  Standard: Little Endian annehmen, unabhängig vom Feld. Die Oberfläche bietet „Bytes tauschen“,
  falls das Bild sichtbar falsch aussieht (Entscheidung Entwurf). Der Schalter wirkt nur auf die
  Interpretation; die Ausgabe behält die Bytefolge der Eingabe und das Feld im Kopf.
- Verarbeitung frameweise als Datenstrom. Der Arbeitsspeicherbedarf hängt nicht von der Frame-Anzahl ab.
- Bayer-Typ wird aus `ColorID` vorbelegt, falls 8–11. Bei 0 (Mono) wird „None“ vorbelegt (Entscheidung
  Entwurf). Die Mars-Beispiele sind GBRG (`ColorID` 10, FireCapture `Debayer(Pattern)=GB`).
- Eine neben der SER liegende gleichnamige `.txt` von FireCapture wird unverändert unter dem neuen
  Namen mit in den Ausgabeordner kopiert (Entscheidung Entwurf).

### TIFF/PNG

Einzelbild, 8 oder 16 Bit Graustufen. Eine Folge von Einzeldateien ist eine Serie.
Mehrseitige TIFFs werden abgelehnt (Entscheidung Entwurf).

### FITS

Selten genutzt, einfacher Umfang: primäres HDU mit `NAXIS=2`, `BITPIX` 8 oder 16 einschließlich
der üblichen `BZERO=32768`-Konvention für vorzeichenlose Werte. Header werden erhalten. Andere
Varianten (`NAXIS=3`, Fließkomma) werden mit Meldung abgelehnt. FITS wird zuletzt umgesetzt und
kann ohne Folgen für den Rest verschoben werden, sollte der Aufwand unerwartet wachsen.

## Korrektur

- Pro Filterposition eine Kurve mit Stützpunkten (alt, neu). 0 und Maximum sind immer enthalten.
- Tabelle je Filterposition für alle Eingabewerte (256 oder 65536 Einträge): ganzzahliger Wert
  plus Nachkommaanteil. Berechnung für Wert `v` im Abschnitt zwischen (a, A) und (b, B):
  `A + (v - a) * (B - A) / (b - a)`.
- Serie: `Ausgabe = floor(f) + (Zufall < frac(f) ? 1 : 0)`, auf den Wertebereich begrenzt.
  Vorschau ohne Zufall. Zufallsquelle mit festem, je Lauf setzbarem Startwert, damit Tests
  reproduzierbar sind.
- Nicht monotone Kurven sind erlaubt, erzeugen aber einen sichtbaren Warnhinweis (Entscheidung Entwurf).
- Bayer-Muster: RGGB, GRBG, GBRG, BGGR, None. „None“ = eine gemeinsame Kurve für alle Pixel.
  In der Oberfläche nutzt None die rote Zeile als gemeinsame Kurve, G1/G2/B sind dann gesperrt
  (Entscheidung Entwurf). Beim Wechsel des Musters bleiben die Kurven bei ihrer Filterposition,
  und es erscheint ein Hinweis, falls bestätigte Stützpunkte existieren; von oder nach None
  werden sie nach Rückfrage zurückgesetzt.

Der Ablauf bleibt: Bild/Serie laden, in die Übersicht klicken, in der Vorschau einen Punkt
anklicken (3×3-Mittel je Filterposition füllt die Felder), neue Werte einstellen, Stützpunkte
festlegen, anwenden. Vorschau-Ausschnitte beginnen immer auf geraden Koordinaten, auch nach
einer Fenstergrößenänderung. Bei SER wählt der Nutzer den betrachteten Frame.

Anzeige: Für die Darstellung werden 16-Bit-Daten auf 8 Bit abgebildet (normiert auf den höchsten
Wert des Frames, damit 12-Bit-Daten nicht schwarz wirken). Das wirkt sich nur auf die Anzeige aus,
nie auf die Daten. Pixel-Abfragen und Histogramme arbeiten mit den echten Werten; Histogramme
werden für 16 Bit in 256 Klassen zusammengefasst.

## Parametersätze (.cps)

- Alte `.cps`-Dateien bleiben lesbar. Sie enthalten die XML-Namensräume `.../AntiBeyer` und
  `.../Antibayer`; die Umbenennung darf das nicht brechen (feste `Namespace`-Angabe an den
  DataContracts oder gezielte Abbildung). Eine Beispieldatei wird dafür im Test mit der alten
  Fassung bzw. nach deren Format erzeugt.
- Beim Laden zählen nur bestätigte Stützpunkte. Gespeicherte Tabellen werden ignoriert und neu
  berechnet, weil alte Tabellen den Kurvenfehler enthalten. Vorschau-Stützpunkte werden verworfen.
- Passt die Bittiefe eines Satzes nicht zum Bild, gibt es eine Meldung und der Satz wird nicht
  angewendet (Entscheidung Entwurf: keine automatische Umrechnung). Dasselbe gilt, wenn ein Satz
  mit einer Kurve auf ein Bayer-Muster trifft oder einer mit vier Kurven auf None.
- Neue `.cps` speichern das Bayer-Muster. Weicht es vom gewählten Muster ab, muss der Nutzer das
  Anwenden ausdrücklich bestätigen. Alte Dateien ohne Muster gelten für das gewählte (Entscheidung Entwurf).

### Aufnahmedaten im Parametersatz (Vorschlag an den Nutzer, Freigabe mit diesem Dokument)

Ein `.cps` speichert zusätzlich, soweit bekannt: Sensor bzw. Kamera, Filter, Belichtung, Gain, die
Color Gains (`WRed`, `WGreen`, `WBlue`, nur aus der `.txt`), Temperatur (optional, nicht jede Kamera
liefert sie) sowie ein freies Notizfeld, etwa für die Folie. Filter und Color Gains sind wichtig, weil
sie das Muster direkt bestimmen: Ein Satz für IR740 passt nicht zu IR680 oder zu anderen Color Gains.
Fehlende Werte bleiben leer; alte `.cps` lesen sich mit leeren Feldern. Beim Laden werden die Werte
angezeigt; weichen sie von der aktuellen Aufnahme ab, erscheint eine Warnung, das Anwenden bleibt
aber möglich. Die Ausgabedateien ändern sich dadurch nicht.

Quelle der Werte (an echten FireCapture-2.7.14-Dateien geprüft):

1. Die gleichnamige `.txt` neben der SER, falls vorhanden: `Camera=QHY5III462C`, `Shutter=12.45ms`,
   `Gain=95 (13%)`, außerdem `Filter=RGB`.
2. Sonst die SER-Textfelder, die FireCapture zweckentfremdet: Observer `offset=0`, Instrument
   `QHY=QHY5III462C`, Telescope `fps=80.00gain=95exp=12.45`. Fremde Inhalte anderer Programme dürfen
   dabei nicht stören: Was nicht passt, bleibt leer.
3. Die Felder bleiben von Hand ausfüllbar. Temperatur kommt in den vorliegenden Dateien nicht vor;
   das Feld wird nur gefüllt, wenn eine Datei sie enthält.

## Fehlerliste, die behoben sein muss

Kurve und Kern:
1. `ConversionParameters.cs`: Kurve rechnet von `baseGrey` statt vom neuen Grauwert aus.
2. `IImagingdotNet.ApplyConversion`: verdrehte Prüfung auf den Parametersatz „None“;
   Graustufenbilder scheitern.
3. Endlosschleifen mit `ushort`-Zähler bis 65535.
4. `GetBayerType()` kennt GBRG und BGGR nicht.
5. „GRGB“ ist kein gültiges Muster und wird GRBG (G R / B G).
6. Konstruktor von `ConversionParameterSet` ignoriert `maxGrayLevel`.

Stützpunkte:
7. Unbestätigte Vorschau-Stützpunkte gelangen in die Serienkonvertierung.
8. Vorschau-Stützpunkt mit gleichem Grauwert zerstört einen bestätigten Stützpunkt.
9. Löschen eines Stützpunkts wirkt sich erst bei der nächsten Neuberechnung aus.

Bedienung:
10. „save image“: ungültiger Dateifilter, `CheckFileExists`, nur Ausschnitt doppelt konvertiert.
    Neu: speichert das geladene Bild vollständig korrigiert. Bei SER wird der aktuelle Frame als
    Einzelbild in TIFF mit gleicher Bittiefe gespeichert, nicht als SER. Das ist bewusst eine
    Momentaufnahme außerhalb der Regel „Format bleibt“. Gerechnet wird ohne Zufallsrundung und nur
    mit bestätigten Stützpunkten (Entscheidung Entwurf).
11. Erster Start: `Configuration` ist null, Schließen stürzt ab.
12. Konfiguration liegt in `ProgramData` (ohne Schreibrecht); neu `%AppData%\AntiBayer`.
13. Freigegebene Bitmaps in der Vorschau; „Source“ zeigt die unkorrigierten Daten. Das
    verdeckende `try/catch` in `OnPaint` entfällt.
14. Histogramme: Ganzzahl-Skalierung bis 0, falsche Maxima, falsche X-Position der korrigierten
    Einzelhistogramme, Einzelhistogramme leer nach „load image“, keine Aktualisierung bei
    Wertänderung.
15. Drag & Drop lädt am Bildkern vorbei; es nutzt künftig denselben Ladeweg.
16. Serienkonvertierung ohne geladenes Bild stürzt ab; Nicht-Bilddateien im Ordner werden still
    übergangen (künftig siehe „Serienlauf“).
17. Namenskollision verliert Präfix und Suffix.
18. Das Laden eines Bildes mit nicht unterstütztem Format bleibt nicht mehr unbemerkt
    (Status/Meldung).

## Serienlauf (Entscheidungen Entwurf)

- **Was verarbeitet wird:** Der Nutzer wählt die Eingabedateien aus; die geladene Datei ist
  vorausgewählt. Bei SER genügt also die eine geladene Datei, bei Einzelbildern wählt man die
  Serie. Es wird nie automatisch „alles im Ordner“ verarbeitet.
- **Wohin:** in einen getrennten Ausgabeordner, Vorgabe ist der Unterordner `AntiBayer` neben der
  Quelle. Er darf nicht der Quellordner sein, damit nichts doppelt korrigiert wird. Präfix und
  Suffix bleiben wählbar.
- **Vorabprüfung:** Vor dem Start liest das Tool die Köpfe aller Dateien und listet Abweichungen auf:
  anderer Dateityp oder andere Bittiefe als der Parametersatz, Farbdaten, eine SER mit weniger
  Daten als im Kopf angegeben, zu wenig freier Speicherplatz. Vor allem warnt es, wenn `ColorID` nicht zum gewählten Muster
  passt; maßgeblich bleibt die Auswahl in der Oberfläche. Der Nutzer kann mit den passenden Dateien
  starten oder abbrechen. Ein Lauf enthält nur einen Dateityp und eine Bittiefe.
- **Während des Laufs:** Läuft im Hintergrund mit Fortschritt und Abbrechen. Geschrieben wird in eine
  temporäre Datei, die erst am Ende den endgültigen Namen bekommt. Schlägt eine Datei fehl, wird
  ihre Temporärdatei entfernt und der Lauf setzt mit der nächsten fort; bei vollem Datenträger
  stoppt er. Der Abschlussbericht nennt jede ausgelassene oder fehlgeschlagene Datei.

## Umbenennung

Alle Vorkommen von „Beyer“ und die uneinheitliche Schreibweise „Antibayer“ werden zu „AntiBayer“:
Namespaces, Solution, Projektdateien, Assembly, Fenstertitel, README. Der Repo-Name
auf GitHub und der lokale Ordner sind die letzten Schritte und werden vorher mit dem Nutzer
abgesprochen, weil sie nach außen wirken bzw. laufende Sitzungen betreffen.

## Struktur und Tests

Vorschlag (dem Bauenden überlassen, solange der Kern ohne UI testbar ist): Kernbibliothek
`AntiBayer.Core` (Kurven, Bayer-Geometrie, Dateiformate, Serienverarbeitung), `AntiBayer` (WinForms),
`AntiBayer.Tests`. `MainForm` wird so weit aufgeteilt, wie die Fehlerbehebung es verlangt, nicht mehr.

Pflicht-Tests: Kurvenwerte einschließlich Beispiel 100→120 (101 liefert > 120, 255 bleibt 255),
16-Bit-Grenzen ohne Hänger, alle vier Bayer-Muster, Vorschau-Stützpunkte erreichen die Serie
nicht, Rundtrip je Format und Bittiefe (Identitätskurve ergibt bytegleiche Pixeldaten; SER:
Kopf und Zeitstempel bytegleich), Mittelwert über viele Zufallsläufe entspricht der Kurve,
alte `.cps` lesbar. Testdaten werden synthetisch erzeugt; echte Aufnahmen kommen nicht ins Repo.

**Reihenfolge:** Migration auf .NET 10 mit Tests, Kurve und Bayer-Fehler, Dateiformate (SER zuerst),
Serienverarbeitung, Stützpunkte/Bedienung, FITS, Umbenennung von Ordner und Repo.

## Dem Bauenden überlassen

Aufteilung in Klassen, Testbibliothek, konkrete Bedienelemente für Frame-Auswahl und Fortschritt,
Performance-Technik (solange große SER-Serien flüssig laufen; Parallelisierung nur bei Erhalt
der Reproduzierbarkeit mit festem Startwert).

## Offene Eingaben vom Nutzer

Für die Abnahme: eine 462C-SER mit sichtbarem Muster (Weißlicht-Sonne oder NIR 680/742 nm). Vorhanden
und geprüft sind nur `C:\Firecapture\Mars\170225` (462C, 16 Bit, `Filter=RGB`, also Farbaufnahmen)
und Venus-Serien (678M, Mono, 8 Bit). Für den Bau reichen die Mars-Dateien als Formattest, das Muster
lässt sich an ihnen aber nicht bewerten, weil die Kanalunterschiede dort auch Motivfarbe sind (R und
B im ersten Frame etwa 7 % über bzw. unter G, G1 und G2 fast gleich). Für 8 Bit mit Bayer-Muster gibt
es noch kein echtes Beispiel; das wird synthetisch getestet.
