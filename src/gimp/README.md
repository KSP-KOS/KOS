XCF files (Gimp)
----------------

The .xcf files here are in GIMP format.  They contain the full
layer information so you can edit the images easier.  The PNG
files they create are the result of exporting from GIMP.

As such, the .xcf files were added to GIT because they are,
essentially, the "source code" for the images.  However, they
don't need to be put into exported ZIP files for installing.

### terminal-icon.xcf and network-zigzag.xcf

These file is used to create:

* terminal-icon-open.png
  * Make the layer "Ready?" and the layer "Highlight" visible.
  * Temporarily resize to width 32.  Do not save the resized small version.
  * export to PNG
* terminal-icon-closed.png
  * Make the layer "Ready?" and the layer "Highlight" invisible.
  * Temporarily resize to width 32.  Do not save the resized small version.
  * export to PNG
* terminal-icon-open-with-zigzag.png
  * This is just the merger of terminal-icon-open and network-zigzag together.  Unity is not good at overlaying two PNG's into one Texture2D so instead I just did that work external to the code and made it a separate file.
* terminal-icon-closed-with-zigzag.png
  * This is just the merger of terminal-closed-open and network-zigzag together.  Unity is not good at overlaying two PNG's into one Texture2D so instead I just did that work external to the code and made it a separate file.


