# Handoff: Serienverarbeitung parallelisieren

Stand: 2026-11-07. Idee des Nutzers, noch nicht entworfen oder gebaut.

## Idee

Das Schreiben einer `.ser` (und der anderen Formate) beschleunigen: mehrere Threads, je Thread ein Frame.
Threadzahl nach den verfügbaren Kernen, etwa `Max(1, Kerne - 2)`, damit der Rechner bedienbar bleibt.

## Heutiger Stand im Code

`SeriesRunner.ProcessFile` (`src/AntiBayer.Core/Series/SeriesRunner.cs`) arbeitet strikt nacheinander:
Frame lesen, `FrameCorrector.Apply` (Kurven mit Dither), `ColorShiftCorrector.Apply` (Versatz), Frame
schreiben, Fortschritt melden. Eine 16-Bit-Serie mit 7366 Frames (596×412, 3,4 GB) dauerte im
Release-Build einige Minuten.

## Was dabei zu beachten ist

- **Erst messen.** Bei Dateien von mehreren GB kann die Platte der Engpass sein, nicht die Rechnung.
  Vorher Lesen, Rechnen und Schreiben getrennt stoppen; nur wenn die Rechnung klar überwiegt, lohnt es.
- **Reihenfolge beim Schreiben.** Die Sinks schreiben nacheinander (`IFrameSink.WriteFrame`). Die Frames
  müssen in der ursprünglichen Reihenfolge ankommen, also z. B. Lesen und Schreiben in einem Thread,
  dazwischen eine begrenzte Warteschlange parallel gerechneter Frames (begrenzt, damit der Speicher nicht
  wächst, wenn die Platte langsamer ist).
- **Lesen.** Ob `SerSource`, `FitsFile` und `WicSource` gleichzeitig aus mehreren Threads lesen dürfen, ist
  nicht geprüft. Einfacher: ein Lese-Thread, der die Frames verteilt.
- **Dither.** Heute ein `Random` je Datei (`job.Seed + fileIndex`), in Pixelreihenfolge über alle Frames.
  Parallel braucht jeder Frame einen eigenen Startwert (z. B. aus Seed, Datei und Frame-Nummer), sonst ist
  das Ergebnis nicht mehr wiederholbar. Das Ergebnis weicht dann geringfügig vom heutigen ab; Tests, die
  feste Ausgaben erwarten, entsprechend anpassen.
- **Zustand der Korrektoren.** `ColorShiftCorrector` hält Puffer (`GreenField`) und darf nicht von mehreren
  Threads zugleich benutzt werden: einer je Thread. `FrameCorrector` liest nur seine Tabellen, das sollte
  geteilt gehen (prüfen, dass `CurveTable.Map`/`MapDithered` keinen Zustand ändert).
- **Abbruch, Fehler, Platte voll.** Abbruch per Token, Ausnahmen aus einem Arbeitsthread und
  `DiskFullException` müssen wie heute beim Aufrufer ankommen; die `.tmp`-Datei wird weiter gelöscht.
- **Fortschritt.** Nicht aus jedem Thread melden, sondern beim Schreiben, in Reihenfolge.
- **Einzelbilder (FITS, PNG, TIFF).** Dort hat eine Datei oft nur ein Frame; parallel lohnt dann eher über
  mehrere Dateien einer Serie.
- **Kerne.** `Environment.ProcessorCount` zählt logische Kerne; ob `- 2` darauf passt, beim Messen prüfen.

## Mögliches Ergebnis

Je nach Messung: nichts tun (Platte ist der Engpass), parallele Frames in einer Datei, oder parallele
Dateien. Vor dem Bau kurz entwerfen und vom Nutzer freigeben lassen.
