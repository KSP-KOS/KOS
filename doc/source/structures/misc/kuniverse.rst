.. kuniverse:

KUniverse 4th wall methods
==========================


.. structure:: KUniverse

    :struct:`KUniverse` is a special structure that allows your Kerboscript programs to access some of the functions that break the "4th Wall".  It serves as a place to access object directly connected to the KSP game itself, rather than the interaction with the KSP world (vessels, planets, orbits, etc.).

    .. list-table:: Members and Methods
        :header-rows: 1
        :widths: 3 1 1 4

        * - Suffix
          - Type
          - Get/Set
          - Description

        * - :attr:`CANREVERT`
          - boolean
          - Get
          - Is any revert possible?
        * - :attr:`CANREVERTTOLAUNCH`
          - boolean
          - Get
          - Is revert to launch possible?
        * - :attr:`CANREVERTTOEDITOR`
          - boolean
          - Get
          - Is revert to editor possible?
        * - :attr:`REVERTTOLAUNCH`
          - none
          - Method
          - Invoke revert to launch
        * - :attr:`REVERTTOEDITOR`
          - none
          - Method
          - Invoke revert to editor
        * - :attr:`REVERTTO(name)`
          - string
          - Method
          - Invoke revert to the named editor
        * - :attr:`ORIGINEDITOR`
          - string
          - Get
          - Returns the name of this vessel's editor, "SPH" or "VAB".
        * - :attr:`HOURSPERDAY`
          - scalar
          - Get
          - Number of hours per day (6 or 24) according to your game settings.
        * - :attr:`DEBUGLOG(message)`
          - none
          - Method
          - Causes a string to append to the Unity debug log file.
        * - :attr:`DEFAULTLOADDISTANCE`
          - :struct:`LoadDistance`
          - Get
          - Returns the set of default load and pack distances for the game.
        * - :attr:`ACTIVEVESEL`
          - :struct:`Vessel`
          - Get/Set
          - Returns the active vessel, or lets you set the active vessel.
        * - :attr:`FORCEACTIVE(vessel)`
          - n/a
          - Set
          - Lets you switch active vessels even when the game refuses to allow it.


.. attribute:: KUniverse:CANREVERT

    :access: Get
    :type: boolean.

    Returns true if either revert to launch or revert to editor is available.  Note: either option may still be unavailable, use the specific methods below to check the exact option you are looking for.

.. attribute:: KUniverse:CANREVERTTOLAUNCH

    :access: Get
    :type: boolean.

    Returns true if either revert to launch is available.

.. attribute:: KUniverse:CANREVERTTOEDITOR

    :access: Get
    :type: boolean.

    Returns true if either revert to the editor is available.  This tends
    to be false after reloading from a saved game where the vessel was
    already in existence in the saved file when you loaded the game.

.. attribute:: KUniverse:REVERTTOLAUNCH

    :access: Method
    :type: None.

    Initiate the KSP game's revert to launch function.  All progress so far will be lost, and the vessel will be returned to the launch pad or runway at the time it was initially launched.

.. attribute:: KUniverse:REVERTTOEDITOR

    :access: Method
    :type: None.

    Initiate the KSP game's revert to editor function.  The game will revert to the editor, as selected based on the vessel type.

.. method:: KUniverse:REVERTTO(editor)

    :parameter editor: The editor identifier
    :return: none

    Revert to the provided editor.  Valid inputs are `"VAB"` and `"SPH"`.

.. attribute:: KUniverse:ORIGINEDITOR

    :access: Get
    :type: string.

    Returns the name of the originating editor based on the vessel type.
    The value is one of:

    - "SPH" for things built in the space plane hangar,
    - "VAB" for things built in the vehicle assembly building.
    - "" (empty string) for cases where the vehicle cannot remember its editor (when KUniverse:CANREVERTTOEDITOR is false.)

.. attribute:: KUniverse:DEFAULTLOADDISTANCE

    :access: Get
    :type: :struct:`LoadDistance`.

    Get or set the default loading distances for vessels loaded in the future.
    Note: this setting will not affect any vessel currently in the universe for
    the current flight session.  It will take effect the next time you enter a
    flight scene from the editor or tracking station, even on vessels that have
    already existed beforehand.  The act of loading a new scene causes all the
    vessels in that scene to inherit these new default values, forgetting the
    values they may have had before.

    (To affect the value on a vessel already existing in the current scene
    you have to use the :LOADDISTANCE suffix of the Vessel structure.)

.. attribute:: KUniverse:ACTIVEVESSEL

    :access: Get/Set
    :type: :struct:`Vessel`.

    Returns the active vessel object and allows you to set the active vessel.  Note: KSP will not allow you to change vessels by default when the current active vessel is in the atmosphere or under acceleration.  Use :meth:`FORCEACTIVE` under those circumstances.

.. method:: KUniverse:FORCEACTIVE(vessel)

    :parameter vessel: :struct:`Vessel` to switch to.
    :return: none

    Force KSP to change the active vessel to the one specified.  Note: Switching the active vessel under conditions that KSP normally disallows may cause unexpected results on the initial vessel.  It is possible that the vessel will be treated as if it is re-entering the atmosphere and deleted.

.. attribute:: KUniverse:HOURSPERDAY

    :access: Get
    :type: Scalar (integer)

    Has the value of either 6 or 24, depending on what setting you used
    on Kerbal Space Program's main settings screen for whether you wanted
    to think in terms of Kerbal days (6 hours) or Kerbin days (24 hours).
    This only affects what the clock format looks like and doesn't
    change the actual time in game, which is stored purely as a number of
    seconds since epoch anyway and is unaffected by how the time is presented
    to the human being watching the game.  (i.e. if you allow
    25 hours to pass in the game, the game merely tracks that 39000 seconds
    have passed (25 x 60 x 60).  It doesn't care how that translates into
    minutes, hours, days, and years until showing it on screen to the player.)

    This setting also affects how values from :struct:Timespan calculate
    the ``:hours``, ``:days``, and ``:years`` suffixes.

    Note that this setting is not settable.  This decision was made because
    the main stock KSP game only ever changes the setting on the main
    settings menu, which isn't accessible during play.  It's entirely
    possible for kOS to support changing the value mid-game, but we've
    decided to deliberately avoid doing so because there may be other mods
    with code that only reads the setting once up front and then assumes
    it never changes after that.  Because in the stock game, that
    assumption would be true.

.. _debuglog:

.. method:: KUniverse:DEBUGLOG(message)

    :parameter message: string message to append to the log.
    :return: none

    All Unity games (Kerbal Space Program included) have a standard
    "log" file where they can store a lot of verbose messages that
    help developers trying to debug their games.  Sometimes it may
    be useful to make your script log a message to *THAT* debug file,
    instead of using kOS's normal ``Log`` function to append a
    message to some file of your own making.

    This is useful for cases where you are trying to work with a kOS
    developer to trace the cause of a problem and you want your script
    to mark the moments when it hit different parts of the program, and
    have those messages get embedded in the log interleaved with the
    game's own diagnostic messages.

    Here is an example.  Say you suspected the game was throwing an error
    every time you tried to lock steering to up.  So you experiment with
    this bit of code::

        kuniverse:debuglog("=== Now starting test ===").
        kuniverse:debuglog("--- Locking steering to up----").
        lock steering to up.
        kuniverse:debuglog("--- Now forcing a physics tick ----").
        wait 0.001.
        kuniverse:debuglog("--- Now unlocking steering again ----").
        unlock steering.
        wait 0.001.
        kuniverse:debuglog("=== Now done with test ===").

    This would cause the messages you wrote to appear in the debug log,
    interleaved with any error messages kOS, and any other parts of the
    entire Kerbal Space Program game, dump into the same log.

    The location of this log varies depending on your platform.  For
    some reason, Unity chooses a different filename convention for
    each OS.  Consult the list below to see where it is on your platform.

    - Windows 32-bit: [install_dir]\KSP_Data\output_log.txt
    - Windows 64-bit: [install_dir]\KSP_x64_Data\output_log.txt (not officially supported)
    - Mac OS X: ~/Library/Logs/Unity/Player.log 
    - Linux: ~/.config/unity3d/Squad/"Kerbal Space Program"/Player.log

    For an example of what it looks like in the log, this::

        kuniverse:debuglog("this is my message").

    ends up resulting in this in the KSP output log::

        kOS: (KUNIVERSE:DEBUGLOG) this is my message


****

Examples
--------

Switch to an active vessel called "vessel 2"::

    SET KUNIVERSE:ACTIVEVESSEL TO VESSEL("vessel 2").

Revert to VAB, but only if allowed::

    PRINT "ATTEMPTING TO REVERT TO THE Vehicle Assembly Building."
    IF KUNIVERSE:CANREVERTTOEDITOR {
      IF KUNIVERSE:ORIGINEDITOR = "VAB" {
        PRINT "REVERTING TO VAB.".
        KUNIVERSE:REVERTTOEDITOR().
      } ELSE {
        PRINT "COULD REVERT, But only to space plane hanger, so I won't.".
      }
    } ELSE {
      PRINT "Cannot revert to any editor.".
    }
