.. _kuniverse:

KUniverse 4th wall methods
==========================


.. structure:: KUniverse

    :struct:`KUniverse` is a special structure that allows your Kerboscript programs to access some of the functions that break the "4th Wall".  It serves as a place to access object directly connected to the KSP game itself, rather than the interaction with the KSP world (vessels, planets, orbits, etc.).

    .. list-table::
        :header-rows: 1
        :widths: 3 1 1 4

        * - Suffix
          - Type
          - Get/Set
          - Description

        * - :attr:`CANREVERT`
          - :struct:`Boolean`
          - Get
          - Is any revert possible?
        * - :attr:`CANREVERTTOLAUNCH`
          - :struct:`Boolean`
          - Get
          - Is revert to launch possible?
        * - :attr:`CANREVERTTOEDITOR`
          - :struct:`Boolean`
          - Get
          - Is revert to editor possible?
        * - :meth:`REVERTTOLAUNCH`
          - None
          - Method
          - Invoke revert to launch
        * - :meth:`REVERTTOEDITOR`
          - None
          - Method
          - Invoke revert to editor
        * - :meth:`REVERTTO(name)`
          - :struct:`String`
          - Method
          - Invoke revert to the named editor
        * - :attr:`ORIGINEDITOR`
          - :struct:`String`
          - Get
          - Returns the name of this vessel's editor, "SPH" or "VAB".
        * - :meth:`PAUSE`
          - None
          - Method
          - Pauses KSP, bringing up the "Escape Menu".
        * - :attr:`CANQUICKSAVE`
          - :struct:`Boolean`
          - Get
          - Returns true if quicksave is currently enabled and available.
        * - :meth:`QUICKSAVE()`
          - None
          - Method
          - Invoke KSP's built in quicksave.
        * - :meth:`QUICKLOAD()`
          - None
          - Method
          - Invoke KSP's built in quickload.
        * - :meth:`QUICKSAVETO(name)`
          - :struct:`String`
          - Method
          - Perform quicksave to the save with the given name.
        * - :meth:`QUICKLOADFROM(name)`
          - None
          - Method
          - Perform quickload from the save with the given name.
        * - :attr:`QUICKSAVELIST`
          - :struct:`List` of :struct:`String`
          - Get
          - A list of all quicksave files for this game.
        * - :attr:`HOURSPERDAY`
          - :struct:`Scalar`
          - Get
          - Number of hours per day (6 or 24) according to your game settings.
        * - :meth:`DEBUGLOG(message)`
          - None
          - Method
          - Causes a :struct:`String` to append to the Unity debug log file.
        * - :attr:`DEFAULTLOADDISTANCE`
          - :struct:`LoadDistance`
          - Get
          - Returns the set of default load and pack distances for the game.
        * - :attr:`TIMEWARP`
          - :struct:`TimeWarp`
          - Get
          - Returns a value you can use to manipulate Kerbal Space Program's time warp.
        * - :attr:`ACTIVEVESSEL`
          - :struct:`Vessel`
          - Get/Set
          - Returns the active vessel, or lets you set the active vessel.
        * - :meth:`FORCESETACTIVEVESSEL(vessel)`
          - None
          - Method
          - Lets you switch active vessels even when the game refuses to allow it.
        * - :meth:`FORCEACTIVE(vessel)`
          - None
          - Method
          - Same as :meth:`FORCESETACTIVEVESSEL`
        * - :meth:`GETCRAFT(name, editor)`
          - :struct:`CraftTemplate`
          - Method
          - Get the file path for the craft with the given name, saved in the given editor.
        * - :meth:`LAUNCHCRAFT(template)`
          - None
          - Method
          - Launch a new instance of the given craft at it's default launch site.
        * - :meth:`LAUNCHCRAFTFROM(template, site)`
          - None
          - Method
          - Launch a new instance of the given craft at the given site.
        * - :meth:`LAUNCHCRAFTWITHCREWFROM(template, crewlist, site)`
          - None
          - Method
          - Launch a new instance of the given craft with this crew list at the given site.
        * - :meth:`CRAFTLIST()`
          - :struct:`List` of :struct:`CraftTemplate`
          - Method
          - A list of all craft templates in the save specific and stock folders.
        * - :attr:`REALTIME`
          - :struct:`Scalar`
          - Get only
          - Real world timestamp (outside of game) in seconds since 1970


.. attribute:: KUniverse:CANREVERT

    :access: Get
    :type: :struct:`Boolean`.

    Returns true if either revert to launch or revert to editor is available.  Note: either option may still be unavailable, use the specific methods below to check the exact option you are looking for.

.. attribute:: KUniverse:CANREVERTTOLAUNCH

    :access: Get
    :type: :struct:`Boolean`.

    Returns true if either revert to launch is available.

.. attribute:: KUniverse:CANREVERTTOEDITOR

    :access: Get
    :type: :struct:`Boolean`.

    Returns true if either revert to the editor is available.  This tends
    to be false after reloading from a saved game where the vessel was
    already in existence in the saved file when you loaded the game.

.. method:: KUniverse:REVERTTOLAUNCH()

    :access: Method
    :type: None.

    Initiate the KSP game's revert to launch function.  All progress so far will be lost, and the vessel will be returned to the launch pad or runway at the time it was initially launched.

.. method:: KUniverse:REVERTTOEDITOR()

    :access: Method
    :type: None.

    Initiate the KSP game's revert to editor function.  The game will revert to the editor, as selected based on the vessel type.

.. method:: KUniverse:REVERTTO(editor)

    :parameter editor: The editor identifier
    :return: None

    Revert to the provided editor.  Valid inputs are `"VAB"` and `"SPH"`.

.. attribute:: KUniverse:ORIGINEDITOR

    :access: Get
    :type: :struct:`String`.

    Returns the name of the originating editor based on the vessel type.
    The value is one of:

    - "SPH" for things built in the space plane hangar,
    - "VAB" for things built in the vehicle assembly building.
    - "" (empty :struct:`String`) for cases where the vehicle cannot remember its editor (when KUniverse:CANREVERTTOEDITOR is false.)

.. method:: KUniverse:PAUSE()

    :access: Method
    :type: None.

    Pauses Kerbal Space Program, bringing up the same pause menu that would
    normally appear when you hit the "Escape" key.

    **Warning:** *NO lines of Kerboscript code can run while the game is
    paused!!!  If you call this, you will be stopping your script there
    until a human being clicks "resume" on the pause menu.*

    kOS is designed to thematically act like a computer that lives *inside*
    the game universe. That means it stops when the game clock stops, for
    the same reason a bouncing ball stops when the game clock stops.

    Until a human being resumes the game by clicking the Resume button
    in the menu, your script will be stuck.  This makes it impossible
    to have the program run code that decides when to un-pause the game.
    Once the Resume button is clicked, then the program will
    continue where it left off, just after the point where it called
    ``KUniverse:PAUSE().``.

    Note, if you use Control-C in the terminal to kill the program,
    that *will* work while the game is paused like this.  If you make
    the mistake of having your script keep re-pausing the game every
    time the game resumes (i.e. you call ``Kuniverse:PAUSE()``
    again and again in a loop), then using Control-C in the terminal
    can be a way to break out of this problem.

.. attribute:: KUniverse:CANQUICKSAVE

    :access: Get
    :type: :struct:`Boolean`

    Returns true if KSP's quicksave feature is enabled and available.

.. method:: KUniverse:QUICKSAVE()

    :access: Method
    :type: None.

    Initiate the KSP game's quicksave function.  The game will save the current
    state to the default quicksave file.

.. method:: KUniverse:QUICKLOAD()

    :access: Method
    :type: None.

    Initiate the KSP game's quickload function.  The game will load the game
    state from the default quickload file.

.. method:: KUniverse:QUICKSAVETO(name)

    :parameter name: The name of the save file
    :return: None

    Initiate the KSP game's quicksave function.  The game will save the current
    state to a quicksave file matching the name parameter.

.. method:: KUniverse:QUICKLOADFROM(name)

    :parameter name: The name of the save file
    :return: None

    Initiate the KSP game's quickload function.  The game will load the game
    state from the quicksave file matching the name parameter.

.. attribute:: KUniverse:QUICKSAVELIST

    :access: Get
    :type: :struct:`List` of :struct:`String`

    Returns a list of names of all quicksave file in this KSP game.

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

.. attribute:: KUniverse:TIMEWARP

    :access: Get
    :type: :struct:`TimeWarp`.

    Returns the :struct:`TimeWarp` structure that you can use to manipulate
    Kerbal Space Program's time warping features.   See the documentation
    on :struct:`TimeWarp` for more details.
    
    example: ``set kuniverse:timewarp:rate to 50.``
    
.. attribute:: KUniverse:ACTIVEVESSEL

    :access: Get/Set
    :type: :struct:`Vessel`.

    Returns the active vessel object and allows you to set the active vessel.  Note: KSP will not allow you to change vessels by default when the current active vessel is in the atmosphere or under acceleration.  Use :meth:`FORCEACTIVE` under those circumstances.

.. method:: KUniverse:FORCESETACTIVEVESSEL(vessel)

    :parameter vessel: :struct:`Vessel` to switch to.
    :return: None

    Force KSP to change the active vessel to the one specified.  Note: Switching the active vessel under conditions that KSP normally disallows may cause unexpected results on the initial vessel.  It is possible that the vessel will be treated as if it is re-entering the atmosphere and deleted.

.. method:: KUniverse:FORCEACTIVE(vessel)

    :parameter vessel: :struct:`Vessel` to switch to.
    :return: None

    Same as :meth:`FORCESETACTIVEVESSEL`.

.. attribute:: KUniverse:HOURSPERDAY

    :access: Get
    :type: :struct:`Scalar` (integer)

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

    This setting also affects how values from :struct:`TimeSpan` and
    :struct:`TimeStamp` calculate the ``:hours``, ``:days``, and ``:years``
    suffixes.

    Note that this setting is not settable.  This decision was made because
    the main stock KSP game only ever changes the setting on the main
    settings menu, which isn't accessible during play.  It's entirely
    possible for kOS to support changing the value mid-game, but we've
    decided to deliberately avoid doing so because there may be other mods
    with code that only reads the setting once up front and then assumes
    it never changes after that.  Because in the stock game, that
    assumption would be true.

.. method:: KUniverse:GETCRAFT(name, editor)

    :parameter name: :struct:`String` craft name.
    :parameter facility: :struct:`String` editor name.
    :return: :struct:`CraftTemplate`

    Returns the :struct:`CraftTemplate` matching the given craft name saved from
    the given editor.  Valid values for editor include ``"VAB"`` and ``"SPH"``.

.. method:: KUniverse:LAUNCHCRAFT(template)

    :parameter template: :struct:`CraftTemplate` craft template object.

    Launch a new instance of the given :struct:`CraftTemplate` from the
    template's default launch site.

    **NOTE:** The craft will be launched with the KSP default crew assignment,
    as if you had clicked launch from the editor without manually adjusting the
    crew.

    **NOTE:** Due to how KSP handles launching a new craft, this will end the
    current program even if the currently active vessel is located within
    physics range of the launch site.

.. method:: KUniverse:LAUNCHCRAFTFROM(template, site)

    :parameter template: :struct:`CraftTemplate` craft template object.
    :parameter site: :struct:`String` launch site name.

    Launch a new instance of the given :struct:`CraftTemplate` from the given
    launch site. Valid values for site include ``"RUNWAY"`` and ``"LAUNCHPAD"``.

    **NOTE:** The craft will be launched with the KSP default crew assignment,
    as if you had clicked launch from the editor without manually adjusting the
    crew.  To pick which crew are on the craft use
    :meth:`Kuniverse:LAUNCHCRAFTWITHCREWFROM()` instead.

    **NOTE:** Due to how KSP handles launching a new craft, this will end the
    current program even if the currently active vessel is located within
    physics range of the launch site.

.. method:: KUniverse:LAUNCHCRAFTWITHCREWFROM(template, crewlist, site)

    :parameter template: :struct:`CraftTemplate` craft template object.
    :parameter crewlist: :struct:`List` of :struct:`String` kerbal names.
    :parameter site: :struct:`String` launch site name.

    Launch a new instance of the given :struct:`CraftTemplate` with the given crew
    manifest from the given launch site.
    Valid values for site include ``"RUNWAY"`` and ``"LAUNCHPAD"``.

    If any of the kerbal names you use in the ``crewlist`` parameter don't
    exist in the game, there will be no error.  Instead that name just 
    gets ignored in the list.

    **NOTE:** Due to how KSP handles launching a new craft, this will end the
    current program even if the currently active vessel is located within
    physics range of the launch site.

.. method:: KUniverse:CRAFTLIST()

    :return: :struct:`List` of :struct:`CraftTemplate`

    Returns a list of all :struct:`CraftTemplate` templates stored in the VAB
    and SPH folders of the stock Ships folder and the save specific Ships folder.

.. _debuglog:

.. method:: KUniverse:DEBUGLOG(message)

    :parameter message: :struct:`String` message to append to the log.
    :return: None

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

.. attribute:: KUniverse:REALTIME

    :access: Get Only
    :type: :struct:`Scalar`

    Returns the current time in the real world (outside of the game).
    It uses the so called "UNIX time" convention - that is the number
    of seconds since the start of 1970, right at midnight, 1st January.
    
.. attribute:: KUniverse:REALWORLDTIME

    :access: Get Only
    :type: :struct:`Scalar`

    An alias for :struct:`KUniverse:REALTIME`.

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
