# AntiBayer

Motivation

Using a so called one shot camera for astronomy imaging I often encountered image artefacts from the bayer matrix of the chip during image processing. During unsharp masking these artefacts could even become dominant over the actual image. They are caused when during stacking the stacking algorithm snaps in on bayer artefacts rather than real image detail.
This is not only limited to largely monochrome appearing images like sun spot or moon images. It also happened processing Jupiters images etc. Additionally there are new sensors on the market like IMX 462 or IMX 678 that are very responsive in NIR. While the bayer matrix becomes (almost!) invisible around 850nm allowing full resolution CH4 Methane images it ist still very prominent when using IR742 or IR680 Filters. This can only partially be biased with the RGB Gain settings of the chip. Even seperating RGB channels doesn't work as I found that the two green pixels of the matrix behave differently.

The program

So my idea was to develop a program that can apply correction curves to each bayer channel. A first approch was very simple: I used a scaling factor and an offset value for each of the four(!) channels. But I found that this would work only for a small range of gray values while brighter or darker parts of the image got worse artefacts.

Not knowing exactly how a perfect correction would look like I decided to program a linear interpolation between an (almost) unlimited number of strong points. The idea was to pick a point from an image of the series and provide the correct value, then repeat this for another point of the image (with a different gray value). Of course for each bayer channel seperately. Using 256 strongpoints for an 8 bit image will simply nail the curve to the strongpoints as there's no room for interpolation.

The code is quite experimental and not meant to teach up to date programming techniques. Currently this is kind of an engineering sample that can maybe convince developers of stacking software to implement similar image corrections.
The GUI has been designed this way and that way. I'm not even sure about the best user interface. As I am still experimenting on what a perfect correction curve for my camera would look like I still don't know if there  an algorithm can be set up to calculate a perfect or near perfect setting e.g. by detecting or minimizing certain texture parameters. FFT would definately provide strong indications for bayer artefacts.

As some kind of a neat trick I prepared some randomization to kind of smuggle interpolated decimal values into the batch conversion. Say an interpolation would convert a gray value of 100 to 101.5 then I'd randomly add 1 to the target grayscale of 101 by a chance of 0.5 (50 percent). Doing this randomly should hit good frames just as likely as bad frames (that would be excluded during stacking).

Currently the program implements:
- loading an image in a format that .NET can handle
- clicking a point of the preview will show a 5x zoom around the clicking point in the zoom window
- clicking a pixel in the zoom window will highlight the respective bayer channel, show the original gray value und will allow to set a new value while displaying the resulting change in the zoom window.
- Clicking and holding the buttons R,G1,G2,B next to the "new value" column can be used to mark the pixels of the same channel in the zoom window. That way you can identify pixels you want to correct.
- Clicking the right column of buttons R G1, G2 and B under the "set Strong-point" label will set a Strongpoint for the selected channel or using buttn RGGB for all four channels with the current values.
- Current strongpoints will be shown in a list below the preview. Checking one or more strongpoints and clicking "Delete selected values" will remove them from the list.
- Selecting a strongpoint will put the values into the respective fields above to allow to correct the value.
- "save values" and "load values" will save or load a set of strong points. My idea to use the dictionary structure for the conversion parameters backfired here as it prevented me from using xml serialization. I didn't try jsonconvert so far but it would be a favorable, more human readable format.

The buttons "save image" and "convert folder" have outdated code currently. They have to be re-implemented.
The fields "Filename prefix" and "Filename suffix" are meant to change filenames in a batch conversion.
The Bayer Matrix won't be detected. The correct pattern must be set with the radio buttons. Should be RGGB for most chips on the market.

I'm currently planning to put some more work into it before really going public. Of course the repository is public and if you already found it - then that's OK!

Sven Wienstein
2023 April 17
