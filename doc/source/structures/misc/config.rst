.. config:

Configuration of kOS
====================


.. structure:: Config

    :struct:`Config` is a special structure that allows your kerboscript
    programs to set or get the values stored in the kOS plugin's configuration.
    Some of the global values are stored in an external file, while save game
    specific values are stored in that save file.

    .. note::
        .. versionadded:: v1.0.2
            Prior to this version of kOS, all settings were stored globally in
            a single external file.  KSP version 1.2.0 introduced a new way to
            store settings within the save file itself, and most settings were
            migrated to this system.

    .. note::
        If your save file has not yet migrated to the new settings storage
        system and an old config file is present, you will be prompted with a
        dialog box offering to migrate the old settings or use the defaults.
        You may also choose to prevent further attempts to migrate settings.
        If you do so, kOS will set the ``InstructionsPerUpdate`` to a
        negative value in the old config file, as a flag to indicate no
        further migrations should happen.  (Note the old config file is
        still actively used for global settings such as the telnet settings,
        even after you've done this, so don't delete it.)

    The options here can also be set by using the :ref:`App Control Panel <applauncher>`
    or the :ref:`kOS section of KSP's Difficulty Settings<settingsWindow>`

    Because the Telnet server runs as a global instance for KSP, the telnet
    specific settings are stored globally in kOS's external config file.  These
    values are noted as **global** below, but all other values may be presumed
    to be local to the current save file.

    The config file may be found at :file:`{[KSP Directory]}/GameData/kOS/Plugins/PluginData/kOS/`

    .. list-table:: Members (all Gettable and Settable)
        :header-rows: 1
        :widths: 2 1 1 4

        * - Suffix
          - Type
          - Default
          - Description

        * - :attr:`IPU`
          - :struct:`Scalar` (integer)
          - 150
          - Instructions per update
        * - :attr:`UCP`
          - :struct:`Boolean`
          - False
          - Use compressed persistence
        * - :attr:`STAT`
          - :struct:`Boolean`
          - False
          - Print statistics to screen
        * - :attr:`RT`
          - :struct:`Boolean`
          - False
          - Enable RemoteTech2 integration
        * - :attr:`ARCH`
          - :struct:`Boolean`
          - False
          - Start on archive (instead of volume 1)
        * - :attr:`OBEYHIDEUI`
          - :struct:`Boolean`
          - True
          - Obey the KSP Hide user interface key (usually mapped to F2).
        * - :attr:`SAFE`
          - :struct:`Boolean`
          - False
          - Enable safe mode
        * - :attr:`AUDIOERR`
          - :struct:`Boolean`
          - False
          - Enable sound effect on kOS error
        * - :attr:`VERBOSE`
          - :struct:`Boolean`
          - False
          - Enable verbose exceptions
        * - :attr:`TELNET`
          - :struct:`Boolean`
          - False
          - activate the telnet server
        * - :attr:`TPORT`
          - :struct:`Scalar` (integer)
          - 5410
          - set the port the telnet server will run on
        * - :attr:`LOOPBACK`
          - :struct:`Boolean`
          - True
          - Force the telnet server to use loopback (127.0.0.1) address
        * - :attr:`BRIGHTNESS`
          - :struct:`Scalar`
          - 0.7 (from range [0.0 .. 1.0])
          - Default brightness setting of new instances of the in-game terminal
        * - :attr:`DEFAULTFONTSIZE`
          - :struct:`Scalar`
          - 12 (from range [6 .. 20], integers only)
          - Default font size in pixel height for new instances of the in-game terminal
        * - :attr:`DEBUGEACHOPCODE`
          - :struct:`Boolean`
          - false
          - Unholy debug spam used by the kOS developers

.. attribute:: Config:IPU

    :access: Get/Set
    :type: :struct:`Scalar` integer. range = [50,2000]

    Configures the ``InstructionsPerUpdate`` setting.

    This is the number of kRISC psuedo-machine-langauge instructions that each kOS CPU will attempt to execute from the main program per :ref:`physics update tick <cpu hardware>`.

    This value is constrained to stay within the range [50..2000]. If you set it to a value outside that range, it will reset itself to remain in that range.

.. attribute:: Config:UCP

    :access: Get/Set
    :type: :struct:`Boolean`

    Configures the ``useCompressedPersistence`` setting.

    If true, then the contents of the kOS local volume 'files' stored inside the campaign save's persistence file will be stored using a compression algorithm that has the advantage of making them take less space, but at the cost of making the data impossible to decipher with the naked human eye when looking at the persistence file.

.. attribute:: Config:STAT

    :access: Get/Set
    :type: :struct:`Boolean`

    Configures the ``showStatistics`` setting.

    If true, then executing a program will log numbers to the screen showing execution speed statistics.

    When this is set to true, it also makes the use of the
    :ref:`ProfileResult() <profileresult>` function available, for
    deep analysis of your program run, if you are so inclined.

.. attribute:: Config:RT

    :access: Get/Set
    :type: :struct:`Boolean`

    Configures the ``enableRTIntegration`` setting.

    If true, then the kOS mod will attempt to interact with the Remote Tech 2 mod, letting RT2 make decisions about whether or not a vessel is within communications range rather than having kOS use its own more primitive algorithm for it.

    Due to a long stall in the development of the RT2 mod, this setting should still be considered experimental at this point.


.. attribute:: Config:ARCH

    :access: Get/Set
    :type: :struct:`Boolean`

    Configures the ``startOnArchive`` setting.

    If true, then when a vessel is first loaded onto the launchpad or runway, the initial default volume will be set to volume 0, the archive, instead of volume 1, the local drive.

.. attribute:: Config:OBEYHIDEUI

    :access: Get/Set
    :type: :struct:`Boolean`

    Configures the ``obeyHideUI`` setting.

    If true, then the kOS terminals will all hide when you toggle the user
    interface widgets with Kerbal Space Program's Hide UI key (it is
    set to F2 by default key bindings).

.. highlight:: none

.. attribute:: Config:SAFE

    :access: Get/Set
    :type: :struct:`Boolean`


    Configures the ``enableSafeMode`` setting.
    If true, then it enables the following error messages::

        Tried to push NaN into the stack.
        Tried to push Infinity into the stack.

    They will be triggered any time any mathematical operation would result in something that is not a real number, such as dividing by zero, or trying to take the square root of a negative number, or the arccos of a number larger than 1. Performing such an operation will immediately terminate the program with one of the error messages shown above.

    If false, then these operations are permitted, but the result may lead to code that does not function correctly if you are not careful about how you use it. Using a value that is not a real number may result in freezing Kerbal Space Program itself if that value is used in a variable that is passed into Kerbal Space Program's API routines. KSP's own API interface does not seem to have any protective checks in place and will faithfully try to use whatever values its given.

.. highlight:: kerboscript

.. attribute:: Config:AUDIOERR

    :access: Get/Set
    :type: :struct:`Boolean`

    Configures the ``audibleExceptions`` setting.

    If true, then it enables a mode in which errors coming from kOS will
    generte a sound effect of a short little warning bleep to remind you that
    an exception occurred.  This can be useful when you are flying
    hands-off and need to realize your autopilot script just died so
    you can take over.

.. attribute:: Config:VERBOSE

    :access: Get/Set
    :type: :struct:`Boolean`

    Configures the ``verboseExceptions`` setting.

    If true, then it enables a mode in which errors coming from kOS are very long and verbose, trying to explain every detail of the problem.

.. attribute:: Config:TELNET

    :access: Get/Set
    :type: :struct:`Boolean`

    **GLOBAL SETTING**

    Configures the ``EnableTelnet`` setting.

    When set to true, it activates a
    `kOS telnet server in game <../../general/telnet.html>`__ that allows you to
    connect external terminal programs like Putty and Xterm to it.
    Turning the option off or on immediately toggles the server.  (When
    you change it from false to true, it will start the server right then.
    When you change it from true to false, it will stop the server right
    then.)  Therefore **to restart the server** after changing a setting like
    :attr:`TPORT`, DO this::

      // Restart telnet server:
      SET CONFIG:TELNET TO FALSE.
      WAIT 0.5. // important to give kOS a moment to notice and kill the old server.
      SET CONFIG:TELNET TO TRUE.

    Of course, you can do the equivalent of that by using the GUI config panel and just
    clicking the button off then clicking it on.

.. attribute:: Config:TPORT

    :access: Get/Set
    :type: :struct:`Scalar` (integer)

    **GLOBAL SETTING**

    Configures the ``TelnetPort`` setting.

    Changes the TCP/IP port number that the
    `kOS telnet server in game <../../general/telnet.html>`__
    will listen to.

    To make the change take effect you may have to
    stop, then restart the telnet server, as described above.

.. attribute:: Config:LOOPBACK

    :access: Get/Set
    :type: :struct:`Boolean`

    **GLOBAL SETTING**

    Configures the ``TelnetLoopback`` setting.

    If true, then it tells the
    `kOS telnet server in game <../../general/telnet.html>`__
    to refuse to use the computer's actual IP address, and
    instead use the loopback address (127.0.0.1).  This is
    the default mode the kOS mod ships in, in order to
    make it impossible get external access to your computer.

    To make the change take effect you may have to
    stop, then restart the telnet server, as described above.

.. attribute:: Config:BRIGHTNESS

    :access: Get/Set
    :type: :struct:`Scalar`. range = [0,1]

    Configures the ``Brightness`` setting.

    This is the default starting brightness setting a new
    kOS in-game terminal will have when it is invoked.  This
    is just the default for new terminals.  Individual terminals
    can have different settings, either by setting the value
    :attr:`Terminal:BRIGHTNESS` in a script, or by manually moving the
    brightness slider widget on that terminal.

    The value here must be between 0 (invisible) and 1 (Max brightness).

.. attribute:: Config:DEFAULTFONTSIZE

    :access: Get/Set
    :type: :struct:`Scalar` integer-only. range = [6,20]

    Configures the ``TerminalFontDefaultSize`` setting.

    This is the default starting font height (in pixels. not "points")
    for all newly created kOS in-game terminals.  This
    is just the default for new terminals.  Individual terminals
    can have different settings, either by setting the value
    :attr:`Terminal:CHARHEIGHT` in a script, or by manually clicking
    the font adjustment buttons on that terminal.

    The value here must be at least 6 (nearly impossible to read)
    and no greater than 30 (very big).  It will be rounded to the
    nearest integer when setting the value.

.. attribute:: Config:DEBUGEACHOPCODE

    :access: Get/Set
    :type: :struct:`Boolean`

    Configures the ``debugEachOpcode`` setting.

    NOTE: This makes the game VERY slow, use with caution.

    If true, each opcode that is executed by the CPU will be accompanied by
    an entry in the KSP log. This is a debugging tool for those who are very
    familiar with the inner workings of kOS and should rarely be used outside
    the kOS dev team.

    This change takes effect immediately.
