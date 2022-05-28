.. _changes:

Changes from version to version
===============================

This is a place to mention important documentation changes that
come with each version release.  Mostly it's here so you can
have links to jump to where the changes are elsewhere in this
documentation site.  The changes are not fully described here
because the intention is to let you go through the list and click
on the links to see the changes.

This list is NOT a comprehensive list of everything.  Specifically,
minor one-line changes, or bug fixes that didn't alter the 
documentation, are not mentioned here.

Also changes that might alter the user visual interface but not
change what a script does won't be mentioned here.  (For example,
putting a new button on the toolbar dialog.)

Most importantly, changes that might have broken previously working
scripts are not always signposted here.  To be sure, you should read
the change log in the main github repository, which is repeated in the
release announcements that are made in various places with each
release.

.. contents::
    :local:
    :depth: 3

****

Changes in 1.3
--------------

Nodes with ETA or Universal Time
::::::::::::::::::::::::::::::::

:func:`NODE(time, radial, normal, prograde)` now accepts time in
:struct:`TimeStamp` and :struct:`TimeSpan` inputs, as well as
the old practice of using scalar numbers of seconds.  If a
:struct:`TimeSpan` is used, the time is an ETA time, else it's a
UT time.  There is also a new suffix, :attr:`Node:TIME` to
report the UT time.

TimeSpan split into two types TimeStamp and TimeSpan
::::::::::::::::::::::::::::::::::::::::::::::::::::

:ref:`What was TimeSpan is now two different types<timestamp_timespan_diff>`.

Search on sub-branches of a vessel
::::::::::::::::::::::::::::::::::

Addition of :attr:`Part:PARTSTAGGED` and :attr:`Part:PARTSDUBBED` and
:attr:`Part:PARTSNAMED`

Can Set the RCS Deadband
::::::::::::::::::::::::

Addition on :attr:`RCS:DEADBAND`

SteeringManager Epislon
:::::::::::::::::::::::

Addition of :attr:`SteeringManager:TORQUEEPSILONMIN` and
:attr:`SteeringManager:TORQUEEPSILONMAX`

Random seed
:::::::::::

Added :func:`RANDOMSEED(key, seed)`.

Suppress Autopilot 
::::::::::::::::::

Added :attr:`Config:SUPPRESSAUTOPILOT`


GET/SET Player's trim
:::::::::::::::::::::

Added :ref:`PILOTYAWTRIM <SHIP CONTROL PILOTYAWTRIM>`,
:ref:`PILOTPITCHTRIM <SHIP CONTROL PILOTPITCHTRIM>`,
:ref:`PILOROLLTRIM <SHIP CONTROL PILOTROLLTRIM>`,
:ref:`PILOTWHEELSTEERTRIM <SHIP CONTROL PILOTWHEELSTEERTRIM>`, and
:ref:`PILOTWHEELTHROTTLETRIM <SHIP CONTROL PILOTWHEELTHROTTLETRIM>`,
which are set-able ways to control without locking out manual control.

Addon Trajectories changes
::::::::::::::::::::::::::

To support Trajectories 2.4 and up, many new things
are on the documentation page for :ref:`trajectories`.

Launch craft picking the crew list
::::::::::::::::::::::::::::::::::

:meth:`KUniverse:LAUNCHCRAFTWITHCREWFROM(template, crewlist, site)`

Asteroid-related vessel suffixes
::::::::::::::::::::::::::::::::

:meth:`Vessel:STOPTRACKING`, and :attr:`Vessel:SIZECLASS`.

Stock Delta-V Info
::::::::::::::::::

:attr:`Vessel:DELTAV`, :attr:`Vessel:DELTAVASL`, :attr:`Vessel:DELTAVVACUUM`,
:attr:`Vessel:BURNTIME`, :attr:`Stage:DELTAV`

RCS Part type
:::::::::::::

:ref:`A new part type<rcs>` for when a part is an RCS thruster.

Engine fuel info
::::::::::::::::

:attr:`Engine:CONSUMEDRESOURCE`, which returns a Lexicon of
a new type, :struct:`ConsumedResource`.  This will show you
what fuels an engine consumes and in what quantities.

CreateOrbit from position and velocity
::::::::::::::::::::::::::::::::::::::

A new variant of CreateOrbit - :func:`CREATEORBIT(pos, vel, body, ut)`,
that lets you make an orbit out of state vectors instead of
Kepplerian values.

Ranges take fractional numbers
::::::::::::::::::::::::::::::

:ref:`Ranges <range>` now accept fractional values for start, stop
and step.  (Previously everthing had to be an integer.)

Numerous mistake fixes
::::::::::::::::::::::

Not so much new documentation, but repairing typos and incorrect
descriptions in the documentation.  Too numerous to mention
in detail - see the associated Github issues:

https://github.com/KSP-KOS/KOS/pull/2675
https://github.com/KSP-KOS/KOS/pull/2680
https://github.com/KSP-KOS/KOS/pull/2707
https://github.com/KSP-KOS/KOS/pull/2712
https://github.com/KSP-KOS/KOS/pull/2724
https://github.com/KSP-KOS/KOS/pull/2751
https://github.com/KSP-KOS/KOS/pull/2772
https://github.com/KSP-KOS/KOS/pull/2775
https://github.com/KSP-KOS/KOS/pull/2776
https://github.com/KSP-KOS/KOS/pull/2777
https://github.com/KSP-KOS/KOS/pull/2784
https://github.com/KSP-KOS/KOS/pull/2788
https://github.com/KSP-KOS/KOS/pull/2791
https://github.com/KSP-KOS/KOS/pull/2800
https://github.com/KSP-KOS/KOS/pull/2819
https://github.com/KSP-KOS/KOS/pull/2833


Changes in 1.2
--------------

FLOOR and CEILING by decimal place
::::::::::::::::::::::::::::::::::

:func:`FLOOR(a,b)` and :func:`CEILING(a,b)` now allow you to chose
the decimal place where the cutoff happens.

Added ROLL to HEADING()
:::::::::::::::::::::::

:func:`HEADING(dir,pitch,roll)` Now has a third parameter for roll.
The new roll parameter is optional, so scripts using just the first
two parameters should still work.

Bodyexists
::::::::::

New Function, :func:`BODYEXISTS(name)`

Wordwrap on Labels
::::::::::::::::::

You can set wordrap off for labels by the new suffx, :attr:`Style:WORDWRAP`.

CreateOrbit
:::::::::::

:func:`CREATEORBIT(inc, e, sma, lan, argPe, mEp, t, body)` added.

Docking port partner query
::::::::::::::::::::::::::

Two new suffixes:  :attr:`DockingPort:PARTNER` and 
:attr:`DockingPort:HASPARTNER``.

Waypoint ISSELECTED
:::::::::::::::::::

:attr:`WayPoint:ISSELECTED`

Changes in 1.1.9.0
------------------

BOUNDING BOX
::::::::::::

Added the new :struct:`BOUNDS` structure for bounding box
information, and made an :ref:`example using it <display_bounds>`
on the tutorials page.

TERNARY OPERATOR "CHOOSE"
:::::::::::::::::::::::::

A new expression ternary operator exists in kerboscript, called
:ref:`CHOOSE <choose>`.  (Similar to C's "?" operator, but with
different syntax.)

New suffixes for Vecdraw
::::::::::::::::::::::::

New suffixes giving you more control over the appearance of
vecdraws: :attr:`Vecdraw:POINTY` :attr:`Vecdraw:WIPING`

Lexicon Suffixes
::::::::::::::::

:ref:`Describe using suffixes with lexicons. <lexicon_suffix>`

Terminal default size
:::::::::::::::::::::

Two new config settings for a default terminal size for
new terminals:

:struct:`Config:DEFAULTWIDTH`, :struct:`Config:DEFAULTHEIGHT`

Additional Atmospheric information
:::::::::::::::::::::::::::::::::::

Added some more information to the :struct:`atmosphere` structure,
(mostly for people trying to perform drag calculations: 
MOLARMASS, ADIABATICINDEX, ALTITUDETEMPERATURE).

Also added the ability to read some more of the values the
game uses for :ref:`mathematical constants <constants>`, to 
work with this information: Avogadro, Boltzmann, and IdealGas.

UNSET documentation
:::::::::::::::::::

Explicitly mention the :ref:`unset command <unset>`, which has existed
for a long time but apparently wasn't in the documentation.

LIST command
::::::::::::

Removed obsolete documentation about a no-longer-existing "FROM"
variant of the LIST command that went like this:
LIST *things* FROM *vessel* IN *variable*.

DROPPRIORITY()
::::::::::::::

Described the new :func:`DROPPRIORITY()` built-in function that you
can use when you want to write a long-lasting trigger body without
it preventing other triggers from interrupting like it normally would.




Changes in 1.1.8.0
------------------

Nothing but minor documentation error corrections - no new features
documented.

Changes in 1.1.7.0
------------------

IR Next
:::::::

Documented the change to using :ref:`IR Next instead of IR <IR>`.

CORE:TAG
::::::::

Documented :attr:`CORE:TAG`.

TIME
::::

Documented :func:`TIME(universal_time)`.

PAUSE
:::::

Added ability to pause the game with :meth:`Kuniverse:PAUSE()`.

List Fonts
::::::::::

Added :ref:`FONTS <list_fonts>` to the things you can LIST.

Changes in 1.1.6.2
------------------

Nothing of significance changed in the docs.  This was a fix to
switch files from PNG format to DDS format for GUI icons kOS uses.

Changes in 1.1.6.1
------------------

The various thrust and ISP calculations that take pressure
as a parameter prevent you from using negative values for
pressure.  Now they are clamped to be no lower than zero.
This change documents this fact.

Changes in 1.1.6.0
------------------

GUI tooltips
::::::::::::

Described how to make GUI tooltips work.  See:

- :attr:`Label:TOOLTIP`
- :attr:`GuiWidgets:TOOLTIP`
- :attr:`TIPDISPLAY`

5% null zone
::::::::::::

Mentioned the stock :ref:`null zone<raw null zone>` issue with RCS
translation.

Part:CID
::::::::

Added new suffix, :attr:`Part:CID`

An External Tutorial
::::::::::::::::::::

Added an external tutorial link to the :ref:`Tutorials <tutorials>` page.

G and G0 constants
::::::::::::::::::

Added :attr:`constant:G` and :attr:`constant:G0`.

Removed old notices
:::::::::::::::::::

Some "this changed in version ...." notices had aged beyond their usefulness
and were removed.

Document Simulate in BG
:::::::::::::::::::::::

Documented the need to have Simulate in BG enabled when playing in windwed mode,
on the :ref:`Telnet <telnet>` page.

Stage/decouple docs
:::::::::::::::::::

Many edits to the pages about :ref:`stages<stage>` and
:ref:`decouplers<decoupler>` to clarify points.

Vecdraw delegate
::::::::::::::::

Documented that the :ref:`Vecdraw constructor<vecdraw>` can
now take delegates.

Vector math link changes
::::::::::::::::::::::::

External links explaining vector operations such as dot product and
cross product now link to different sites on the
:ref:`Vectors<vectors>` page.

New suffixes on Body page
:::::::::::::::::::::::::

:ref:`Body <body>` page now has more fleshed-out examples and documentation
to go with the new :HASOCEAN, :HASSURFACE, and :CHILDREN suffixes

New Basic tutoial
:::::::::::::::::

New Basic Tutorial page.

Clarified CPU hardware page
:::::::::::::::::::::::::::

Much of the :ref:`CPU hardware<cpu hardware>` page has been re-done to reflect
some of the refactors that have happened in this revision.


Changes in 1.1.5.2
------------------

This was a compatibility release for KSP 1.4.1

Changes in 1.1.5.0
------------------

This was a compatibility release for KSP 1.3.1

Changes in 1.1.4.0
------------------

There were numerous optimizations applied to the source code that most end
users will not see directly.  Users should however see a performance boost.
Notable modifications were to the regular expressions engine used to parse
script files, optimization of internal string operations, better caching of
suffix information, and migrating to a dual stack cpu instead of a single stack
with hidden offsets.

File scope was also modified so that each file properly defines a scope.  This
means that local variables declared in script files called from other scripts
are no longer treated as part of the global scope.  It also means that script
parameters are local to the file itself and will not overwrite global variables.

Work also began to include identifier information within opcodes themselves
rather than as a pushed string literal to be evaluated separately.  This should
help with execution time and reduce the number of opcode calls within the kOS
virtual machine.

Changes in 1.1.3.0
------------------

Made documentation of how SAS fights with lock steering more prominent
and mentioned in more places.

Documentation for :meth:`Skin:ADD` fixed to mention the second parameter.

Documentation no longer implies TERMVELOCITY is a suffix (it was obsoleted,
but the documentation wasn't removed).

Changes in 1.1.2
----------------

None: This was a dummy version increase needed to "kick" CKAN and alert it
to a version number change that we messed up on in the previous release.

Changes in 1.1.1
----------------

None:  This was a pure compatibility with KSP 1.3 update, nothing more.

Changes in 1.1.0
----------------

GUI
:::

The :ref:`GUI system <gui>` was added new with version 1.1.0.

Terminal Font
:::::::::::::

Now that the terminal can display any font from your OS, you
can now display any Unicode character you like.

Regex Part Searches
:::::::::::::::::::

You may now use :meth:`Vessel:PARTSTAGGEDPATTERN` to perform regular
expression searches for part tags.

Triggers take locals
::::::::::::::::::::

The previous restriction that triggers such as WHEN and ON must only
use global variables in their check expressions has been removed.
Now they can use local variables and will remember their closures.

Altitude pressure
:::::::::::::::::

:meth:`Atmosphere:ALTITUDEPRESSURE` added.

LATLNG of other body
::::::::::::::::::::

New suffix :meth:`Body:GEOPOSITIONLATLNG` lets you get a LATLNG from a body
other than the current body you are orbiting.

Changes in 1.0.3
----------------

No significant changes, compiled for KSP v1.2.2.

Changes in 1.0.2
----------------

Sound/Kerbal Interface Device (SKID)
::::::::::::::::::::::::::::::::::::

The SKID chip allows scripts to output procedural sound clips.  Great for custom
error tones, or for playing simple music.  A basic example would be::

    SET V0 TO GETVOICE(0).      // Gets a reference to the zero-th voice in the chip.
    V0:PLAY( NOTE(400, 2.5) ).  // Starts a note at 400 Hz for 2.5 seconds.
                                // The note will play while the program continues.
    PRINT "The note is still playing".
    PRINT "when this prints out.".

For an example of a song, check out the :ref:`Example song section of voice documentation<voicesong>`

Also check out the :ref:`SKID chip documentation<skid>` for an indepth explaination.

CommNet Support
:::::::::::::::

kOS now supports communications networks through KSP's stock CommNet system as
well as RemoteTech (only one networking system may be enabled at a time).  The
underlying system was modified and abstracted to allow both systems to use a
common interface.  Other mods that would like to add network support can
implement this system as well without a need to update kOS itself.

Check out the :ref:`Connectivity Managers documentation here<connectivityManagers>`

Trajectories Support
::::::::::::::::::::

If you have the Trajectories mod for KSP installed, you can now access data from
that structure using :struct:`ADDONS:TR<TRAddon>`.  This provides access to
impact prediction through the Trajectories mod.  For example::

    if ADDONS:TR:AVAILABLE {
        if ADDONS:TR:HASIMPACT {
            PRINT ADDONS:TR:IMPACTPOS.
        } else {
            PRINT "Impact position is not available".
        }
    } else {
        PRINT "Trajectories is not available.".
    }

For more information see the :ref:`Trajectories Addon Documentation<Trajectories>`

Also Added
::::::::::

* :attr:`GeoCoordinates:VELOCITY` and :meth:`GeoCoordinates:ALTITUDEVELOCITY()`
* :meth:`String:TONUMBER()`
* :attr:`SteeringManager:ROLLCONTROLANGLERANGE`

Changes in 1.0.1
----------------

Terminal Input
::::::::::::::

A new structure :struct:`TerminalInput` is available as a suffix of
:attr:`Terminal<Terminal:INPUT>`, allowing scripts to respond to user input.

Example::

    terminal:input:clear().
    print "Press any key to continue...".
    terminal:input:getchar(). // blocking callback
    print "Input will be echoed back to you.  Press q to quit".
    set done to false.
    until done {
        if (terminal:input:haschar) {
            set input to terminal:input:getchar().
            if input = "q" {
                set done to true.
            }
            else {
                print "Input read was: " + input + " (ascii " + unchar(input) + ")".
            }
        }
        wait 0.
    }

Timewarp
::::::::

The new :struct:`TimeWarp` structure provides better access to information about
timewarp.  It provides lists of warp rates, information about the physics
timestep, and can tell you if the warp rate has settled.

Example::

    print kuniverse:timewarp:ratelist. // prints the rates available in the current mode
    set eta to 150 * 6 * 60 * 60. // 150 days
    kuniverse:timewarp:warpto(time:seconds + eta).
    print "delta t: " + kuniverse:timewarp:physicsdeltat.  // see the step change
    wait 0.
    print "delta t: " + kuniverse:timewarp:physicsdeltat.  // see the step change
    wait 0.
    print "delta t: " + kuniverse:timewarp:physicsdeltat.  // see the step change
    wait 0.
    print "delta t: " + kuniverse:timewarp:physicsdeltat.  // see the step change
    wait 0.
    print "delta t: " + kuniverse:timewarp:physicsdeltat.  // see the step change
    wait 60 * 60.
    kuniverse:timewarp:cancelwarp().
    print "delta t: " + kuniverse:timewarp:physicsdeltat.  // see the step change
    print "rate:    " + kuniverse:timewarp:rate.
    wait until kuniverse:timewarp:issettled.
    print "delta t: " + kuniverse:timewarp:physicsdeltat.  // see the step change
    print "rate:    " + kuniverse:timewarp:rate.

Changes in 1.0.0
----------------

Subdirectories
::::::::::::::

See :ref:`Understanding directories <directories>`.

You are now able to store subdirectories ("folders") in your volumes,
both in the archive and in local volumes.  To accomodate the new feature
new versions of the file manipulation commands had to be made (please
go over the documentation in the link given above).

Boot Subdirectory
^^^^^^^^^^^^^^^^^

See :ref:`Special Handing of files in the "boot" directory <boot>`.

To go with Subdirectories, now you make a subdirectory in your archive
called ``boot/``, and put all the candidate boot files there.

PATH structure
^^^^^^^^^^^^^^

You can now get information about a
:ref:`file's path and location <path>`.

New RUNPATH command
^^^^^^^^^^^^^^^^^^^

:ref:`New RUNPATH command <runpath>` lest you make the program to run
be a varying expression.

Communications
::::::::::::::

:ref:`Communication between scripts <communication>`
on different CPUs of the same vessel or between different vessels.

Message Structure
^^^^^^^^^^^^^^^^^

A :ref:`Message structure <message>` added  to be used with
the new communications system.

Anonymous functions
:::::::::::::::::::

:ref:`Anonymous functions <anonymous_functions>` now implemented.

Allow scripted vessel launches
::::::::::::::::::::::::::::::

``GETCRAFT()``, ``LAUNCHCRAFT()``, ``CRAFTLIST()``, ``LAUNCHCRAFTFROM()``
were added as new suffixes to the :ref:`Kuniverse <kuniverse>` structure.

ETA to SOI change
:::::::::::::::::

:attr:`ORBIT:NEXTPATCHETA` to get the time to the next orbit patch
  transition (SOI change).

VESSEL:CONTROLPART
::::::::::::::::::

:attr:`VESSEL:CONTROLPART` to get the part which has been used
as the current "control from here".

Maneuver nodes as a list
:::::::::::::::::::::::::

:global:`ALLNODES` bound variable added.

More pseudo-action-groups
:::::::::::::::::::::::::

:ref:`Some new Pseudo-Action-Groups added <kos-boolean-flags>` for
handling a lot of new groups of parts.

Get Navball Mode
::::::::::::::::

:global:`NAVMODE` bound variable:

UniqueSet
:::::::::

Added a :ref:`UniqueSet <uniqueset>` collection for holding a
generic set of things where order is irrelevant and duplicates are
guaranteed not to exist.


Changes in 0.20.1
-----------------

This release is just a bug fix release for the most part, with only just
one new feature:

3-axis Gimbal Disabling
:::::::::::::::::::::::

You can now selectively choose which of the 3-axes of an engine gimbal you want
to lock, rather than having to lock the entire gimbal or none of it.

(See suffixes "PITCH", "YAW", and "ROLL" of the
:ref:`gimbal documentation <gimbal>`.)

Changes in 0.20.0
-----------------

This release is functionally identical to v0.19.3, it is recompiled against the
KSP 1.1 release binaries (build 1230)

Changes in 0.19.3
-----------------

Interuptable Triggers
:::::::::::::::::::::

Triggers are no longer required to complete within a single update frame,
allowing them to be more than the IPU instructions long.  This also means that
they are no longer guaranteed to be atomic, and that long running triggers may
prevent the execution of other triggers or the mainline code.  See
:ref:`the trigger documentation <triggers>` for details.

Script Profiling
::::::::::::::::

You may now profile the performance of your scripts to better understand how the
underlying opcodes operate, as well as to identify slow executing sections of
code.  See :ref:`the function ProfileResult <profileresult>` for more information.

Compiled LOCK
:::::::::::::

In previous versions, attempting to create a lock with a duplicate identifier
from within a compiled script would throw an error regarding label replacement.
In this version, the handling of lock objects is updated to be more flexible at
run-time, instead of relying on compile-time information.

ON Using Expressions
::::::::::::::::::::

In previous versions, ``ON`` would not accept an expression as a parameter like
this: ::

    ON STAGE:READY {
        PRINT "STAGE: " + STAGE:READY.
    }
    ON ROUND(MAX(2000, ALT:RADAR)) {
        PRINT ROUND(ALT:RADAR).
    }

``ON`` will now evaluate the expression instead of treating it like a variable
identifer.

Changes in 0.19.2
-----------------

This was mostly a bug fix release.  Not much changed in the documentation.

FORCEACTIVE
:::::::::::

New alias ``KUNIVERSE:FORCEACTIVE()`` can be used instead of the
longer name ``KUNIVERSE:FORCESETACTIVEVESSEL()``.

Changes in 0.19.1
-----------------

This change was mostly for small bug fixes and didn't affect the
documentation much.

Mentioned PIDLoop() function in tutorial
::::::::::::::::::::::::::::::::::::::::

:ref:`Added section to PID loop tutorial <struct_pidloop_in_tutorial>`
that explains better that there's a new function for doing PID loops.
The tutorial had been originally written before that function existed.


New Terminal brightness and char size features
::::::::::::::::::::::::::::::::::::::::::::::

:struct:`Terminal` structure now has suffixes, :attr:`TERMINAL:BRIGHTNESS`,
:attr:`TERMINAL:CHARWIDTH`, and :attr:`TERMINAL:CHARHEIGHT` to go with
the new widgets on the terminal GUI.

Changes in 0.19.0
-----------------

Art asset changes
:::::::::::::::::

Though not represented in these documents, numerous changes to the
part models and artwork are included as part of this update, including
the new KAL9000 high-end computer part.

Varying Power Consumption
:::::::::::::::::::::::::

:ref:`Electrical drain <electricdrain>` is now handled in a dynamically
changing way that actually notices how much you are using the CPU and
uses less power if the CPU is mostly idling (if it spends most of its
time on WAIT statements).

For mods that want to re-balance the meaning of electric charge units,
the drain factor is also editable in
:ref:`module config fields <kospartmodule>` in the various ``part.cfg``
files the mod ships with.  This opens them up to being changed by
ModuleManager rules.

Delegates (function pointers)
:::::::::::::::::::::::::::::

User functions and built-in functions (but not suffixes yet) can
now be referred to with function pointers called :ref:`delegates <delegates>`
along with "currying" of pre-loaded arguments.

Optional Defaulted Parameters
:::::::::::::::::::::::::::::

User functions and user programs can now be configured to have
:ref:`optional trailing parameters <default_parameters>` that receive
unmentioned when calling them.

File I/O
::::::::

:ref:`VolumeFile <volumefile>` now lets you read and write arbitrary
strings in files in a more natural way than using the LOG command,
and allows you to read the whole file into one big string in one go.

Serialization in JSON
:::::::::::::::::::::

Automatic serialization system added to the :ref:`file operations <files>`
to save/load some kinds of data values to
`JSON-format files. <https://en.wikipedia.org/wiki/JSON#Example>`__

Universal Object Suffixes
:::::::::::::::::::::::::

All user values now are a kind of :ref:`structure <structure>` and thus
there are a few universal suffixes that can be used to query what
type of data a thing is (``:ISTYPE`` and ``:TYPENAME``).

Multimode Engine and Gimbal Support
:::::::::::::::::::::::::::::::::::

:ref:`Engines <engine>` can now support multiple-mode information, and can
acces thei gimbal information in the ``:GIMBAL`` suffix.

DMagic Orbital Science
::::::::::::::::::::::

Better support for :ref:`DMagic's Orbital Science mod <orbitalscience>`

Range
:::::

New :ref:`Range <range>` type for getting arbitrary iterable collections
of ranges of integers.

Char and Unchar
:::::::::::::::

:func:`CHAR(a)` and :func:`UNCHAR(a)` functions for getting the Unicode
value of a character or making a character from its Unicode value.

For loop on string chars
::::::::::::::::::::::::

The for loop can now iterate over the characters of a :ref:`string <string>`.

HASTARGET, HASNODE
::::::::::::::::::

:ref:`HASTARGET <hastarget>`.
:global:`HASNODE`.

JOIN
::::

Join suffix on :ref:`lists <list>` now lets you make a string with a
delimeter of the list's elements.

Hours per day
:::::::::::::

:ref:`KUniverse <kuniverse>` now has a suffix to let you read the
user setting for whether the clock is using a 24 hour day or a
Kerbin 6 hour day.

Archive
:::::::

The reserved word ``Archive`` is now a first class citizen so that
``SET FOO TO ARCHIVE.`` works like you'd expect it to.

Changes in 0.18.2
-----------------

Queue and Stack
:::::::::::::::

:ref:`Queues <queue>` and :ref:`Stacks <stack>` are now a feature
you can use along with lists.

Run Once
::::::::

:ref:`New ONCE argment to the run command <run_once>`

Volumes and Processors integration
::::::::::::::::::::::::::::::::::

:ref:`Volumes <volume>` now get a default name equal to the core
processor's nametag, and have several suffixes that can be queried.

Get the volume that goes with a :ref:`core <core>`

Debuglog
::::::::

:ref:`Debuglog <debuglog>` suffix of KUNIVERSE for writing messages to the
Unity log file.


Changes in 0.18.1
-----------------

(This update had only bug fixes and nothing that affected these
user documentation pages.)

Changes in 0.18 - Steering Much Betterer
----------------------------------------

Steering Overhaul
:::::::::::::::::

A major change to Cooked Steering!

Should help people using torque-less craft like with Realism Overhaul.
Removed the old steering logic and replaced it with a nice auto-tuning system.

:ref:`SteeringManager <steeringmanager>` structure now lests you acccess and alter parts of the cooked steering system.

:ref:`PIDLoop <pidloop>` structure now lets you borrow the PID mechanism used by the new cooked steering, for your own purposes.

Lexicon
:::::::

New :ref:`Lexicon <lexicon>` structure now allows associative arrays.

String methods
::::::::::::::

New :ref:`String <string>` structure now allows string manipulations.

Science Experiment Control
::::::::::::::::::::::::::

New :ref:`ScienceExperimentModule <scienceexperimentmodule>` allows you to fire off science experiments bypassing the user
interface dialog.

Crew Member API
:::::::::::::::

New :ref:`CrewMember <crewmember>` structure allows you to query the registered crew - their class, gender, and skill.

LOADISTANCE
:::::::::::

New :struct:`LOADDISTANCE` obsoletes the previous way it worked.

Infernal Robotics Part suffix
:::::::::::::::::::::::::::::

Renamed built-ins
:::::::::::::::::

"AQUIRE" on docking ports is now "ACQUIRE".
"SURFACESPEED" is now "GROUNDSPEED" instead.

Enforces control of own-vessel only
:::::::::::::::::::::::::::::::::::

It was previously possible to control vessels that weren't attached to the kOS computer
running the script.  This has been corrected.

New quickstart tutorial
:::::::::::::::::::::::

`http://ksp-kos.github.io/KOS_DOC/tutorials/quickstart.html <http://ksp-kos.github.io/KOS_DOC/tutorials/quickstart.html>`_

A few more constants
::::::::::::::::::::

:ref:`constants <constants>`

Dynamic pressure
::::::::::::::::

DYNAMICPRESSURE, or Q, a new suffix of :struct:`Vessel`.

DEFINED keyword
:::::::::::::::

:ref:`DEFINED keyword <defined>` that can be used to check if a variable has been declared.

KUNIVERSE
:::::::::

:struct:`KUniverse` structure letting you break the 4th wall and revert from a script

SolarPrimeVector
::::::::::::::::

:ref:`SolarPrimeVector <solarprimevector>`, a bound variable to provide universal longitude direction.


****

Changes in 0.17.3
-----------------

New Looping control flow, the FROM loop
:::::::::::::::::::::::::::::::::::::::

There is now a new kind of loop, :ref:`the FROM loop <from>`,
which is a bit like the typical 3-part for-loop seen in a
lot of other languages with a separate init, check, and increment
section.

Short-Circuit Booleans
::::::::::::::::::::::

Previously, kerboscript's AND and OR operators were not
short-circuiting.  :ref:`Now they are <short_circuit>`.

New Infernal Robotics interface
:::::::::::::::::::::::::::::::

There are a few new helper addon utilities for the Infernal
Robotics mod, on the :ref:`IR addon page <IR>`.

New RemoteTech interface
::::::::::::::::::::::::

There are a few new helper addon utilities for the RemoteTech
mod, on the :ref:`RemoteTech addon page <remotetech>`.

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

The :ref:`core <core>` bound variable gives you a structure you can use
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

New sounds and terminal features
::::::::::::::::::::::::::::::::

For purely cosmetic purpopses, there are new sound features and
 a few terminal tweaks.

- A terminal keyclick option for the in-game GUI terminal.
- The ability to BEEP when printing ascii code 7 (BEL), although
  the only way currently to achieve this is with the KSlib's spec_char.ksm
  file, as kOS has no BEL char, but this will be addressed later.
- A sound effect on exceptions, which can be turned off on the CONFIG panel.

Clear vecdraws all at once
::::::::::::::::::::::::::

For convenience, you can clear all vecdraws off the screen at once
now with the :ref:`clearvecdraws() <clearvecdraws>` function.

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
