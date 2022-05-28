To Edit the DDS image files in this folder
==========================================

We had to switch these GUI icons to DDS files instead of PNG
files (which they were before) because of how Kerbal loads
mod's files at launch time.  It tries to convert all images
into the DDS format upon loading them, and that conversion
from PNG to DDS can fail on some PCs depending on the D3D driver
and GPU on the PC that's doing it.  By having them in DDS
format already *ourselves* prior to release, we cause Kerbal
Space Program to bypass its conversion step (which sometimes fails
on some users' computers).

DDS format is not as commonly supported by many editors like PNG
is, so you need to have the right editor to edit these files now.

Use Photoshop or Gimp
:::::::::::::::::::::

You can edit DDS files directly with Photoshop, but Photoshop is
a professional (i.e. expensive) tool.  If that's outside your
budget, you can also use the free tool "Gimp", however it will
requires a plugin to be able to read DDS files.  (There is a
legal requirement that reading/writing DDS files be kept
closed-source because it uses a proprietary compression.  That's
why Gimp has to use a plugin for it, so it doesn't intermingle with
the rest of Gimp's open source code.)

INSTALLING DDS plugin For GIMP
------------------------------

1. Search the interwebs for a plugin called "gimp-dds".  It should be
distributed as a ZIP file for your platform, with an executable file
inside the ZIP (i.e. "gimp-dds.exe" on Windows).

2. Quit from Gimp.  Make sure the program isn't running.

3. Get that gimp-dds executable file, find where your GIMP software
is installed, then put the gimp-dds executable into this
directory inside your GIMP install:

    lib/gimp/2.0/plug-ins/

Note, if your GIMP is newer, that might be this instead:

    lib/gimp/3.0/plug-ins/

4. Re-run Gimp.

USING GIMP ON THESE DDS FILES
-----------------------------

Once you have the plugin, note that it has several export options
because there's more than one kind of DDS file.  Be sure to
use the following exact settings when you "export as" a DDS file
from GIMP:

Compression: "BC3 / DXT5"
Format: Default
Mipmaps: Your choice here varies:
  - If this image is for the purpose of painting GUI 2D icons, then pick "no mipmaps".
  - If this image is for the purpose of painting a decal over a 3D mesh, then you should
    generate mimmaps here.

As usual when saving image formats in Gimp, to save a file as DDS,
use "File" -> "Export As", then the filename extension (".dds")
should tell Gimp your desire to save it in that format.

Do not use "SAVE" or "SAVE AS", as those only save to Gimp's own XCF
format.  To save to anything other than XCF, use "export".  Note that
when you use "export", Gimp will pretend that you haven't saved the file,
warning you "are you sure you want to quit without saving?" when you
try to leave Gimp.  This is normal for Gimp because it considers
any format other than its own XCF to be a "lossy" way to save the file,
so it doesn't count as really saving all the information Gimp might
know about the image.  (For our purposes, you don't need to save the
file as XCF - you can ignore that warning.)


Files are upside down on purpose!!!!
------------------------------------

Note that DDS format inverts the meaning of the Y-axis in the image.
The gimp-dds plugin does not account for this in its editing.
You need to draw your image *VERTICALLY FLIPPED UPSIDE DOWN* in
Gimp when working in the DDS format.


