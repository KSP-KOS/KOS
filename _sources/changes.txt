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

Reading from the INCOMMRANGE bound variable will now throw a
deprecation exception with instructions to use the new
:struct:`RTAddon` structure for the RT mod.

Updated thrust calculations for 1.0.x
:::::::::::::::::::::::::::::::::::::

KSP 1.0 caused the thrust calculations to become a LOT more
complex than they used to be and kOS hadn't caught up yet.
For a lot of scripts, trying to figure out a good throttle
setting is no longer a matter of just taking a fraction of the
engine's MAXTHRUST.

We fixed the existing suffixes of MAXTHRUST and AVAILABLETHRUST for
:struct:`engine` and :struct:`vessel` to account for the new changes
in thrust based on
ISP at different altitudes.  MAXTHRUST is now the max the engine can
put out at the CURRENT atmospheric pressure and current velocity.
It might not be the maximum it could put out under other conditions.
The AVAILABLETHRUST suffix is now implemented for engines (it was
previously only available on vessels).  There are also new
suffixes MAXTHRUSTAT (engines and vessels), AVAILABLETHRUSTAT
(engines and vessels), and ISPAT (engines only) to
read the applicable value at a given atmospheric pressure.

New CORE struct
:::::::::::::::

The :struct:`CORE` bound variable gives you a structure you can use
to access properties of the current in-game CPU the script is running on,
including the vessel part it's inside of, and the vessel it's inside
of, as well as the currently selected volume.  Moving forward this
will be the struct where we enable features that interact with
the processor itself, like local configuration or current
operational status.

Updated boot file name handling
:::::::::::::::::::::::::::::::

Boot files are now copied to the local hard disk using their original
file name.  This allows for uniform file name access either on the
archive or local drive and fixes boot files not working when kOS is
configured to start on the Archive.  You can also get or set the boot
file using the BOOTFILENAME suffix of the :struct:`CORE` bound variable.

Docking port, element, and vessel references
::::::::::::::::::::::::::::::::::::::::::::

You can now get a list of docking ports on any element or vessel using
the DOCKINGPORTS suffix.  Vessels also expose a list of their elements
(the ELEMENTS suffix) and an element will refernce it's parent vessel
(the VESSEL suffix).

****

Changes in 0.17.0
-----------------

Variables can now be local
::::::::::::::::::::::::::

Previously, the kOS runtime had a serious limitation in which
it could only support one flat namespace of global-only variables.
Considerable archetecture re-work has been done to now support
:ref:`block-scoping <scope>` in the underlying runtime, which can
be controlled through the use of :ref:`local declarations <declare syntax>`
in your kerboscript files.

Kerboscript has User Functions
::::::::::::::::::::::::::::::

The primary reason for the local scope variables rework was in
support of the new :ref:`user functions feature <user_functions>`
which has been a long-wished-for feature for kOS to support.

Community Examples Library
::::::::::::::::::::::::::

There is now a :ref:`new fledgling repository of examples and library
scripts<library>` that we hope to be something the user community
contributes to.  Some of the examples shown in the kOS 0.17.0 release
video are located there.  The addition of the ability to make user
functions now makes the creation of such a library a viable option.

Physics Ticks not Update Ticks
::::::::::::::::::::::::::::::

The updates have been :ref:`moved to the physics update <physics tick>`
portion of Unity, instead of the animation frame rate updates.
This may affect your preferred CONFIG:IPU setting.  The new move
creates a much more uniform performance across all users, without
penalizing the users of faster computers anymore.  (Previously,
if your computer was faster, you'd be charged more electricity as
the updates came more often).

Ability to use SAS modes from KSP 0.90
::::::::::::::::::::::::::::::::::::::

Added a new :ref:`third way to control the ship <sasmode>`,
by leaving SAS on, and just telling KSP which mode
(prograde, retrograde, normal, etc) to put the SAS
into.

Blizzy ToolBar Support
::::::::::::::::::::::

If you have the Blizzy Toolbar mod installed, you should be able
to put the kOS control panel window under its control.

Ability to define colors using HSV
::::::::::::::::::::::::::::::::::

When a color is called for, such as with VECDRAW or HIGHLIGHT, you
can now use the :ref:`HSV color system (hue, saturation, value)<hsv>`
instead of RGB, if you prefer.

Ability to highlight a part in color
::::::::::::::::::::::::::::::::::::

Any time your script needs to communicate something to the user about
which part or parts it's dealing with, it can use KSP's :ref:`part
highlighting feature <highlight>` to show a part.

Better user interface for selecting boot scripts
::::::::::::::::::::::::::::::::::::::::::::::::

The selection of :ref:`boot scripts for your vessel <boot>` has been
improved.

Disks can be made bigger with tweakable slider
::::::::::::::::::::::::::::::::::::::::::::::

All parts that have disk space now have a slider you can use in the VAB
or SPH editors to tweak the disk space to choose whether you want it to
have 1x, 2x, or 4x as much as its default size.  Increasing the size
increases its price and its weight cost.

You Can Transfer Resources
::::::::::::::::::::::::::

You can now use kOS scripts to :ref:`transfer resources between
parts <resource transfer>` for things like fuel, in the same way
that a manual user can do by using the right-click menus.

Kerbal Alarm Clock support
::::::::::::::::::::::::::

If you have the Kerbal Alarm Clock Mod isntalled, you can now
:ref:`query and manipulate its alarms <KAC>` from within your
kOS scripts.

Query the docked elements of a vessel
:::::::::::::::::::::::::::::::::::::

You can get the :ref:`docked components of a joined-together
vessel <element>` as separate collections of parts now.

Support for Action Groups Extended
::::::::::::::::::::::::::::::::::

While there was some support for the Action Groups Extended
mod before, it has :ref:`been greatly improved <AGX>`.

LIST constructor can now initialize lists
:::::::::::::::::::::::::::::::::::::::::

You can now do this::

    set mylist to list(2,6,1,6,21).

to initialize a :ref:`list of values <list>` from the start, so
you no longer have to have a long list of list:ADD commands to
populate it.

ISDEAD suffix for Vessel
::::::::::::::::::::::::

Vessels now have an :ISDEAD suffix you can use to detect if the
vessel has gone away since the last time you got the handle to it.
(for example, you LIST TARGETS IN FOO, then the ship foo[3] blows
up, then foo[3]:ISDEAD should become true to clue you in to this fact.)

