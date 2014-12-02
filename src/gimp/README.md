XCF files (Gimp)
----------------

The .xcf files here are in GIMP format.  They contain the full
layer information so you can edit the images easier.  The PNG
files they create are the result of exporting from GIMP.

As such, the .xcf files were added to GIT because they are,
essentially, the "source code" for the images.  However, they
don't need to be put into exported ZIP files for installing.

### terminal-icon.xcf

This file is used to create:

* terminal-icon-open.png
  * Make the layer "Ready?" and the layer "Highlight" visible.
  * Temporarily resize to width 32.  Do not save the resized small version.
  * export to PNG
* terminal-icon-closed.png
  * Make the layer "Ready?" and the layer "Highlight" invisible.
  * Temporarily resize to width 32.  Do not save the resized small version.
  * export to PNG


(As of this writing there is only one .xcf file)

