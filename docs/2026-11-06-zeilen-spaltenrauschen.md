# Handoff: Zeilen- und Spaltenrauschen prüfen

Stand: 2026-11-06. Erst angehen, wenn die Reparatur fertig ist
(`docs/2026-11-06-antibayer-reparatur-design.md`). Das Tool muss dann
16-Bit-SER-Dateien korrekt lesen; darauf baut diese Prüfung auf.

## Frage des Nutzers

Neben dem Bayer-Muster könnten bei Planetenfotos weitere feststehende Muster den Sensor prägen.
Zeilen- und Spaltenrauschen wären gute Zusatzoptionen. Der Nutzer weiß noch nicht, wie sie
auftreten und ob man sie gut erkennt. Verstärkerglühen ist für ihn ausdrücklich egal.

## Vorschlag für den ersten Schritt: nur messen, nichts korrigieren

An echten SER-Aufnahmen des Nutzers (die vor dem Stacken korrigiert werden sollen):

1. Über viele Frames mitteln, damit nur feststehende Anteile übrig bleiben.
2. Zeilen- und Spaltenmittel je Filterposition bilden, damit das Bayer-Muster die Messung nicht überdeckt.
3. Den Planeten bzw. die Sonnenscheibe getrennt vom Hintergrund betrachten.
4. Feststehende Anteile von solchen trennen, die sich je Frame ändern. Nur Letztere ließen sich
   nicht mit einer festen Korrektur behandeln.
5. Größe im Vergleich zum Bayer-Muster und zum Bildrauschen angeben.

## Mögliches Ergebnis

Erst anhand der Messung entscheiden, ob es sich lohnt und was gebaut wird: gar nichts, eine
Anzeige im Tool oder eine zuschaltbare Korrektur. Dazu neu brainstormen.

## Noch unbekannt

Ob und wie stark Zeilen- und Spaltenrauschen bei der Kamera des Nutzers auftreten. Bekannt: QHY5III462C
(IMX462, CMOS-Farbsensor) mit FireCapture, Auswertung als Monochrom-Aufnahme. Gemessen werden sollte
an Weißlicht- oder NIR-Aufnahmen, nicht an Farbaufnahmen.
