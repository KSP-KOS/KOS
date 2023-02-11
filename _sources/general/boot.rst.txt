.. _boot:

Boot Files
==========

There is a boot system you can use in kOS to have a kOS computer
automatically load a kerboscript file and run it when the computer
boots up.

Short version and example
-------------------------

Here's the tl;dr version of how to use a boot file:

  1. Place a kerboscript program file in the archive ``"0:/boot/"``
     directory.  For example, ``"0:/boot/myboot.ks"``.  Make sure
     the file ends in ``.ks``.
  2. Bring up the vessel into the VAB or SPH.  Rightclick the
     kOS computer part, and use the slider widget to select your
     file from step 1 above.
  3. When you save and launch this vessel, it should copy that
     file to the local drive and run it once the ship is loaded
     onto the launchpad.

Example::

  // Place this file in your archive and call it "boot/myboot.ks".
  wait until ship:unpacked.
  clearscreen.
  print "Hello. I am the boot file.".
  print "If you see this, that proves the boot file ran.".

Then do steps 2 and 3 above.  Launch the vessel and you should
see the message in the boot script was run if you open the
terminal.

There's a lot more you should learn about how the booting
system works before you use it - but that's the short version.

Read on to learn more.

Telling kOS which file is the boot file
---------------------------------------

There are two ways to tell the kOS mod which file is the one
you wish to have be your boot file - one is through the
VAB or SPH editor building when creating the vessel,
and the other is through kerboscript code itself once the
vessel is loaded and on screen.

In the VAB or SPH
~~~~~~~~~~~~~~~~~

.. figure:: /_images/general/bootVAB.png
    :width: 50%
    :align: right
    :alt: Figure of selecting a boot file

You may select a boot file from the Vehicle Assembly Building
or Spaceplane Hangar.  To do this you must have placed the
file(s) you wish to be available for booting into the ``boot/``
directory of your archive folder.  For a reminder, this means it
is located here on your computer:

``[Kerbal Space Program's Folder]/Ships/Script/boot/``

where "[Kerbal Space Program's Folder]" is wherever your kerbal
space program game is installed.

**Files placed here MUST end in ``.ks``**.  The files will not be
seen in the VAB or SPH if they don't have a .ks ending.

When you have files available there in the archive boot folder,
then they will appear in the VAB or SPH when you right-click on
a part that contains a kOS computer, as shown in the figure here.

**Be Aware that only the files that were present when you first
entered the VAB/SPH will be in the list.** *If you add a new file
to the ``boot/`` folder, you must exit and re-enter the VAB/SPH
to make that file appear in the list.*

Please pay attention to the topic below, "Booting When Spawning
to launchpad", to see exactly when and how the files get copied
to the computer when you use this method of selecting the boot
file (from the VAB or SPH).

From Script Code
~~~~~~~~~~~~~~~~

Kerboscript code can also alter the boot file on the fly with this
command::

    set core:bootfilename to "any_filename_here".

When you do this, then "any_filename_here" becomes the new
boot file for that kOS computer, and it will be what gets
booted on that kOS computer in the future, instead of
whatever may have been selected for it in the VAB/SPH.

When using this method of selecting the boot file, unlike when
using the VAB/SPH method, the file will not automatically
get copied from the archive to your local drive on the kOS
computer.  It must be a file that *already* exists on the
kOS computer's local drive when the booting occurs.  You
can use a file from any folder of the local volume you like
(it does not have to have ``boot/`` in the pathname), but it
*must* be from the local volume and not from the archive. In
fact if you use a drive indicator such as "0:" or "1:" in the
filename, it won't work.

When do boot files run
----------------------

If a boot file is set, then the boot file gets run under any
of the following conditions:

  1. Whenever the kOS computer is turned on after having been turned off.
  2. When electric charge is depleted but gets restored later.  (This is
     just a special case of case (1) above, since losing power turns the
     computer off and regaining power turns it back on.)
  3. When you leave the scene, then return to the scene later.  The kOS
     computer reboots when you reload the scene.
  4. When you first launch the vessel to the launchpad or runway.  See
     below to see the exact sequence of events that gets the file copied
     to the ship when you do this.
  5. When the ``REBOOT.`` command is run by a script or at the terminal.

Because boot files are run whenever the scene is reloaded, they can
be a useful way to make your probe do things even when they have no
antenna contact from home.

Warning about ship:unpacked
~~~~~~~~~~~~~~~~~~~~~~~~~~~

Because the boot file begins the instant the scene loads, there can
be a problem.  Kerbal Space Program loads the scene immediately,
starts up the kOS modules, and then a second or two later it
"unpacks" the vessel.  Without going into too much detail about
what "unpacks" means here, the short version is that for the few
seconds the vessel is loaded but not yet unpacked, half the stuff
on the vessel *doesn't work yet*.  You try to move the throttle and
nothing happens.  You try to press spacebar to stage and nothing
happens.  You try to steer with WASD keys and nothing happens.  This
also affects kOS's own attempts to control the ship.  If you run
a boot script and let's say the very first thing the boot script
tries to do is throttle up and turn on the engine, it might not
actually work because Kerbal Space Program still has the ship
in its "packed" state when you tried to do that.  The command
executes without complaint, but has no effect.

To avoid this problem, you can put this line at the top of your
boot file::

  // put at the top of most boot files:
  print "Waiting for ship to unpack.".
  wait until ship:unpacked.
  print "Ship is now unpacked.".
  // 
  // .. The rest of your boot file goes here ..
  //

**Then why doesn't kOS itself just wait until the ship is unpacked
before it starts booting the computer?** The reason this is not
done is because of the following three things taken in combination:

  1. If the vessel is not the current active vessel, but it *IS*
     within 2.5 km of the active vessel, then it will be loaded
     but still packed, and stay packed until you get close to it
     with the active vessel.  Waiting until ship:unpacked would mean
     the boot script on that vessel will never run at all until you
     bring the active vessel close enough to it to unpack it.
  2. There are still valid things a kOS script can accomplish while
     the vessel is in a packed state.  It just can't make the ship move.
  3. The ship also becomes packed when under time warp.  You might
     still want a script to be running while in time warp, especially
     if what it's doing is waiting for the right conditions where it
     will choose to stop the time warp.

More information on what "packed" and "loaded" actually mean can
:ref:`be found here <loaddistance>`, but be warned, it can be a complex
topic.

Booting when spawning to launchpad
----------------------------------

When you first spawn a new vessel on the launchapd from the VAB (or when
you spawn it to the runway from the SPH), kOS performs the following
initial steps to get the boot file copied from archive to the ship:

  1. Creates a folder called ``boot/`` on the kOS computer's local volume
     (``1:/``).
  2. Copies the boot file from the archive's ``boot/`` folder to the
     local volume's boot folder.
  3. **Important**: NOW is the point where Kerbal Space Program saves
     the game for the purpose of being able to "revert to launch".
  4. kOS begins running that local copy of the boot file.

Please make note of when during those steps Kerbal Space Program saved
the game for the sake of doing a *revert to launch*.  If you edit
the boot file on the archive, and then *revert to launch*, then your
vessel will not have the newly edited boot file copied to it because
it doesn't go all the way back to do step 1 and 2 from the above list
again.  To force it to use the new version of the boot file you will
either have to revert it all the way to the assembly building and
re-launch it from there, or stop the boot file with ctrl-C and manually
copy the new file and reboot.
