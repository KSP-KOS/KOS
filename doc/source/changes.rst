.. _changes:

Changes from version to version
===============================

This is a slightly more verbose version of the new features
mentioned in the CHANGELOG, specifically for new features and for
users familiar with older versions of the documentation who want
only a quick update to the docs without reading the entire set
of documentation again from scratch.

.. contents::
    :local:
    :depth: 3

Changes in 0.17.3
-----------------

Deprecated INCOMMRANGE
::::::::::::::::::::::::::

Reading from the INCOMMRANGE suffix will now throw a deprecation exception with instructions to use the new Addons methods.

Updated boot file name handling
::::::::::::::::::::::::::

Boot files are now copied to the local hard disk using their original file name.  This allows for uniform file name access either on the archive or local drive and fixes boot files not working when kOS is configured to start on the Archive.  You can also get or set the boot file using the CORE:BOOTFILENAME suffix.

Updated thrust calculations for 1.0.x
::::::::::::::::::::::::::::::

We fixed the existing suffixes of MAXTHRUST and AVAILABLETHRUST for engines and vessels to account for the new changes in thrust based on ISP at different altitudes.  The AVAILABLETHRUST suffix is now implemented for engines (it was previously only available on vessels).  There are also new suffixes MAXTHRUSTAT (engines and vessels), AVAILABLETHRUSTAT (engines and vessels), and ISPAT (engines only) to read the applicable value at a given atmospheric pressure.

New CORE struct
::::::::::::::::::::::::::::::

The CORE struct can be used to access properties of the current processor, including it's associated part and vessel, as well as the currently selected volume.  Moving forward this will be the struct where we enable features that interact with the processor itself, like local configuration or current operational status.

Docking port, element, and vessel references
::::::::::::::::::::::::::

You can now get a list of docking ports on any element or vessel using the DOCKINGPORTS suffix.  Vessels also expose a list of their elements (the ELEMENTS suffix) and an element will refernce it's parent vessel (the VESSEL suffix).
