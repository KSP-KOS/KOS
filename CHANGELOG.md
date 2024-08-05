# kOS Mod Changelog

## 1.5.0.0 - 2024-08-05

### New Features

- New crewmember suffixes [commit](https://github.com/KSP-KOS/KOS/commit/8b6246d328f376f431df11f32edadde3d4db7035)
- Added `COM` suffix to parts to get the accurate center of mass (thanks SofieBrink) [commit](https://github.com/KSP-KOS/KOS/commit/496bf3fe7e29b4b8917ec214a6d9d8ab3780cd46)
- 3rd party addons can now add custom suffixes to PartModules [commit](https://github.com/KSP-KOS/KOS/commit/9b83c9ee01e0fb0ce2699f42ff71028d4db71b36)
- Integrated KSPBuildTools for easier local setup and automated build process [commit](https://github.com/KSP-KOS/KOS/commit/7bde357c9d23c60f95f1d64b0490724a4b1544a4)

### Bug Fixes

- Documentation: renamed `gui` to `my_gui` to avoid conflict with global [commit](https://github.com/KSP-KOS/KOS/commit/bc2d2aad469d939b549e73a54b2f6d2f2741f376)
- Fixed an issue where probe cores in RP-1 would get incorrect cost and mass [commit 1](https://github.com/KSP-KOS/KOS/commit/96baa4836bf16ab1bf1b67524c497ea7e0b8db87) [commit 2](https://github.com/KSP-KOS/KOS/commit/142a68c0ef511cacafaf9badc083ee735066cc56)
- Quicksaveto no longer checks ClearToSave [commit](https://github.com/KSP-KOS/KOS/commit/3a24b86d9f50bc85e2aedfc1bc699dd9905e5b0b)
- Several memory leak fixes  
  - [when vessel is destroyed](https://github.com/KSP-KOS/KOS/commit/eb55ed8fb69fe58ba1c367d0403046406f4589eb)
  - [function call results](https://github.com/KSP-KOS/KOS/commit/4aec51d6211ecb8f590fe8e8e169ddff47abf061)
  - [maneuver nodes and vessels](https://github.com/KSP-KOS/KOS/commit/f66b51a04290b663f26c95d430cc0a47387eaa48)
  - [commnet connectivity](https://github.com/KSP-KOS/KOS/commit/d77de08e9aa07de03954cba2c7575d3caac28af3)
  - [managedwindow](https://github.com/KSP-KOS/KOS/commit/41a0a0665af6f2e6f55cc19a899f15b3cccfab09)
  - [static part caches](https://github.com/KSP-KOS/KOS/commit/ba4098a08ca015bd72d66e2f3539ed3486c0596d)
- Invoke UI field change callbacks when settings fields (thanks ricmatsui) [commit](https://github.com/KSP-KOS/KOS/commit/09864894469119004eb8c9a2eca7ca91ae058a32)


## v1.4.0.0 - Catch-up for over a year of little things

It's been 3 years since the last kOS release, and a lot of
small changes have trickled in.  None were big enough on
their own for a full release but there's been enough of
them and it's been long enough that a release has been
needed for a while now.  Since KSP 2 is about to start
hitting early access, it seemed right to get all these little
things out for kOS for KSP 1 just before that happens.

This will also make it so people won't have to keep
overriding the complaints of CKAN for trying to use
kOS on KSP 1.11.x or KSP 1.12.x.  (Which it worked for
but CKAN didn't know that. Now it should know that.)

### Breaking Changes

- The bugfix to prevent a local variable from clobbering a
  builtin name could make existing scripts have to rename
  a variable or two.

  Previously if you tried to create a variable that matches
  the name of a built-in variable, it would let you but then
  the built-in variable would be permanently masked and
  unreachable.

  Now by default it won't let you.  BUT you can get the old
  behavior back again if you use the @CLOBBERBUILTINS directive,
  if you really want to let yourself do that.
  [pull request](https://github.com/KSP-KOS/KOS/pull/3016)

### New Features

- kOS parts are now findable by typing "kos" into the
  VAB's part search bar.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2980)
- kOS parts can be placed inside the KSP cargo inventory system.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2916)
- Comma-separated list of LOCAL or SET declarations can
  now be parsed.  Example:

  old: ``local a is 3. local b is 5. local c is 10.``
  can now be: ``local a is 3, b is 5, c is 10.``

  This is similar to how it works with PARAMETER.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2975)

- Added VESSEL:THRUST, VESSEL:ENGINES, VESSEL:RCS.

  ``VESSEL:THRUST`` is the sum of the engine:THRUST of all
  the engines.

  ``VESSEL:ENGINES`` is the same list returned by LIST ENGINES,
  but using a somewhat nicer syntax.

  ``VESSEL:RCS`` is the list of all the RCS parts on the vessel.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2977)

- Added OPCODESLEFT bound variable.  This bound variable
  returns the number of instructions yet to execute (how
  much of CONFIG:IPU's instructions there are to go in
  this fixedupdate).  Intended to help decide if a `WAIT 0.`
  would be prudent before entering a critical section of
  code.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2890)
- Better integration with RP-1's avionics tech progression.
  (No longer have to buy into the tech from the R&D building
  to cause the kOS cores in avionics parts to get the upgrade.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2955)
- Better integration with RP-1's avionics lockouts when the
  avionics doesn't support the mass.  (Previously kOS couldn't
  use ANY of the controls when avionics were insufficient, even
  ones RP-1 meant to still work with insuficient avionics,
  like RCS fore and aft.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2971)
- kOS parts are now findable by typing "kos" into the
  VAB's part search bar.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2980)
- Can now read a binary file as a LIST of numeric values (one
  per byte).
  [pull request](https://github.com/KSP-KOS/KOS/pull/2986)

### Bug Fixes

- Documentation: Many small one-line documentation fixes that are
  too numerous to mention all of them one by one.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2886)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2951)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2960)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2962)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2967)
  [pull request](https://github.com/KSP-KOS/KOS/pull/3070)
- A change to make it backward compatible with a call
  kOSPropMonitor was doing.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2865)
- Cause the mod RocketSoundEnhancement to stop muffling
  kOS's sounds.  (By explicitly telling Unity those
  sounds don't emit from the kOS Part's "location" and
  instead are ambient.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2866)
- Make PART:DECOUPLER behave more consistently with what the
  documentation says about docking ports.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2864)
- Reduce excessive repeats of GUI ONCONFIRM calls being triggered
  when they werent' supposed to be triggered.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2872)
- Remove legacy old version of kOS's computer from the parts
  definition file so it can't appear by accident in the parts bin.
  This is no longer needed for backward compatibility like it
  was before because this version of kOS cannot run on the
  very old versions of KSP that part was for anyway.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2893)
- When reporting the terrainheight of a geoposition, it no
  longer returns false results caused by seeing certain
  stock parts that put trigger colliders on the "terrain layer".
  [pull request](https://github.com/KSP-KOS/KOS/pull/2900)
- Fix SteeringManager believing RCS blocks were capable of
  more thrust than they were (causing steering to be tuned
  wrong when steering via RCS).  Problem was caused when stock
  parts now have multiple alternate RCS nozzle arrangements,
  and kOS was summing up all the thrust all the nozzle variants
  can do even though only a subset of those nozzles actually
  exist in any given variant.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2923)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2974)
- When setting the volume name for a disk drive by copying the
  vessel's name to the volume's name, it now strips out
  characters that are not allowed in volume names (but are 
  in vessel names, thus the bug).
  [pull request](https://github.com/KSP-KOS/KOS/pull/2944)
- BOUNDS now does a better job of calculating based on
  part's *colliders* rather than their visual meshes which
  don't always agree with the colliders.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2945)
- No longer bogs down as much when someone creates the same
  LOCK expression repeatedly in a loop. (Still not a good idea,
  but kOS tolerates it better now.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2965)
- Performance:  No longer pays the cost of tracking a stopwatch
  when the user doesn't even have profiling turned on so they're
  not looking at the timings anyway.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2969)
- A VOICE's volume is now persisting properly after playing a
  NOTE.  Previously playing the NOTE caused the VOICE volume
  setting to get clobbered by the NOTE's volume.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2978)
- Make it so kOS's ModuleCargoPart settings don't break in
  older KSP 1.10.x (which doesn't have ModuleCargoPart).
  [pull request](https://github.com/KSP-KOS/KOS/pull/3003)
- Fix a bug when a thing that is locked is used as the
  left side of a suffix when setting the suffix.
  [pull request](https://github.com/KSP-KOS/KOS/pull/3010)
- Prevent a local variable from clobbering a builtin name
  [pull request](https://github.com/KSP-KOS/KOS/pull/3016)
- Allow kOS code to "see" a change to a manuever node's ETA
  made outside the script, after having obtained the node
  in a variable.
  [pull request](https://github.com/KSP-KOS/KOS/pull/3040)
- Fix Compiler exceptions not showing the filename correctly.
  [pull request](https://github.com/KSP-KOS/KOS/issues/3018)
- Fix ALT:RADAR sometimes wrong when high above ground.
  [pull request](https://github.com/KSP-KOS/KOS/issues/2902)
- Fix race condition that caused terminal to spam the log
  on scene changes and sometimes spam the log enough to
  lag the game for some people.
  [pull request](https://github.com/KSP-KOS/KOS/issues/2925)
- Fix throwing exception when setting SASMODE while the
  navball is hidden.
  [pull request](https://github.com/KSP-KOS/KOS/issues/3045)
- Made the doc generation scripts work on python 3.x
  [pull request](https://github.com/KSP-KOS/KOS/issues/3069)


## v1.3.2.0 - Don't Steer Me wronger

A quick patch to v1.3.0.0 that fixes issue #2857 that would
zero controls for just a brief single physics frame if
raw control neutralize had been previously used or if a
reboot had occurred while raw controls were in use.  Most
players won't notice a single physics frame of zeroed
controls, but if you're using realism mods with limited
engine ignitions, it would unfairly consume an engine

was disasterous for those engines that only get one
ignition.)

Normally one bug fix wouldn't warrant a release, but this
bug was caused by changes in v1.3.0.0, and the consumed
ignition was unfair.


## v1.3.1.0 - Don't Steer Me Wrong, this time

A quick patch to v1.3.0.0 that fixed issue #2850 where
one or two places in the code still used TimeSpan where
they were supposed to have been changed to use TimeStamp.


## v1.3.0.0 - Don't Steer Me Wrong

There's a lot of small changes over the last year that have added
up to a big release.  This release supports KSP 1.10 and KSP 1.11.

The most important changes are probably in steering and control.
Cooked steering shouldn't waste as much RCS as it used to, and if you
are using raw control you now have the ability to set the player's trim
settings for yaw, pitch, and roll so you can steer using those and
not completely lock the player out of control.  There is also a
panic button for telling kOS to suppress all of its controls if the
player needs to take over the controls regardless of what the script
is doing.

As always, recompile KSM files with this release.  Especially as there
was an important KSM bugfix.

### Breaking Changes

- ``TimeSpan`` used to mean a fixed stamp in time (the name was not
  really accurate).  Now there are two types, ``TimeStamp`` and
  ``TimeSpan``, and the one that USED to be called ``TimeSpan``
  is now called ``TimeStamp``, with ``TimeSpan`` now being a new
  type that didn't exist before.  This could affect scripts if
  you ever did a check for ``:istype("TimeSpan")`` (because of
  the rename) but shouldn't affect anything else.
- **Even more than usual it's important to recompile any KSM files.**
  A major bug in how KSM files were written was discovered that this
  release fixes.  There's a chance your existing KSM files may
  already be wrong.  If you have any bug reports about a KSM file
  not working right, please try testing again with this release by
  recreating the KSM file.  There's a small chance you might have
  had the bug this release fixes.  (Look for "KSM" in the bug section
  below.)
- If you are using the output of ``SteeringManager:WRITECSVFILES``,
  be warned that output now has a new column in the second-to-last
  position, the MinOutput column.  That means the MaxOutput column
  has shifted one position to the right.  This should only affect
  people who are analyzing that data with external software.
- In order to support Kerbal Alarm Clock version 3.0.0.2 or higher
  it was necessary to break compatibiltiy with versions of Kerbal
  Alarm Clock that are older than that.  The API wrapper changed
  enough that backward compatibility is too messy to maintain.
- The ``:LIST`` suffix of ``VOLUME`` said in the documentation
  that it returns a ``LIST`` when in reality it always returned
  a ``LEXICON``.  If you relied on this and wanted the lexicon
  not the list, you need to now use the new suffix ``:LEXICON``
  because the old suffix ``:LIST`` has been changed to match the
  documentation and be a real actual LIST now.
- Temperature tolerance for the kOS parts was way too high, making
  them effective heat shields when they shouldn't be. If you had been
  taking advantage of this before that might not work anymore.
- CREATEORBIT() used to take mean anomaly at epoch as a value in
  radians, which didn't match how everything else in kOS works.
  It is now expecting it in degrees as described in BUG FIXES
  below.
- If you ever happend to have a string literal with a backslash
  followed by a quote mark (``\"``) that has now become a special
  escaped quote char and is no longer literally a backslash and
  quote mark.

### New Features

- Maneuver Nodes can now be constructed with either ETA time or
  with UT time, and you can read their time either as UT (``:TIME``)
  or as ETA (``:ETA``).  They also can take in the new ``Timestamp``
  or ``TimeSpan`` types instead of just a Scalar number of seconds if
  you like.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2846)
- There's a new button, "Reread Boot Folder", on the kOS toolbar
  window when you're in the VAB or the SPH.  This button lets you
  tell kOS to re-read the boot directory when you've just added
  a new file to it, so you don't have to leave the VAB and come
  back for it to show up in the list of boot files.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2839)
- The old ``TimeSpan`` type has been renamed to ``TimeStamp`` and
  a new ``TimeSpan`` type has been made in its place.  This is to
  fit the design pattern where a "stamp" is a fixed point in time (a
  date and a time of day) and a "span" is a time offset.  The main
  difference in Kerbal is whether you count years and days
  starting at 1 or at 0.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2837)
- Suffixes ``:PARTSTAGGED``, ``:PARTSNAMED``, and ``:PARTSDUBBED``
  can now be used with parts instead of with entire vessels. Doing
  so searches just the sub-branch of the ship starting from that
  part, instead of the whole ship.
  [pull reqeust](https://github.com/KSP-KOS/KOS/pull/2821)
- Added a new Suffix to RCS parts, ``:DEADBAND`` that lets you
  finally override the game's enforced 5% deadband on RCS
  controls. It turns out the deadband isn't in the *controls*,
  but rather it's in the RCS Parts themselves and doesn't apply
  to other torque sources like reaction wheels.  That's why
  you notice it when translating (where only RCS works) and not
  when rotating (where reaction wheels do something and take
  up the slack left by the RCS thrusters not responding).
  [pull request](https://github.com/KSP-KOS/KOS/issues/2811)
- **Big overhaul to SteeringManager's internals**:
  There's been some important refactoring in SteeringManager that
  should reduce the control vibrations and consequently the
  RCS fuel wastage especially in Realism Overhaul (which relies
  more on RCS than stock does).  Also, there's some
  user-settable epsilon values - if you want to change the tuning
  you can adjust ``SteeringManger:ROTATIONEPSILONMIN`` and
  ``SteeringManager:ROTATIPONEPSILONMAX``.
  - Dynamic Epsilon logic to reduce control jitter: [pull request 2810](https://github.com/KSP-KOS/KOS/pull/2810) [pull request 2813](https://github.com/KSP-KOS/KOS/pull/2813)
  - The stock KSP's available torque calculations are random wrong values for RCS parts.  kOS now replaces that with its own calculation instead: [pull request 2820](https://github.com/KSP-KOS/KOS/pull/2820)
- The random number generator now can be fed a seed.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2801)
- PIDLoop is now a serializable structure so you can save
  your PID's settings and bring them back from a file. Also
  PIDloop's CSV output now has a Minoutput column.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2795)
- Enlarged max allowed terminal font size to 48, to benefit
  people with tiny pixels (i.e. 4k monitors).
  [pull request](https://github.com/KSP-KOS/KOS/pull/2794)
- Uses the changes to Kerbal Alarm Clock's API that started
  with Kerbal Alarm Clock v3.0.0.2.  (This does break compatibility
  with older versions of Kerbal Alarm Clock, though.)
  [pull reqeust](https://github.com/KSP-KOS/KOS/pull/2790)
- *On-Screen warning when SAS is fighting kOS*:  The message
  appears when both SAS and lock stering have been active for a
  few seconds and goes away when one or the other is turned off.
  [pull request 2780](https://github.com/KSP-KOS/KOS/pull/2780)
  [pull request 2783](https://github.com/KSP-KOS/KOS/pull/2783)
- **Emergency Suppress Autopilot**: You can now click an emergency
  toggle button on the kOS toolbar dialog window that will
  temporarily suppress all of kOS's locked steering so you have
  manual control.  If you use this, the script will still keep
  running and *think* it's moving the controls, but the steering
  manager will ignore the script's commands until you turn the
  suppression toggle off.  *This can also be bound to an
  action group for the kOS PartModule if you want a fast hotkey
  for it.*
  [pull request](https://github.com/KSP-KOS/KOS/pull/2779)
- ``Part`` suffixes that allow you to traverse the symmetrical
  sets of parts.  (i.e. if you place 4 fins in radial symmetry,
  and have a reference to one of the fins, you can find the other
  3 fins that it is symmetrical with.)in the same symmetry set)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2771/files)
- The player's own TRIM controls are now settable by script.
  (Example use case: You want an autopilot to control an
  airplane by moving the trim but not the main controls so
  the player is still free to push the main control stick at
  any time).
  [pull request](https://github.com/KSP-KOS/KOS/pull/2769)
- ``ETA:NEXTNODE`` now an alias for ``NEXTNODE:ETA``
  [pull request](https://github.com/KSP-KOS/KOS/issues/2648)
- Trajectories Addon updated to support Trajectories v2.4 changes.
  (Thanks PiezPiedPy)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2747)
- Kuniverse:launchcraftwithcrewfrom()
  (Thanks JonnyOThan)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2740)
- New suffixes for the special case where a Vessel is really an
  asteroid. (Thanks JonnyOThan)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2736)
- Ability to read the stock game's Delta-V readouts for the vessel.
  (Thanks ThunderousEcho)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2719)
- New subtype for ``Part`` - the ``RCS`` part type, with information
  about how its nozzles are aimed, what fuel it uses, the ISP,
  max thrust, etc. (Thanks RCrockford)
  [pull request 2678](https://github.com/KSP-KOS/KOS/pull/2678)
  [pull request 2809](https://github.com/KSP-KOS/KOS/pull/2809)
- Can now use ``\"`` in string literals for embedded quote marks.
  Also can prepend the string with ``@`` to turn this off and keep
  it literal. (thanks thexa4)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2673/)
- New Engine value suffix ``:CONSUMEDRESOURCES``, and new
  Type ``ConsumedResource`` it returns. These
  give more information about fuels the engine uses.  Mostly
  relevant when RealFuels mod is installed so every engine is a
  bit different. (Thanks RCrockford)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2662)
- Can CreateOrbit() from position and velocity (before
  it only worked with Keplerian parameters).
  (Thanks ThunderousEcho)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2650)

### Bug Fixes

- Fixed: Kerbal Alarm Clock alarms had no ToString() so when you
  printed them you saw nothing.  Now they show the alarm info.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2845)
- Fixed: The suffix ``Widget:HASPARENT`` was documented but didn't
  actually exist.  It exists now.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2844)
- Fixed: Primitives like Scalars, Strings, and Booleans previously
  were not serializable with WRITEJSON() on their own as bare
  variables.  They could only be written when inside containers like
  LIST() or LEXICON().  Now they can be written directly.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2842)
- Fixed: **KSM** files would corrupt one of the kRISC instruction
  operands (leading to any number of random results when running the
  program) if the size of the operand pack happened to be *just barely*
  over 2^8, 2^16, or 2^24 bytes. (When calculating how many bytes
  addresses need to be to access the enire operand pack, its count
  of the size of the pack was off by 3. This could make the last
  operand in the pack get garbled when it loaded into memory from
  some random other part of the file instead of where it was supposed
  to come from.)
  (Thanks to newcomb-luke for discovering the problem and the cause)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2827)
- Fixed how positions of packed vessels were off by one
  physics frame from the positions of everything else.  This
  is apparently how things are reported b the KSP API and this
  had to be adjusted for.
  (Thanks marianoapp)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2818)
- Fix Vecdraw labels no longer showing up in flight view
  [pull request 2799](https://github.com/KSP-KOS/KOS/pull/2799)
  [pull request 2804](https://github.com/KSP-KOS/KOS/pull/2804)
- Remove strange blank setting on the difficulty options
  screen.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2797)
- ``OPENPATH()`` now returns false on file not found rather
  than bombing out with an exception.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2793)
- RangeValue now allows use of bigger ranges and for ranges
  that increment by fractional amounts.
  (Before, it couldn't do floating point and couldn't do
  anything bigger than 2^31.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2792)
- Fix raw control ``:NEUTRALIZE`` never having quite done
  what it said it did right.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2770)
- EVA Kerbals no longer have duplicate KOSNameTags when
  you have the Breaking Ground DLC installed.  (The problem
  came from how KSP mashes two kerbal templates together
  into one kerbal to put the DLC science features into an
  EVA Kerbal.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2765)
- VOLUME:LIST now actually returns a list like it says
  in the documentation.  Use VOLUME:LEXICON to get
  the lexicon you used to get from VOLUME:LIST.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2763)
- UNSET now fails silently on non-existant variables as the
  documentation claims it should, instead of crashing with
  a nullref error.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2761)
- Fixed a mistake that made it possible to process lines of
  input out of order if they flood into the terminal very fast.
  It was noticed in JonnyOThan's TwichPlaysKSP, which pastes
  entire scripts of input into the interpreter in one big chunk.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2754)
- ADDONS:KAC:ALARM[n]:NOTES now returns the right thing.
  (It used to just return the same thing as :NAME.
  (Thanks JonnyOThan)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2738/)
- The ConnectionManager dialog box at the start of a career was
  repositioned to where it is unlikely to appear secretly hidden
  behind other mod's dialog boxes.  (Other mods putting their)
  dialogs in front of kOS's and not blocking clickthroughs made
  some users accidentally pick a ConnectionManager and dismiss
  the dialog before they ever saw it.)
  [pull request](https://github.com/KSP-KOS/KOS/issues/2733)
- UI sound effects from kOS (error beep, SKID sounds) no longer
  have an origin point in 3-D space inside the part.  They are
  now "ambient".  This is to get sound mods to stop dampening
  the volume the same way they'd dampen sounds from engine
  parts, etc.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2717)
- Parts no longer have excessive temperature tolerance.
  (Thanks robopitek)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2699)
- CREATEORBIT() now takes mean anomaly at epoch as degrees.  It
  was in radians before which didn't match how other things worked.
  (Thanks vzynev)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2689)
- Better fuel stability (ullage in RealFuels) calculation.
  (thanks RCrockford)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2677)
- Documentation fixes.  Too numerous to mention each.  You can
  click each of the links below to see them all:
  [pull request 2675](https://github.com/KSP-KOS/KOS/pull/2675)
  [pull request 2680](https://github.com/KSP-KOS/KOS/pull/2680)
  [pull request 2707](https://github.com/KSP-KOS/KOS/pull/2707)
  [pull request 2712](https://github.com/KSP-KOS/KOS/pull/2712)
  [pull request 2724](https://github.com/KSP-KOS/KOS/pull/2724)
  [pull request 2751](https://github.com/KSP-KOS/KOS/pull/2751)
  [pull request 2772](https://github.com/KSP-KOS/KOS/pull/2772)
  [pull request 2775](https://github.com/KSP-KOS/KOS/pull/2775)
  [pull request 2776](https://github.com/KSP-KOS/KOS/pull/2776)
  [pull request 2777](https://github.com/KSP-KOS/KOS/pull/2777)
  [pull request 2784](https://github.com/KSP-KOS/KOS/pull/2784)
  [pull request 2788](https://github.com/KSP-KOS/KOS/pull/2788)
  [pull request 2791](https://github.com/KSP-KOS/KOS/pull/2791)
  [pull request 2800](https://github.com/KSP-KOS/KOS/pull/2800)
  [pull request 2819](https://github.com/KSP-KOS/KOS/pull/2819)
  [pull request 2833](https://github.com/KSP-KOS/KOS/pull/2833)
- kOS can now handle KSP's technique of having multiple KSPfields
  of the same name that resolve the name clash by only having one
  visible at a time.  KSP started doing this on a few fields
  about a year ago and caused bugs like "authority limiter"
  not working. (https://github.com/KSP-KOS/KOS/issues/2666)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2667)
- kOS no longer allows ModuleManager configs to give it negative mass.
  (Antimatter summons the Kraken.)
  [pull reqeust](https://github.com/KSP-KOS/KOS/pull/2661)
- ETA:APOAPSIS no longer returns Infinity on hyperbolic
  orbits (While infinity is a correct answer, kOS scripts
  would crash when they get infinity on the stack. So now
  it says zero instead).
  [pull request](https://github.com/KSP-KOS/KOS/pull/2646)


## v1.2.1 Pathsep fix

v1.2'S DDS fix had a backslash path separator that broke it on UNIX
platoforms.  This quick fix does nothing more than switch it to a
normal slash, as that will work on all platforms.  Apparently the .Net
file libraries convert the path names one way but not the other.
(They will map things so Windows can work with the "wrong" separator,
but not do a similar mapping to make UNIX work with the "wrong"
separator, so UNIX separators are the only cross-platform path
separator to use.)


## v1.2 Unity Update

This update is mostly to make kOS compatible with KSP 1.8.x, which
started using a newer version of Unity, and a newer version of .Net,
which have some consequent changes in the code and build process.

### Breaking Changes

None that are known about, other than the usual reminder that
KSM files need a recompile after every version update of kOS.

### New Features

* Now forces both the toolbar window and the telnet welcome menu
  to list the kOS CPUs in a consistent unchanging sort order.
  Previously, it was pretty much random what order you would
  see kOS CPU's listed in the menu, which made it hard for
  JonnyOThan's Twitch-Plays-KSP chatbot to know which CPU it
  was attaching to when it sent commands to kOS.  This has been
  changed to a predictable sort order as follows: (1) Sort by
  which vessel the CPU is on, starting from the active vessel,
  and then for other vessels, sorting by distance from the active
  vessel, closest first. (2) When the same vessel has more than
  one CPU, break that tie by number of "hops" from the root part,
  such that CPU's attached closer to the root come first.  This
  is by "number of parts to walk through to reach root" rather
  than by actual physical distance, since using physical distance
  might have led to inconsistent sort order given that some ship
  parts can hinge and extend, changing that distance.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2601)
* New suffixes ``Dockingport:PARTNER`` and ``Dockingport:HASPARTER``
  will tell you which docking port this docking port is docked with.
  [issue](https://github.com/KSP-KOS/KOS/issues/2613)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2629)
* HEADING() Now allows optional 3rd argument, "roll".
  [issue](https://github.com/KSP-KOS/KOS/issues/2609)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2629)
* Let user-made GUIs toggle IMGUI's wordwrap flag with a 
  new suffix: ``Style:WORDWRAP``.  This should let you fix
  that annoying problem where a GUI Label would insist on
  wrapping even when it could have fit by making the window
  wide enough.  Setting wordwrap to false will force the
  GUI layout engine to keep the label's area wide enough
  to not wrap the text.
  [issue](https://github.com/KSP-KOS/KOS/issues/2599)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2629)
* Add BODYEXISTS test
  [issue](https://github.com/KSP-KOS/KOS/issues/2587)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2629)
* Allow FLOOR() and CEILING() to specify a decimal place other
  than the one's place, like ROUND() can do.
  [issue](https://github.com/KSP-KOS/KOS/issues/2556)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2629)
* Add a constructor, ``CREATEORBIT()`` that will make a new
  ``Orbit`` object for any hypothetical orbit given Keplerian
  parameters, without it coming from a vessel or a body that
  already exists.
  [issue](https://github.com/KSP-KOS/KOS/issues/2530)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2629)
* Added new suffix to waypoint: ``:ISSELECTED``, which will
  tell you if the waypoint is the one the user has selected
  for their navball.
  [issue](https://github.com/KSP-KOS/KOS/issues/2565)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2630)

### Bug Fixes

* Bound variables like SHIP, UP, VELOCITY, etc stopped existing
  in the KSP 1.8.x update.  This was because kOS makes use of 
  reflection techniques to store information about C# Attributes
  that help it find the bound variables in its code, and .Net 4.x
  changed the meaning of Attribute.Equals() in such a way that it
  broke what kOS was doing to store this reflection information.
  A Dictionary that kOS was using to track bound variables by Attributes
  started having key clashes because of that change to what it means
  for an Attribute to be Equal to another Attribute.
  ((No link to a github issue because this was part of the general
  KSP 1.8 update PR and didn't have an issue.))
* Prevent waypoints with bogus body names.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2593)
* Fix a problem that made the GUI terminal sometimes get stuck
  refusing to repaint when resized to a size too small to
  hold all the text it previously had showing.
  [issue](https://github.com/KSP-KOS/KOS/issues/2611)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2612)
* Several minor doc typos
  [pull request](https://github.com/KSP-KOS/KOS/pull/2628)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2638)
* The startup message about default font and "if you want the old look" was
  quite obsolete by now and needed to be removed.
  [issue](https://github.com/KSP-KOS/KOS/issues/2606)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2629)
* Changed the technique used to load DDS icons used in the
  kOS GUI terminal and the kOS toolbars, to bypass KSP's
  strange API and go directly to Unity.  This may or may
  not help people who had the purple square icon problem.
  ((No issue - SlimJimDodger contributed PR out of the blue.))
  [pull request](https://github.com/KSP-KOS/KOS/pull/2637)


## v1.1.9.0 Breaking Bounds

This update is a mix of new features, mostly

### Breaking Changes

### New Features

- Bounding box information for parts and for the vessel as
  a whole is now exposed for scripts to read.
  [pull request 1](https://github.com/KSP-KOS/KOS/pull/2563).
  [pull request 2](https://github.com/KSP-KOS/KOS/pull/2564).
- The above bounding box feature also came with some new suffixes 
  for Vecdraw so you can now draw plain lines (suppress the
  arrowhead, suppress the opacity fade) with them.
- Lexicons can now use the suffix syntax.  i.e. where you 
  say ``mylex["key1"]`` you can now say ``mylex:key1``,
  provided the key is something that works as a valid identifier
  string (no spaces, etc).
  [pull request](https://github.com/KSP-KOS/KOS/pull/2553).
- Can now set the default terminal Width and Height for all
  newly spawned terminals.
  [pull request 1](https://github.com/KSP-KOS/KOS/pull/2573).
- A ternary conditional operator exists in kerboscript now,
  using the syntax ``CHOOSE expr1 IF bool_expr ELSE expr2``.
  If *bool_expr* is true, it returns expr1.  If it's false,
  it returns expr2.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2549).
- Added support to read more atmospheric values from KSP.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2557).

### Bug Fixes

- TimeSpan now peeks at the KSP game to learn its notion of
  how long a day is, and how long a year is, rather than hardcoding
  the values.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2582).
- Fix cooked control triggers not working during a WHEN/ON trigger.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2534).
- Fix mangled state if kOS is out of electricity when scenes switch
  or the game is saved.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2521).
- Obsolete list command documentation removed.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2520).
- Allow part modules'd fields to work even when no GUI name is defined.
  It seems that the main game allows the GUI name to be left out and if
  so it inherits from the base name under tne hood.  Now kOS follows
  this behaviour.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2519).
- Prevent using UNSET on built-in variable names like SHIP, ALTITUDE,
  and so on.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2510).
- RP-1 used a different technique to lock out controls due to
  insufficient avionics that kOS didn't know about.  kOS bypassed
  this lockout and still controlled the vessel anyway.  This is no
  longer the case.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2546).
- PartModule:SETFIELD now works properly with the new type of slider
  widget that robotic parts use in KSP 1.7.x.  KSP introduced a new
  type of slider widget that presents false information when kOS tried
  to obey its min, max, and detent values, those being only dummy
  placeholders for these types of sliders, not actually populated with
  the real values.  For these sliders, the real limit values come from
  another field, requiring a more indirect method call to get the information.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2554).
- GUI windows no longer use the KSP control lock system to emulate
  keyboard focus, instead relying on the built-in Unity IMGUI
  focus rules for widgets, thus they won't 'steal focus' as much.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2577).


## v1.1.8.0 Engines and KSP 1.7 compatibility

Mostly this was motivated by a need to get an officially
recompiled-for-KSP-1.7 version out there (even though the previous
version worked on KSP 1.7, it wasn't officially compiled for KSP
1.7.)

Along the way there were one or two bug fixes and documenation
cleanups.

### Breaking Changes

- Not that we know of, unless you were unaware that some of 
  the bugs fixed were in fact bugs and had written a script
  to expect that behaviour as normal.  (Read the bug fixes
  below to be sure.)

### New Features

- Support of multiple-at-the-same-time engines that exist in
  some mods (but not in stock, as far as we can tell).  Stock
  contains single engines in a part, and multi-mode engines
  in a part (where only one of the engines in the part is
  active at a time, i.e. wet/dry mode engines or jet/rocket
  mode engines).  But some mods contain parts that have more
  than one engine in them that are selected *at the same time*,
  rather than toggle-switched like the stock multi-mode engines.
  One example is the RD-108 engine that the RealEngines mod
  provides.  Its main "straight" engines are one Engine Module,
  and its smaller "gimbal" engines around the edge are a second
  Engine Module.  Both modules are active at once and need their
  information aggregated to work with kOS's "an engine part is
  just one module" system.  This PR does so.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2498)
  **Special thanks to first time contributer RCrockford for doing
  all the legwork on this**.

### Bug Fixes

- The behaviour of ``LIST ENGINES`` in regards to multi-mode engines
  was restored to what it was supposed to have been.  Becuase of a
  small change KSP made, it's been wrong since KSP 1.5, apparently.
  Prior to KSP 1.5 it worked correctly by giving a list that contains
  one entry in the LIST ENGINES per engine. But since then it has been
  returning 3 duplicate instances in the list per each multi-mode engine.
  This release fixes it, and the previous correct behavior is restored
  (just returning one, not three).
  The problem was discovered during regression testing of
  the [pull request](https://github.com/KSP-KOS/KOS/pull/2498),
  so the fix is inside that same pull request.
- kOS could be rendered completely inert and broken if other mods not
  under kOS's control had broken DLL files.  Specifically, kOS would
  abort partway through initializing itself if any other DLL file in the
  entire KSP game had failed to load during the KSP loading screen.  kOS
  has a "reflection" walk through all the classes that hadn't accounted
  for the fact that .net apparently keeps a null stub of a class in memory
  after a class fails to load, rather than it just not existing at all
  like one would expect.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2492)
  (This was discovered with KSP 1.7 because KSP 1.7 broke some other
  mod's DLLs making them not load, but the problem was actually there
  all along waiting for some DLL file to trigger it.)
- Reworking the position of the Connectivity Manager Dialog box.
  Our exploratory reverse-engineering of just what the undocumented
  arguments to KSP's MultiOptionDialog mean, which was used to move the
  box to fix [issue 2456](https://github.com/KSP-KOS/KOS/issues/2456)
  were still wrong.  They didn't do exactly what we thought they did.
  (The misinterpretation became relevant when the player has UI scaling
  set higher than 100% and that pushed the dialog box off screen.)
  **Thanks to contributor madman2003 for doing more reverse-engineering
  on this and submitting the fix.**
  [pull request](https://github.com/KSP-KOS/KOS/pull/2493)
- Fix to bug where kOS scripts could no longer ``SET TARGET`` to a
  Celestial Body and could only set targets to vessels or parts.
  This bug was introduced in the previous release of kOS by a
  hamfisted typing error while fixing the fact that Body wasn't
  serializable.  It was an error that unfortunately didn't result
  in any noticable problem when compiling or testing, as it 
  *only* removed the Body's declaration that "I am the kind of class
  that knows how to be a target" and it affected nothing else.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2501)
- Several small documentation edits:
  [pull request](https://github.com/KSP-KOS/KOS/pull/2503),
  [pull request](https://github.com/KSP-KOS/KOS/pull/2505),
  [pull request](https://github.com/KSP-KOS/KOS/pull/2506)
- Trying to toggle the ``panels`` value on or off would result
  in infinite log spam if the ship contained a fixed undeployable
  solar panel like the OX-STAT.  kOS was watching for the existence
  of ModuleDeployableSolarPanel to see if the part could be deployed
  or not, but apparently at some point KSP started defining all
  solar panels as having ModuleDeployableSolarPanel, even if they're
  not actually deployable.  Now kOS doesn't treat the panel as
  deployable unless it also has its animation property defined in
  addition to claiming to be a ModuleDeployableSolarPanel.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2504)


## v1.1.7.0 Lets get Serial

Mostly fixes.  The motivation for this release is to get fixes
out to the public before KSP 1.7 comes.

Built for KSP 1.6.1

### Breaking Changes

- Compatibility for the old Infernal Robotics is officially removed
  in favor of support for the "IR Next" mod.

### New Features

- Support for the "IR Next" mod. (The only infernal robotics
  mod was no longer being updated anyway and didn't work on
  KSP 1.6.1.  But IR Next, although not officially released yet,
  does work on 1.6.1, so we switched to that.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2354)
- More types are now serializable as messages or JSON files:
  Notevalue, Direction, RGBAcolor, and HSVAcolor.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2466)
- ``CORE:TAG`` is now settable
  [pull request](https://github.com/KSP-KOS/KOS/pull/2472)
- ``KUNIVERSE:PAUSE`` suffix added.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2477)
- Added a new ``TIME(seconds)`` Constructor to make a 
  ``Timespan`` out of a Universal timestamp.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2478)
- New ``LIST FONTS.`` feature so the user can see which font
  names are loaded into Unity for use in user GUIs.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2481)

### Bug Fixes

- Several documentation alterations:
  [pull request](https://github.com/KSP-KOS/KOS/pull/2442)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2446)
- kOS would throw a Nullref if a script tried to check for a CommNet
  connection to a vessel that has been classified as type "debris".
  [pull request](https://github.com/KSP-KOS/KOS/pull/2447)
- Sometimes kOS broke the Space Center, making the buildings impossible
  to click on.  This was caused by input locks not letting go when the
  terminal is open while the kOS physical part gets exploded.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2443)
- Fix to the kOS icon being broken (showing just a purple square) in Blizzy
  Toolbar mod.
  [pull request(https://github.com/KSP-KOS/KOS/pull/2454)
- GeoPosition was written improperly in messages or JSON files.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2460)
- The "hue" part of HSV colors was never quite implemented properly from
  when it was first introduced. (It was mapping all hue numbers down
  into just 1/6th of the full range of hues, so greens and blues
  were not available.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2462)
- When using the message queue system while Remote Tech is installed,
  you could not send messages to vessels far away outside the load
  distance bubble (i.e. 2.5km-ish).  This is fixed.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2457)
- Vecdraws were incapable of drawing dark colors like black because they
  were using an additive-only shader.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2468)
- Fix a case where cooked steering from the terminal refused to let go if
  a subsequent kerboscript error is typed into the same terminal.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2471)
- If "run once" was used, and the system chose not to run the program
  because it was already run, it was possible for the stack to get
  corrupted in a way that confused defaulted parameters to programs.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2476)
- Fixed Multimode engine bug that was introduced in v1.1.6.1.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2479)
- Moved kOS dialog box to a new position to fix a clickthrough
  problem that caused you to secretly pick a kOS connectivity
  manager without realizing it when you click on things in the
  Remote Tech dialog box.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2480)


## v1.1.6.3 Folder Path Protection

Built for KSP 1.6.1

This is a patch for protecting against some kinds of file folder
access that concerned us for those people using kOS to set up
"Twitch Plays kOS" streams.

### Bug Fix:

If you currently have a "Twitch Plays kOS" stream, or plan to
set up one in the future, PLEASE see this writup:

    https://github.com/KSP-KOS/KOS/issues/2439


## v1.1.6.2 Quickfix (Image Files to DDS)

Built for KSP 1.6.1

Nothing but a quick patch to v1.1.6.0.

### Bug Fix

The v1.1.6.0 update resized a few of the PNG images used
in the GUI panels, which exposed a bug that only manifests
on some graphics cards.  KSP converts PNGs to DDS format
upon loading them, and appears to use the Direct3D graphics
driver to do so.  Older graphics cards refuse to do that
conversion on images that aren't exactly expected sizes.
We were just "lucky" that this never happened in the past
with the image sizes we were using.  Converting them to
DDS ourselves and shipping them that way, we bypass this
problem because the user's own graphics drivers aren't
responsible for doing the conversion.


## v1.1.6.1 Quickfix (MAXTHRUST air pressure)

Built for KSP 1.6.1

Nothing but a quick patch to v1.1.6.0.

### Bug Fix

v1.1.6.1 had a flaw in MAXTHRUST, AVAILABLETHRUST,
and engine ISP calculations that always calculated them
as if your ship was in vacuum even when it's not.  This
was deemed an important enough problem to warrant a
quick-fix release.


## v1.1.6.0 It's been too long without a release.

Built for KSP 1.6.1

It's been a long time without a release.  We kept putting it off until
"that one more thing" was merged in, and there was always "that one more
thing", again and again, that kept putting off the release more and more.
Eventually we decided to release what we had since there's so many fixes
the public weren't getting.

This release incorporates 50 separate Pull Requests from many individuals.
As always, thanks to everyone who contributed over the last year.  (Has it
really been that long?  Almost.)

### Breaking Changes:

(None that we know of, but this is a big update so we could have missed
something.)

### Bug Fixes:

- Was reading POSITIONAT() from the wrong orbit patch when getting a
  prediction for the moment when a patch transition should occur.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2253)
- Stage:resources gave wrong values in cases of stages without a decoupler.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2256)
- Several documentation clarifications.  See individual links below for
  more details:
  - [dead links on Vector doc page](https://github.com/KSP-KOS/KOS/pull/2269)
  - [a typo](https://github.com/KSP-KOS/KOS/pull/2300)
  - [a spelling error](https://github.com/KSP-KOS/KOS/pull/2333)
  - [sphinx old code deprecated](https://github.com/KSP-KOS/KOS/pull/2339)
  - [HUDtext style corner documented wrong](https://github.com/KSP-KOS/KOS/pull/2340)
  - [cleanup gh-pages branch having source in it](https://github.com/KSP-KOS/KOS/pull/2342)
  - [Mention Simulate in BG needed](https://github.com/KSP-KOS/KOS/pull/2368)
  - [SKID clarification](https://github.com/KSP-KOS/KOS/pull/2388)
  - [Error in basic tutorial example](https://github.com/KSP-KOS/KOS/pull/2392)
  - [Removed old obsolete notices](https://github.com/KSP-KOS/KOS/pull/2401)
- Fixed error detecting VT100 terminals in telnet (used wrong substring compare).
  [pull request](https://github.com/KSP-KOS/KOS/pull/2273)
- Fixed bug of multiple ON triggers melting their "prev value" trackers together
  if the triggers came from the same line of source code.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2275)
- Fix a bug with RemoteTech autopilot premissions getting lost.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2276)
- WHEN/ON statements inside anonymous functions now working properly.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2291)
- (attempt to?) Fix problem where bootfiles weren't copied in Mission Builder
  missions.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2292)
- Massive refactor of how trigger interrupts work, that allows them
  to behave more consistently and allows more complex layering
  of triggers.  (In this CHANGELOG, This is listed both under "new
  features" and "bug fixes" since it's both.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2296)
- Fix stack alignment bug that happened when a bootfile runs a
  KSM file that locks steering:
  [pull request](https://github.com/KSP-KOS/KOS/pull/2298)
- Fix: Locked steering refusing to let go if the IPU boundary
  lands right in the middle of kOS's steering trigger (kOS
  not having "atomic sections", the ordering of the opcodes
  mattered a lot). 
  [pull request](https://github.com/KSP-KOS/KOS/pull/2302)
- Fix: Undocking/Decoupling while a kOS unit on the lower half
  has locked steering used to cause the lower stage's kOS unit to spin
  the upper stage's steering and never let go of it.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2315)
- Fix: Hyperbolic orbits now allow negative anomaly angles to
  represent measures "prior to periapsis" correctly.  (Previously
  it represented a value like -10 degrees as +350 degrees, which
  doesn't make sense if the orbit isn't closed and won't come back
  around.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2325)
- Fix: E,S, and R keys now working right in text editor widget in
  Linux port. kOS incorrectly prevented the E, S, and R keys from
  passing through to other widgets before.  This error was only
  noticed on Linux because Unity3d's event queue passes through
  widgets in a different order on different OS ports.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2334)
- kOS will now let go of the steering when the program dies
  due to a lack of electricity.  This allows your vessel to get some
  power recharging again when it starts getting sun on the solar panels
  again.  (Previously the steering deflection was still present, meaning
  the ship needed a recharge rate higher than the power the torque wheel
  expended in order to actually get a net positive recharge.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2336)
- Fix: UTF-8 text files that contain a BOM (Byte Order Mark) are now
  parse-able.  (Notepad.exe was really the only text editor that
  triggered this problem.  No other editors put a BOM in UTF-8 files.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2353)
- Fix: If you lock steering from the interpreter, then also run
  a program that locks steering, that program used to bomb with error
  when it tried to exit and return to the interpreter.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2357)
- Fix: Using the meta-key AltGr on some European keyboards was causing
  garbage to appear in the terminal interactive prompt, but only on the
  Linux port of Unity3d.  Again, Unity3d does weird things in its Linux
  port for no apparent reason (they're not because of the OS itself),
  that we have to accommodate.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2386)
- Fix: Bulkhead profile added to part files.  It is required for the
  new KSP 1.6.x filtering "by diameter" feature.  Without it, the VAB
  could hang forever when a user clicks that tab.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2386)
- Fix: Map View no longer rotates with the vessel when focus is on
  the terminal window.  It's a stock bug that required a bit of 
  trial and error to pin down, then an ugly kludge to keep it from
  being triggered.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2403)
- Fix: OrbitInfo:TOSTRING now prints the body name properly.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2408)

### New Features:

- Made several of the string parameters to GUI widgets optional.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2293)
- Massive refactor of how trigger interrupts work, that allows them
  to behave more consistently and allows more complex layering
  of triggers.  (In this CHANGELOG, This is listed both under "new
  features" and "bug fixes" since it's both.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2296)
- Allow "close window" button to exist on the RMB menu.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2329)
- New suffixes to read if Body has a surface, an ocean, or children.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2355)
- Added KUNIVERSE:REALTIME and KUNIVERSE:REALWORLDTIME.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2362)
- Vecdraw now can set updater delegates directly in its constructor.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2369)
- All command codes in a script text file will be treated as whitespace
  now, just in case there's any in there junking up the file.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2374)
- Add a "CID" Craft-ID suffix to Parts.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2378)
- Constant:G is now being calculated from the game itself instead of
  being a manually typed constant in the kOS source.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2410)
- New value, Constant:g0 - useful for ISP calculations.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2415)
- Make terminal's "dim" unfocused mode stop being transparent, for extra
  readability.  (It was never transparent enough to usefully see through,
  but it was transparent enough to make it hard to see the letters.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2417)
- GUI tooltips now implemented.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2414)
- Fix: All the image files and texture files are using .DDS format now,
  and both X and Y resolutions for them have been resized to exact powers
  of 2, which DDS requires.  (Unity loads DDS files faster, and they
  form a smaller download ZIP).
  [pull request](https://github.com/KSP-KOS/KOS/pull/2389)


## v1.1.5.2 Basic Compatibilty for KSP 1.4.1

Built for KSP 1.4.1

This release is mostly just a recompile to make kOS work with
KSP 1.4.1, with the few changes that were needed to keep it
working, and whatever bug fixes happened to already be 
implemented when when KSP 1.4.1 came out.

### Bug Fixes:

- Callbacks where the delegate was created using :BIND now work.
  (Thanks to firda-cze for finding and fixing the problem.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2238)


## v1.1.5.0 HotFix for nested function scope.

Built for KSP v1.3.1

This release is just to fix one bug introduced by v1.1.4.0
that was discovered post-release by the users, during the
Christmas-NewYears time.  The fix was quick but release
was delayed for after the holidays.

### Breaking Changes:

None that we know of.  This change shouldn't even require
recompiling KSM files, presuming you had them recompiled
already for v1.1.4.0.

### Bug Fixes:

- The default scope for ``declare function`` when you say  neither
  ``local`` nor ``global``, was always defaulting to ``global``
  in the previous release (kOS 1.1.4.0), when it was supposed to be
  context dependent.  It was meant to be ``global`` only when the
  function is at outermost file scope, but ``local`` when the
  function is nested at any inner scope deeper than that.  This is
  now fixed, and this bug is the main reason for this hotfix release.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2206)
- The above bug also exposed a vulnerability in how kOS's own errors
  (ones that are the dev's fault, not the user's fault) are dealt
  with.  If ReplaceLabels() (a final step of loading a script into memory,
  that happens when you RUN a .ks or .ksm file) threw an exception,
  then the user would see the same error message repeating forever,
  and never get control of that kOS computer back again.  (This
  vulnerability was introduced when compiling was moved to its own
  thread, for complex reasons, but only just discovered now because
  this was the first time ReplaceLabels() had an exception since that
  move had happened.)  It is fixed now.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2205)


## v1.1.4.0 Does faster compilation break a work flow?

Built for KSP v1.3.1

This release was primarily focused on speedups and smoothness
of execution.  We welcomed a new developer (github username @tsholmes)
who contributed a lot of bottleneck analysis and code speedups.  The
goal was to reduce the burden kOS causes to the physics rate of the
game, and consequently also allow tech tree scaled performance by era
for the kOS computer parts themselves (slow at first, faster later).

### Breaking Changes:

- If you use the compiled script feature **YOU MUST RECOMPILE ALL KSM FILES,
  USING KSM FILES COMPILED IN A PREVIOUS VERSION WILL RESULT IN AN ERROR.**
- Files now have an implied local scope, causing the following change:
  - **Previously:** If you declared a variable as ``local`` at the
    outermost scope of a program file (outside any curly braces),
    then it had the same effect as ``global``, creating a variable
    that you could see from anywhere outside that program file.
  - **New behavior:** Now that there is an outermost scope for a file,
    ``local`` actually means something in that scope.  To get the
    old behavior you would need to explicitly call the variable
    ``global``.
  (The variables magically created via the lazyglobal system will still
  be global just like they were before.)
- Parameters to programs now have local scope to that program file.
  (Previously they were sort of global and visible everywhere, which
  they shouldn't have been.  If you relied on this behavior your
  script might break.)  This is of particular note when working with locks and
  triggers as the local parameters may conflict with the global scope of these
  features.
- Functions declared at the outermost scope of a program will now
  keep proper closure, making them see variables local to that program
  file even when called from outside that file.  This may hide a global
  variable with a more local variable of the same name, when previously
  the global variable would have been accessible from the function.
  (You probably weren't relying on this buggy behavior before, but
  if you were, this fix will break your script.)

### New Features:

- **File scope**: Previously, kerboscript did not wrap program files
  in their own local scope.  (Declaring a ``local`` in a file had
  the same effect as declaring a ``global`` there).  Now each program file
  has its own scope (and also the parameters passed to a program file
  are local to that file scope).
  - NOTE: For backward compatibility, there is one important exception
    to the file scope - functions declared at the outermost level by
    default can be globally seen in other programs.  You *CAN* get functions
    that are local to the file's scope, but you have to explicitly include
    the ``local`` keyword in the function declaration to make that happen.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2157)

### Optimizations:

- The regular expression syntax used to compile programs has been heavily
  modified to speed up file parsing using start string anchors and eliminating
  string copying.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2145)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2172)
- Suffix lists are no longer initialized on every call, saving both execution
  time and memory.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2136)
- Various string operation optimizations for internal string lookups.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2137)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2142)
- The cpu stack was re-written to use two stacks instead of using a single stack
  with hidden offsets.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2138)
- Cache type lookup data for suffix delegates.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2144)
- Begin encoding identifiers directly in opcodes instead of pushing a string
  identifier prior to executing the opcode.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2156)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2181)
- General optimizations for the C# source code, including for unit tests.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2139)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2140)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2148)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2150)

### Bug Fixes:

- Functions at the outermost file scope level now have closures that can
  see the file scope variables properly.  Previously they could not (but
  this did not matter since there was no file scope to matter.  This bug
  got exposed by the other file scope changes.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2157)
- Fixed inability to use flight controls on a craft with local control when
  RemoteTech is installed, both with and without a probe core installed.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2128)
- Fixed a crash to desktop when attempting to parse very large numbers.
  [pull requst](https://github.com/KSP-KOS/KOS/pull/2134)
- Fixed syntax errors in the exenode tutorial documents.  The code as displayed
  has been tested to work correctly as of this release.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2188)
- Parsing numbers on host computers that normally expect the `,` character to
  be used as a decimal symbol will no longer be blocked.  kOS now forces the use
  of `CultureInvariant` when parsing numbers, so all locales will be required
  to use the `.` character for decimals.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2196)
- Action Groups Extended support should once again work as the the method used
  to detect that the mod is installed has been repaired.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2189)
- Attempting to delete a path that does not exist no longer throws a null
  reference error.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2201)
- Documentation was added for `part:hasmodule` suffix.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2202)


## v1.1.3.2 (for KSP 1.3.1) New KSP version HOTFIX

This version is functionally identical to v1.1.3.0, however the binaries are
compiled against KSP 1.3.1 to allow it to properly load with the updated version
of KSP

### Breaking Changes:

- This build will not work on previous versions of KSP.

### New Features:

(None)

### Bug Fixes:

(None)


## v1.1.3.1 (for KSP 1.2.2) Backward compatibility version of v1.1.3.0

### Only Use If You Are Stuck On Ksp 1.2.2.

If you are on KSP 1.3, use kOS v1.1.3.0 instead of this one.
This version *will fail* if you use it on KSP 1.3.

This is identical to kOS v1.1.3.0 except that code specific to KSP 1.3
was removed, and it was re-compiled against KSP 1.2.2 libraries.

(The incentive to make such a release available was mostly because
Realism Overhaul typically stays a version behind for quite a while).


## v1.1.3.0 (for KSP 1.3) Bug Swatting Release

For this release we instituted a rule partway through that only bug fixes
should be allowed (some of the first few changes were enhancements rather
than bug fixes, but after that, its all bug fixes).  This was in a vain
hope that doing so would get a release out faster than normal.

### Breaking Changes

(Can't think of any.)

### New Features

* Terminal input using any Unicode character, not just ASCII.
  (Technically not a new feature, but a bug fix to a feature
  from the previous version, but since the bug made the feature
  never work *at all* in the past, it feels like a new feature).
  [pull request](https://github.com/KSP-KOS/KOS/pull/2062)
* New StartTracking suffix for "unknown objects" (asteroids).
  [pull request](https://github.com/KSP-KOS/KOS/pull/2077)

### Bug Fixes

* A large refactor of how the various flight control methods track
  which vessel they control.  This appears to have fixed a lot of
  bugs where kOS lost the ability to control the ship unless
  you reloaded the scene.  (After a docking, undocking, staging,
  vessel switch, or scene switch, this would sometimes happen,
  but not consistently enough to be easy to debug).
  [pull request](https://github.com/KSP-KOS/KOS/pull/2100)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2063)
* Program aborts caused by external events such as poweroff,
  shutdown, or control-C no longer leave garbage behind in
  memory still hooked into parts of kOS.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2019)
* Documentation now more explicitly mentions how SAS and lock steering
  fight with each other.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2111)
* Documentation for GUIskin:add() was wrong.  Fixed.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2098)
* The waypoint() constructor used to fail on waypoints which
  were *not* part of a cluster yet were named as if they
  were part of a cluster anyway ("my waypoint Alpha",
  "my waypoint Beta", "my waypoint Gamma", etc).  This doesn't
  happen in stock, but does happen with several mods that use
  ContractConfigurator.  kOS will now deal with such waypoints.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2093)
* Documentation that claimed obsoleted TERMVELOCITY still
  exists has been removed or edited.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2067)
* Trying to examine the NoDelegate object no longer causes
  nullref error.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2082)
* Equality operator ( == ) when comparing a Path to a Path now
  fires off correctly.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2089)
* GUI's ONRADIOCHANGE callback hook now no longer depends
  on the existence of an ONTOGGLE hook to fire off.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2088)
* Compiler no longer creates incorrect opcodes for indexed
  collections used as arguments to a function call that's
  on the lefthand side of an assignment statement.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2079)
* Font resizing in scripts no longer causes the terminal to mangle
  its size and width/height character count
  [pull request](https://github.com/KSP-KOS/KOS/pull/2081)
* Signal delay progress bar (when using Remote Tech) will now resize
  properly when you have a nonstandard sized terminal window.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2076)
* Compile command now works properly when run from the interpreter.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2071)
* Vessel:isDead working properly now
  [pull request](https://github.com/KSP-KOS/KOS/pull/2070)
* Stretching the terminal to a large size no longer causes
  the rounded corner to obscure text in the window.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2060)
* Full unicode keyboard and file save support was getting
  mangled by wiping out the high byte leaving only the 8-bit
  ASCII part left.  Fixed.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2062)
* Toolbar Panel setting changes no longer require there to
  exist a kOS part loaded into the scene.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2058)


## v1.1.2 (for KSP 1.3) No change - just fixing version.

There was a version number problem in our CKAN files that
required us to issue an update and doing so required a
version number increase.  There is no other change in
this version.


## v1.1.1 (for KSP 1.3) KSP 1.3 compatibility recompile.

No known intentional changes other than editing a few method calls
to the KSP API to make it work with KSP 1.3.

Also updated the included ModuleManager to version 2.8, which
is a necessity for compatibility with KSP 1.3.


## v1.1.0 (for KSP 1.2.2) Ewww, everything's GUI.

### Breaking Changes

* Because of changes to make the terminal use a real font from your OS, we had
  to obsolete TERMINAL:CHARWIDTH.  You can only choose TERMINAL:CHARHEIGHT.
  Each font has its own hardcoded notion of how wide a letter will be at a
  given height, which you can't override.
* CONFIG:BRIGHTNESS was moved back to the global config section, and is no longer
  set on the "difficulty" options screen, because it's not supposed to be a
  per-saved-game setting, but a user-interface preference that spans all saved games.
* ATM:SEALEVELPRESSURE now gives the answer in different units than it used to.
  (It was in KiloPascals even though the documentation claimed it was in atmospheres.
  Now it's in atmospheres to agree with the documentation.)

### New Features

* **GUI-making toolkit**. You are now able to make a GUI window that your kerboscript
  code can control, including buttons, sliders, toggles, checkboxes, etc.  It uses the
  KSP game's default skin (kind of big letters) but the skin can be customized by the
  script a bit to change things.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1878)
  [pull request](https://github.com/KSP-KOS/KOS/pull/2006)
  documentation: search for "GUI" (http://ksp-kos.github.io/KOS_DOC/structures/gui.html).
* **Background compilation**.  Now the game continues its simulation normally and physical
  events keep happening, while kOS is taking a few seconds to compile a script.
  (Gets rid of that familiar frozen game effect when you first issue a ``RUN`` command.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/1941)
* **Terminal Font**.  Now the kOS in-game terminal window uses a real font from your OS itself
  to render the text terminal.  (This allows the display of any Unicode character the font can
  render, and it allows nicer looking font size changes.)  Previously kOS painted images for
  letters from a hardcoded texture image file.
  [pull request 1](https://github.com/KSP-KOS/KOS/pull/1948)
  [pull request 2](https://github.com/KSP-KOS/KOS/pull/2008)
* **Allow any unicode**.  The kerboscript parser now allows identifiers and literal strings to
  contain letters outside the limited ASCII-only range it used to accept.  The in-game terminal now
  allows you to type any letter your keyboard can type.  (But it does not implement the ALT-numpad
  technique of entering characters.  You have to have a keyboard that types the character directly.
  However, the ALT-numpad technique will work through the telnet terminal, if your telnet client's
  window can do it.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/1994)
* **Regular expression part searches** for part/tag names.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1918), documenation: search for "PARTSTAGGEDPATTERN" (http://ksp-kos.github.io/KOS_DOC/structures/vessels/vessel.html#method:VESSEL:PARTSTAGGEDPATTERN).
* **Choose the IP address** of the telnet server, from the ones your computer has available, instead
  of kOS picking one arbitrarily.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1976)
* **Allow local variables in triggers** In order to support the kOS callback
  system used by the GUI, we also finally had to add support for proper local
  variable scoping to triggers like WHEN and ON.  A trigger's condition
  variables are no longer limited to having to be global.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2031)
* **Pressure at a given altitude** is now something you can query from an atmosphere.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2000), documentation: search for "ALTITUDEPRESSURE" (http://ksp-kos.github.io/KOS_DOC/structures/celestial_bodies/atmosphere.html#method:ATMOSPHERE:ALTITUDEPRESSURE).
* **Get a LATLNG for some other body than the current one.**
  [pull request](https://github.com/KSP-KOS/KOS/pull/2001), documentation: search for "GEOPOSITIONLATLNG" (http://ksp-kos.github.io/KOS_DOC/structures/celestial_bodies/body.html#method:BODY:GEOPOSITIONLATLNG).

### Bug Fixes

* Fix kOS toolbar button sometimes failing to appear in Blizzy Toolbar Mod.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1902)
* Fix SKID Chip emulator's sync lag when physics is slow.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1915/commits/c9d9dcd18561903e122531605194b2685fc4fb15)
* Fix SKID Chip emulator unable to use voices 6 through 9 because of how they were initialized.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1927)
* Forgot to document GETMODULEBYINDEX.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1962)
* Fix inability of a script to SET TARGET when KSP game is not the focused window.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1934)
* Fix iterator that lets you walk the characters in a string with "for" loop.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1938)
* Removed some Unity hooks that despite being empty and doing nothing,
  nonetheless still ate up a bit of time to pointlessly call and return from.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1965)
* Fix use of the min()/max() function on string comparisons
  [pull request](https://github.com/KSP-KOS/KOS/pull/1967)
* Fix science data transmissions
  [pull request](https://github.com/KSP-KOS/KOS/pull/1979)
* Fix unnessary duplicated of clones of vessel objects (was causing large garbage collection hangs).
  [pull request](https://github.com/KSP-KOS/KOS/pull/1983)
* Fixed several small documentation errors:
  [pull request](https://github.com/KSP-KOS/KOS/pull/1928)
  [pull request](https://github.com/KSP-KOS/KOS/pull/1986)
  [pull request](https://github.com/KSP-KOS/KOS/pull/1992)
* Fixed float->boolean mapping error.  The values no longer round to integer before becoming boolean.
  (i.e. 0.01 should be True, not get rounded to False (0) like it used to.)
  [pull request](https://github.com/KSP-KOS/KOS/pull/1990)
* Fixed ATM:SEALEVELPRESSURE units to agree with the documentation.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2000)
* Fixed bug that had made the sounds fail to emit for beep and keyclick.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2003)
* Fixed vessel:TOSTRING to return "Vessel(blarg)" instead of "Ship(blarg").
  [pull request](https://github.com/KSP-KOS/KOS/pull/2005)
* Fixed null-ref errors when using NEXTPATCH when there is no next patch.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2009)
* Fixed a few bugs related to kOS cleaning up after itself when the vessel splits into two
  or two vessels join together, or a vessel blows up.
  [pull request](https://github.com/KSP-KOS/KOS/pull/2010)


## v1.0.3 (for KSP 1.2.2) Make a little noise! (Part Deux)

This release is nearly identical to v1.0.2, except that it was compiled against
binaries from KSP v1.2.2 (released just before we published) and the version numbers
have been advanced.  While it appears that kOS v1.0.2 is compatible with KSP v1.2.2,
we wanted to err on the side of caution and provide an explicitly compatible release.
Please review the changelog for v1.0.2 if you are upgrading from an earlier version.


## v1.0.2 (for KSP 1.2.1) Make a little noise!

### Breaking Changes

* As always, if you use the compiler feature to make KSM files, you should
  recompile the KSM files when using a new release of kOS or results will
  be unpredictable.
* Most in game settings are now integrated with KSP's difficulty settings window.
  You will be prompted to migrate existing settings when you load your save game.
  Telnet settings are still stored in the old config file, but everything else is
  now stored within the save file.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1843) | [documentation](http://ksp-kos.github.io/KOS_DOC/general/settingsWindows.html#ksp-difficulty-settings-window)
* Calls to resource suffixes on the `stage` bound variable are no longer rounded to 2 decimal places.
  Previously they were rounded to assist in detecting "zero" fuel, but they cause inequality issues
  when comparing to the newer `stage:resources` list or `stage:resourceslex` values.
* The behavior of the resource suffixes on the `stage` bound variable has changed with regard
  to asparagus staging.  If you have smaller tanks that **can** be staged, `stage:liquidfuel`
  will return `0` even if you still have an engine firing.  This is a break from previous versions
  of kOS, but is aligned with the current UI design.  Previous versions also aligned with the KSP
  UI, but the UI mechanic was updated with KSP 1.2.x

### New Features

* Official release for KSP version 1.2.1!
* kOS now has a procedural sound system!  You can use it to play customized error
  tones or make your own musical notes.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1859) | [documentation](http://ksp-kos.github.io/KOS_DOC/general/skid.html)
* Support for CommNet and modifications to make RemoteTech and CommNet use similar systems.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1850) | [documentation](http://ksp-kos.github.io/KOS_DOC/commands/communication.html#connectivity-managers)
* Trajectories integration enabled via new `ADDONS:TR`
  [pull request](https://github.com/KSP-KOS/KOS/pull/1603) | [documentation](http://ksp-kos.github.io/KOS_DOC/addons/Trajectories.html)
* Added new setting for default terminal brightnes, and updated default value to 70%
  [pull request](https://github.com/KSP-KOS/KOS/pull/1872) | [documentation](http://ksp-kos.github.io/KOS_DOC/general/settingsWindows.html#ksp-difficulty-settings-window)
* Added `VELOCITY` and `ALTITUDEVELOCITY` suffixes to `geocoordinates
  [pull request](https://github.com/KSP-KOS/KOS/pull/1874) | [documentation](http://ksp-kos.github.io/KOS_DOC/math/geocoordinates.html#attribute:GEOCOORDINATES:VELOCITY)
* Added `TONUMBER` and `TOSCALAR` suffixes to `string` values for parsing numerical values
  [pull request](https://github.com/KSP-KOS/KOS/pull/1883) | [documentation](http://ksp-kos.github.io/KOS_DOC/structures/misc/string.html#method:STRING:TONUMBER)
* New `steeringmanager` suffix `ROLLCONTROLANGLERANGE` to dictate the maximum value
  of `ANGLEERROR` for which the manager will attempt to control roll
  [commit](https://github.com/KSP-KOS/KOS/commit/3c1d5d15fa5834858204a07870c2e768870fce72) | [documentation](http://ksp-kos.github.io/KOS_DOC/structures/misc/steeringmanager.html#attribute:STEERINGMANAGER:ROLLCONTROLANGLERANGE)
* KSM files are now gzip compressed internally, dramatically reducing the file size.
  Existing KSM files **should** still load, but see above for the recommendation to
  recompile all KSM files.
  [pull request](https://github.com/KSP-KOS/KOS/pull/1858)

### Bug Fixes

* Fix for throwing errors when another mod uses dynamic assembly
  [pull request](https://github.com/KSP-KOS/KOS/pull/1851)
* Update Blizzy toolbar wrapper to the most recent version
  [pull request](https://github.com/KSP-KOS/KOS/pull/1851)
* Fix for local kOS hard disks breaking when loading with 4 byte long files
  [pull request](https://github.com/KSP-KOS/KOS/pull/1858)
* kOS no longer uses a write-only lock when writing to the archive, preventing
  an error when accessing a file opened for reading by another program
  [pull request](https://github.com/KSP-KOS/KOS/pull/1870)
* Fix for duplicate functions/locks breaking ksm files
  [pull request](https://github.com/KSP-KOS/KOS/pull/1871)
* Fix for null ref error when editing node suffixes on KSP 1.2.1
  [pull request](https://github.com/KSP-KOS/KOS/pull/1876)
* Fix for issue where a body with the same name as one of our bound variables would block
  access to said variable (specifically Eta in Galileo's Planet Pack blocked the `eta` bound variable)
  [pull request](https://github.com/KSP-KOS/KOS/pull/1888)
* Fix for getting the science value and transmit value in sandbox mode
  [pull request](https://github.com/KSP-KOS/KOS/pull/1889)
* Fix error where `unlock all` inside a trigger will try to
  unlock functions too
  [pull request](https://github.com/KSP-KOS/KOS/pull/1889)


## v1.0.1 (for KSP 1.1.3) Let's take some input!


## Why 1.1.3 and not 1.2?

We wanted to get the last bug fixes and new features into the hands of any users
who might not update KSP to 1.2 right away.  Traditionally there are some mods
that take a while to update when KSP releases a new version, and many users
choose to wait for all of their favorite mods to update before upgrading KSP.
By releasing in conjunction with the update, we can ensure that as many users as
possible have access to these latest updates.  We will be releasing a version of
kOS that is compatible with KSP 1.2 as soon as possible after the final build is
released to the public.

### Breaking Changes

* As always, if you use the compiler feature to make KSM files, you should
  recompile the KSM files when using a new release of kOS or results will
  be unpredictable.
* The `stage` command/function now implements the yield behavior, waiting until
  the next physics tick to return.  This ensures that all vessel stats are
  updated together. (https://github.com/KSP-KOS/KOS/pull/1807)
* As always, if you use the compiler feature to make KSM files, you should
  recompile the KSM files when using a new release of kOS or results will
  be unpredictable.
* New Subdirectories ability has deprecated several filename commands such
  as ``delete``, ``copy``, and ``rename``.  They will still work, but will
  complain with a message every time you use them, as we may be removing
  them eventually.  The new commands ``deletepath``, ``copypath``, and
  ``movepath`` described below are meant to replace them.
* When using a RemoteTech antenna that requires directional aiming,
  in the past you could aim it at mission control with
  ``SETFIELD("target", "mission-control")`` and now you have to
  say ``SETFIELD("target", "Mission Control")`` instead, due to
  changes in RT's naming schemes.
* Previously the Y and Z axes of SUN:VELOCITY:ORBIT were swapped.
  (https://github.com/KSP-KOS/KOS/issues/1764)
  This has been fixed so it is now the same as for any other body,
  however scripts might exist that had previously been swapping them
  back to compensate for this, and if there were they would now break
  since that swapping is no longer needed.
* `STEERINGMANAGER:SHOWRCSVECTORS` and `STEERINGMANAGER:SHOWENGINEVECTORS` are now obsolete and will throw an error.
* Triggers may now go beyond the limits of the IPU (https://github.com/KSP-KOS/KOS/pull/1542) but are no longer guaranteed to execute within a single update frame.  See http://ksp-kos.github.io/KOS_DOC/general/cpu_hardware.html#triggers and http://ksp-kos.github.io/KOS_DOC/general/cpu_hardware.html#cpu-update-loop for more details.
* As usual, you must recompile any KSM files when using the new version.
* Vecdraw :SCALE no longer applied to :START.  Only applied to :VEC.
* Varying power consumption might make it so if you have high IPU settings some designs might run out of power when they didn't before.  (in most cases it should draw less power for most people).
* !!!! Default extension of ".ks" is no longer applied to all new filenames created.  But it still will be looked for when reading existing files if you leave the extension off !!!!
* FileInfo information now moved to Volume (http://ksp-kos.github.io/KOS_DOC/structures/volumes_and_files/volume.html).
* VOLUME:FILES was returning a LIST(), now it returns a LEXICON who's keys are the filename.
* String sort-order comparisons with "<" and ">" operators were implemented wrongly and just compared lengths.  Now they do a character-by-character comparison (case-insensitively).  On the off chance that anyone was actually trying to use the previous weird length-comparison behavior, that would break.

### New Features

* Functions and opcodes can now tell the CPU to yield (wait) based on their own
  arbitrary logic.  This allows future functions to be "blocking" (preventing
  further execution) without blocking KSP itself.
  (https://github.com/KSP-KOS/KOS/issues/1805,
  https://github.com/KSP-KOS/KOS/pull/1807, and
  https://github.com/KSP-KOS/KOS/pull/1820)
* New `timewarp` structure, available on the `kuniverse` bound variable. This
  structure provides additional information and control over time warp. The old
  warp bound variables remain in place.
  (https://github.com/KSP-KOS/KOS/issues/1790 and
  https://github.com/KSP-KOS/KOS/pull/1820)
* Introducing a new `terminalinput` structure for keyboard interaction from
  within scripts!  Currently support is only provided for getting single
  characters.
  (https://github.com/KSP-KOS/KOS/pull/1830)

Please check http://ksp-kos.github.io/KOS_DOC/changes.html for more detailed
explanations for the new features.

* **Subdirectories:** (http://hvacengi.github.io/KOS/commands/files.html)
  You are now able to store subdirectories ("folders") in your volumes,
  both in the archive and in local volumes.  To accomodate the new feature
  new versions of the file manipulation commands had to be made (please
  go over the documentation in the link given above).  In the Archive,
  which is really your ``Ships/Script/`` directory on your computer,
  these subdirectories are stored as actual directories in your computer
  filesystem.  (For example, the file ``0:/dir1/dir2/file.ks`` would be
  stored at ``Kerbal Space Program/Shipts/Script/dir1/dir2.file.ks`` on
  your real computer.) In local volumes, they are stored in the persistence.sfs
  savegame file like usual.
  (Pull Request discussion record: https://github.com/KSP-KOS/KOS/pull/1567)
  * Boot subdirectory: (http://hvacengi.github.io/KOS/general/volumes.html#special-handling-of-files-in-the-boot-directory)
    To go with Subdirectories, now you make a subdirectory in your archive
    called ``boot/``, and put all the candidate boot files there.  When
    selecting a boot file in the VAB or SPH, the selections are taken from
    there and need not contain the "boot_" prefix to the filename anymore.
    Old boot files will be grandfathered in that are named the old way,
    however.
  * CORE:BOOTFILENAME is now a full path.  i.e. ``boot/myfile.ks``.
  * PATH structure now allows you to get information about
    the new full subdirectories system from your scripts.
    (http://hvacengi.github.io/KOS/structures/volumes_and_files/path.html)
  * New RUNPATH command now allows any arbitrary string expression to be
    used as the name of the file to be run.  i.e.
    ``set basename to "prog". set num to 1. runpath(basename+num, arg1). // same as run prog1(arg1)``.
    As part of the support for this, programs with a large number of RUN
    commands (or RUNPATH commands) should now take up a bit less
    of a memory footprint than they used to in their compiled form
    (and thus in KSM files too).
    (http://hvacengi.github.io/KOS/commands/files.html#runpath-and-runoncepath)
* **Communication between scripts** on different CPUs of the same vessel or
  between different vessels.
  (http://hvacengi.github.io/KOS/commands/communication.html)
  * A new structure, the ``Message``, contains some arbitrary piece of
    data you choose (a number, a string, a list collection, etc), and
    some header information kOS will add to it that describes where it
    came from, when it was sent, and so on.  What you choose to do
    with these arbitrary chunks of data is up to you.  kOS only lets
    you send them.  You design your own protocol for what the data means.
  * If RemoteTech is installed, a connection is needed to send a message
    to another vessel (but not to a CPU on the same vessel).  And, the
    message won't actually show up in the other vessel's queue until the
    required lightspeed delay.
  * To handle KSP's inability to have different vessels far away from each
    other both fully loaded and active, you do have to switch scenes back
    and forth between distant vessels if you want them to have a conversation
    back and forth.  Messages that were meant to arrive on a vessel while
    it wasn't within active loading range will wait in the recever's vessel
    queue until you switch to it, so you don't have to hurry and switch
    "in time" to get the message.
* **Added anonymous functions :**
  (http://hvacengi.github.io/KOS/language/anonymous.html)
  By placing arbitrary braces containing the body of a function anywhere
  within the script that an expression is expected, the compiler builds
  the function code right there and then returns a delegate of it as the
  value of the expression.
* **New 3rd-party addon framework** (https://github.com/KSP-KOS/KOS/tree/develop/src/kOS/AddOns/Addon%20Readme.md)
  allows authors of other KSP mods to add hooks into kOS so that kOS
  scripts can interface with their mods more directly, without kOS
  developers having to maintain that code themselves in the kOS
  repository.
  (Pull Request discussion record: https://github.com/KSP-KOS/KOS/pull/1667)
* **allow scripted vessel launches**
  ``KUNIVERSE:GETCRAFT()``, ``KUNIVERSE:LAUNCHCRAFT()``, ``KUNIVERSE:CRAFTLIST()``,
  and ``KUNIVERSE:LAUNCHCRAFTFROM()`` allow you to script the changing of scenes
  and loading of vessels into those scenes.  While this breaks the 4th wall
  quite a bit (how would an autopilot choose to manufacture an instance of the
  plane?), it's meant to help with script testing and scripts that try to
  repeatedly run the same mission unattended.
  (http://hvacengi.github.io/KOS/structures/misc/kuniverse.html)
* **eta to SOI change:**
  Added SHIP:OBT:NEXTPATCHETA to get the time to the next orbit patch
  transition (SOI change).
  (http://hvacengi.github.io/KOS/structures/orbits/orbit.html#attribute:ORBIT:NEXTPATCHETA)
* **get control-from:**
  Added ``SHIP:CONTROLPART`` to return the ``Part`` of the vessel that is
  currently set as its "control from here" part.
  (http://hvacengi.github.io/KOS/structures/vessels/vessel.html#attribute:VESSEL:CONTROLPART)
* **maneuver nodes as a list:**(
  New ``ALLNODES`` bound variable that returns a list of all the currently
  planned manuever nodes (the nodes you could iterate through with
  ``NEXTNODE``, but rendered into one list structure).
  (http://hvacengi.github.io/KOS/bindings#allnodes)
* Several new **pseudo-action-groups** (akin to "panels on", that aren't
  action groups as far as stock KSP is concerned, but kOS treats them like
  action groups) were added.  (http://hvacengi.github.io/KOS/commands/flight/systems#kos-pseudo-action-groups)
* Ability to **get/set the navball mode** (surface, orbital, target) with
  the ``NAVMODE`` bound variable:
  i.e. ``SET NAVMODE TO "SURFACE".``.
* **UniqueSet structure.** (http://hvacengi.github.io/KOS/structures/collections/uniqueset.html)
  A collection intended for when all you care about is whether a equivalent
  object exists or doesn't exist yet in the collection, and everything else
  (order, etc) doesn't matter.
* KSP 1.1 now allows you to lock the gimbals for the three pitch/yaw/roll axes individually on engines, as 3 different settings, rather than just lock the whole gimbal for all directions.  kOS now lets you access this ability (https://github.com/KSP-KOS/KOS/pull/1622).


## v0.20.0 KSP 1.1 Hype!

This release is functionally identical to v0.19.3, it is recompiled against the
KSP 1.1 release binaries (build 1230)

* Profiling output via `ProfileResult()` (https://github.com/KSP-KOS/KOS/pull/1534)
* New alias KUNIVERSE:FORCEACTIVE() can be used instead of the longer name KUNIVERSE:FORCESETACTIVEVESSEL().
* More robust use of the font_sml.png file allows for replacement of font_sml.png by the end-user.
  (However this may only be useful for a limited time, as Unity5 might make us implement the font differently
  anyway.)
* PIDLoop tutorial section in the docs edited to mention new PIDLoop()
  function that did not exist back when that page was first written.
  (http://ksp-kos.github.io/KOS_DOC/tutorials/pidloops.html)
* New Terminal GUI doodads and widgets: A brightness slider,
  and the ability to zoom the character width and height.  Also
  made the transparency and dimming of the 'non-active' terminals
  a bit less severe so you can still read them when un-focused.
  Also, these new features can be script controlled by new
  suffixes, however it is unclear if that feature (doing it from
  a script) will remain in the future so use it with care:
  (http://ksp-kos.github.io/KOS_DOC/structures/misc/terminal.html)
* Art asset rework.  The meshes and textures of the kOS CPU parts have recieved an update, and a new KAL9000 high-end computer part was included.
* Varying power consumption.  Units of electric charge used now varies depending on CPU speed and how much the CPU is being actually used.  If your IPU setting is low, or if your program isn't doing very much and is just stuck on a `wait` statement, it won't use as much power. (http://ksp-kos.github.io/KOS_DOC/general/cpu_hardware#electricdrain)
* Ability to read and write whole files at a time as one big string. (http://ksp-kos.github.io/KOS_DOC/structures/volumes_and_files/volumefile.html)
* User Functions can now be referred to with function pointers, or "delegates".  (http://ksp-kos.github.io/KOS_DOC/language/delegates.html)
* Automatic serialization system to save/load some kinds of data values to JSON-format files (http://ksp-kos.github.io/KOS_DOC/commands/files.html#writejson-object-filename)
* User Programs and Functions now allow trailing optional parameters with defaulted values. (http://ksp-kos.github.io/KOS_DOC/language/user_functions.html#optional-parameters-parameter-defaults).
* There are now some suffixes that work on all value types, even primitive scalars.  To accomplish this, a new "encapsulation" system has wrapped all kOS structures and primitive types inside a generic base type.  (http://ksp-kos.github.io/KOS_DOC/structures/reflection.html)
* ENGINE type now supports multi-mode cases and has its gimbal accessible through :GIMBAL suffix (http://ksp-kos.github.io/KOS_DOC/structures/vessels/engine.html)
* Added GIMBAL:LIMIT suffix. (http://ksp-kos.github.io/KOS_DOC/structures/vessels/gimbal.html)
* Better support for DMagic's Orbital Science mod (http://ksp-kos.github.io/KOS_DOC/addons/OrbitalScience.html)
* Char() and Unchar() functions for translating unicode numbers to characters and visa versa (http://ksp-kos.github.io/KOS_DOC/math/basic.html#function:CHAR)
* New Range type for iterating over hardcoded lists (http://ksp-kos.github.io/KOS_DOC/structures/collections/range.html).
* Ability to iterate over the characters in a string using a FOR loop, as if the string was a LIST() of chars.
* New higher level cpu part. (https://github.com/KSP-KOS/KOS/pull/1380)
* HASTARGET and HASNODE functions (http://ksp-kos.github.io/KOS_DOC/bindings.html?highlight=hastarget)
* :JOIN suffix for LIST to make a string of the elements (http://ksp-kos.github.io/KOS_DOC/structures/collections/list.html#method:LIST:JOIN)
* KUNIVERSE now lets you read hours per day setting (http://ksp-kos.github.io/KOS_DOC/structures/misc/kuniverse.html#attribute:KUNIVERSE:HOURSPERDAY)
* The reserved word ARCHIVE is now a first-class citizen with proper binding, so you can do SET FOO TO ARCHIVE and it will work like you'd expect.
* New Lexicon creation syntax to make a Lexicon and populate it all in one statement. (http://ksp-kos.github.io/KOS_DOC/structures/collections/lexicon.html?highlight=lexicon#constructing-a-lexicon)

### Bug Fixes

* Fix for formatting of `time:clock` to pad zeros
  (https://github.com/KSP-KOS/KOS/issues/1771 and
  https://github.com/KSP-KOS/KOS/pull/1772)
* Fix for not being able to construct a `vessel("foo")` if "foo" is the name of
  the current vessel (https://github.com/KSP-KOS/KOS/issues/1565 and
  https://github.com/KSP-KOS/KOS/pull/1802)
* RemoteTech steering should be fixed.  At worst you may see a 1sec gap with
  the controls, as we now refresh the steering callback about once per second.
  (https://github.com/KSP-KOS/KOS/issues/1806 and
  https://github.com/KSP-KOS/KOS/pull/1809)
* Named functions defined within anonymous functions will no longer throw an
  error (https://github.com/KSP-KOS/KOS/issues/1801 and
  https://github.com/KSP-KOS/KOS/pull/1811)
* `lock steering` no longer throws an exception inside of an anonymous functions
  (https://github.com/KSP-KOS/KOS/issues/1784 and
  https://github.com/KSP-KOS/KOS/pull/1811)
* Compiled programs that include a large number of named functions should no
  longer throw an error (https://github.com/KSP-KOS/KOS/issues/1796 and
  https://github.com/KSP-KOS/KOS/pull/1812)
* Fixed the first call to `wait` after the cpu boots
  (https://github.com/KSP-KOS/KOS/issues/1785)
* Various documentation fixes (https://github.com/KSP-KOS/KOS/pull/1810,
  https://github.com/KSP-KOS/KOS/pull/1823, and
  https://github.com/KSP-KOS/KOS/pull/1834)


## v1.0.0 (for KSP 1.1.3) Hey let's stop calling it Beta.

* In some cases (https://github.com/KSP-KOS/KOS/issues/1661) the program
  wouldn't stop immediately when you execute a  ``kuniverse`` command that
  reloads a save or switches scenes.  It would instead finish out the
  remainder of the IPU instructions in the current physics tick.
  After the fix, causing a scene change (or reload) automatically stops the
  program right there since anything it does after that would be moot as
  the game is about to remove everything it's talking about from memory.
* If using "Start on archive", with Remote Tech, a misleading "power starved"
  error was thrown when you reboot a probe that's out of antenna range.
  (https://github.com/KSP-KOS/KOS/issues/1363)
* ``unchar("a")`` was apparently broken for a few months and we hadn't noticed.
  The root cause was that its implementation had to be edited to comply with
  the change that enforced the VM to only use kOS ``Structure`` types on the
  stack.  The need for that change had been missed.
  (https://github.com/KSP-KOS/KOS/issues/1692)
* Previously Infernal Robotics allowed you to move servos that weren't even
  on your own vessel and you shouldn't have direct control over.  This has
  been fixed.  (https://github.com/KSP-KOS/KOS/issues/1540)
* Refactored previous non-working technique for quicksave/quickload to
  turn it into something that works.
  (https://github.com/KSP-KOS/KOS/issues/1372)
* There were cases where using CTRL-C to abort a program would cause some
  old cruft to still be leftover in the VM's stack.  This made the system
  fail to clear out the names of functions that were no longer loaded in
  memory, making it act like they were still reachable and call-able.
  (https://github.com/KSP-KOS/KOS/issues/1610)
* Some types of ``Resource`` didn't contain the ``:DENSITY`` suffix like the
  documentation claimed they would.
  (https://github.com/KSP-KOS/KOS/issues/1623)


## v0.20.1 KSP 1.1.2 and bug repair

The biggest reason for this release is to handle two game-breaking
problems caused by recent alterations in the API that kOS hadn't
adapted to correctly yet.

The "remit" of this release is purely to fix a few bugs, and patch up
a few things where KSP 1.1 had changes we didn't catch.  Mostly,
that's cases where previously working code in kOS had now become a
bug, but it also includes a few other bug fixes not related to KSP 1.1.

But any new features (rather than bug fixes) in the pipeline not directly
related to that "remit" are not in this release.

* Infinitely growing mass:  Realism Overhaul users could not use kOS anymore, because kOS was re-adding its small module mass to the part again and again each physics tick.  Even though the mass of kOS is small, adding it to the part 25 times a second quickly made the vessel grow too massive to do anything with.  The bug was not caught earlier because it only happened if kOS was added to parts other than the parts kOS ships with (i.e. by using ModuleManager), and those parts also had other mass-affecting modules on them.  Although discovered in Realism Overhaul, the problem could have been affecting any users who used kOS in that same fashion.  The cause was traced to an incorrect use of the new mass API by kOS and has been fixed. (https://github.com/KSP-KOS/KOS/pull/1644).
* "SET TARGET TO FOO." while the terminal is open was failing.  Now it works.  (The kOS terminal locks out all other inputs so your keypresses don't affect the ship, but as of KSP 1.1 the "all" input lock it was using to do so also includes the ability to set target, which it didn't before.) (https://github.com/KSP-KOS/KOS/pull/1636)
* Incorrect value for MeanAnomalyAtEpoch fixed.  It was multiplying the value by the conversion factor for radians-to-degrees twice, rather than just once.  (https://github.com/KSP-KOS/KOS/pull/1642)
* GeoCoordinates were not serializing properly.  Now they are. (https://github.com/KSP-KOS/KOS/pull/1615).
* Finally fully obsoleted the years-old suffixes for trying to do antenna range the old way (before we just relied on Remote Tech to do antenna work for us).  (https://github.com/KSP-KOS/KOS/pull/1607).
* Bug fixes for catching a few more cases where staging or decoupling part of the craft away was still confusing SteeringManager into trying to lock out, or take control of, the wrong half of the craft.  (https://github.com/KSP-KOS/KOS/pull/1544).
* [KSP1.1] Removing a node leaves an artifact (https://github.com/KSP-KOS/KOS/issues/1572 https://github.com/KSP-KOS/KOS/issues/1576)
* [KSP1.1] Toolbar button doesn't display (https://github.com/KSP-KOS/KOS/issues/1573 https://github.com/KSP-KOS/KOS/issues/1569)


## v0.19.3 Last (intended) 1.0.5 update.

(This is the last planned update to work with KSP 1.0.5 unless
it breaks something big that requires an emergency patch.)

* Removed delay when enabling/disabling auto changeover for multi mode engines (https://github.com/KSP-KOS/KOS/pull/1451)
* Improve performance of various math functions (https://github.com/KSP-KOS/KOS/issues/1553 https://github.com/KSP-KOS/KOS/pull/1523 https://github.com/KSP-KOS/KOS/pull/1563)
* `on` logic now evaluates expressions and suffixes, instead of requiring a raw variable (https://github.com/KSP-KOS/KOS/issues/1376 https://github.com/KSP-KOS/KOS/pull/1542)
* Documentation no longer inserts a space around highlighted search terms (https://github.com/KSP-KOS/KOS/pull/1548)
* You can now use lock objects with the same identifier from within compiled scripts, like `lock throttle...` (https://github.com/KSP-KOS/KOS/issues/691 https://github.com/KSP-KOS/KOS/issues/1253 https://github.com/KSP-KOS/KOS/issues/1557 https://github.com/KSP-KOS/KOS/pull/1561)
* The script parsing logic has been updated to improve compile times by roughly 50% (https://github.com/KSP-KOS/KOS/pull/1566)


## v0.19.2

This release is here primarily to fix a problem that made
the new v0.19.1 terminal unusable for users who have to
use low resolution texture settings in the Unity graphics
configuration panel.

* New terminal now works again at low texture resolution settings
  (https://github.com/KSP-KOS/KOS/issues/1513).
* New terminal shows grey color on power-off again
  (https://github.com/KSP-KOS/KOS/issues/1525).
* Terminal now shows a boot message that mentions the documentation URL
  (https://github.com/KSP-KOS/KOS/issues/1527).
* Fixed a situation that could make KSP itself crash if a script
  attempted to perform an equality comparison on types that hadn't
  had a meaningful implementation of equality defined.  (Instead
  of a proper error message about it from kOS, kOS got stuck in
  recursion.)


## v0.19.1

This release is a patch to v0.19.0, fixing some things
found by the user community in the two days shortly after
v0.19.0 released.

It also happens to contain a few terminal window features
that were being worked on before v0.19.0 but were not deemed
ready yet when 0.19.0 was released.

* Fixed file rename bug on local hard disks:
  (https://github.com/KSP-KOS/KOS/issues/1498)
* Fixed boot files can be larger than the local disk
  (https://github.com/KSP-KOS/KOS/issues/1094)
* Fixed a bug where Infernal Robotics would break when switching vessels or
  reverting. (https://github.com/KSP-KOS/KOS/issues/1501)
* Fixes problems with using PartModule's SetField(), and infernal Robotics which
  had been failing for all cases where the field was a "float".
  (https://github.com/KSP-KOS/KOS/issues/1503).
  There may have been other places this bug affected, but this is
  where it was noticed.  Hypothetically, anywhere the stock game's
  library insists on only accepting a single-precision float and
  not a double would have had the problem.
* Improve steering when small control magnitudes are required.
  (https://github.com/KSP-KOS/KOS/issues/1512)


## v0.19.0

* Numerous additional checks to prevent control of other vessels the kOS CPU isn't attached to.
* The error beep and keyboard click sounds now obey game's UI volume settings. (https://github.com/KSP-KOS/KOS/pull/1287)
* Fixed two bugs with obtaining waypoints by name. (https://github.com/KSP-KOS/KOS/issues/1313) (https://github.com/KSP-KOS/KOS/pull/1319)
* Removed unnecessary rounding of THRUSTLIMIT to nearest 0.5%, now it can be more precise. (https://github.com/KSP-KOS/KOS/pull/1329)
* Removed the ability to activate both modes on multi-mode engine simultaneously.
* LIST ENGINES now lists all engines and displays part names instead of module names. (https://github.com/KSP-KOS/issues/1251)
* Fixed bug that caused hitting ESC to crash the telnet server. (https://github.com/KSP-KOS/KOS/issues/1328)
* Some exceptions didn't cause beep, now they all do. (https://github.com/KSP-KOS/KOS/issues/1317)
* Vecdraw :SCALE no longer applied to :START.  Only applied to :VEC. (https://github.com/KSP-KOS/KOS/issues/1200)
* Fixed bug that made up-arrow work incorrectly when the cursor is at the bottom of the terminal window. (https://github.com/KSP-KOS/KOS/issues/1289)
* A multitude of small documentation fixes (https://github.com/KSP-KOS/KOS/pull/1341)
* Fixed a bug when performing an undock (https://github.com/KSP-KOS/KOS/issues/1321)
* IR:AVAILABLE was reporting incorrectly ()
* Boot files now wait until the ship is fully unpacked and ready (https://github.com/KSP-KOS/KOS/issues/1280)
* The Vessel :HASBODY (aliases :HASOBT and :HASORBIT) suffix was in the documentation, but had been lost in a refactor last year.  It is put back now.
* String sort-order comparisons with "<" and ">" operators were implemented wrongly and just compared lengths. Now they do a character-by-character comparison (case-insensitively)
* Small documentation edits and clarifications all over the place.

### About The Name:

kOS has been around long enough that we figured it was long overdue
for us to stop calling it 0.something.  Lots of people are using it,
and we're worried about backward compatibility enough that we're not
really treating it like a Beta anymore.  This version contains mostly
a few things that we knew might break backward compatibility so we'd
been putting them off for a long time.  A jump to 1.0 seems a good time
to add those changes.

Of course, it has lots of other changes for whatever else was being
worked on since the last release.

### Breaking

* Nothing new breaking in this version is known about.

### Known Issues

* Using `lock` variables in compiled scripts with a duplicate identifier (like "throttle") throws an error (https://github.com/KSP-KOS/KOS/issues/1347 and https://github.com/KSP-KOS/KOS/issues/1253).
* Occasionally staging with a probe core or root part in the ejected stage will break cooked steering (https://github.com/KSP-KOS/KOS/issues/1492).
* The limitations of RemoteTech integration can be bypassed by storing a volume in a variable before the ship looses a connection to the KSC (https://github.com/KSP-KOS/KOS/issues/1464).

### Contributors This Release

(These are generated from records on Github of anyone who's Pull Requests are part of this release.)
(Names are simply listed here alphabetically, not by code contribution size.  Anyone who even had so much as one line of change is mentioned.)

Stephan Andreev (ZiwKerman) https://github.com/ZiwKerman
Bert Cotton (BertCotton) https://github.com/BertCotton
Kevin Gisi (gisikw) https://github.com/gisikw
Peter Goddard (pgodd) https://github.com/pgodd
Steven Mading (Dunbaratu) https://github.com/Dunbaratu
Eric A. Meyer (meyerweb) https://github.com/meyerweb
Tomek Piotrowski (tomekpiotrowski) https://github.com/tomekpiotrowski
Brad White (hvacengi) https://github.com/hvacengi
Chris Woerz (erendrake) https://github.com/erendrake  (repository owner)
(name not public in github profile) (alchemist_ch) https://github.com/AlchemistCH
(name not public in github profile) (tdw89) https://github.com/TDW89
Philip Kin (pipakin) https://github.com/pipakin


## v0.18.2


## [Insert witty title here :-P]

### Breaking Changes

* As usual, you MUST recompile all KSM files before running them on the new version.  Some of the changes have altered how the VM works.
* Nothing else... we hope.

### New Features

* Compatibility with KSP version 1.0.5
* `run once ...` syntax to run a script only once per session ( http://ksp-kos.github.io/KOS_DOC/commands/files.html#run-once-program )
* Volumes and processors have better integration ( http://ksp-kos.github.io/structures/vessels/volume.html#structure:VOLUME )
* Volume titles default to the name tag of the Processor part (only on launch) ( http://ksp-kos.github.io/KOS_DOC/general/volumes.html#naming-volumes )
* New suffixes for interacting with kOS Processor modules (including `core`) ( http://ksp-kos.github.io/KOS_DOC/commands/processors.html )
* `debuglog(...)` function to print directly to the KSP log file ( http://ksp-kos.github.io/KOS_DOC/structures/misc/kuniverse.html#method:KUNIVERSE:DEBUGLOG )
* New `queue` and `stack` data structures ( http://ksp-kos.github.io/KOS_DOC/structures/misc/queue.html and http://ksp-kos.github.io/KOS_DOC/structures/misc/stack.html )

### Bug Fixes

* The processor's mode (on/off/starved) is now saved and restored ( https://github.com/KSP-KOS/KOS/issues/1172 )
* Fixed stage resources again to address a change in KSP 1.0.5 ( https://github.com/KSP-KOS/KOS/issues/1242 )
* Fix occasional instances of flight controls getting disabled during a docking/undocking/staging event ( https://github.com/KSP-KOS/KOS/issues/1205 )
* kOS can now trigger module events with RemoteTech installed and no KSC connection ( https://github.com/RemoteTechnologiesGroup/RemoteTech/issues/437 )
* Fixed handling of multiple thrust/gimbal transforms and corrected some of their directions ( https://github.com/KSP-KOS/KOS/issues/1259 )


## v0.18.1


## Steering More Much Betterer

### Changes

* Changed default MaxStoppingTime to 2 seconds ( was 1 )

### Bug Fixes

* Fixed a issue where the effect of the Kd parameter of PIDLoop was having a reversed effect #1229
* Fixes an issue where NO_FLOW resources ( eg SolidFuel ) were not reporting correctly #1231


## v0.18


## Steering Much Betterer

### Breaking Changes

* As usual, you MUST recompile all KSM files before running them on the new version.  Some of the changes have altered how the VM works.
* New LOADDISTANCE obsoletes the previous way it worked ( http://ksp-kos.github.io/KOS_DOC/structures/misc/loaddistance.html )
* Fixed broken spelling of "ACQUIRE" on docking ports.  The old spelling of "AQUIRE" won't work anymore.
* Changed the bound variable "SURFACESPEED" to "GROUNDSPEED" instead, as the meaning of "SURFACESPEED" was confusingly ambiguous.
* New arg/param matching checks make some previously usable varying argument techniques not work.  (We don't think anyone was using them anyway).
* Disabled the ability to control vessels the kOS computer part is not actually attached to.  This always used to be possible, but it shouldn't have been as it breaks the theme of kOS.  This affects all the following: vessel:control, part:controlfrom, part:tag (can still get, but not set), partmodule:doaction, partmodule:doevent, partmodule:setfield (can still getfield).  These things become read-only when operating on any vessel other than the one the executing kOS module is actually part of.

### New Features

* THE BIG ONE:  Fix to Cooked Steering! Should help people using torque-less craft like with Realism Overhaul. Removed the old steering logic and replaced it with a nice auto-tuning system. ( https://github.com/KSP-KOS/KOS/pull/1118 )
* SteeringManager structure to let users tweak parts of the new steering system ( http://ksp-kos.github.io/KOS_DOC/structures/misc/steeringmanager.html )
* PIDLoop structure to let users see parts of the new steering system, and to let them use the built-in PID system for their own needs ( http://ksp-kos.github.io/KOS_DOC/structures/misc/pidloop.html  )
* String manipulation methods. ( http://ksp-kos.github.io/KOS_DOC/structures/misc/string.html )
* New Lexicon (Associateive Array) type. ( http://ksp-kos.github.io/KOS_DOC/structures/misc/lexicon.html )
* New Science Experiment control interface allows scripts to directly execute science experiments, bypassing the on-screen prompts. ( http://ksp-kos.github.io/KOS_DOC/structures/vessels/scienceexperiment.html )
* CrewMember API to let you query the registered crew - their class, gender, and skill ( http://ksp-kos.github.io/KOS_DOC/structures/vessels/crewmember.html )
* Infernal Robotics API now lets you get Part containing a servo ( https://github.com/KSP-KOS/KOS/issues/1103 )
* (user docs) Better tutorial for KSP 1.0 areo mode. ( https://github.com/KSP-KOS/KOS/pull/1081 )
* A few more constants: C, ATMTOKPA, KPATOATM. ( http://ksp-kos.github.io/KOS_DOC/math/basic.html )
* DYNAMICPRESSURE, or Q ( https://github.com/KSP-KOS/KOS/pull/1085 )
* DEFINED keyword ( http://ksp-kos.github.io/KOS_DOC/language/variables.html#defined )
* Load and Pack Distance manipulation ( http://ksp-kos.github.io/KOS_DOC/structures/misc/loaddistance.html )
* KUniverse structure letting you break the 4th wall and revert from a script ( http://ksp-kos.github.io/KOS_DOC/structures/misc/kuniverse.html )
* Added SolarPrimeVector to provide universal longitude direction ( http://ksp-kos.github.io/KOS_DOC/bindings.html#solarprimevector )

### Bug Fixes

* Made `stage:liquidfuel` more sane. ( https://github.com/KSP-KOS/KOS/issues/513 )
* LIST BODIES returned unusuable structure type ( https://github.com/KSP-KOS/KOS/issues/1090 )
* Made "ORBIT" and alias for "OBT" and visa versa ( https://github.com/KSP-KOS/KOS/issues/1089 )
* Made vecdraws stop showing bogus atmospheric burning effects ( https://github.com/KSP-KOS/KOS/pull/1108 )
* Removed non-functional broken attempts to save/restore variables ( https://github.com/KSP-KOS/KOS/issues/1098 )
* KSM files didn't store relative jumps right, breaking short-circuit boolean logic ( https://github.com/KSP-KOS/KOS/issues/1137 )
* (user docs) many minor docs fixes.
* Lock throttle inside a FROM loop was broken ( https://github.com/KSP-KOS/KOS/issues/1117 )
* Unlock anything inside a Trigger body was broken ( https://github.com/KSP-KOS/KOS/issues/1151 )
* Replaced KSP's incorrect ground speed with our own calculation ( https://github.com/KSP-KOS/KOS/issues/1097 )
* SASMODE "radialin" and "raidialout" were swapped in the KSP API ( https://github.com/KSP-KOS/KOS/issues/1130 )
* Bug with remote tech allowing access without antenna in one case ( https://github.com/KSP-KOS/KOS/pull/1171 )
* Wheelsteering by integer compass heading was broken ( https://github.com/KSP-KOS/KOS/issues/1141 )
* SHUTDOWN didn't shut down immediately ( https://github.com/KSP-KOS/KOS/issues/1120 )
* Remote Tech delay, and the `wait` command, were ignoring the time warp multiplier ( https://github.com/KSP-KOS/KOS/issues/723 )
* Better detection of arg/param matching.  ( https://github.com/KSP-KOS/KOS/issues/1107 )
* Doing PRINT AT that runs offscreen threw an error ( https://github.com/KSP-KOS/KOS/issues/813 )


## v0.17.3


## 1.0.4 Release

### Breaking Changes

* Removed all `ETA_` and `ALT_` bindings, please use `ETA:` and `ALT:` instead
* `TRUEANOMALY` and `MEANANOMALYATEPOCH` are now expressed in degrees to conform to our policy
* Deprecated INCOMMRANGE - now throws an exception with instructions to use the new addons:rt methods.
* Updated maxtthrust and availablethrust calculations for KSP v1.0.x.  Due to the way KSP handles thrust, neither available thrust nor maxthrust values are constant at all altitudes around bodies with atmospheres.
* Boot files are now stored on local hard drives with their original names.  You may get or set the boot file name using CORE:BOOTFILENAME suffix.
* Some undocumented and nonsensical bool math operations have been removed
* The Steering deadzone is much smaller now, this will allow for every precise RCS maneuvers.

### New Hotness

* You can now point RemoteTech antenna directly from script
* You can now get RemoteTech's 'local control' status
* Infernal Robotics integration improvements
* New loop structure to allow for more flexible iteration
* New struct object `CORE:` to interact with the currently running processor.
* Added vessel:dockingports and vessel:elements suffixes.
* Added element:dockingports and element:vessel suffixes.
* Added availablethrust suffix to engines which mirrors the availablethrust suffix for vessels.
* Added maxthrustat, availablethrustat, and ispat suffixes to engines to read the values at specified atmoshperic pressures.  See the documentation for details.
* Added maxthrustat and availablethrustat suffixes to vessels to read the values at a specified atmospheric pressures.  See the documentation for details.
* You can now use bootfiles while "Start on Archive volume" is enabled
* Many new sound effects have been added (error, beep, and an option for key click)
* Boolean AND and OR operations can now short circuit
* Add new WARPTO command that uses the new KSP function
* Added new `BODY:SOIRADIUS`
* Added new suffixes to part that lets you get the bare names of events, actions, and modules
* Many new sound effects have been added (error, beep, and an option for key click)
* Added `CLEARVECDRAWS` that will remove all VECDRAWS
* Any floating point value that has no floating component will be converted to an integer

### Old And Busted

* Fixed empty return statements crashing with an argument count exception #934
* Fix setting vector:mag to a new value actually setting the magnitude to 1 #952
* Fix electricity being consumed while the game was paused #526
* Fix Part Resource string representation #1062
* Fix UNLOCK inside brace statements #1048 #1051
* Fix setting PHYSICS warp mode #989
* Fix printing engine list duplication #1026, #1057
* Fix terminal lockout when RemoteTech has no connection to the KSC, but the ship has local control.
* Fixed a crappy parser error that was causing `,` to do bizarre things to some code #925
* Fix running an empty program resetting the parent #858
* Fix some error printing related to nodes #905
* Fix kOS processor sinking into launch pad #980
* Fix `rename file` command #971
* Fix `return` statement breaking closure #923
* Fix docking port query #937
* better expression support inside square brackets #935
* you can now `LOCK` in a loop #954
* the kOS toolbar button should be better behaved now
* Volume indexes will truncate floating values rather than throwing an error
* `LIST FILES IN` syntax now works for archive
* electricity consumption is better behaved
* setting the target to an empty string will always unset target


## v0.17.2


## 1.0 Release

### New Hotness

* New infernal robotics integration
* Better error reporting

### Old And Busted

* fixes keyword lexxing


## v0.17.1


## Corrections and omissions

### "New" Features

* Due to erendrake's inability to correctly use git. The new list constructor was omitted from the 0.17.0 release binaries.

### Bug Fixes:

* Many Doc fixes
* Fixed bug with setting KAC Alarm action to correct value
* Fixed some unneeded log spamming


## v0.17.0


## FUNCTIONS! FUNCTIONS! FUNCTIONS!

Big feature: You can make your own user-defined functions, that
can handle recursion, and can use local variable scoping.  You can
build a library of your own function calls and load them into your
script.

**New Documentation change page**:

For those users who just want to see what new features
exist without reading the entire documentation again
from scratch, we have created a changes page in the main documentation:

* New Changes Page: http://ksp-kos.github.io/KOS_DOC/changes.html

For the features mentioned below, you can go to the page above
and get a more verbose description of the new features.

### New Features:

A brief list of what's new:

* Variables can now be local
* Kerboscript has User Functions
* Community Examples Library
* LIST() now takes args to initialize the list.
* Physics Ticks not Update Ticks
* Ability to use SAS modes from KSP 0.90
* Blizzy ToolBar Support
* Ability to define colors using HSV
* Ability to highlight a part in color
* Better user interface for selecting boot scripts
* Disks can be made bigger with tweakable slider
* You Can Transfer Resources
* Kerbal Alarm Clock support
* Query the docked elements of a vessel
* Support for Action Groups Extended
* ISDEAD suffix for Vessel

This update is so full of new features that instead of describing all of their
details here, you can go see them on the main docs page at the following link:

http://ksp-kos.github.io/KOS_DOC/changes.html

### Bug Fixes:

- Using the same FOR iterator in two loops no longer name clashes because it's not global anymore.
- Repaired a number of boot file selection bugs.
- Removed a few unnecessary debug log spamming message.
- Fixed a minor issue with the special hidden file .DS_Store that Macs insert into the Scripts folder.
- Fixed bug spamming nullrefs when panel was open in the VAB/SPH editor.
- Fixed bugs where setting warp could crash KSP. Now it clamps warp to valid values.
- Fixed bug where kOS CPU's were drawing power from the batteries even when the game was paused.
- Fixed bug where rate of power consumption varied depending on animation frame rate.
- Fixed bug where WAIT 0 crashed scripts.  Now WAIT 0 waits the min. possible time (1 physics tick).
- Fixed small order of operations problem with expressions containing unary operators like '-', '+', and 'not'.
- Fixed problem where SET TARGET didn't really set it until the next physics tick.  Now it sets immediately.
- Fixed some issues with the use of Action Groups above 10, when Action Groups Extended is installed.
- Fixed bug where VOLUME:RENAMABLE returned the name string, rather than a boolean.
- Fixed bun when printing a VOLUME to the screen and failing to "stringify" it properly.
- Using the unary negation '-' on vectors and directions now works.
- Fixed some major bugs in how the kOS toolbar panel was dealing with scene changes and getting "stuck" on screen.
- Fixed some bugs with the kos Name Tag typing window getting stuck on screen and locking the user out of the UI.
- Fixed bug with reboot not clearing out the state properly.
- Fixed bug where any syntax error caught by the compiler resulted in bogus additional second error message.

###BREAKING:

- **RECOMPILE YOUR KSM FILES!!!** - If you used the COMPILE command in
  the past, changes to the kOS machine code that were needed to support
  variable scoping ended up invalidating any existing compiled KSM files.
- **KSM FILES ARE BIGGER** - compiled KSM files are now larger than
  they used to be for the same source code.  They might not be an
  efficient way to pack your code down to a small disk footprint
  anymore.
- *CONFIG:IPU should be slightly increased*  The new default
  we ship with is 200, to reflect both the change in ML code, and the
  movement to Unity's FixedUpdate for physics ticks.  However if you
  have played kOS in the past, your settings don't get automatically
  overwritten.  You will need to change the setting manually.
- *DECLARE has a new syntax*
  DECLARE _VARNAME_ now requires an initializer syntax as follows:
  - DECLARE _VARNAME_ TO _VALUE_.
  If you leave the TO _VALUE_ off, it will now be a syntax error.
  Also, you can say LOCAL or GLOBAL instead of, or in addition to,
  the word DECLARE.
- *DECLAREd variables are now local*
  Using the DECLARE _VARNAME_ TO _VALUE_ statement now causes the
  variable to have local scope that only exists within the local block
  of curly braces ('{'...'}') that it was declared inside of. To get
  the old behavior you can explicitly say:
  DECLARE GLOBAL _VARNAME_ to _VALUE.
- *FOR iterator now is local*
  The _VARIABLE_ in loops of the form FOR _VARIABLE_ IN _SOMELIST_ now
  has local scope to just that loop, meaning it stops existing after
  the loop is done and you can't use it outside the loop's body.


## v0.16.2

##HOTFIX

* Fixes #609 KOS ignores run command in FOR loop
* Fixes #610 Print AT draws in the wrong place on telnet after clearscreen.
* Fixes #612 doesn't update telnet screen when cur command is longer than prev and you up-arrow


## v0.16.1

##HOTFIX

this fixes #603 the mess that I made of the Node structure, thanks Tabris from the forums for bringing this to our attention.


## v0.16.0

### Breaking

* Body:ANGULARVEL is now a Vector instead of a Direction.  (This is the same as the change that was done to Vessel:ANGULARVEL in v0.15.4, but we missed the fact that Body had the same problem).  It was pretty useless before so this shouldn't hurt many scripters :)
* Both Body:ANGULARVEL and Vessel:ANGULARVEL now are expressed in the same SHIP_RAW coordinate system as everything else in kOS, rather than in their own private weirdly mirrored reference frame. (Thanks to forum user @thegreatgonz for finding the problem and the fix)
* #536 the 1.5m kOS part has always had trouble with clipping into other parts due to the rim of the cylinder sticking up past the attachment points. The part definition has been changed to fix this, but in KSP new part definitions don't affect vessels that have already been built or have already had their design saved in a craft file in the VAB/SPH.  To see the fix you'll need to start a new vessel design from scratch, otherwise you'll still have the old clipping behavior.
* PART:UID is now a string. This will only break you if you were doing math on UIDs?
* ELEMENT:PARTCOUNT was poorly named and duplicated by ELEMENT:PARTS:LENGTH so it was removed.

### New Features

* TELNET SERVER.  The biggest new feature this update is the introduction of a **telnet server** you can use to access the terminals in game.  For security, it's turned off by default, but you can enable it with the config radio button.  Full documentation on this new feature is at http://ksp-kos.github.io/KOS_DOC/general/telnet.html
	* Synopsis:
		* Telnet to 127.0.0.1, port 5410
		* Select CPU from welcome menu by typing a number and hitting Return.
		* Your telnet client is now a clone of that CPU's terminal window and can control it.
		* If you want to open it up to others to use (i.e. controlling your KSP game from a second computer),
		  you can use an ssh tunnel to access the local loopback address, or if you just want to throw
		  caution to the wind, you can tell it to stop using loopback and use your real IP address.
		  Be aware of the security risk if you choose this.
* Added HUDTEXT that lets you add text to the screen. Thanks @pgodd !
	* more information here: http://ksp-kos.github.io/KOS_DOC/commands/terminal.html#HUDTEXT
* #72 - Added STAGE:NUMBER and STAGE:READY to allow for staging very close together
* #522 - Added BODY:GEOPOSITIONOF and BODY:ALTITUDEOF for getting body-relative info about a 3D point in space.
* #524 and #523 - mission waypoints now have 3d positions
* In game Terminal is now resizable!  From a script with SET TERMINAL:WIDTH and SET TERMINAL:HEIGHT, or from dragging the lower-right corner of the GUI window.
* (AGX) Action Groups Extended Support! Thanks @SirDiazo
	* Getting or setting groups 11-250 should behave the same as the stock groups if you have AGX installed.
	* Groundwork is laid for getting parts and modules by the new action groups.
* Gimbals are now a well known module. providing read access to its state
* Added PART:GETMODULEBYINDEX(int). This is most useful when you have a part with the same module twice. Thanks @jwvanderbeck
* More documentation work. http://ksp-kos.github.io/KOS_DOC/

### Bug Fixes

* Fixes #389 - LOCK STEERING broken for RCS-only (no torque) ships.
* Fixes #516 - kOSTags are now applied in the correct MM pass
* Fixes #541 - All BODY: suffixes should now work properly when the body is the Sun without crashing.
* Fixes #544 - Terminal subbuffer won't shrink when up-arrowing to a previous smaller command.
* Fixes #548 - If SHIP is not the same as ActiveVessel, then executing STAGE stages the wrong vessel.
* Fixes #581 - SHIP:CONTROL:PILOTFORE and SHIP:CONTROL:PILOTSTARBOARD are no longer inverted.
* Fixes #578 - renamed our use of RemoteTech2 to RemoteTech to follow their new naming.
* Fixes #427 - Stack now clears when interactive commands throw exceptions.  (no longer reports false stack traces).
* Fixes #409 - Delete no longer leaves file in memory.
* Fixes #172 - Lock states no longer persist through power cycling unit. Now they become default for unlocked state
   * Also #358, #362, #568
* Fixes #580 - RT "signal lost. waiting to re-aquire signal" check previously disallowed manned terminal use.  Now it only disables the terminal if the vessel is unmanned.
* Fixes #344 - KOSArgumentMismatchException reported wrong arg number (i.e. it would claim your 3rd argument is wrong when it's really your 1st argument).  Fixed.


## v0.15.6

* Fixes RemoteTech Integration
* Structures can now be correctly ==, <> and concatenated with +
* STAGE:RESOURCE[?]:CAPACITY is now spell correctly :P


## v0.15.5

The KSP 0.90 compatibility release.
(The full thematic following of KSP 0.90's new way of
thinking will come in a future version. This is just
to make sure everything works.)

###BREAKING CHANGES

* Now respects the limitations of [0.90 career mode upgrades](http://ksp-kos.github.io/KOS/general/career_limits.html), which may make a few features not work anymore in career mode until you get further progressed along in your building upgrades.

###New Stuff

* Thanks to a new dev team contributer Johann Goetz (@theodoregoetz on github), we have a new, much better and cleaner looking [documentation site](http://ksp-kos.github.io/KOS_DOC/)
* Better flight input handling to detect the pilot controls and keep them isolated.
* "plays nice" with other autopilots a bit better, using KSP 0.90's new autopiloting hooks.
* Ability to read [more data about a ship resource](TODO - Are these in the docs?  Put URL here if so.) TODO:  i.e. SingleResourceValue:FLOWMODE, for example - see PR #452)
* New [suffixes to handle directions better](http://ksp-kos.github.io/KOS/math/direction.html) as mentioned in [long detail in this video](https://www.youtube.com/watch?v=7byYiZZBBVc)
* Separate Dry Mass, Wet Mass, and Current Mass readings for parts and for the vessel as a whole (TODO: Link here, but the public gh-pages hasn't be regenned yet so I don't know the link yet)
* Added new [WAYPOINT object](http://ksp-kos.github.io/KOS/structures/waypoint.html) to help with locations of some contracts.
* Added new :POSITION and :ALTITUDEPOSITION suffixes to [Geocoordinates](http://ksp-kos.github.io/KOS/math/geocoordinates.html) to obtain 3D vectors of their positions in ship-raw coordinate space.
* ADDED muliple new ways to deal with resources.
	* STAGE:RESOURCES, SHIP:RESOURCES and TARGET:RESOURCES will let you get a list of the resources for the craft, the difference being that SHIP: and TARGET: includes all resources and STAGE: includes only the resoures that are for "this stage". All three of these will let you get a list of :PARTS that can contain that resource.
	* Part resources now gives you access to the resource's tweakable :ENABLE and :TOGGLEABLE can let you remove add a resource to the normal resource flow.

###Bug Fixes

* Better handling of range checking and loading the boot file when remotetech is installed (thanks to hvacengi for this contribution)
* Boot file overwrite fix (thanks to pakrym)
* (For developers) fixed compile error on UNIX platforms that was due to filename case-sensitivity differences.
* LOG command to the Archive now appends to the file properly instead of rewriting the entire contents each time just to tack on one line.  It is now possible to read its output from outside KSP using a tool like the UNIX "tail -f" program.
* Better calculations of stage resource values, using SQUAD'S provided API for it instead of trying to walk the tree ourselves (which broke in 0.90).
* Fixed lonstanding [bug with geocoordinates:TERRAINHEIGHT](https://github.com/KSP-KOS/KOS/issues/478)

###Small maintenence issues

* Bundling a newer version of ModuleManager
* Better use of the "skin" system for the app panel.  Should see no obvious effect on the surface.


## v0.15.4

###BREAKING CHANGES

* Issue #431: SHIP:ANGULARMOMENTUM and SHIP:ANGULARVEL have been changed from directions to vectors to me more consistant with their nature

#### New Stuff:

* Should now be compatible with KSP 0.90

#### Bug Fixes:

* Issue #421: some local files are corrupt
* Issue #423: its possible to create a file with no extension
* Issue #424: additional bootfile suffix protection
* Issue #429: files sent to persistence file no longer get truncated


## v0.15.3

BugFixes:

* Issue #417: No error message on nonexistent function.
* Issue #413: Name tag window should get keyboard focus when invoked.
* Issue #405: Equality operator is broken for Wrapped structure objects.
* Issue #393: Files on local volume do not persist through save/load.


## v0.15.2

BugFixes:

* :MODULESNAMED returns something useful now #392
* array syntax bugs #387
* Added :PORTFACING to docking ports that should always have the correct facing for the port itself #398
* BREAKING: Partfacing should now come out of the top rather than the side #394


## v0.15.1

BugFixes:

* All Lists have suffixes again
* in the config panel, IPU no longer gets stuck at 50


## v0.15


## NEW FEATURES:

Please follow the links to see the full information on the new features.

* [Added new kOS GUI panel to the KSP Applauncher system](http://ksp-kos.github.io/KOS_DOC/summary_topics/applauncher_panel/index.html).  With this you can alter config values, and open/close terminals from one common panel.  Just click the little kOS logo button in either the editors (VAB/SPH) or in flight view.
* [Added pilot input to flight controls](http://ksp-kos.github.io/KOS_DOC/structure/control/index.html#pilot-commands) which lets you read/write the users control state, you can use this to set the exit behavior for the mainthrottle.
* Several suffixes are now [methods that you can call](ksp-kos.github.io/KOS_DOC/#structure_methods) with arguments.
	* eg before to add to a list it was SET LIST:ADD TO "FOO". Now it would be LIST:ADD("FOO").
* Suffix methods that perform an action do not need to be assigned to anything.  No more having to say *SET DUMMY TO MYLIST:CLEAR.*  You can now just say *MYLIST:CLEAR.* like it was a statement.
* Added suffixes to OBT for [walking orbit conic patches](http://ksp-kos.github.io/KOS_DOC/structure/orbit/index.html)
	* ORB:HASNEXTPATCH - A boolean that shows the presence of a future patch
	* ORB:NEXTPATCH - The next OBT patch
* Added better techniques for selecting the Part you want from a Vessel:
  * Ability to give any part any name you like with the [new nametag feature](http://ksp-kos.github.io/KOS_DOC/summary_topics/nametag/index.html).
  * [Directly querying a vessel for parts](http://ksp-kos.github.io/KOS_DOC/summary_topics/ship_parts_and_modules/index.html#parts), searching for [nametags](http://ksp-kos.github.io/KOS_DOC/summary_topics/nametag/index.html), or part names or part titles.
	* SHIP:PARTSDUBBED(string)
	* SHIP:PARTSNAMED(string)
	* SHIP:PARTSTAGGED(string)
	* SHIP:PARTSTITLED(string)
	* SHIP:PARTSINGROUP(string)
	* SHIP:MODULESNAMED(string)
  * [Walking the parts Tree](http://ksp-kos.github.io/KOS_DOC/structure/part/index.html):
	* PART:CHILDREN - A ListValue of parts that are descendant from the current part
	* PART:PARENT - A PART that is the ancestor of the current part
	* PART:HASPARENT - A boolean that shows the presence of a Parent PART
	* SHIP:ROOTPART - The first part of a ship.  The start of the tree of parts.  identical to SHIP:PARTS[0].
  * *SET MyList TO SHIP:PARTS.* now does the same thing as *LIST PARTS IN MyList.*
* A [new system lets you access the PartModules](http://ksp-kos.github.io/KOS_DOC/structure/partmodule/index.html) that the stock game and modders put on the various parts.  Through this, you now have the ability to manipulate a lot of the things that are on the rightclick menus of parts:
  * PART Suffixes:
	* GETMODULE(string)
	* ALLMODULES.
  * PartModule Suffixes:
	* GETFIELD(field_name) - read a value from a rightclick menu
	* SETFIELD(field_name, new value) - change a value on a rightclick menu, if it would normally be adjustable via a tweakable control.
	* DOACTION(name_of_action_) - cause one of the actions that would normally be available to action groups *even if it hasn't been assigned to an action group*.
	* DOEVENT(event_name) - "presses a button" on the rightclick part menu.
	* Several others..
* [Lists are now saner to work with](http://ksp-kos.github.io/KOS_DOC/structure/list/index.html) with no longer needing to use weird side effects to get things done, now that there's proper methods available:
  * :ADD has changed:
	* Old Way: *SET MyList:ADD TO NewVal.*
	* New Way: *MyList:ADD(NewVal).*
  * :REMOVE has changed:
	* Old Way: *SET MyList:REMOVE TO indexnumber.*
	* New Way: *MyList:REMOVE(indexnumber).*
  * :CLEAR has changed:
	* Old Way: *SET Dummy to MyList:CLEAR.*
	* New Way: *MyList:CLEAR().*
* Added ENGINE:AVAILABLETHRUST suffix. A value that respects the thrust limiter
* Added SHIP:AVAILABLETHRUST suffix. A sum of all of the ship's thrust that respects thrust limiters
* Added a [new experimental COMPILE command](http://ksp-kos.github.io/KOS_DOC/command/file/index.html#compile-1-to-2), for making smaller executable-only programs to put on your probes without punishing you for writing legible code with comments and indenting.
* [Filename convention changes](http://ksp-kos.github.io/KOS_DOC/command/file/index.html#volume-and-filename-arguments):
  * Commands that deal with filenames will now allow any arbitrary expressions as the filename, except for the RUN command.
  * *Exception*: The above does NOT apply to the ```RUN``` command.  The run command requires that the filenames are known at compile time, and it cannot allow arbitrary expressions evaluated later at runtime.
  * Program files are now called *.ks instead of *.txt.  When you first run the new version, it will give you an option to rename your files to the new name for you as they are moved to the new location.
  * Although the default script filenames use *.ks, you can override this and explicitly mention filename extensions in all the filename commands.  What you can't do is have filenames with no extensions.
* Added support for CKAN
* Added config file so kOS will now show up in Automatic Version Checker (AVC)


## CHANGES BREAKING OLD SCRIPTS:

* BREAKING: **.txt files are now .ks files**: The new assumed default file extension for files used by kOS is *.ks rather than *.txt.  This may confuse old IDE's, or your computer's assumed file associations.
* BREAKING: **VesselName**: Removed previously deprecated term "VESSELNAME", use "SHIPNAME"
* BREAKING: **SHIP:ORB:PATCHES**: Moved SHIP:ORB:PATCHES to SHIP:PATCHES and it now contains all orbit patches
* BREAKING: **Lists**: New syntax for using :ADD, and :REMOVE suffixes for lists requires old code to be altered.  See features above for the new way.
* WARNING: **Bundled ModuleManager**: Because kOS now needs ModuleManager, and ModuleMangaer complains about being run on Windows 64bit, you now see a new warning message if you run kOS on Windows 64bit, but the message is ignorable and kOS still runs.
* BREAKING: **identifiers as filenames**: If you use the same name for a filename as the name of a variable, in a file command such as COPY, DELETE, etc, then kOS will no longer use the variable's name as the filename but will now use the variable's contents as the filename.


## BUG FIXES:

* [PartValue:POSITION not using ship-relative coords](https://github.com/KSP-KOS/KOS/issues/277)
* [Boot file name is case sensitive](https://github.com/KSP-KOS/KOS/issues/311)
* [Engine reports maxthrust on :ISP suffix](https://github.com/KSP-KOS/KOS/issues/331)
* [LIST VOLUMES IN <list> makes an empty list.](https://github.com/KSP-KOS/KOS/issues/308)
* [Parser doesn't understand THING[num]:THING[NUM] or thing[index]:suffix(arg)](https://github.com/KSP-KOS/KOS/issues/268)
* [ship:obt:patches seems to be missing some of the patches](https://github.com/KSP-KOS/KOS/issues/252)  (Note: in addition to fixing it, the patches list was moved to just ship:patches, which makes more sense).
* [Compiler should throw exception on trying to put a WAIT in a trigger.](https://github.com/KSP-KOS/KOS/issues/254)
- - -


## V0.14.2

* Added entry cost to kOS parts
* extended button lockout to include the full throttle button (default z)
* updated the reference to RemoteTech rather than RemoteTech2


## V0.14.1

* Kerbal Space Program 0.25 Support
* OnSave and OnLoad should no longer disappear your craft. This is a bit of a stop-gap fix that doesn't guarantee that the kOS part will be happy, but at least your craft will still be there.
* Resolves #257 by unbinding the X key from throttle cutoff while the window is in focus.
* KSP AVC Support
* Added Body:RotationalPeroid


## v0.14

### New Hotness

* Updated fonts, Thanks @MrOnak
* Now runtime errors show source location and call stack trace (Github issues #186 and #210).  Example:
~~~
	Tried To push Infinity into the stack.
	At MyProgramFile2 on Archive, line 12
		PRINT var1/var2.
				  ^
	Called from MyProgramFile1 on Archive, line 213
	RUN MyProgramFIle2("hello").
	^
	Called from StartMission on Archive, line 2.
	RUN MyProgramFile1.
	^
	_
~~~
* (WHEN and ON) Triggers that are taking longer than an Update is meant to take, and thus can freeze KSP are caught and reported (Github issue #104).  Gives the user an explanatory message about the problem.
  * WARNING: Because of a change that had to be done for this, it is **_Highly_ recommended that you increase your *InstructionsPerUpdate* setting in config.xml to 150% as much** as it was before (i.e. from 100 to 150, or if it was 200, make it 300.).
* Multiple Terminal Windows - possible to have one open per CPU part.  (Github issue #158)

![Multiple Windows!](https://github.com/KSP-KOS/KOS/blob/master/Docs/Images/MultiEdit.png)

### Old And Busted ( Now Fixed )

* "rename" was deleting files instead of moving them. (Github issue #220).
* Was parsing array index brakets "[..]" incorrectly when they were on the lefthand side of an assignment.  (Github issue #219)
* SHIP:SENSORS were reading the wrong ship's sensors sometimes in multi-ship scenarios.  (GIthub issue #218 )
* Integer and Floating point numbers were not quite properly interchangable like they were meant to be. (Github issue #209)


## v0.13.1

* Fixed an issue with Dependancies that kept kOS modules from registering


## v0.13


## MAJOR

* BREAKING: Commrange has more or less been removed from stock kOS, we realized that most of the behavior of it was copied by other mods and was invisible to users
* BREAKING: All direction references are now relative to the controlling part, not the vessel, this will only break on vessels there these two directions are not the same.
* BREAKING: Direction:Vector will always return a unit vector.
* BREAKING: Body:Velocity now returns a <a href="http://ksp-kos.github.io/KOS_DOC/structure/orbitablevelocity/">pair of orbit/surface velocities</a> just like Vessel:Velocity does. (previously it returned just the orbit velocity as a single vector.)
* BREAKING: Direction*Vector now returns the rotated Vector, and vectors can be rotated with DIRECTION suffix.
* BREAKING: DOCKINGPORT:DOCKEDVESSELNAME is not DOCKINGPORT:DOCKEDSHIPNAME
* SHIP:APOAPSIS and SHIP:PERIAPSIS are deprecated for removal later, you can find them both under SHIP:OBT
* SHIP:VESSELNAME is deprecated for later removal, use SHIP:NAME or SHIPNAME


## New Features

* Added the ability to get and set the current timewarp "Mode" either RAILS or PHYSICS
* Added Boot files that will run when you get to the pad automatically, you select which one will run in the VAB thanks @WazWaz
* <a href="http://ksp-kos.github.io/KOS_DOC/structure/vessel/">Vessels</a> and <a href="http://ksp-kos.github.io/KOS_DOC/structure/body/">Bodies</a> now <a href="http://ksp-kos.github.io/KOS_DOC/structure/orbitable/">can be used interchangeably as much as possible.</a>
* Three new prediction routines for <a href="http://ksp-kos.github.io/KOS_DOC/command/prediction/"> finding state of an object at a future time: </a>
* POSITIONAT( Object, Time ).
* VELOCITYAT( Object, Time ).
* ORBITATAT( Object, Time ).
* you can now get the FACING of all parts.
* ITERATOR:END is now split into :NEXT and :ATEND
* Direction can now always return a proper vector.
	* IE SHIP:FACING returned V(0,0,0) before
* Added a 3d Drawing tool for letting you draw lines and labels.
	* Tour: https://www.youtube.com/watch?v=Vn6lUozVUHA
* Added a new and improved file editor so the edit command actually works again in game!
* Added the ability to switch to MapView and back in code
* ACTIVESHIP alias links to the ship that is currently under user direct control
* added GEOPOSITION suffixes BODY and TERRAINHEIGHT


## Known Issues

* due to issues with the new version of RemoteTech, you will always have a connection available for use with kOS.


## Fixes

* if you have a target and attempt to set a new target and that fails, you would no longer have a target
* increased power requirement of the kOS Module
* Bodies are now targetable
* MAXTHRUST no longer includes flamed out engines
* resource floating values are now truncated to 2 significant digits to match the game UI and behavior
* files saved to the local volume maintain their linebreaks
* radar altimiter now returns a double
* fixed an issues where setting some controls blocked the rest.
* allow empty bodies on {} blocks
* locks called from another lock are not correctly recognized
* Neutralizing the controls will clear the values of all controls.
* fixed node initialization
* Better resource processing
* LIST:COPY returns a kOS type that you can actually use
* ORBIT:TRANSITION returns a string type that you can actually use.
* Comments in code dont cause data loss on load/save


## v0.12.1

BREAKING: DOCKINGPORT:ORIENTATION is now DOCKINGPORT:FACING

* Fixed Terminal linewrap @ the bottom of the terminal
* Fixed "Revert to Launch" button, it was blowing up the world and not allowing control before
* Fixed LOCK s in subprograms
* Fixed RemoteTech integration blowing up everything
* Fixed flight controls not releasing when they should
* Disabled RemoteTech Integration while RT development is stalled
* Fix exception when trying to type a multiline instruction in the interpreter
* srfprograde is available as a new shortcut
* BODY now has an OBT suffix
* Parts now have a SHIP suffix
* You can now work with your target if that target is a docking port
* Added a new PRESERVE keyword for repeating a trigger.
* all active triggers are removed when a script is finished.


## v0.12.0

* the aforementioned new parser by @marianoapp with all of its speed improvements and other goodies.
* There's a config SpecialValue that can be used to control how some of the mod features work, like setting the execution speed, the integration with RT2 and starting from the archive volume.
* The terminal screen can be scrolled using PageUp and PageDown
* Negative numbers/expressions can be written starting with a minus sign, so no more "0-..."
* Added ELSE syntax!
* Added ELSE syntax!
* Added NOT syntax!!
* Added List square brackets [] as list subelement accessor
* you can use variables as arguments for PRINT AT statements

This version adds a new 0.625m part. Thanks to SMA on this neat new addition.

* it works as a kOS computer core
* has 5000 units of code space
* as a smaller part it is unlocked with "precision engineering" in career mode.
* also has a light that will be controllable before the actual release

Bug fixes

* Cannot "set" a variable that later will become a "lock" #13
* Sanitize values sent to KSP #14
* Strange order of operations: "and" seems to evaluate before ">" #20
* moved some names back to "kOS"
* Work on some structure's ToString return.
* Parameters now get passed in the correct order
* Ship resources no longer generate an error if they arent present
* Ctrl+C now interrupts correctly once again.
* ETA:TRANSITION returns the correct time.
* Better handling of types.


## v0.11.1

* BREAKING: Disk Space is now defined by the kOS part, existing missions might have the available space reduced to 500. (whaaw)
* BREAKING: Vector * Vector Operator has been changed from V( X * X, Y * Y, Z * Z) to a dot product.
* Added Ctrl+Shift+X hotkey to close the terminal window (jwvanderbeck)
* Improved RemoteTech integration (jwvanderbeck) Current state is discussed https://github.com/erendrake/KOS/pull/51
* Added engine stats to the enginevalue
	* ACTIVE (get/set)
	* ALLOWRESTART (get
	* ALLOWSHUTDOWN (get)
	* THROTTLELOCK (get)
	* THRUSTLIMIT (get/set)
* Added to BODY:ATM:SEALEVELPRESSURE
* Added a DockingPort Part Type, You can access it by "LIST DOCKINGPORTS IN ..."
* Added PART:CONTROLFROM which centers the transform on that part.
* Vector now has two new Suffixes
	* NORMALIZED - Vector keeps same direction, but will have a magnitude of 1.
	* SQRMAGNITUDE - https://docs.unity3d.com/Documentation/ScriptReference/Vector3-sqrMagnitude.html
* New math operators involving Vectors
	* VECTORCROSSPRODUCT (VCRS)
	* VECTORDOTPRODUCT (VDOT)
	* VECTOREXCLUDE (VXCL) - projects one vector onto another
	* VECTORANGLE (VANG) - Returns the angle in degrees between from and to.
* Direct control of vessel and nearby vessels (SHIP:CONTROL, TARGET:CONTROL)
	* __GETTERS__
	* YAW - Rotation (1 to -1)
	* PITCH - Rotation (1 to -1)
	* ROLL - Rotation (1 to -1)
	* FORE - Translation (1 to -1)
	* STARBOARD - Translation (1 to -1)
	* TOP - Translation (1 to -1)
	* ROTATION - Vector
	* TRANSLATION - Vector
	* NEUTRAL - bool,
	* MAINTHROTTLE (1 to -1)
	* WHEELTHROTTLE (1 to -1)
	* WHEELSTEER (1 to -1)
	* __SETTERS__
	* YAW - Rotation (1 to -1)
	* PITCH - Rotation (1 to -1)
	* ROLL - Rotation (1 to -1)
	* FORE - Translation (1 to -1)
	* STARBOARD - Translation (1 to -1)
	* TOP - Translation (1 to -1)
	* ROTATION - Vector
	* TRANSLATION - Vector
	* NEUTRALIZE - bool, releases vessel control,
	* MAINTHROTTLE (1 to -1)
	* WHEELTHROTTLE (1 to -1)
	* WHEELSTEER (1 to -1)
* changing systems vessel load distance
	* LOADDISTANCE get/set for adjusting load distance for every vessel
	* VESSELTARGET:LOAD bool - is the vessel loaded
	* VESSELTARGET:PACKDISTANCE - Setter for pack distance for every vessel.
* Added RANDOM() generator (0 - 1)
* Power requirements are now directly tied to the active volume's size, the ARCHIVE's size is unlimited so it is capped at the equivalent of 50KB.

### 0.11.0

- Thanks to enkido and jwvanderbeck for your help.
- BREAKING: BODY, SHIP:BODY, TARGET:BODY now all return a Body structure rather than the name of the body
- BREAKING: Removed NODE:APOAPSIS and NODE:PERIAPSIS. They are now available in NODE:ORBIT:APOAPSIS
- Basic RemoveTech Intergration
- Added VOLUME:NAME to getting the current volume
- Lists can now be populated with basic data that you can loop over or index [Full Info](/wiki/List/)
	- Bodies (eg Kerbin, Mun, Duna)
	- Targets - All Vessels other than current
	- Engines - Engines on the craft
	- Resources - All Ship Resources
	- Parts - All Ship Parts (slow)
	- Sensors - (eg Pres, Grav, Accel)
	- Elements - All flights connected to the active vessel
- A Lot of bug fixes and refactoring
- Constants (eg G, E, PI) are now retrieved using CONSTANT() rather than spreadout.
- Commands resolve in order of descending specificity, rather than in the pseudorandom order they were in before
- Added Math operators LN, LOG10, MIN, MAX.

### 0.10.0

- Compatible with KSP 0.23 Thanks to Logris and MaHuJa for Commits
- Added List() which creates a collection and the following commands
	- ADD - Adds the value of any variable
	- CONTAINS - Tests and returns if the value exists in the list
	- REMOVE - removes the item from the list if the list contains the item
	- LENGTH - returns a count of the items in the list
	- COPY - creates a copy of the list
	- You can also index into a list with # (ie LIST#1 gives you the second item in the list).
- Added the following stats
	- OBT:PERIOD - http://en.wikipedia.org/wiki/Orbital_period
	- OBT:INCLINATION - http://en.wikipedia.org/wiki/Orbital_inclination
	- OBT:ECCENTRICITY - http://en.wikipedia.org/wiki/Orbital_eccentricity
	- OBT:SEMIMAJORAXIS - http://en.wikipedia.org/wiki/Semi-major_axis
	- OBT:SEMIMINORAXIS - http://en.wikipedia.org/wiki/Semi-major_axis
	- VOLUME:NAME - Name of the current Volume
	- ETA:TRANSITION - Seconds until next patch
	- OBT:TRANSITION - Type of next patch: possibilities are
		- FINAL
		- ENCOUNTER
		- ESCAPE
		- MANEUVER
- Adding a few BODY members
	- RADIUS
	- MU - G * Body Mass
	- G - Gravitational Constant
	- ATM atmosphere info with sub elements
		- EXISTS
		- HASOXYGEN
		- SCALE
		- HEIGHT
- Added ORBIT to NODE
- Added the following commands
	- UNSET #VARIABLE - remove the variable, ALL removes all variables Thanks a1070
	- FOR #USERVARIABLE IN #LIST takes a list and loops over it, exposing each item in the collection as a user defined variable
- New close window action binding
- Performance fixes

### 0.9.2

- Fixed a bug where you couldn't have an IF or WHEN inside an UNTIL
- Added INLIGHT

### 0.9.1

- Fixed a bug where AND and OR wouldn't work on boolean values

### 0.9

- New expression system added
- Version Info can be grabbed programatically
- Targeting of bodies, getting stats
- Get values from other ships, just like current ship
- Send commands to chutes, legs and solar panels individually
- Round function to x decimals, modulo
- Setting values on structures
- Vectors now use double precision
- Get apoapsis and periapsis of a node

### 0.85

- Problems adding R() structures to headings
- Some commands wouldn't allow dashes in filenames
- Editor X-scrolling bug fixed
- Delimeter matcher would cause error inside comments
- Editor was crashing when you hit end
- Error messages wouldn't have line numbers if the error occurred inside curly braces.
- For nodes, :DELTAV:MAG now works correctly. Note that :DELTAV is now effectively the same as :BURNVECTOR
- You can now use right & left arrows to edit lines in immediate mode
- There is now a LEGS binding for landing legs, use it just like SAS or GEAR
- Same with CHUTES for parachutes

### 0.82

- Couldn't switch to a volume using it's number
- All curly braces were horribly horribly broken

### 0.8

- Maneuver node support added
- Time structure added
- Programs that call other programs no longer give multiple "Program ended" messages

### 0.7

- Official release of mod interoperability
- New altitude radar system
- Improved whitespace support

### 0.65

- Trigonometry Functions ARCSIN, ARCCOS, ARCTAN and ARCTAN2 are now implemented.
- Programs can now contain parameters
- You can now get the distance to an arbitrary latlng with :distance
- Cpu clock speed has been raised from 1 to 5.
- Variables now persist
- Ranges have been increased

### 0.6

- Simplified steering system
- Support for driving your rover wheels to a specific location
- Support for surface vectors
- Rovers can be steered towards arbitrary geo coordinates
- Preliminary support for 3rd party mod integration (Still testing)
- Bug fixes

### 0.5

- Rover bindings
- Range limits for archive drive
- Bug fixes
- Trig functions
- ABS function

### 0.45

- Fix for the matching on boolean operators

### 0.44

- Support for AND and OR in IF statements added
- Fix for nested IF statements

### 0.43

- Fixes locking and waiting for compound values

### 0.42

- Fixes handling of compound values in certain circumstances

### 0.41

- Fixes a bug dealing with the Archive folder

### 0.4

- Bug fixes
- Interact with subelements of R()
- Targetting
- Radar altitude
- Plaintext editing
- Toggle terminal & power from action group
- Maximum thurst variable
- Surface speed variable

### 0.35

- Fix for the typing when not focussed bug
- Fix for file renaming not checking for previous existing file
- Resource tags re-enabled
- Support for limited sub-expressions within R() expressions

### 0.34

- Added non-qwerty keyboard support
- Fix for laggy systems dropping typed characters

### 0.33

- Fix for the case of texture paths that would break on linux

### 0.32

- Implemented a friendly message that your textures are missing

### 0.31

- Fixed an issue that would kill saves
- Added support for maneuver nodes from Ehren Murdick's code
- Added stage:solidfuel from Pierre Adam's code

### 0.3

- Support for loops
- Support for the IF statement
- Support for the BREAK statement

### 0.2

- Initial public release!
- Execution system redesigned to be more heirarchical
- Added support for compound statements
- Successful test of synchronized orbiting

### 0.1

- First trip to orbit successfully done
- Terminal created
- KerboScript designed and implemented
- VAB Part created
- Flight stats and bindings created