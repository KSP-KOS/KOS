.. _volumes:


Files and Volumes
=================

Using the COPYPATH, SWITCH, DELETEPATH, and RENAMEPATH commands, you can manipulate the archive and the volumes as described in the :ref:`File I/O page <files>`. But before you do that, it's useful to know how kOS manages the archive and the volumes, and what they mean.

.. contents::
    :local:
    :depth: 2

Script Files
------------

There is one file per program. You can use the words "file" and "program" interchangeably in your thinking for the most part. Files are stored in volumes and there can be more than one file in a volume provided there's enough room. Volumes have small storage and there's no way to span a file across two volumes, so the limit to the size of a volume is also effectively a limit to the size of a program.

File Storage Behind the Scenes
------------------------------

In the Archive:

-  If a file is stored on the volume called "Archive" (or volume number
   zero to put it another way), then behind the scenes it's really
   stored as an actual file, with the extension ``.ks``, on your
   computer (As of right now it's located in ``Ships/Script`` but that
   location is likely to change to somewhere in GameData in a future
   version.) Each program is a simple text file you can edit with any
   text editor, and your edits will be seen immediately by KOS the next
   time it tries running that program from the archive volume.

-  Historical note: older versions of kOS (0.14 and earlier) used the
   directory ``Plugins/PluginData/Archive`` for the archive.

In any other volume besides Archive:

-  If a file is stored on any volume other than archive, then behind the
   scenes it's stored actually inside the saved game's persistence file
   in the section for the KOS part on the vessel.
   What's a Volume

A Volume is a small unit of disk storage that contains a single hard
drive with very limited storage capacity. It can store more than one
program on it. To simulate the sense that this game takes place at the
dawn of the space race with 1960's and 1970's technology, the storage
capacity of a volume is very limited.

For example, the CX-4181 Scriptable Control System part defaults to only
allowing 1000 bytes of storage.

The byte count of a program is just the
count of the characters in the source code text. Writing programs with
short cryptic variable names instead of long descriptive ones does save
space, although you can also save space by compiling your programs to
KSM files where the variable names are only stored once in the file, but
that's another topic for another page.

Each of the computer parts that kOS supports have their own different default
storage capacity limits for their local volume.  As you get better parts
higher up the tech tree, they come with bigger default size limits.

You can get more space by paying extra cost in money and mass
-------------------------------------------------------------

.. figure:: /_images/general/disk_space_slider.png

If you wish to have more disk space on your local volume, and are willing to
pay a little extra cost in money and in mass, you can use the disk space
slider in the vehicle assembly building to increase the limit.

Every part comes with 3 different multiplier options:

  * 1x default size,
  * 2x default size,
  * 4x default size

The higher the multiplier the more mass it will
cost you, to represent that you're using old storage technology,
so it costs a lot of mass to have more storage.

The disk size is only settable like this in the assembly building.  Once
you launch a vessel, its volume size is stuck the way it was when you
launched it.

Multiple Volumes on One Vessel
------------------------------

Each kOS CX-4181 Scriptable Control System part contains '''one''' such
volume inside it.

If you have multiple CX-4181 parts on the same craft, they are assumed
to be networked together on the same system, and capable of reading each
other's hard drives. Their disk drives each have a different Volume, and
by default they are simply numbered 1,2,3, … unless you rename them.

For example, if you have two CX-4181's on the same craft, called 1 and
2, with volumes on them called 1 and 2, respectively, it is possible for
CPU 1 to run code stored on volume 2 by just having CPU number 1 issue
the command ''SWITCH TO 2.''

Naming Volumes
--------------

It's important to note that if you have multiple volumes on the same
vessel, the numbering conventions for the volumes will differ on
different CPUs. The same volume which was called '2' when one CPU was
looking at it might instead be called '1' when a different CPU is
looking at it. Each CPU thinks of its OWN volume as number '1'.

Therefore using the SET command on the volumes is useful when dealing
with multiple CX-4181's on the same vessel, so they all will refer to
the volumes using the same names::

  SET VOLUME("0"):NAME TO "newname".

Volume Name inherits from Tag Name
----------------------------------

If the part that contains the Volume (a kOS processor core part
typically) has a kOS nametag set, then the Volume will have its
name initially set to the value of the name tag.

When doing this, if the tag contains characters that are not 
allowed in a volume name, the volume name will have those characters
deleted, except for spaces which get replaced with underscores
rather than being deleted.

Archive
-------

The "archive" is a special volume that behaves much like any other
volume but with the following exceptions:

-  It is globally the same even across save games.
-  The archive represents the large bank of disk storage back at mission
   control's mainframe, rather than the storage on an indivdual craft.
   While "Volume 1" on one vessel might be a different disk than "Volume
   1" on another vessel, there is only one volume called "archive" in the
   entire solar system. Also, there's only one "archive" across all
   saved universes. If you play a new campaign from scratch, your
   archive in that new game will still have all the files in it from
   your previous saved game. This is because behid the scenes it's
   stored in the ``Ships/Script`` directory, not the save game directory.
-  It is infinitely large.
-  Unlike the other volumes, the archive volume does not have a byte
   limit. This is because the mainframe back at home base can store a
   lot more than the special drives sent on the vessels - so much so
   that it may as well be infinite by comparison.
-  Files saved there do not revert when you "revert flight".
-  When you revert a flight, you are going back to a previous saved
   state of the game, and therefore any disk data on the vessels
   themselves also reverts to what it was at the time of the saved game.
   Because the archive is saved outside the normal game save, changes
   made there are retained even when reverting a flight.
-  Files in Archive are editable with a text editor directly and they
   will have the ``.ks`` extension.
-  Files in the Archive are stored on your computer in the subdirectory:
   ``Ships/Script``. You can pull them up in a text editor of your
   choice and edit them directly, and the KOS Mod will see those changes
   in its archive immediately. Files stored in other volumes, on the
   other hand, are stored inside the vessel's data in the persistence
   file of the saved game and are quite a bit harder to edit there.
   Editing the files in the Archive directory is allowed and in fact is
   an officially accepted way to use the plugin. Editing the section in
   a persistence file, on the other hand, is a bad idea and probably
   constitutes a form of cheating similar to any other edit of the
   persistence file.

.. _boot_directory:

Special handling of files in the "boot" directory
-------------------------------------------------

For users requiring even more automation, the feature of custom boot scripts
was introduced. If you have at least 1 file in the :code:`boot` directory on
your Archive volume, you will be presented with the option to choose one of
those files as a boot script for your kOS CPU.  (As with any other kerboscript
file, the filename extension has to be ``.ks`` for text file scripts, or
``.ksm`` for compiled scripts.


.. image:: http://i.imgur.com/05kp7Sy.jpg

The first time that you load kOS without a directory named
:code:`boot` in the archive root, kOS will prompt you for automatic
migration.

.. warning::
    .. versionadded:: v1.0.0
        Older versions of kOS used file names starting with the word "boot" to
        determine which files should be considered as boot files.  When support
        was added for directories, it made sense to instead use a directory
        named :code:`boot`.  Care was taken to maximize backwards compatibility:
        if an existing craft file is opened in the editor, kOS will first look
        for the saved boot file name in the boot directory, then it will check
        the archive root, and finally it will check the boot directory again
        after stripping :code:`boot` or :code:`boot_` from the beginning of the name.
        Vessels in flight will continue to work with the existing structure, so
        long as :attr:`CONFIG:ARCH` is set to false.  If :attr:`CONFIG:ARCH` is
        set to true, you will need to leave copies of the originally named boot
        files in your archive root for ships already in flight to access.

As soon as you vessel leaves VAB/SPH and is being initialised on the launchpad
(e.g. its status is PRELAUNCH) the assigned script will be copied to CPU's
local hard disk with the same name.  If kOS is configured to start on the
archive, the file will not be copied locally automatically. This script will
be run as soon as CPU boots, e.g. as soon as you bring your CPU in physics
range or power on your CPU if it was turned off.  You may get or set the name
of the boot file using the :attr:`kOSProcessor:BOOTFILENAME` suffix.

Important things to consider:

  * kOS CPU hard disk space is limited, avoid using complex boot scripts or increase disk space using MM config.
  * Boot script runs immediately on initialisation, it should avoid interaction with parts/modules until physics fully load. It is best to wait for couple seconds or until certain trigger.

Possible uses for boot scripts:

  * Automatically activate sleeper/background scripts which will run on CPU until triggered by certain condition.
  * Create basic station-keeping scripts - you will only have to focus your probes once in a while and let the boot script do the orbit adjustment automatically.
  * Create multi-CPU vessels with certain cores dedicated to specific tasks, triggered by user input or external events (Robotic-heavy Vessels)
  * Anything else you can come up with
