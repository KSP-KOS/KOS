.. _terminal:

Terminal and game environment
=============================

.. global:: CLEARSCREEN


    Clears the screen and places the cursor at the top left::

        CLEARSCREEN.

.. global:: PRINT

    Prints the selected text to the screen. Can print strings, or the result of an expression::

        PRINT “Hello”.
        PRINT 4+1.
        PRINT “4 times 8 is: “ + (4*8).

.. function:: AT(col,line)

    :parameter col: (integer) column starting with zero (left)
    :parameter line: (integer) line starting with zero (top)

    Used in combination with :global:`PRINT`. Prints the selected text to the screen at specified location. Can print strings, or the result of an expression::

        PRINT “Hello” AT(0,10).
        PRINT 4+1 AT(0,10).
        PRINT “4 times 8 is: “ + (4*8) AT(0,10).

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

    Reboots the kOS module.

.. global:: SHUTDOWN

    Causes kOS module to shutdown.
