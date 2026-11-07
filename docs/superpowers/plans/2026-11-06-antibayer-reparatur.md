# AntiBayer-Reparatur Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** AntiBayer rechnet korrekt, verarbeitet SER/TIFF/PNG/FITS in 8 und 16 Bit verlustfrei und läuft auf .NET 10.

**Architecture:** Eine UI-freie Kernbibliothek (`AntiBayer.Core`) enthält Kurven, Bayer-Geometrie, Dateiformate und Serienlauf und ist vollständig getestet. Die bestehende WinForms-Oberfläche (`AntiBayer.App`) wird darauf umgestellt; alle Pixeldaten laufen als `ushort[]`, `System.Drawing` dient nur noch der Anzeige. Bestätigte Stützpunkte und Vorschau-Stützpunkte liegen getrennt, sodass Vorschauwerte strukturell nicht in eine Serie gelangen.

**Tech Stack:** C#, .NET 10 (`net10.0-windows`), WinForms, WIC über `System.Windows.Media.Imaging` (`UseWPF`), xUnit.

**Spec:** `docs/superpowers/specs/2026-11-06-antibayer-reparatur-design.md`

## Global Constraints

- Zielframework `net10.0-windows`; SDK 10.0.402 ist installiert. Keine Fremdbibliotheken außer dem Testframework; Lizenz bleibt AGPL.
- Originaldateien werden nie geschrieben, nur lesend geöffnet.
- Bittiefe (8/16), Frame-Anzahl, SER-Kopf und SER-Zeitstempel-Anhang bleiben erhalten.
- Eine SER wird frameweise verarbeitet, nie ganz in den Speicher geladen. Dateioffsets sind `long`.
- Schreibweise überall „AntiBayer“; auch Texte in Code und UI.
- Echte Aufnahmen kommen nicht ins Repo; Testdaten sind synthetisch. `C:\Firecapture` wird nur gelesen.
- Arbeitsverzeichnis der Sitzung ist `C:\Claude`: Befehle mit absoluten Pfaden, ohne `cd` (`git -C C:/Claude/antibeyer …`, `dotnet test C:/Claude/antibeyer/AntiBayer.sln`).
- Arbeit auf Branch `reparatur`; Commit-Botschaften enden mit `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Kein Push ohne Rückfrage.
- Texte der Oberfläche auf Englisch wie bisher.

## Review Focus

1. **Abgebrochene Aufnahme (SER kürzer als im Kopf angegeben, unvollständiger letzter Frame):** Das Tool öffnet die vollständigen Frames, meldet die Abweichung und meldet sie in der Vorabprüfung als Problem. → Test in Task 4.
2. **Kleine ROI (136×116) bei großem Fenster:** Der Vorschau-Ausschnitt passt sich an das Bild an, beginnt auf geraden Koordinaten und wirft nie eine Ausnahme, auch nicht bei Klick in die Ecke. → Test in Task 2.
3. **Ausgabedatei gesperrt oder Datenträger voll:** Kein halbes Ergebnis unter dem Endnamen, Fehler im Bericht; Datenträger voll stoppt den Lauf. → Test in Task 8.
4. **Klick am Bildrand:** Das 3×3-Mittel enthält immer jede Filterposition, keine Division durch null, auch bei „None“. → Test in Task 2.
5. **Neue Datei laden, während bestätigte Stützpunkte existieren:** Die Arbeit geht nicht still verloren; das Tool fragt nach. Eine Kurve mit anderer Bittiefe bleibt dabei nicht aktiv. → Test `ParameterSet.CompatibleWith` in Task 3, Rückfrage in Task 10.

---

### Task 1: Migration auf .NET 10 und neue Struktur

**Files:**
- Create: `AntiBayer.sln`, `src/AntiBayer.Core/AntiBayer.Core.csproj`, `src/AntiBayer.App/AntiBayer.App.csproj`, `tests/AntiBayer.Tests/AntiBayer.Tests.csproj`, `tests/AntiBayer.Tests/SmokeTest.cs`
- Move (`git mv`): alle `*.cs`, `Interfaces/*`, `MainForm.resx` und `Properties/*` nach `src/AntiBayer.App/`
- Delete: das alte Projekt `AntiBayer.csproj` im Wurzelordner, `AntiBeyer.sln`, `App.config`

**Interfaces:**
- Produces: Projekte `AntiBayer.Core` (`net10.0-windows`, `UseWPF=true`, `RootNamespace=AntiBayer.Core`), `AntiBayer.App` (`net10.0-windows`, `UseWindowsForms=true`, `OutputType=WinExe`, `AssemblyName=AntiBayer`, `RootNamespace=AntiBayer`, Referenz auf Core), `AntiBayer.Tests` (xUnit, Referenz auf Core). Alle mit `Nullable=enable`.

- [ ] **Step 1: Branch anlegen und freigegebene Dokumente committen**

```bash
git -C C:/Claude/antibeyer switch -c reparatur
git -C C:/Claude/antibeyer add docs
git -C C:/Claude/antibeyer commit -m "docs: Design, Handoff und Plan für die Reparatur"
```

- [ ] **Step 2: SDK-Projekte anlegen, Dateien verschieben**

`Application.SetHighDpiMode` nicht einführen; das Fenster bleibt wie es ist. In `Properties/AssemblyInfo.cs` nur die Attribute behalten, die das SDK nicht selbst erzeugt, oder `GenerateAssemblyInfo=false` setzen.

- [ ] **Step 3: Namespaces vereinheitlichen**

`AntiBeyer`, `Antibayer` und `AntiBeyer.Interfaces` werden zu `AntiBayer`. Im Designer-Code `Antibayer.PictureBoxWithInterpolationMode` anpassen; `.resx`-Zuordnung über den Dateinamen prüfen. Die Verwendung von `using static System.Windows.Forms.AxHost` und anderen ungenutzten Imports entfernen, falls sie den Build stören.

- [ ] **Step 4: Rauchtest schreiben**

```csharp
public class SmokeTest { [Fact] public void Tests_run() => Assert.True(true); }
```

- [ ] **Step 5: Bauen und testen**

Run: `dotnet build C:/Claude/antibeyer/AntiBayer.sln` und `dotnet test C:/Claude/antibeyer/AntiBayer.sln`
Expected: 0 Fehler, 1 Test bestanden. `grep -ri beyer` über `src` und `tests` findet nichts.

- [ ] **Step 6: Commit** `refactor: Umstieg auf .NET 10, Projektstruktur, Schreibweise AntiBayer`

### Task 2: Bayer-Geometrie und Bildausschnitte

**Files:**
- Create: `src/AntiBayer.Core/Bayer.cs`, `src/AntiBayer.Core/PreviewGeometry.cs`
- Test: `tests/AntiBayer.Tests/BayerTests.cs`, `tests/AntiBayer.Tests/PreviewGeometryTests.cs`

**Interfaces:**
- Produces:
  - `enum BayerPattern { None, RGGB, GRBG, GBRG, BGGR }`
  - `enum BayerFilter { Red = 0, Green1 = 1, Green2 = 2, Blue = 3, All = 4 }`
  - `static BayerFilter Bayer.FilterAt(BayerPattern p, int x, int y)`
  - `static BayerPattern? Bayer.FromSerColorId(int colorId)` (8 RGGB, 9 GRBG, 10 GBRG, 11 BGGR, 0 → `None`, sonst `null`)
  - `static IReadOnlyDictionary<BayerFilter, double> Bayer.NeighbourhoodMean(ushort[] frame, int width, int height, BayerPattern p, int x, int y)`
  - `readonly record struct PixelRect(int X, int Y, int Width, int Height)`
  - `static PixelRect PreviewGeometry.Compute(int centerX, int centerY, int wantedWidth, int wantedHeight, int imageWidth, int imageHeight)`

- [ ] **Step 1: Tests schreiben**

```csharp
[Theory]
[InlineData(BayerPattern.RGGB, BayerFilter.Red,    BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Blue)]
[InlineData(BayerPattern.GRBG, BayerFilter.Green1, BayerFilter.Red,    BayerFilter.Blue,   BayerFilter.Green2)]
[InlineData(BayerPattern.GBRG, BayerFilter.Green1, BayerFilter.Blue,   BayerFilter.Red,    BayerFilter.Green2)]
[InlineData(BayerPattern.BGGR, BayerFilter.Blue,   BayerFilter.Green1, BayerFilter.Green2, BayerFilter.Red)]
public void FilterAt_liefert_Muster(BayerPattern p, BayerFilter a, BayerFilter b, BayerFilter c, BayerFilter d)
{ /* (0,0)=a, (1,0)=b, (0,1)=c, (1,1)=d und (2,2)=a */ }

[Fact] public void FilterAt_None_ist_All() { /* alle Positionen: BayerFilter.All */ }

[Fact] public void Mittel_am_Rand_enthaelt_alle_Positionen()
{ /* 4x4-Frame, Ecken (0,0) und (3,3), RGGB: Dictionary hat Red, Green1, Green2, Blue; Werte korrekt */ }

[Fact] public void Mittel_bei_None_liefert_nur_All() { }

[Fact] public void Vorschau_beginnt_auf_geraden_Koordinaten()
{ /* Für viele Kombinationen: X%2==0, Y%2==0, Rechteck vollständig im Bild */ }

[Fact] public void Vorschau_groesser_als_ROI_wird_begrenzt()
{ /* Compute(0,0,200,200,136,116) == (0,0,136,116); Compute(135,115,68,60,136,116) liegt im Bild */ }
```

- [ ] **Step 2: Tests laufen lassen, Fehlschlag prüfen**

Run: `dotnet test C:/Claude/antibeyer/AntiBayer.sln --filter "FullyQualifiedName~BayerTests|FullyQualifiedName~PreviewGeometryTests"`
Expected: FAIL (Typen fehlen).

- [ ] **Step 3: `Bayer` und `PreviewGeometry` umsetzen**

`NeighbourhoodMean` verschiebt das Zentrum so, dass ein 3×3-Fenster ganz im Bild liegt; für Bilder unter 3 Pixel Kantenlänge wird das Fenster kleiner und fehlende Positionen fehlen im Ergebnis. Grün wird je Position getrennt gemittelt.

- [ ] **Step 4: Tests laufen lassen**: PASS.

- [ ] **Step 5: Commit** `feat: Bayer-Geometrie mit GRBG, begrenzte Vorschau auf geraden Koordinaten`

### Task 3: Tonwertkurve und Parametersatz

Behebt Fehler 1, 3, 6, 7, 8, 9.

**Files:**
- Create: `src/AntiBayer.Core/CurveTable.cs`, `src/AntiBayer.Core/ParameterSet.cs`, `src/AntiBayer.Core/CaptureInfo.cs`
- Test: `tests/AntiBayer.Tests/CurveTableTests.cs`, `tests/AntiBayer.Tests/ParameterSetTests.cs`

**Interfaces:**
- Consumes: `BayerPattern`, `BayerFilter`
- Produces:
  - `readonly record struct CurvePoint(ushort Original, ushort New)`
  - `sealed class CurveTable { static CurveTable Build(IEnumerable<CurvePoint> points, int bitDepth, ushort whiteLevel); ushort Map(ushort v); ushort MapDithered(ushort v, Random r); bool IsMonotonic { get; } }`
  - `sealed record CaptureInfo(string? Camera, string? Filter, double? ExposureMs, double? Gain, double? WRed, double? WGreen, double? WBlue, double? TemperatureC, string? Note)`; Werte optional; `IReadOnlyList<string> DifferencesTo(CaptureInfo other)` (nennt nur Felder, die auf beiden Seiten gesetzt sind und abweichen; `Note` zählt nicht)
  - `sealed class ParameterSet { ParameterSet(BayerPattern pattern, int bitDepth); BayerPattern Pattern; int BitDepth; CaptureInfo Capture; IReadOnlyList<CurvePoint> Confirmed(BayerFilter f); void SetConfirmed(BayerFilter f, CurvePoint p); void RemoveConfirmed(BayerFilter f, ushort original); void SetPreview(BayerFilter f, CurvePoint p); void ClearPreview(); void ChangePattern(BayerPattern p); bool HasConfirmedPoints; IReadOnlyDictionary<BayerFilter, CurveTable> BuildConfirmedTables(ushort whiteLevel); IReadOnlyDictionary<BayerFilter, CurveTable> BuildPreviewTables(ushort whiteLevel); IReadOnlyList<BayerFilter> NonMonotonicFilters(ushort whiteLevel); SetCompatibility CompatibleWith(int bitDepth, BayerPattern pattern); }`
  - `enum SetCompatibility { Ok, PatternDiffers, BitDepthMismatch, CurveCountMismatch }`

  Bei Pattern `None` gibt es nur den Filter `All`, sonst Red, Green1, Green2, Blue.

- [ ] **Step 1: Tests schreiben**

```csharp
[Fact] public void Abschnitt_startet_beim_neuen_Grauwert()
{
    var t = CurveTable.Build(new[] { new CurvePoint(100, 120) }, 8, 255);
    Assert.Equal(120, t.Map(100));
    Assert.True(t.Map(101) > 120);
    Assert.Equal(255, t.Map(255));
}

[Fact] public void Sechzehn_Bit_ohne_Haenger_und_begrenzt_auf_Weisswert()
{
    var t = CurveTable.Build(new[] { new CurvePoint(30000, 31000) }, 16, 65520);
    Assert.Equal(31000, t.Map(30000));
    Assert.Equal(65520, t.Map(65520));
    Assert.Equal(65520, t.Map(65535));
}

[Fact] public void Stuetzpunkt_ueber_Weisswert_wird_ignoriert_und_Weisswert_erzwungen() { /* (65535,60000) bei White 65520 */ }

[Fact] public void Null_ist_setzbar() { /* (0,10): Map(0)==10 */ }

[Fact] public void Dither_trifft_im_Mittel_die_Kurve()
{ /* Punkt (100,105) 8 Bit, seed 1: Mittel von 100000x MapDithered(50) liegt innerhalb 0.01 von 52.5 */ }

[Fact] public void Dither_ueberschreitet_Weisswert_nie() { }

[Fact] public void Identitaet_aendert_keinen_Wert() { /* leere Punkte, 16 Bit: Map(v)==v für viele v; MapDithered ebenso */ }

[Fact] public void Vorschaupunkte_erreichen_bestaetigte_Tabellen_nicht()
{ /* SetPreview Red (100,150): Confirmed-Tabelle Map(100)==100, Preview-Tabelle ==150 */ }

[Fact] public void Vorschaupunkt_gleichen_Werts_zerstoert_bestaetigten_nicht()
{ /* SetConfirmed (100,120), SetPreview (100,150), ClearPreview: Confirmed enthält (100,120) */ }

[Fact] public void Loeschen_wirkt_sofort() { /* RemoveConfirmed, danach Build: Map(100)==100 */ }

[Fact] public void Nicht_monoton_wird_gemeldet() { /* (100,200),(150,120) */ }

[Theory] /* 16-Bit-Satz gegen 8 Bit → BitDepthMismatch; None-Satz gegen RGGB und umgekehrt → CurveCountMismatch;
            GBRG-Satz gegen RGGB → PatternDiffers; gleich → Ok */
```

- [ ] **Step 2: Tests laufen lassen, Fehlschlag prüfen**: FAIL.

- [ ] **Step 3: `CurveTable` umsetzen**

Zwei Arrays mit `2^bitDepth` Einträgen: `ushort[] floor`, `float[] frac`. Punkte mit `Original > whiteLevel` und jeder Punkt auf `whiteLevel` selbst werden durch `(white, white)` ersetzt; ohne Punkt bei 0 wird `(0,0)` ergänzt. Rechnung `A + (v-a)*(B-A)/(b-a)` in `double`. Werte über `whiteLevel`: `floor=white`, `frac=0`. Ergebnis auf `[0, white]` begrenzen. `MapDithered`: `r.NextDouble() < frac ? floor+1 : floor`, begrenzt auf `white`.

- [ ] **Step 4: `ParameterSet` und `CaptureInfo` umsetzen**

Vorschaupunkte in eigenem Dictionary; `BuildPreviewTables` überlagert bestätigte Punkte mit Vorschaupunkten. `ChangePattern` behält Kurven innerhalb der vier Bayer-Muster; von und nach `None` leert es sie (die Rückfrage stellt die Oberfläche).

- [ ] **Step 5: Tests laufen lassen**: PASS.

- [ ] **Step 6: Commit** `feat: korrekte Tonwertkurven, getrennte Vorschau-Stützpunkte`

### Task 4: Bildquelle und SER

Behebt die Datenseite von Fehler 2. Umfasst Weißwert und Bytefolge.

**Files:**
- Create: `src/AntiBayer.Core/Imaging/ImageInfo.cs`, `Imaging/IFrameSource.cs`, `Imaging/IFrameSink.cs`, `Imaging/WhiteLevel.cs`, `Imaging/SerHeader.cs`, `Imaging/SerSource.cs`, `Imaging/SerSink.cs`, `Imaging/UnsupportedImageException.cs` (alle unter `src/AntiBayer.Core/`)
- Test: `tests/AntiBayer.Tests/SerTests.cs`, `tests/AntiBayer.Tests/WhiteLevelTests.cs`, `tests/AntiBayer.Tests/TestData.cs`

**Interfaces:**
- Consumes: `BayerPattern`, `Bayer.FromSerColorId`, `CaptureInfo`
- Produces:
  - `enum ImageFormat { Ser, Tiff, Png, Fits }`
  - `sealed record ImageInfo(string Path, ImageFormat Format, int Width, int Height, int BitDepth, int FrameCount, ushort WhiteLevel, BayerPattern? SuggestedPattern, CaptureInfo Capture, string? Warning)`
  - `interface IFrameSource : IDisposable { ImageInfo Info; void ReadFrame(int index, ushort[] target); }`
  - `interface IFrameSink : IDisposable { void WriteFrame(ReadOnlySpan<ushort> frame); void Complete(); }`
  - `static ushort WhiteLevel.Detect(ushort[] firstFrame, int bitDepth, int headerDepth)`
  - `sealed record SerHeader { static SerHeader Parse(ReadOnlySpan<byte> bytes178); long FrameOffset(int index); long DataBytes; int ColorId, LittleEndianFlag, Width, Height, PixelDepth, FrameCount; string Observer, Instrument, Telescope; }`
  - `sealed class SerSource : IFrameSource { SerSource(string path, bool swapBytes = false); SerHeader Header; bool HasTimestampTrailer; }`
  - `sealed class SerSink : IFrameSink { SerSink(SerSource source, string targetPath); }`
  - `TestData.WriteSer(string path, int w, int h, int depth, int colorId, int frames, Func<int,int,int,ushort> pixel, bool trailer = true)` in Tests

- [ ] **Step 1: Tests schreiben**

```csharp
[Fact] public void Weisswert_linksbuendig_12_Bit() => /* Werte Vielfache von 16, Tiefe 16 → 65520 */
[Fact] public void Weisswert_rechtsbuendig_12_Bit() => /* Kopf 12, Werte < 4096 → 4095 */
[Fact] public void Weisswert_volle_16_Bit() => /* niedrigstes Bit gesetzt → 65535 */
[Fact] public void Weisswert_8_Bit() => /* 255, auch wenn niedrige Bits 0 */
[Fact] public void Weisswert_leeres_Schwarzbild() => /* alle 0, Tiefe 16 → 65535 */

[Fact] public void Liest_FireCapture_Kopf()
{ /* ColorId 10, LittleEndianFlag 0, Tiefe 16, Daten Little Endian: Pixelwert korrekt, SuggestedPattern == GBRG */ }

[Fact] public void Rundgang_Identitaet_bytegleich()
{ /* 8 und 16 Bit, mit und ohne Anhang: Datei → SerSource → SerSink → File.ReadAllBytes identisch */ }

[Fact] public void Bytes_tauschen_aendert_nur_Interpretation()
{ /* swapBytes: Pixelwert getauscht; Rundgang bleibt bytegleich */ }

[Fact] public void Kopf_und_Anhang_bleiben_bei_Korrektur_gleich()
{ /* veränderte Pixel: erste 178 Byte und letzte 8*n Byte identisch */ }

[Theory] /* ColorId 100, 101 → UnsupportedImageException mit Text */
[Fact] public void Abgebrochene_SER_oeffnet_vollstaendige_Frames()
{ /* Kopf 10 Frames, Daten für 7,5: Info.FrameCount == 7, Warning != null */ }

[Fact] public void Offset_ueber_2_GB_ohne_Ueberlauf()
{ /* Kopf 1920x1080, 16 Bit: FrameOffset(2000) == 178 + 2000L*4147200 */ }

[Fact] public void Original_bleibt_lesbar_waehrend_Quelle_offen() { /* FileShare.Read, Datei unverändert */ }
```

- [ ] **Step 2: Tests laufen lassen, Fehlschlag prüfen**: FAIL.

- [ ] **Step 3: Umsetzen**

Standard Little Endian unabhängig vom Feld (Spec, Beleg FireCapture 2.7.14). `SerSink` schreibt zunächst den Originalkopf, dann Frames, in `Complete` den Original-Anhang; `FrameCount` im Kopf bleibt. Abgebrochene SER-Dateien lassen sich ansehen, aber nicht ausgeben: Die Vorabprüfung schließt sie aus, und der `SerSink`-Konstruktor wirft für sie `UnsupportedImageException`. Ein Frame von 8 Bit wird zu `ushort` erweitert, ohne Skalierung. Pufferung über `FileStream` mit 1 MiB; keine Daten über die Frame-Größe hinaus im Speicher.

- [ ] **Step 4: Tests laufen lassen**: PASS.

- [ ] **Step 5: Gegenprobe mit echter Datei**

Mit einem temporären Test oder `dotnet script`-freien Testlauf per Umgebungsvariable `ANTIBAYER_SAMPLE=C:/Firecapture/Mars/170225/2025-02-17-1833_6-U-RGB-Mars.ser` (Test wird ohne Variable übersprungen): Rundgang in den Scratchpad-Ordner bytegleich, `WhiteLevel == 65520`, `SuggestedPattern == GBRG`, `FrameCount == 490`.

- [ ] **Step 6: Commit** `feat: SER lesen und schreiben, Weißwert-Erkennung`

### Task 5: Aufnahmedaten lesen

**Files:**
- Create: `src/AntiBayer.Core/CaptureInfoReader.cs`
- Modify: `src/AntiBayer.Core/Imaging/SerSource.cs` (füllt `Info.Capture`)
- Test: `tests/AntiBayer.Tests/CaptureInfoReaderTests.cs`

**Interfaces:**
- Consumes: `CaptureInfo`, `SerHeader`
- Produces: `static CaptureInfo CaptureInfoReader.ForFile(string imagePath, SerHeader? header)`, `static CaptureInfo CaptureInfoReader.ParseSidecar(string text)`, `static CaptureInfo CaptureInfoReader.ParseSerFields(string observer, string instrument, string telescope)`

- [ ] **Step 1: Tests schreiben**

```csharp
[Fact] public void Sidecar_liefert_alle_Werte()
{ /* Camera=QHY5III462C, Filter=IR740, Shutter=12.45ms, Gain=95 (13%), WBlue=128, WGreen=64, WRed=64
     → Camera, Filter, ExposureMs 12.45, Gain 95, WRed 64, WGreen 64, WBlue 128, TemperatureC null */ }

[Fact] public void Sidecar_liest_nicht_in_die_Zeitstempel() { /* "Frame 1:" und folgende Zeilen ignoriert */ }

[Fact] public void Sidecar_mit_Temperatur() { /* Schlüssel mit "temp" (case-insensitive) und Zahl */ }

[Fact] public void SER_Felder_FireCapture()
{ /* "offset=0","QHY=QHY5III462C","fps=80.00gain=95exp=12.45" → Camera QHY5III462C, Gain 95, ExposureMs 12.45 */ }

[Fact] public void Fremde_SER_Felder_bleiben_leer() { /* "Max","ZWO ASI462MC","C11" → Camera "ZWO ASI462MC", Rest null, keine Ausnahme */ }

[Fact] public void Sidecar_hat_Vorrang_vor_SER_Feldern() { }
[Fact] public void Dezimalkomma_und_Punkt_invariant() { /* Punkt ist Dezimaltrenner unabhängig von der Kultur de-DE */ }
```

- [ ] **Step 2: Tests laufen lassen, Fehlschlag prüfen**: FAIL.

- [ ] **Step 3: Umsetzen**

Sidecar: gleichnamige `.txt`, Latin-1 gelesen (FireCapture schreibt `°` nicht in UTF-8), bis zur Zeile mit `Timestamps`. Felder einzeln vom Sidecar bevorzugt, fehlende aus der SER ergänzt.

- [ ] **Step 4: Tests laufen lassen**: PASS.

- [ ] **Step 5: Commit** `feat: Aufnahmedaten aus FireCapture-Log und SER-Feldern`

### Task 6: Parametersätze (.cps)

**Files:**
- Create: `src/AntiBayer.Core/CpsFile.cs`, `tests/AntiBayer.Tests/Fixtures/old-8bit.cps`
- Modify: `src/AntiBayer.Core/ParameterSet.cs` (`PatternKnown`; `CompatibleWith` liefert bei `PatternKnown == false` nie `PatternDiffers`)
- Test: `tests/AntiBayer.Tests/CpsFileTests.cs`

**Interfaces:**
- Consumes: `ParameterSet`, `CaptureInfo`
- Produces: `static ParameterSet CpsFile.Load(string path)`, `static void CpsFile.Save(string path, ParameterSet set)`, `class CpsFormatException : Exception`; `ParameterSet.PatternKnown` (`false` bei alten Dateien)

- [ ] **Step 1: Echte Alt-Datei erzeugen**

Im Scratchpad eine Konsolenanwendung (`net10.0`) mit den Dateien `ConversionParameters.cs`, `Classes.cs`, `DebugTools.cs` aus `git show 76d81e0:<datei>` anlegen. Sie erzeugt `new ConversionParameterSet(true, 255)`, setzt mit `Sets[BayerFilter.red].StrongPoints.Add(new StrongPoint(BayerFilter.red, 100, 120, false))`, einen Vorschaupunkt `(green1, 50, 60, true)`, ruft `CreateConversionDictionary(255)` für beide Sätze auf und schreibt per `DataContractSerializer` nach `tests/AntiBayer.Tests/Fixtures/old-8bit.cps`. Die Konsolenanwendung kommt nicht ins Repo. Die Fixture ins Testprojekt als `CopyToOutputDirectory` aufnehmen.

- [ ] **Step 2: Tests schreiben**

```csharp
[Fact] public void Alte_Datei_laedt_nur_bestaetigte_Punkte()
{ /* Confirmed(Red) enthält (100,120); Confirmed(Green1) enthält keinen (50,60); BitDepth 8; PatternKnown false */ }

[Fact] public void Alte_Tabellen_werden_nicht_uebernommen()
{ /* BuildConfirmedTables(255)[Red].Map(101) > 120, obwohl die Datei falsche Tabellen enthält */ }

[Fact] public void Neu_Rundgang_mit_Muster_und_Aufnahmedaten() { /* alle Felder inkl. Note, Sonderzeichen */ }
[Fact] public void Neue_Datei_schreibt_keine_Vorschaupunkte() { }
[Fact] public void Kaputte_Datei_wirft_CpsFormatException() { /* leer, kein XML, fremdes XML */ }
```

- [ ] **Step 3: Tests laufen lassen, Fehlschlag prüfen**: FAIL.

- [ ] **Step 4: Umsetzen**

Lesen mit `XDocument` über `LocalName`, nicht über `DataContractSerializer`, damit die alten Namensräume keine Rolle spielen. Altformat erkennen an Wurzel `ConversionParameterSet`: nur `StrongPoint`-Elemente mit `PreviewStrongPoint=false`, Filter aus `BayerFilter` (`red`, `green1`, `green2`, `blue`, `None`), Tiefe aus `MaxGreyLevel` (255 → 8, sonst 16), die automatischen Endpunkte `(0,0)` und `(max,max)` weglassen. Kurvenanzahl 1 ergibt `Pattern=None`; bei vier Kurven und unbekanntem Muster übernimmt `CompatibleWith` das aktuelle Muster als passend (`PatternKnown=false`). Neues Format: Wurzel `AntiBayerParameterSet` mit `version="2"`, Elementen für Muster, Tiefe, Aufnahmedaten, Kurven; Zahlen invariant.

- [ ] **Step 5: Tests laufen lassen**: PASS.

- [ ] **Step 6: Commit** `feat: Parametersätze mit Aufnahmedaten, alte Dateien bleiben lesbar`

### Task 7: TIFF und PNG

**Files:**
- Create: `src/AntiBayer.Core/Imaging/WicSource.cs`, `Imaging/WicSink.cs`, `Imaging/ImageFiles.cs`
- Test: `tests/AntiBayer.Tests/WicTests.cs`, `tests/AntiBayer.Tests/ImageFilesTests.cs`

**Interfaces:**
- Consumes: `IFrameSource`, `IFrameSink`, `WhiteLevel`, `SerSource`, `SerSink`, `CaptureInfoReader`
- Produces:
  - `static IFrameSource ImageFiles.Open(string path, bool swapBytes = false)` (Formatwahl nach Endung `.ser .tif .tiff .png`; FITS kommt in Task 13)
  - `static IFrameSink ImageFiles.CreateSink(IFrameSource source, string targetPath)`
  - `static bool ImageFiles.IsSupportedExtension(string path)`

- [ ] **Step 1: Tests schreiben**

```csharp
[Theory] /* PNG, TIFF × 8, 16 Bit Grau: Rundgang liefert identische Pixelwerte und gleiche Bittiefe */
[Fact] public void RGB_mit_gleichen_Kanaelen_gilt_als_einkanalig()
{ /* Bgr24 und Bgra32, R=G=B: BitDepth 8; Ausgabe wieder im selben WIC-PixelFormat, Werte stimmen */ }
[Fact] public void Echtes_Farbbild_wird_abgelehnt() { /* UnsupportedImageException */ }
[Fact] public void Mehrseitiges_TIFF_wird_abgelehnt() { }
[Fact] public void Unbekannte_Endung_und_kaputte_Datei_liefern_UnsupportedImageException() { /* mit Text, keine andere Ausnahme */ }
[Fact] public void Quelle_sperrt_Datei_nicht_zum_Lesen_und_laesst_sie_frei_nach_Dispose() { }
```

- [ ] **Step 2: Tests laufen lassen, Fehlschlag prüfen**: FAIL.

- [ ] **Step 3: Umsetzen**

`BitmapDecoder` mit `BitmapCacheOption.OnLoad` über einen selbst geöffneten `FileStream`, kein Farbmanagement (`IgnoreColorProfile`). Graupixel direkt per `CopyPixels`, RGB-Quellen über Kanalvergleich. `WicSink` schreibt das ursprüngliche `PixelFormat` mit dem passenden Encoder; PNG und TIFF verlustfrei.

- [ ] **Step 4: Tests laufen lassen**: PASS.

- [ ] **Step 5: Commit** `feat: TIFF und PNG in 8 und 16 Bit`

### Task 8: Korrektur und Serienlauf

Behebt Fehler 17 sowie die Kernlogik von Fehler 16 und 7.

**Files:**
- Create: `src/AntiBayer.Core/FrameCorrector.cs`, `src/AntiBayer.Core/Series/OutputNaming.cs`, `Series/Preflight.cs`, `Series/SeriesRunner.cs`
- Test: `tests/AntiBayer.Tests/FrameCorrectorTests.cs`, `tests/AntiBayer.Tests/PreflightTests.cs`, `tests/AntiBayer.Tests/SeriesRunnerTests.cs`

**Interfaces:**
- Consumes: `ParameterSet`, `CurveTable`, `Bayer`, `ImageFiles`
- Produces:
  - `sealed class FrameCorrector { FrameCorrector(BayerPattern p, IReadOnlyDictionary<BayerFilter, CurveTable> tables); void Apply(ReadOnlySpan<ushort> src, Span<ushort> dst, int width, int height, Random? dither); }`
  - `static string OutputNaming.Build(string input, string folder, string prefix, string suffix)`
  - `sealed record SeriesJob(IReadOnlyList<string> Files, string OutputFolder, string Prefix, string Suffix, ParameterSet Set, BayerPattern Pattern, int Seed, bool SwapBytes)`
  - `sealed record PreflightIssue(string? File, string Message, bool Excludes)`
  - `sealed record PreflightResult(IReadOnlyList<string> Runnable, IReadOnlyList<PreflightIssue> Issues)`
  - `static PreflightResult Preflight.Check(SeriesJob job)`
  - `sealed record SeriesProgress(int FileIndex, int FileCount, int Frame, int FrameCount)`
  - `sealed record SeriesReport(IReadOnlyList<string> Completed, IReadOnlyList<(string File, string Reason)> Failed, IReadOnlyList<string> Skipped, bool Cancelled, bool StoppedDiskFull)`
  - `static SeriesReport SeriesRunner.Run(SeriesJob job, IReadOnlyList<string> runnable, IProgress<SeriesProgress>? progress, CancellationToken token)`

- [ ] **Step 1: Tests schreiben**

```csharp
[Fact] public void Jede_Filterposition_nutzt_eigene_Kurve() { /* GBRG, vier verschiedene Punkte; None nutzt All */ }
[Fact] public void Vorschau_ohne_Dither_ist_deterministisch() { }

[Fact] public void Serie_ist_mit_gleichem_Seed_reproduzierbar() { /* zwei Läufe bytegleich; anderer Seed unterscheidet sich */ }
[Fact] public void Serie_erhaelt_Frameanzahl_Kopf_und_Anhang() { }
[Fact] public void Serie_nutzt_nur_bestaetigte_Punkte() { /* SetPreview vor dem Lauf hat keine Wirkung */ }
[Fact] public void Original_bleibt_unveraendert() { /* Hash vorher und nachher */ }
[Fact] public void Sidecar_wird_mit_neuem_Namen_kopiert() { }

[Fact] public void Name_behaelt_Praefix_und_Suffix_bei_Kollision() { /* BA_x_k.ser existiert → BA_x_k_1.ser */ }

[Fact] public void Preflight_lehnt_Ausgabe_gleich_Quelle_ab() { }
[Fact] public void Preflight_meldet_andere_Bittiefe_Dateityp_Farbe_und_abgebrochene_SER() { /* Excludes==true, Runnable ohne diese Dateien */ }
[Fact] public void Preflight_warnt_bei_ColorID_ungleich_Auswahl_ohne_Ausschluss() { /* Excludes==false */ }
[Fact] public void Preflight_meldet_zu_wenig_Speicherplatz() { /* über austauschbaren Func<string,long> freeBytes-Parameter */ }
[Fact] public void Dateiliste_mit_Nichtbild_wird_gemeldet() { }

[Fact] public void Gesperrte_Datei_wird_berichtet_Lauf_geht_weiter()
{ /* Zielname exklusiv gesperrt: erste Datei Failed, zweite Completed; keine .tmp übrig */ }
[Fact] public void Abbruch_hinterlaesst_nichts_unter_Endnamen() { }
[Fact] public void Datentraeger_voll_stoppt_Lauf()
{ /* Sink-Fabrik austauschbar, wirft IOException mit HResult 0x80070070 (ERROR_DISK_FULL): StoppedDiskFull, keine .tmp, Rest in Skipped */ }
```

- [ ] **Step 2: Tests laufen lassen, Fehlschlag prüfen**: FAIL.

- [ ] **Step 3: `FrameCorrector` und `OutputNaming` umsetzen**

Vorab eine Filterzuordnung für 2×2-Positionen bilden, nicht `FilterAt` je Pixel. Der Zufallsgenerator läuft sequenziell (`new Random(Seed + Dateiindex)`), damit der Lauf reproduzierbar bleibt.

- [ ] **Step 4: `Preflight` und `SeriesRunner` umsetzen**

Ziel ist `<Endname>.tmp`, am Ende `File.Move`. Ein Parameter `Func<IFrameSource,string,IFrameSink>` mit Standard `ImageFiles.CreateSink` macht den Vollfehler testbar. Vorabprüfung öffnet nur die Köpfe. Tabellen aus `BuildConfirmedTables(info.WhiteLevel)`, je Datei neu. Ein Lauf hat nur einen Dateityp (SER, TIFF, PNG oder FITS) und eine Bittiefe; maßgeblich ist die erste Datei der Liste.

- [ ] **Step 5: Tests laufen lassen**: PASS.

- [ ] **Step 6: Commit** `feat: Serienlauf mit Vorabprüfung, sicheren Dateinamen und Abbruch`

### Task 9: Histogramm und Anzeige

Kernteil von Fehler 14.

**Files:**
- Create: `src/AntiBayer.Core/Histogram.cs`, `src/AntiBayer.Core/DisplayMapping.cs`
- Test: `tests/AntiBayer.Tests/HistogramTests.cs`, `tests/AntiBayer.Tests/DisplayMappingTests.cs`

**Interfaces:**
- Consumes: `Bayer`, `CurveTable`
- Produces:
  - `sealed class Histogram { static Histogram Compute(ushort[] frame, int w, int h, BayerPattern p, int bitDepth); long[] Of(BayerFilter f); long Max(BayerFilter f); long MaxAll; Histogram Transform(IReadOnlyDictionary<BayerFilter, CurveTable> tables); static int BarHeight(long count, long max, int pixels); }` (immer 256 Klassen, 16 Bit: Wert / 256)
  - `static byte[] DisplayMapping.To8Bit(ReadOnlySpan<ushort> frame)` (normiert auf Frame-Maximum; Frame nur 0 → alles 0; 8-Bit-Daten mit Maximum 255 werden unverändert ausgegeben)

- [ ] **Step 1: Tests schreiben**

```csharp
[Fact] public void Zaehlt_je_Filterposition_getrennt() { /* G1 und G2 getrennt, Summe = Pixelzahl */ }
[Fact] public void Sechzehn_Bit_faellt_in_256_Klassen() { /* 65520 → Klasse 255 */ }
[Fact] public void Transform_verschiebt_Werte() { /* Identität gleich; Punkt (100,150) verschiebt Klasse */ }
[Fact] public void Balkenhoehe_kleines_Maximum_ohne_Division_durch_null()
{ /* BarHeight(5,10,200)==100; BarHeight(0,0,200)==0; max kleiner als pixels funktioniert */ }
[Fact] public void Anzeige_normiert_auf_Maximum() { /* 12-Bit-Werte: max → 255 */ }
[Fact] public void Anzeige_aendert_Quelldaten_nicht() { }
```

- [ ] **Step 2: Tests laufen lassen, Fehlschlag prüfen**: FAIL.
- [ ] **Step 3: Umsetzen.**
- [ ] **Step 4: Tests laufen lassen**: PASS.
- [ ] **Step 5: Commit** `feat: Histogramm und 8-Bit-Abbildung für die Anzeige`

### Task 10: Oberfläche auf den Kern umstellen

Behebt Fehler 2, 4, 13, 15, 18 aus Sicht der Oberfläche. Die alten Klassen entfallen.

**Files:**
- Modify: `src/AntiBayer.App/MainForm.cs`, `MainForm.Designer.cs`
- Create: `src/AntiBayer.App/FrameBitmap.cs`, `src/AntiBayer.Core/Variances.cs`
- Test: `tests/AntiBayer.Tests/VariancesTests.cs` (gleichmäßiges Bild ergibt 0, Schachbrett ergibt die berechnete Varianz, Bild unter 3 Pixel Kantenlänge wirft nicht)
- Delete: `Classes.cs`, `ConversionParameters.cs`, `ImageFunctions.cs`, `Interfaces/IImaging.cs`, `Interfaces/IImagingdotNet.cs`, `DebugTools.cs`, `ImageControls.cs` (nur wenn `InterpolationMode` als schlanke Ableitung ohne `try/catch` in `FrameBitmap.cs` wandert)

**Interfaces:**
- Consumes: alle Kern-Typen aus Task 2–9
- Produces: `static Bitmap FrameBitmap.ToBitmap(byte[] pixels, int width, int height)` (24 bpp, `LockBits`, jeder Aufruf erzeugt eine neue Bitmap, Aufrufer gibt die alte frei); `MainForm`-Zustand: `IFrameSource? source`, `ParameterSet set`, `int frameIndex`, `ushort[] rawFrame`

- [ ] **Step 1: Ladeweg vereinheitlichen**

`LoadFile(string path)` wird von „load image“ und Drag & Drop genutzt. Bei `UnsupportedImageException` Meldung, alter Zustand bleibt. Bei vorhandenen bestätigten Stützpunkten fragt `LoadFile` „Keep curves?“; bei `CompatibleWith == BitDepthMismatch` oder `CurveCountMismatch` wird ein neuer leerer Satz angelegt und das gemeldet. Das Muster aus `SuggestedPattern` setzen; `Info.Warning` in die Statuszeile.

- [ ] **Step 2: Muster-Auswahl**

Radiobutton `radioButtonBayerGRGB` wird `radioButtonBayerGRBG` mit Text `GRBG`. `GetBayerType` kennt alle fünf. Wechsel von oder nach None mit bestätigten Punkten fragt nach und ruft `set.ChangePattern`; sonst Hinweis in der Statuszeile. Bei None: rote Zeile aktiv, G1/G2/B gesperrt, Label „All“.

- [ ] **Step 3: Frame-Auswahl und Bytes tauschen**

`TrackBar` und `NumericUpDown` für den Frame (nur sichtbar, wenn `FrameCount > 1`), Kontrollkästchen „Swap bytes“ (nur bei 16-Bit-SER, öffnet die Quelle neu). Beim Frame-Wechsel werden Übersicht und Vorschau neu berechnet.

- [ ] **Step 4: Vorschau und Stützpunkte**

Übersicht zeigt `DisplayMapping.To8Bit(rawFrame)`. Klick: `PreviewGeometry.Compute`. Vorschau mit `BuildPreviewTables`, Bitmaps im Besitz eines Feldes, alte erst nach Zuweisung freigeben. „Source“ zeigt den unkorrigierten Ausschnitt. Mark-Tasten R/G1/G2/B färben die Position über den Rohausschnitt ein, wie zuvor. Klick in die Vorschau: `NeighbourhoodMean` füllt die Felder. Wertänderung ruft `SetPreview`. `buttonSetStrongPoint*` ruft `SetConfirmed`, Löschen `RemoveConfirmed`; nach beiden `ClearPreview` und Vorschau neu. Liste zeigt bestätigte und Vorschaupunkte (●). `NumericUpDown.Maximum` auf `WhiteLevel` setzen. Nicht-monotone Kurven erzeugen Hinweis in der Statuszeile; `Filter=RGB` erzeugt den Hinweis „may be a color capture“, ohne Sperre. Filter und Aufnahmedaten stehen in `textBoxImageInfo`, Varianzen entfallen dort nicht: `VarianceCalc` nach `Core` (`Variances.cs`) übernehmen, auf `ushort[]` umstellen, je Position ohne Ausschnitts-Bitmap.

- [ ] **Step 5: Bauen, Tests, Handprüfung**

Run: `dotnet build C:/Claude/antibeyer/AntiBayer.sln` und `dotnet test C:/Claude/antibeyer/AntiBayer.sln`
Expected: 0 Fehler, alle Tests grün.

Handprüfung mit `C:\Firecapture\Mars\170225\2025-02-17-1833_6-U-RGB-Mars.ser`: lädt, Muster steht auf GBRG, Bild sieht richtig aus, RGB-Hinweis erscheint, Frame wechselbar, Fenster vergrößern plus Klick in die Ecken ohne Absturz, Stützpunkt setzen und löschen wirkt sofort, Drag & Drop lädt dasselbe.

- [ ] **Step 6: Commit** `feat: Oberfläche auf den neuen Kern umgestellt`

### Task 11: Serienlauf in der Oberfläche, danach Kontrollpunkt

**Files:**
- Create: `src/AntiBayer.App/SeriesForm.cs`, `SeriesForm.Designer.cs`
- Modify: `src/AntiBayer.App/MainForm.cs` (`buttonConvertFolder`, Text wird „convert series“)

**Interfaces:**
- Consumes: `SeriesJob`, `Preflight.Check`, `SeriesRunner.Run`, `SeriesReport`

- [ ] **Step 1: `SeriesForm` umsetzen**

Vorausgewählt ist die geladene Datei, weitere per Dateiauswahl (Filter aus unterstützten Endungen). Ausgabeordner vorbelegt mit `<Quelle>\AntiBayer`, Präfix und Suffix aus den bestehenden Feldern. Vorabprüfung beim Öffnen und nach jeder Änderung; Probleme als Liste, „Start“ läuft mit `Runnable`. Lauf in `Task.Run` mit `IProgress`, Fortschrittsbalken, „Cancel“. Ende: Bericht mit jeder fehlgeschlagenen und ausgelassenen Datei. Ohne geladenes Bild ist die Schaltfläche gesperrt.

- [ ] **Step 2: Bauen, Tests, Handprüfung**

`...1833_6-U-RGB-Mars.ser` nach einem Scratchpad-Ausgabeordner konvertieren: Fortschritt läuft, Fenster bleibt bedienbar, Abbruch lässt keine Datei zurück; mit Identitätskurve sind die Ausgabe-Pixel gleich. Ausgabe öffnet sich wieder im Tool. Quelle unverändert.

- [ ] **Step 3: Commit** `feat: Serienlauf mit Vorabprüfung und Fortschritt in der Oberfläche`

- [ ] **Step 4: KONTROLLPUNKT: anhalten und den Nutzer ansprechen**

Der Nutzer sucht jetzt echte 462C-SER-Dateien aus seiner Sicherung heraus: Weißlicht-Sonne, NIR 680/742 nm, ggf. 8 Bit, je kurz, mit `.txt`; optional UV. Gemeinsam prüfen: Laden, Weißwert, Muster, Sichtbarkeit des Musters in der Vorschau, Korrektur, Ergebnis im Stacker. Erkenntnisse vor den restlichen Tasks einarbeiten. Nicht ohne Rückmeldung weitermachen.

### Task 11a: Kurven aus der Serie messen

Grund: Sichtprüfung vom 2026-11-07. Von Hand ist das Muster schwer zu entfernen. Es ist je Farbe stabil über Ort und Zeit, hängt aber von der Helligkeit ab. Der Nutzer hat den Knopf freigegeben; Nachjustieren von Hand muss möglich bleiben.

**Files:**
- Create: `src/AntiBayer.Core/CurveMeasurement.cs`, `tests/AntiBayer.Tests/CurveMeasurementTests.cs`
- Modify: `src/AntiBayer.Core/ParameterSet.cs`, `tests/AntiBayer.Tests/ParameterSetTests.cs`, `src/AntiBayer.App/MainForm.cs`, `src/AntiBayer.App/MainForm.Designer.cs`

**Interfaces:**
- Consumes: `IFrameSource` (`Info`, `ReadFrame(int, ushort[])`), `Bayer.FilterAt`, `CurveTable`, `ParameterSet`.
- Produces:
  - `public sealed record MeasuredCurves(IReadOnlyDictionary<BayerFilter, IReadOnlyList<CurvePoint>> Points, int FramesUsed, int CellsUsed);`
  - `public static class CurveMeasurement { public const int MaxFrames = 40; public const int Bins = 12; public const int MinCellsPerPoint = 100; public static MeasuredCurves Measure(IFrameSource source, BayerPattern pattern); }`
  - `ParameterSet.ReplaceConfirmed(IReadOnlyDictionary<BayerFilter, IReadOnlyList<CurvePoint>> points)`: verwirft alle bestätigten Punkte und die Vorschau, setzt dann die übergebenen.

**Verfahren `Measure` (so festgelegt, am 2026-11-07 an echten Dateien erprobt):**
1. `pattern == None` wirft `ArgumentException`.
2. `min(FrameCount, MaxFrames)` Frames gleichmäßig über die Serie: Index `i * FrameCount / anzahl`. Je Pixel mitteln (double); je Pixel merken, ob es in irgendeinem Frame `>= WhiteLevel` war.
3. Zellen: 2×2 ab geraden Koordinaten; eine ungerade letzte Zeile/Spalte entfällt. Eine Zelle mit einem solchen übersteuerten Pixel entfällt ganz. Referenz einer Zelle = Mittel ihrer vier gemittelten Pixel.
4. `Bins` gleich breite Klassen von minimaler bis maximaler Referenz der verbliebenen Zellen. Von dunkel nach hell Zellen sammeln; sobald die Sammlung `>= MinCellsPerPoint` Zellen hat, ergibt sie je Filter einen Punkt und beginnt neu. Eine zu kleine Restsammlung am hellen Ende entfällt. Alle Zellen in einer Klasse, wenn min == max.
5. Punkt je Filter: `Original = round(Mittel des Filters)`, `New = round(Mittel der Referenz)`. Verwerfen, wenn `Original == 0` oder `Original >= WhiteLevel`, wenn `Original` nicht größer ist als beim vorigen behaltenen Punkt dieses Filters oder wenn `New` kleiner ist als dort.
6. `CellsUsed` = verbliebene Zellen. Zu wenig Zellen ergibt leere Listen, keine Ausnahme.
Die Kurve läuft weiter durch (0,0) und (Weiß,Weiß), das erledigt `CurveTable.Build`.

Die Lesefehler der Quelle laufen durch; die Oberfläche fängt sie ab. Lesen geschieht synchron mit eigenem Puffer: `rawFrame` und der angezeigte Frame ändern sich nicht.

- [ ] **Step 1: Tests schreiben (fehlschlagend), mit einer kleinen `IFrameSource`-Attrappe im Testfile**

Mit deutschen Testnamen wie im Projekt:
- Bekannte nichtlineare Antwort je Filter (z. B. R = lum*1.08−0,0001·lum², B entsprechend tiefer, G1/G2 = lum) über einen Helligkeitsverlauf bis ca. 220 (8 Bit) bzw. 56000 (16 Bit): Nach `FrameCorrector` mit den gemessenen Punkten sinkt die mittlere Filterabweichung je Helligkeitsband auf unter 1 Stufe (8 Bit) bzw. unter 0,1 % (16 Bit), vorher war sie deutlich größer.
- 1000 Frames: genau 40 verschiedene Lesezugriffe, erster Index 0, letzter 975. 3 Frames: alle drei, `FramesUsed == 3`.
- Übersteuerte Zelle: Zellen mit R = Weißwert in nur einem Frame zählen nicht zu `CellsUsed` und verändern die Punkte nicht.
- Gleichmäßiges Bild: keine Ausnahme, höchstens ein Punkt je Filter. Bild mit weniger als 100 Zellen: leere Listen. Ungerade Breite und Höhe: läuft, `CellsUsed` passt.
- `None` wirft. Die Muster GBRG und RGGB ordnen die Punkte der richtigen Farbe zu.
- `ReplaceConfirmed`: alte Punkte und Vorschau sind weg, danach lässt sich ein Punkt per `SetConfirmed` überschreiben und `ConfirmedCurveValue` liefert den neuen Wert (Nachjustieren).

- [ ] **Step 2: Laufen lassen, Fehlschlag sehen**

`dotnet test tests/AntiBayer.Tests/AntiBayer.Tests.csproj`

- [ ] **Step 3: Kern umsetzen und Tests grün**

- [ ] **Step 4: Oberfläche**

- Knopf `buttonMeasureCurves`, Text „Measure curves“, neben „Delete selected values“ bei (715, 292), 143×23, gleiche Verankerung, vor dem Laden gesperrt. In `LoadFile` und `ApplyRowStates` schalten: nur bei geladener Datei und Muster ungleich None frei. Vorher prüfen, dass dort nichts anderes liegt; sonst einen freien Platz in dieser Zeile nehmen.
- `buttonMeasureCurves_Click` in `MainForm.cs`, der Reihe nach:
  - Bei `IsColorFilter(Capture.Filter)` Rückfrage auf Englisch: Das könne eine Farbaufnahme sein, echte Farbunterschiede gingen verloren; Nein bricht ab.
  - Bei `set.HasConfirmedPoints` Rückfrage „Replace the existing curve points?“; Nein bricht ab.
  - Mit Wartecursor messen. `IOException` führt zu einer Statusmeldung, Kurven bleiben unverändert.
  - Keine Punkte (alle Listen leer): Statusmeldung, Kurven bleiben unverändert.
  - Sonst `ReplaceConfirmed`, Status: Anzahl Frames und Punkte je Farbe, bei `FramesUsed < 10` zusätzlich, dass wenige Frames die Messung unsicherer machen. Dann `RefreshCurvesAndPreview`.
- Keine Fortschrittsanzeige, kein eigener Thread (40 Frames lesen dauert kurz).
- Nachjustieren: Vorhandene Wege bleiben (Punkt in der Liste wählen oder Vorschau anklicken, Wert ändern, Punkt setzen, in der Liste abhaken und löschen).
- Damit danach nichts springt: Methode `SyncNewFieldsToCurve()` setzt für jede aktive Zeile mit `OriginalValue > 0` das Feld `NewValue` ohne Ereignisse auf `set.ConfirmedCurveValue(...)`. Aufruf nach dem Messen und am Ende von `buttonDeleteStrongPoints_Click`, jeweils vor `RefreshCurvesAndPreview`.

- [ ] **Step 5: Prüfung an echten Dateien, nur lesend**

Wegwerf-Konsolenprojekt im Scratchpad (nicht ins Repo), das den Kern referenziert. Für `D:\DemoSER\2022-04-18-1021_9-U-L-Sun.ser` und `D:\DemoSER\2022-04-18-1008_0-U-L-Sun.ser`: messen, dann 12 andere Frames (Index = (2k+1)·n/24) mit `FrameCorrector` und Dither korrigieren. Ausgeben: Punkte je Farbe, und je Helligkeitsband (5 gleich volle Bänder über die hellsten 65 % der Zellen) die Abweichung je Filter in Prozent vom Zellmittel, vorher und nachher. Erwartung: im Hauptbereich der Scheibe unter 0,5 %, vorher bis ca. 8 % bei 8 Bit und 6 % bei 16 Bit. Werte in den Report übernehmen.

- [ ] **Step 6: Bauen, alle Tests, Commit**

`dotnet build` (0 Fehler, 0 Warnungen) und `dotnet test` (alle grün).

```bash
git add src tests
git commit -m "feat: Kurven aus der Serie messen"
```

### Task 11b: Im Ausschnitt messen und Wirkung am Pixel zeigen

Grund: Handtest vom 2026-11-07. Flecken sind zu klein für die Gesamtmessung, und beim Absenken ist die Wirkung auf das angesehene Pixel nicht erkennbar. Der Nutzer hat beides freigegeben.

**Files:**
- Modify: `src/AntiBayer.Core/CurveMeasurement.cs`, `src/AntiBayer.Core/ParameterSet.cs`, `src/AntiBayer.Core/FrameCorrector.cs`, `src/AntiBayer.Core/PreviewGeometry.cs`, `src/AntiBayer.App/MainForm.cs`, `src/AntiBayer.App/MainForm.Designer.cs`, zugehörige Testdateien

**Interfaces (Produces):**
- `CurveMeasurement.MeasureRegion(IFrameSource source, BayerPattern pattern, PixelRect region)` liefert `MeasuredCurves`; Konstanten `RegionBins = 6`, `MinSamplesPerPoint = 200`.
- `int ParameterSet.MergeMeasured(IReadOnlyDictionary<BayerFilter, IReadOnlyList<CurvePoint>> points)`: Rückgabe = Zahl ersetzter Punkte.
- `static int FrameCorrector.CountChanged(ReadOnlySpan<ushort> frame, int width, int height, BayerPattern pattern, BayerFilter filter, CurveTable before, CurveTable after)`.
- `static Rectangle`-frei im Kern: `PixelRect? PreviewGeometry.PixelToZoomedRect(int pixelX, int pixelY, int boxWidth, int boxHeight, int imageWidth, int imageHeight)`: Bildschirmrechteck des Pixels in der Box, Umkehrung von `ZoomedClickToPixel`.

**Verfahren `MeasureRegion` (am 2026-11-07 an echten Dateien erprobt):**
1. `pattern == None` wirft `ArgumentException`. Frames wie bei `Measure` (`min(FrameCount, MaxFrames)`, Index `i * FrameCount / anzahl`).
2. Zellen: alle vollständigen 2×2-Zellen ab geraden Koordinaten, die ganz in `region` liegen.
3. Je Frame und Zelle eine Stichprobe: `L` = Mittel der vier Pixel, `S` = Mittel von `L` über 3×3 Zellen um die Zelle, am Bildrand nur über vorhandene Zellen (Nachbarn außerhalb von `region` zählen mit). Eine Zelle mit einem Pixel `>= WhiteLevel` fehlt nur in diesem Frame, und sie zählt auch nicht in `S`.
4. `RegionBins` gleich breite Klassen von minimalem bis maximalem `S`. Von dunkel nach hell sammeln; sobald `>= MinSamplesPerPoint` Stichproben da sind, ein Punkt je Filter, dann neu sammeln; Rest am hellen Ende entfällt.
5. Punktwerte und Verwerfen wie bei `Measure` Schritt 5 (Hilfsmethode teilen statt kopieren). `CellsUsed` = Zahl der Stichproben. Zu wenig Stichproben ergeben leere Listen.

**`MergeMeasured`:** Je Filter mit mindestens einem neuen Punkt: alle bestätigten Punkte mit `kleinstes neues Original <= Original <= größtes neues Original` entfernen, die neuen eintragen. Filter ohne neue Punkte bleiben. Vorschau leeren. Andere Punkte bleiben unverändert.

**`CountChanged`:** Zahl der Pixel dieser Filterposition, bei denen `before.Map` und `after.Map` verschieden sind.

- [ ] **Step 1: Tests schreiben (fehlschlagend)**

Mit deutschen Testnamen:
- `MeasureRegion`: Ausschnitt mit bekannter Antwort je Filter; helle Bildteile außerhalb und übersteuerte Pixel außerhalb ändern die Punkte nicht. Im Ausschnitt sind die Punkte in ±1 Stufe (8 Bit) der bekannten Antwort.
- Ein in einem Frame übersteuertes Pixel im Ausschnitt nimmt nur diese Zelle in diesem Frame heraus: `CellsUsed` ist genau um die betroffene Zahl kleiner.
- Zu kleiner Ausschnitt ergibt leere Listen, `None` wirft, ungerade Breite des Ausschnitts nimmt nur ganze Zellen.
- Frames wandern: Ein dunkler Fleck, der von Frame zu Frame um 1 Zelle versetzt ist, wird trotzdem gemessen und wirft nicht.
- `MergeMeasured`: Punkte außerhalb des neuen Bereichs bleiben, innerhalb werden ersetzt, Rückgabe stimmt; Filter ohne neue Punkte bleiben; Vorschau ist leer; danach ist Überschreiben per `SetConfirmed` möglich.
- `CountChanged`: zählt nur die genannte Filterposition, 0 bei gleichen Tabellen.
- `PixelToZoomedRect`: Für mehrere Boxgrößen liefert `ZoomedClickToPixel` an der Rechteckmitte wieder dasselbe Pixel; das erste und letzte Pixel liegen in der gezeichneten Fläche.

- [ ] **Step 2: Fehlschlag sehen, Kern umsetzen, Tests grün**

`dotnet test C:/Claude/antibeyer/AntiBayer.sln`

- [ ] **Step 3: Oberfläche**

**Platz:** `listViewStrongPointList` wird nach (566, 347), Größe (532, 318) verschoben. Knopf `buttonMeasureHere`, Text „Measure here“, bei (711, 319), 92×23, gleiche Verankerung wie `buttonMeasureCurves`. Überschneidet das etwas Sichtbares, einen freien Platz in dieser Zeile nehmen und im Report nennen.

**Freischalten:** nur wenn eine Datei geladen ist, das Muster nicht None ist und ein Vorschau-Ausschnitt besteht. `ApplyRowStates` und das Setzen von `previewRect` schalten mit.

**`buttonMeasureHere_Click`:** Wartecursor, `MeasureRegion` auf den aktuellen Ausschnitt. `IOException` führt zu einer Statusmeldung, Kurven bleiben. Keine Punkte: Statusmeldung, Kurven bleiben. Sonst `MergeMeasured`, `SyncNewFieldsToCurve`, Status: Punkte je Farbe und Zahl ersetzter Punkte; bei `set.HasConfirmedPoints == false` vor dem Messen zusätzlich der Hinweis, dass zuerst „Measure curves“ sinnvoll ist. Dann `RefreshCurvesAndPreview`. Keine Rückfrage: Nur der Helligkeitsbereich des Ausschnitts ändert sich.

**Rahmen:** Feld `selectedPixel` (Bildkoordinaten, nullable). Klick in die Vorschau setzt es. Leeren beim Laden, beim Klick in die Übersicht und wenn der Ausschnitt nach Größenänderung das Pixel nicht mehr enthält. Paint-Handler auf `_pbPreview` zeichnet mit `PixelToZoomedRect` einen 1 Pixel breiten gelben Rahmen um dieses Pixel, auch während ein Mark-Knopf gehalten wird. Nach dem Setzen und Leeren `_pbPreview.Invalidate()`.

**Statusanzeige:** Eigener Teil `pixelInfo` in `RefreshStatus`, unabhängig von `statusMessage`. `RefreshCurvesAndPreview` setzt ihn bei `selectedPixel != null` auf
`Pixel (x,y) R: 165 -> 150 | 1234 pixels change`
Filter und Rohwert aus `rawFrame`, Zielwert aus der Vorschautabelle dieses Filters, also auch beim Bildwechsel aktuell. Der Teil „pixels change“ erscheint nur, wenn für diesen Filter ein Vorschaupunkt besteht, und zählt mit `CountChanged` zwischen bestätigter und Vorschautabelle über das ganze Bild. Bei None heißt der Filter „All“.

- [ ] **Step 4: Prüfung an echten Dateien, nur lesend**

Wegwerf-Konsolenprojekt im Scratchpad, nicht ins Repo. Für `D:\DemoSER\2022-04-18-1021_9-U-L-Sun.ser` und `D:\DemoSER\2022-04-18-1008_0-U-L-Sun.ser`: `Measure`, dann `MeasureRegion` auf einen Ausschnitt von 68×56 Pixeln um den dunkelsten Bereich, der mindestens 48 Pixel vom Scheibenrand entfernt liegt, dann `MergeMeasured`. Auf 40 anderen Frames ausgeben, Abweichung je Filter in Prozent vom Zellmittel für: (a) Fleckzellen im Ausschnitt (eigene geglättete Helligkeit im dunkelsten Bereich), (b) die Fläche (Helligkeit über dem Median innerhalb der Scheibe), jeweils vor und nach `MergeMeasured`. Erwartung: (a) wird deutlich kleiner, (b) ändert sich um höchstens 0,05 Prozentpunkte. Trifft das nicht zu, Verfahren nicht selbst verändern, sondern Zahlen melden.

- [ ] **Step 5: Bauen, alle Tests, Commit**

`dotnet build` (0 Fehler, 0 Warnungen), `dotnet test` (alle grün).

```bash
git add src tests
git commit -m "feat: Im Ausschnitt messen und Wirkung am Pixel zeigen"
```

### Task 12: Restliche Bedienung

Behebt Fehler 10, 11, 12 und den Oberflächenteil von 14; bindet `.cps` an.

**Files:**
- Create: `src/AntiBayer.Core/AppConfig.cs`, `src/AntiBayer.App/CaptureInfoForm.cs`
- Modify: `src/AntiBayer.App/MainForm.cs`
- Test: `tests/AntiBayer.Tests/AppConfigTests.cs`

**Interfaces:**
- Consumes: `CpsFile`, `Histogram`, `CaptureInfo.DifferencesTo`, `ImageFiles.CreateSink`, `FrameCorrector`
- Produces: `sealed class AppConfig { string? LastFolder; BayerPattern LastPattern; static AppConfig Load(string path); void Save(string path); static string DefaultPath => %AppData%\AntiBayer\AntiBayer.config; }`

- [ ] **Step 1: Tests für `AppConfig` schreiben**

```csharp
[Fact] public void Fehlende_Datei_liefert_Standard_statt_null() { }
[Fact] public void Kaputte_Datei_liefert_Standard() { }
[Fact] public void Rundgang_legt_Ordner_an() { }
```

- [ ] **Step 2: Tests laufen lassen, Fehlschlag prüfen**: FAIL.
- [ ] **Step 3: `AppConfig` umsetzen und in `MainForm` einbinden**; Speichern einmal beim Schließen, auch ohne geladenes Bild; `LastFolder` als Startordner der Dialoge.
- [ ] **Step 4: Tests laufen lassen**: PASS.

- [ ] **Step 5: „save image“ neu**

Einzelbild: Vollbild in gleichem Format und gleicher Bittiefe. SER: aktueller Frame als TIFF. Ohne Dither, nur bestätigte Punkte. Dialog mit gültigem Filter, `OverwritePrompt`, Ziel darf nicht die Quelldatei sein. Dazu `TiffFrameWriter.Write(string path, ReadOnlySpan<ushort> frame, int w, int h, int bitDepth)` in `Imaging/WicSink.cs` samt Test in `WicTests.cs`.

- [ ] **Step 6: `.cps` laden und speichern**

Beim Speichern `set.Capture` aus der aktuellen Datei mit Notiz aus `CaptureInfoForm` (Felder von Hand änderbar, vorbelegt). Beim Laden `CompatibleWith`: `BitDepthMismatch`/`CurveCountMismatch` → Meldung, Satz bleibt ungenutzt; `PatternDiffers` → ausdrückliche Bestätigung; `DifferencesTo` → Warnung mit Liste, Anwenden bleibt möglich. Fehler beim Lesen melden, kein leeres `catch`.

- [ ] **Step 7: Histogramme an den Kern binden**

Berechnet aus `rawFrame`. Korrigierte Hälfte über `Transform(BuildPreviewTables)`, Aktualisierung bei jeder Wertänderung. X-Position der korrigierten Hälfte `(256 + i) * scaleX + j + 15`. Höhe über `BarHeight`. Einzelhistogramme funktionieren nach jedem Ladeweg, G2 fließt ins Maximum ein.

- [ ] **Step 8: Bauen, Tests, Handprüfung**

Tool ohne Konfiguration starten und schließen: kein Fehler, Datei unter `%AppData%\AntiBayer`. „save image“ bei SER erzeugt TIFF gleicher Tiefe. `.cps` speichern und laden samt Warnung bei anderem Filter. Histogramme reagieren auf Eingaben. Alte `.cps` lädt.

- [ ] **Step 9: Commit** `feat: Speichern, Konfiguration, Parametersätze und Histogramme in der Oberfläche`

### Task 13: FITS

**Files:**
- Create: `src/AntiBayer.Core/Imaging/FitsFile.cs` (enthält `FitsSource`, `FitsSink`)
- Modify: `src/AntiBayer.Core/Imaging/ImageFiles.cs` (Endungen `.fit .fits .fts`)
- Test: `tests/AntiBayer.Tests/FitsTests.cs`

**Interfaces:**
- Consumes: `IFrameSource`, `IFrameSink`, `WhiteLevel`
- Produces: `FitsSource : IFrameSource`, `FitsSink : IFrameSink` über `ImageFiles`

- [ ] **Step 1: Tests schreiben**

```csharp
[Theory] /* BITPIX 8; BITPIX 16 mit BZERO=32768: Rundgang-Identität bytegleich, Werte 0 und 65535 korrekt */
[Fact] public void Header_bleibt_erhalten() { /* inkl. COMMENT/HISTORY und zusätzlicher HDU-Anteile nach den Daten */ }
[Fact] public void BITPIX_16_ohne_BZERO_mit_positiven_Werten_rundlauffaehig() { }
[Theory] /* NAXIS=3, BITPIX=-32, BITPIX=32 → UnsupportedImageException */
[Fact] public void Zeilenfolge_und_Padding_auf_2880_Byte_stimmen() { }
```

Pixel liegen in der FITS-Datei in Big Endian. Die Zeilenfolge bleibt wie gespeichert; `FilterAt` bezieht sich auf die Dateireihenfolge.

- [ ] **Step 2: Tests laufen lassen, Fehlschlag prüfen**: FAIL.
- [ ] **Step 3: Umsetzen.**
- [ ] **Step 4: Tests laufen lassen**: PASS.
- [ ] **Step 5: Commit** `feat: FITS in einfachem Umfang`

### Task 14: Abschluss

**Files:**
- Modify: `README.md`, `.gitignore`

- [ ] **Step 1: README schreiben**

Zweck (Monochrom-Auswertung der 462C, keine Farbaufnahmen), Ablauf, Formate, Bau: `dotnet build`, Weitergabe per `dotnet publish -c Release -r win-x64 --self-contained`. Lizenz AGPL.

- [ ] **Step 2: Abschlussprüfung**

Run: `dotnet test C:/Claude/antibeyer/AntiBayer.sln` und `grep -rIi beyer` über alle getrackten Dateien (`git -C C:/Claude/antibeyer grep -il beyer`).
Expected: alle Tests grün; Treffer nur in historischen Dokumenten, im Remote-Namen oder in Tests zum Altformat.

- [ ] **Step 3: Commit** `docs: README`

- [ ] **Step 4: Mit dem Nutzer klären, nichts eigenständig ausführen**

Umbenennung des lokalen Ordners `antibeyer` → `antibayer`, des GitHub-Repos `Okulyt/AntiBeyer`, Merge und Push. Das wirkt nach außen bzw. auf laufende Sitzungen.
