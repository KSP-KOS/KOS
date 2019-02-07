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
on some users' PCS).

DDS format is not as commonly supported by many editors like PNG
is, so you need to have the right editor to edit these files now.

You can edit these directly with Photoshop.

But if you don't have Photoshop, you can use the free program
"Gimp" to do it, but only if you install a particular plugin
for "Gimp".  (*natively*, Gimp doesn't do DDS files.)  See
the next section for how to install the DDS plugin for Gimp.

INSTALLING DDS For GIMP
-----------------------

1. Google for a plugin called "gimp-dds".  It should be distributed
as a ZIP file for your platform, with an executable file inside
the ZIP (i.e. "gimp-dds.exe" on windows).

2. Quit from Gimp.  Make sure the program isn't running.

3. Get that gimp-dds executable file, find where your GIMP software
is installed, then put the gimp-dds executable into this
directory inside your GIMP install:

```
    lib/gimp/2.0/plug-ins/
```

Note, if your GIMP is newer, that might be this instead:

```
    lib/gimp/3.0/plug-ins/
```

4. Re-run Gimp.

USING GIMP ON THESE DDS FILES
-----------------------------

Once you have the plugin, note that it has several export options
because there's more than one kind of DDS file.  Be sure to
use the following exact settings when you "export as" a DDS file
from GIMP:

Compression: "BC3 / DXT5"
Format: Default
Mipmaps: "No mipmaps"

Files are upside down on purpose!!!!
------------------------------------

Note that DDS format inverts the meaning of the Y-axis in the image.
The gimp-dds plugin does not account for this in its editing.
You need to draw your image *MIRRORED UPSIDE DOWN* in Gimp when
working in the DDS format.


