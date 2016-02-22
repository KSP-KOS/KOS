.. config:

Configuration of kOS
====================


.. structure:: Config

    :struct:`Config` is a special structure that allows your kerboscript programs to set or get the values stored in the kOS plugin's config file.

    The options here can also be set by using the user interface panel shown here. This control panel is part of the :ref:`App Control Panel <applauncher>`

    In either case, whether the setting is changed via the GUI panel, or via script code, these are settings that **affect the kOS mod in all saved games** as soon as the change is made. It's identical to editing the config file in the kOS installation directory, and in fact will actually change that file the next time the game saves its state.

    .. list-table:: Members (all Gettable and Settable)
        :header-rows: 1
        :widths: 2 1 1 4

        * - Suffix
          - Type
          - Default
          - Description

        * - :attr:`IPU`
          - :ref:`scalar <scalar>` (integer)
          - 150
          - Instructions per update
        * - :attr:`UCP`
          - :ref:`boolean <boolean>`
          - False
          - Use compressed persistence
        * - :attr:`STAT`
          - :ref:`boolean <boolean>`
          - False
          - Print statistics to screen
        * - :attr:`RT2`
          - :ref:`boolean <boolean>`
          - False
          - Enable RemoteTech2 integration
        * - :attr:`ARCH`
          - :ref:`boolean <boolean>`
          - False
          - Start on archive (instead of volume 1)
        * - :attr:`SAFE`
          - :ref:`boolean <boolean>`
          - False
          - Enable safe mode
        * - :attr:`AUDIOERR`
          - :ref:`boolean <boolean>`
          - False
          - Enable sound effect on kOS error
        * - :attr:`VERBOSE`
          - :ref:`boolean <boolean>`
          - False
          - Enable verbose exceptions
        * - :attr:`TELNET`
          - :ref:`boolean <boolean>`
          - False
          - activate the telnet server
        * - :attr:`TPORT`
          - :ref:`scalar <scalar>` (integer)
          - 5410
          - set the port the telnet server will run on
        * - :attr:`LOOPBACK`
          - :ref:`boolean <boolean>`
          - True
          - Force the telnet server to use loopback (127.0.0.1) address
        * - :attr:`DEBUGEACHOPCODE`
          - :ref:`boolean <boolean>`
          - false
          - Unholy debug spam used by the kOS developers

.. attribute:: Config:IPU

    :access: Get/Set
    :type: :ref:`scalar <scalar>` integer. range = [50,2000]

    Configures the ``InstructionsPerUpdate`` setting.

    This is the number of kRISC psuedo-machine-langauge instructions that each kOS CPU will attempt to execute from the main program per :ref:`physics update tick <cpu hardware>`.

    This value is constrained to stay within the range [50..2000]. If you set it to a value outside that range, it will reset itself to remain in that range.

.. attribute:: Config:UCP

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

    Configures the ``UseCompressedPersistence`` setting.

    If true, then the contents of the kOS local volume 'files' stored inside the campaign save's persistence file will be stored using a compression algorithm that has the advantage of making them take less space, but at the cost of making the data impossible to decipher with the naked human eye when looking at the persistence file.

.. attribute:: Config:STAT

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

    Configures the ``ShowStatistics`` setting.

    If true, then executing a program will log numbers to the screen showing execution speed statistics.

.. attribute:: Config:RT2

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

    Configures the ``EnableRT2Integration`` setting.

    If true, then the kOS mod will attempt to interact with the Remote Tech 2 mod, letting RT2 make decisions about whether or not a vessel is within communications range rather than having kOS use its own more primitive algorithm for it.

    Due to a long stall in the development of the RT2 mod, this setting should still be considered experimental at this point.


.. attribute:: Config:ARCH

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

    Configures the ``StartOnArchive`` setting.

    If true, then when a vessel is first loaded onto the launchpad or runway, the initial default volume will be set to volume 0, the archive, instead of volume 1, the local drive.

.. attribute:: Config:SAFE

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

    Configures the ``EnableSafeMode`` setting.

    If true, then it enables the following error messages::

        Tried to push NaN into the stack.
        Tried to push Infinity into the stack.

    They will be triggered any time any mathematical operation would result in something that is not a real number, such as dividing by zero, or trying to take the square root of a negative number, or the arccos of a number larger than 1. Performing such an operation will immediately terminate the program with one of the error messages shown above.

    If false, then these operations are permitted, but the result may lead to code that does not function correctly if you are not careful about how you use it. Using a value that is not a real number may result in freezing Kerbal Space Program itself if that value is used in a variable that is passed into Kerbal Space Program's API routines. KSP's own API interface does not seem to have any protective checks in place and will faithfully try to use whatever values its given.

.. attribute:: Config:AUDIOERR

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

    Configures the ``AudibleExceptions`` setting.

    If true, then it enables a mode in which errors coming from kOS will
    generte a sound effect of a short little warning bleep to remind you that
    an exception occurred.  This can be useful when you are flying
    hands-off and need to realize your autopilot script just died so
    you can take over.

.. attribute:: Config:VERBOSE

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

    Configures the ``VerboseExceptions`` setting.

    If true, then it enables a mode in which errors coming from kOS are very long and verbose, trying to explain every detail of the problem.

.. attribute:: Config:TELNET

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

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
    :type: :ref:`scalar <scalar>` (integer)

    Configures the ``TelnetPort`` setting.

    Changes the TCP/IP port number that the
    `kOS telnet server in game <../../general/telnet.html>`__ 
    will listen to.

    To make the change take effect you may have to
    stop, then restart the telnet server, as described above.

.. attribute:: Config:LOOPBACK

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

    Configures the ``TelnetLoopback`` setting.

    If true, then it tells the 
    `kOS telnet server in game <../../general/telnet.html>`__ 
    to refuse to use the computer's actual IP address, and 
    instead use the loopback address (127.0.0.1).  This is
    the default mode the kOS mod ships in, in order to
    make it impossible get external access to your computer.

    To make the change take effect you may have to
    stop, then restart the telnet server, as described above.

.. attribute:: Config:DEBUGEACHOPCODE

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

    Configures the ``DebugEachOpcode`` setting.

    NOTE: This makes the game VERY slow, use with caution.

    If true, each opcode that is executed by the CPU will be accompanied by 
    an entry in the KSP log. This is a debugging tool for those who are very 
    familiar with the inner workings of kOS and should rarely be used outside
    the kOS dev team.

    This change takes effect immediately.

