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
   colour. In each frame, every pixel is compared with the mean around it, which is centred on the pixel
   and holds all colours in their proper share. So the slope at the limb does not count as colour, and
   image drift does not blur the measurement.
3. Click into the overview to see a corrected crop in the preview.
4. Adjust by hand if needed. Click a pixel in the preview, which gets a yellow frame; the status line
   shows its value before and after the curve and how many pixels change with it. The magnifier below the
   preview shows the 3x3 Bayer cells around it without a frame, so you can judge the pixel against its
   neighbours while you change the value; small ticks outside the image point at it. Change the value and
   set the point, or tick points in the list and delete them.
5. For stacks in greyscale, click **Measure color shift** after the curves. If red or blue lie at least
   0.1 pixels off from green, you can tick the correction. It is always off until you tick it, and it is
   measured for one series, so measure again for another one. See "Color shift" below.
6. **convert series** writes the corrected series, by default into a subfolder `AntiBayer` beside the
   source. **save values** keeps the curves in a `.cps` file together with the capture data.

Hold **RGB** to see the raw crop in colour, balanced so that a plain area looks grey. Colour fringes at
sunspots and the limb come from the optics and the atmosphere. They depend on the place, not on the
brightness, so tone curves cannot remove them.

Curves act on brightness, not on a place in the image: lowering one value lowers all pixels of that
colour at that brightness. Single pixels that stand out in one frame are mostly noise, which stacking
averages away. Judge the result in the stacker, not by single pixels.

## Color shift

At steep edges such as sunspots, red and blue can lie a fraction of a pixel off from green. Up to about
0.9 pixels were measured on white-light solar series and up to about 0.8 on planets in colour, with the
direction differing between series. Series through a narrow infrared filter showed at most 0.05 pixels.

In a greyscale stack this leaves a local grid at those edges, and the curves cannot remove it. On one
white-light series in AutoStakkert 4, stacked in greyscale, the grid around the worst sunspot fell from 6.1
to 0.8 with this correction (scale 0-255, sharpened stack), and the image did not move by a whole pixel.

After measuring, hold **RGB corrected** to see the crop in colour with the shift taken out, and **RGB** to
see it without. This works whether the box is ticked or not, and shows only the shift, without the curves.
It is a single frame, so the part that changes from frame to frame with the seeing still shows.

Use it for greyscale stacks only. In a colour stack, debayering removes the pattern and RGB alignment in
the stacker corrects the shift. Green is never changed. The correction takes out the mean shift of the
series; the part that changes from frame to frame with the seeing stays.

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
  cannot correct this; see "Color shift". A difference in sharpness is not corrected.
- The colour balance can drift slightly across the field, by about 0.1 steps per 100 pixels on a 0-255
  scale. It is not corrected, because it left no clear grid in the tested stacks.
- Clouds passing during a recording change the surface brightness from frame to frame, by up to about
  25 % on the tested series. A dimmed part of the surface can then have the same grey value as penumbra in
  a bright frame, but a different colour, and the single curve per filter only reaches the mean of both.
  On that series red and blue stayed up to about 2.5 steps (scale 0-255) off green at 70-85 % of the
  surface brightness in bright frames, which shows as a fine grid in the penumbra after strong sharpening.
  If enough good frames remain, cut the cloudy part from the series before using AntiBayer.
- Above the last and below the first support point the curve runs straight to the white level and to
  black.

## Build

Requires the .NET 10 SDK on Windows.

```
dotnet build
dotnet test
dotnet publish src/AntiBayer.App/AntiBayer.App.csproj -c Release -r win-x64 --self-contained
```

## Background

The original motivation and idea, from April 2023. The operating details of that time are replaced by
the sections above.

Using a so called one shot camera for astronomy imaging I often encountered image artefacts from the bayer matrix of the chip during image processing. During unsharp masking these artefacts could even become dominant over the actual image. They are caused when during stacking the stacking algorithm snaps in on bayer artefacts rather than real image detail.

This is not only limited to largely monochrome appearing images like sun spot or moon images. It also happened processing Jupiters images etc. Additionally there are new sensors on the market like IMX 462 or IMX 678 that are very responsive in NIR. While the bayer matrix becomes (almost!) invisible around 850nm allowing full resolution CH4 Methane images it ist still very prominent when using IR742 or IR680 Filters. This can only partially be biased with the RGB Gain settings of the chip. Even seperating RGB channels doesn't work as I found that the two green pixels of the matrix behave differently.

So my idea was to develop a program that can apply correction curves to each bayer channel. A first approch was very simple: I used a scaling factor and an offset value for each of the four(!) channels. But I found that this would work only for a small range of gray values while brighter or darker parts of the image got worse artefacts.

Not knowing exactly how a perfect correction would look like I decided to program a linear interpolation between an (almost) unlimited number of strong points. The idea was to pick a point from an image of the series and provide the correct value, then repeat this for another point of the image (with a different gray value). Of course for each bayer channel seperately. Using 256 strongpoints for an 8 bit image will simply nail the curve to the strongpoints as there's no room for interpolation.

Sven Wienstein, 17 April 2023

## License

AGPL, see `LICENSE.txt`.
