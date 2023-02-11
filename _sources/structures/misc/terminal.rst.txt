.. _terminal:

Terminal
========

The TERMINAL identifier refers to a special structure that lets you access
some of the information about the screen you are running on.

Structure
---------

.. structure:: Terminal

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 1 4

        * - Suffix
          - Type
          - Get/Set
          - Description

        * - :attr:`WIDTH`
          - :struct:`Scalar`
          - get and set
          - Terminal width in characters.

        * - :attr:`HEIGHT`
          - :struct:`Scalar`
          - get and set
          - Terminal height in characters.

        * - :attr:`REVERSE`
          - :struct:`Boolean`
          - get and set
          - Determines if the screen is displayed with foreground and background colors swapped.

        * - :attr:`VISUALBEEP`
          - :struct:`Boolean`
          - get and set
          - Turns beeps into silent visual screen flashes instead.

        * - :attr:`BRIGHTNESS`
          - :struct:`Scalar`
          - get and set
          - Adjusts brightness slider of the terminal between 0.0 (min) and 1.0 (max).

        * - :attr:`CHARWIDTH`
          - :struct:`Scalar`
          - get
          - Width of a character cell in pixels.  Get-only because the font chooses it based on CHARHEIGHT.

        * - :attr:`CHARHEIGHT`
          - :struct:`Scalar`
          - get and set
          - Height of a character cell in pixels.

        * - :attr:`INPUT`
          - :struct:`TerminalInput`
          - get
          - Used to read user's input into the terminal.

.. attribute:: Terminal:WIDTH

    :access: Get/Set
    :type: :struct:`Scalar`

    If you read the width it will return a number of character cells wide the terminal
    is.  If you set this value, it will cause the terminal to resize.
    If there's multiple terminals connected to the same CPU part via telnet clients,
    then kOS will attempt to keep them all the same size, and one terminal being resized
    will resize them all.  (caveat: Some terminal types cannot be resized from the
    server side, and therefore this doesn't always work in both directions).

    This setting is different per kOS CPU part.  Different terminal
    windows can have different settings for this value.

.. attribute:: Terminal:HEIGHT

    :access: Get/Set
    :type: :struct:`Scalar`

    If you read the height it will return a number of character cells tall the terminal
    is.  If you set this value, it will cause the terminal to resize.
    If there's multiple terminals connected to the same CPU part via telnet clients,
    then kOS will attempt to keep them all the same size, and one terminal being resized
    will resize them all.  (caveat: Some terminal types cannot be resized from the
    server side, and therefore this doesn't always work in both directions).

    This setting is different per kOS CPU part.  Different terminal
    windows can have different settings for this value.

.. attribute:: Terminal:REVERSE

    :access: Get/Set
    :type: :struct:`Boolean`

    If true, then the terminal window is currently set to show
    the whole screen in reversed color - swapping the background
    and foreground colors.   Both the telnet terminals and the in-game
    GUI terminal respond to this setting equally.

    Note, this setting can also be toggled with a radio-button on the
    in-game GUI terminal window.

    This setting is different per kOS CPU part.  Different terminal
    windows can have different settings for this value.

.. attribute:: Terminal:VISUALBEEP

    :access: Get/Set
    :type: :struct:`Boolean`

    If true, then the terminal window is currently set to show any
    BEEP characters by silently flashing the screen for a moment
    (inverting the background/foreground for a fraction of a second),
    instead of making a sound.

    Note, this setting can also be toggled with a radio-button on the
    in-game GUI terminal window.

    This will only typically affect the in-game GUI terminal window,
    and **not a telnet client's** terminal window.

    To affect the window you are using in a telnet session, you will
    have to use whatever your terminal or terminal emulator's local
    settings panel has for it.  Most do have some sort of visual
    beep setting, but it is usually not settable via a control character
    sequence sent across the connection.  The terminals are designed to
    assume it's a local user preference that isn't overridable
    by the software you are running.

    This setting is different per kOS CPU part.  Different terminal
    windows can have different settings for this value.

.. attribute:: Terminal:BRIGHTNESS

    :access: Get/Set
    :type: :struct:`Scalar`

    The same thing as the brightness slider on the terminal GUI.
    The values range from 0.0 (minimum) to 1.0 (maximum).  At
    zero, the effect is to entirely hide the letters altogether.

.. attribute:: Terminal:CHARWIDTH

    :access: Get
    :type: :struct:`Scalar`

    Width of a character cell in the display terminal, in pixels.

    Please note that this value is not settable anymore.  It
    can only be changed as a side-effect of changing the 
    :attr:`CHARHEIGHT`.  This is because the font is in
    charge of choosing the ratio between a letter's height and
    its width.  You can't force the font to render a letter
    at a different aspect ratio than it wants to.

.. attribute:: Terminal:CHARHEIGHT

    :access: Get/Set
    :type: :struct:`Scalar`

    Height of a character cell in the display terminal, in pixels.
    The value is forced to remain in the range [4..24] and be
    divisible by 2.  If you try to set it to any other value, it
    will snap to the allowed range and increment.

.. attribute:: Terminal:INPUT

    :access: Get
    :type: :struct:`TerminalInput`

    This gives you a :struct:`TerminalInput` structure, which can be
    used to read user's input into the kOS terminal.
