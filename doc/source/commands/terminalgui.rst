.. _terminalgui:

Terminal and game environment
=============================

.. global:: CLEARSCREEN


    Clears the screen and places the cursor at the top left::

        CLEARSCREEN.

.. global:: PRINT

    Prints the selected text to the screen. Can print strings, or the result of an expression::

        PRINT "Hello".
        PRINT 4+1.
        PRINT "4 times 8 is: " + (4*8).

    This is an alias for TERMINAL:PUTLN, see :ref:`terminal struct <terminal>`.

.. global:: SET TERMINAL:WIDTH. GET TERMINAL:WIDTH

     Gets or sets the terminal's width in characters.
     For more information see :ref:`terminal struct <terminal>`.

.. global:: SET TERMINAL:HEIGHT. GET TERMINAL:HEIGHT

     Gets or sets the terminal's height in characters.
     For more information see :ref:`terminal struct <terminal>`.

.. function:: AT(col,line)

    :parameter col: (integer) column starting with zero (left)
    :parameter line: (integer) line starting with zero (top)

    Used in combination with :global:`PRINT`. Prints the selected text to the screen at specified location. 
    This does **not** move the terminal cursor, as opposed to PRINT and most :ref:`terminal <terminal>` output methods.
    This is an alias for TERMINAL:PUTAT, see :ref:`terminal struct <terminal>`.
    Can print strings, or the result of an expression::

        PRINT "Hello" AT(0,10).
        PRINT 4+1 AT(0,10).
        PRINT "4 times 8 is: " + (4*8) AT(0,10).

.. global:: MAPVIEW

    :access: Get/Set
    :type: boolean

    A variable that controls or queries whether or not the game is in map view::

        IF MAPVIEW {
            PRINT "You are looking at the map.".
        } ELSE {
            PRINT "You are looking at the flight view.".
        }.

    You can switch between map and flight views by setting this variable::

        SET MAPVIEW TO TRUE.  // to map view
        SET MAPVIEW TO FALSE. // to flight view

.. global:: REBOOT

    Stops the script here, and reboots the kOS module.

.. global:: SHUTDOWN

    Stops the script here, and causes kOS module to turn the power off.

GUI display tools
------------------

.. global:: VECDRAW

    See VECDRAWARGS, below

.. global:: VECDRAWARGS

    You can **draw visual vectors on the screen** in kOS to help debugging
    or to help show the player information.  The full description can be
    found on the `Vecdraw Page <../structures/misc/vecdraw.html>`__.

    Note: Very large Vecdraws (for example if you try to draw a
    vector pointing from your ship to another planet) might not get
    drawn at all in the flight view, and only show up in the map
    view.  A longer explanation as to why is on the
    `Vecdraw Page <../structures/misc/vecdraw.html>`__.

.. global:: HUDTEXT

    You can make text messages appear on the heads-up display, in the
    same way that the in-game stock messages appear, by calling the
    HUDTEXT function, as follows::

        HUDTEXT( string Message, 
                 integer delaySeconds,
                 integer style,
                 integer size,
                 RGBA colour,
                 boolean doEcho).

    Message
      The message to show to the user on screen
    delaySeconds
      How long to make the message remain onscreen before it goes away.
      If another message is drawn while an old message is still displaying,
      both messages remain, the new message scrolls up the old message.
    style
      Where to show the message on the screen:
      - 1 = upper left
      - 2 = upper center
      - 3 = upper right
      - 4 = lower center
      Note that all these locations have their own defined slightly
      different fonts and default sizes, enforced by the stock KSP game.
    size
      A number describing the font point size: NOTE that the actual size
      varies depending on which of the above styles you're using.  Some
      of the locations have a magnifying factor attached to their fonts.
    colour
      The colour to show the text in, using `one of the built-in colour names
      or the RGB constructor to make one up <../structures/misc/colors.html>`__
    doEcho
      If true, then the message is also echoed to the terminal as "HUD: message".

    Examples::

      HUDTEXT("Warning: Vertical Speed too High", 5, 2, 15, red, false).
      HUDTEXT("docking mode begun", 8, 1, 12, rgb(1,1,0.5), false).

