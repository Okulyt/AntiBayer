# AntiBayer

Removes the Bayer pattern from raw recordings of a colour camera when they are meant to be evaluated
as monochrome images, so that stacking no longer locks onto the pattern. Written for the QHY5III462C
with FireCapture, and usable for other colour sensors.

## When it helps

- White-light solar recordings through a foil filter.
- Near infrared with 680 nm and 742 nm filters, where the manual colour-gain balance is never perfect.

The tool is meant for stacks in greyscale. In AutoStakkert 4, one white-light series stacked in
greyscale kept a strong grid without the tool (Bayer frequency 2.16 on a 0-255 scale) and almost none
with the measured curves (0.20). Stacked in colour, debayering removed the pattern without the tool
too, and the curves mainly made the colours neutral.

From about 800 nm the 462C behaves like a monochrome sensor, and the tool is not needed there. UV and
CaK bandpass recordings may be beyond correction; treat them as an experiment.

**Do not use it on colour recordings.** It makes the colour channels match, so real colour
differences of the object are lost. The tool warns when the capture data says `Filter=RGB`.

## How it works

Each Bayer position gets its own tone curve: a piecewise-linear curve through support points, which
is fixed at the white level. It is fixed at black only as long as you have set no point of your own
at 0. Series output uses dithered rounding, so values between two
levels stay correct on average. Original files are only ever read.

## Workflow

1. Load an image or a series (button or drag and drop). Check the Bayer pattern and white level.
2. Click **Measure curves**. Up to 40 frames spread over the series give up to 12 support points per
   colour. Each colour is compared with the mean of its 2×2 cell.
3. Click into the overview to see a corrected crop in the preview.
4. Small dark areas such as sunspots hardly count in step 2. Click on one and use **Measure here** to
   replace the points in the brightness range of that crop.
5. Adjust by hand if needed. Click a pixel in the preview, which gets a yellow frame; the status line
   shows its value before and after the curve and how many pixels change with it. The magnifier below the
   preview shows the 3x3 Bayer cells around it without a frame, so you can judge the pixel against its
   neighbours while you change the value; small ticks outside the image point at it. Change the value and
   set the point, or tick points in the list and delete them.
6. **convert series** writes the corrected series, by default into a subfolder `AntiBayer` beside the
   source. **save values** keeps the curves in a `.cps` file together with the capture data.

Hold **RGB** to see the raw crop in colour, balanced so that a plain area looks grey. Colour fringes at
sunspots and the limb come from the optics and the atmosphere. They depend on the place, not on the
brightness, so tone curves cannot remove them.

Curves act on brightness, not on a place in the image: lowering one value lowers all pixels of that
colour at that brightness. Single pixels that stand out in one frame are mostly noise, which stacking
averages away. Judge the result in the stacker, not by single pixels.

## Formats

8 or 16 bit, kept as they are.

| Format | Notes |
|---|---|
| SER | The normal case. Header, frame count and timestamps stay unchanged. FireCapture's little-endian flag is unreliable, so little endian is assumed, and a swap option exists for 16 bit. |
| TIFF, PNG | Single images in greyscale. |
| FITS | Primary HDU, `NAXIS=2`, `BITPIX` 8 or 16. Other variants are refused with a message. |

Camera, exposure and gain stand in the SER header, but the FireCapture `.txt` beside it takes
precedence field by field; the header only fills gaps. Filter and colour gains come from the `.txt`.

## Known limits

- Red and blue can sit a fraction of a pixel off from green, and blue can be less sharp. Up to about
  0.8 pixels for red were measured on white-light solar series, with the direction differing between
  days, and almost none on a series that was probably taken through a red or NIR filter. Tone curves
  cannot correct this, so at steep edges a pixel can stand against the local gradient. A trial
  correction made no clear visible difference in a greyscale stack; the tool does not include one.
- Above the last and below the first support point the curve runs straight to the white level and to
  black.

## Build

Requires the .NET 10 SDK on Windows.

```
dotnet build
dotnet test
dotnet publish src/AntiBayer.App/AntiBayer.App.csproj -c Release -r win-x64 --self-contained
```

## License

AGPL, see `LICENSE.txt`.
