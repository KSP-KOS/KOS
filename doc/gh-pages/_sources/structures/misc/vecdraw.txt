.. _vecdraw:

Drawing Vectors on the Screen
=============================

    You can create an object that represents a vector drawn in the
    flight view, as a holographic image in flight.  You may move it
    to a new location, make it appear or disappear, change its color,
    and label.  This page describes how to do that.

.. function:: VECDRAW(start, vec, color, label, scale, show, width)
.. function:: VECDRAWARGS(start, vec, color, label, scale, show, width)

    Both these two function names do the same thing.  For historical
    reasons both names exist, but now they both do the same thing.
    They create a new ``vecdraw`` object that you can then manipulate
    to show things on the screen::

        SET anArrow TO VECDRAW(
              V(0,0,0),
              V(a,b,c),
              RGB(1,0,0),
              "See the arrow?",
              1.0,
              TRUE,
              0.2
            ).

        SET anArrow TO VECDRAWARGS(
              V(0,0,0),
              V(a,b,c),
              RGB(1,0,0),
              "See the arrow?",
              1.0,
              TRUE,
              0.2
            ).

    All the parameters of the ``VECDRAW()`` and ``VECDRAWARGS()`` are
    optional.  You can leave any of the lastmost parameters off and they
    will be given a default::

        Set anArrow TO VECDRAW().

    Causes it to have these defaults:

    .. list-table:: Defaults
            :header-rows: 1
            :widths: 1 3

            * - Suffix
              - Default

            * - :attr:`START`
              - V(0,0,0)  (center of the ship is the origin)
            * - :attr:`VEC`
              - V(0,0,0)  (no length, so nothing appears)
            * - :attr:`COLO[U]R`
              - White
            * - :attr:`LABEL`
              - Empty string ""
            * - :attr:`SCALE`
              - 1.0
            * - :attr:`SHOW`
              - false
            * - :attr:`WIDTH`
              - 0.2

    Examples::

        // Makes a red vecdraw at the origin, pointing 5 meters north,
        // with defaults for the un-mentioned
        // paramters LABEL, SCALE, SHOW, and WIDTH.
        SET vd TO VECDRAW(V(0,0,0), 5*north:vector, red).

    To make a :struct:`VecDraw` disappear, you can either set its :attr:`VecDraw:SHOW` to false or just UNSET the variable, or re-assign it. An example using :struct:`VecDraw` can be seen in the documentation for :func:`POSITIONAT()`.

.. _clearvecdraws:

.. function:: CLEARVECDRAWS()

    Sets all visible vecdraws to invisible, everywhere in this kOS CPU.
    This is useful if you have lost track of the handles to them and can't
    turn them off one by one, or if you don't have the variable scopes
    present anymore to access the variables that hold them.  The system
    does attempt to clear any vecdraws that go "out of scope", however
    the "closures" that keep local variables alive for LOCK statements
    and for other reasons can keep them from every truely going away
    in some circumstances.  To make the arrow drawings all go away, just call
    CLEARVECDRAWS() and it will have the same effect as if you had
    done ``SET varname:show to FALSE`` for all vecdraw varnames in the
    entire system.

.. structure:: VecDraw

    This is a structure that allows you to make a drawing of a vector on the screen in map view or in flight view.

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 4

        * - Suffix
          - Type
          - Description


        * - :attr:`START`
          - :struct:`Vector`
          - Start position of the vector
        * - :attr:`VEC`
          - :struct:`Vector`
          - The vector to draw
        * - :attr:`COLOR`
          - :ref:`Color <colors>`
          - Color of the vector
        * - :attr:`COLOUR`
          -
          - Same as :attr:`COLOR`
        * - :attr:`LABEL`
          - :ref:`string <string>`
          - Text to show next to vector
        * - :attr:`SCALE`
          - :ref:`scalar <scalar>`
          - Scale :attr:`VEC` and :attr:`WIDTH` but not :attr:`START`
        * - :attr:`SHOW`
          - :ref:`boolean <boolean>`
          - True to enable display to screen
        * - :attr:`WIDTH`
          - :ref:`scalar <scalar>`
          - width of vector, default is 0.2
        * - :attr:`STARTUPDATER`
          - :struct:`KosDelegate`
          - assigns a delegate to auto-update the START attribute.
        * - :attr:`VECUPDATER`
          - :struct:`KosDelegate`
          - assigns a delegate to auto-update the VEC attribute.
        * - :attr:`VECTORUPDATER`
          -
          - Same as :attr:`VECUPDATER`
        * - :attr:`COLORUPDATER`
          - :struct:`KosDelegate`
          - assigns a delegate to auto-update the COLOR attribute.
        * - :attr:`COLOURUPDATER`
          -
          - Same as :attr:`COLORUPDATER`




.. attribute:: VecDraw:START

    :access: Get/Set
    :type: :struct:`Vector`

    Optional, defaults to V(0,0,0) - position of the tail of the vector to draw in SHIP-RAW coords. V(0,0,0) means the ship Center of Mass.

.. attribute:: VecDraw:VEC

    :access: Get/Set
    :type: :struct:`Vector`

    Mandatory - The vector to draw, SHIP-RAW Coords.

.. attribute:: VecDraw:COLOR

    :access: Get/Set
    :type: :ref:`Color <color>`

    Optional, defaults to white. This is the color to draw the vector. There is a hard-coded fade effect where the tail is a bit more transparent than the head.

.. attribute:: VecDraw:COLOUR

    :access: Get/Set
    :type: :ref:`Color <color>`

    Alias for :attr:`VecDraw:COLOR`

.. attribute:: VecDraw:LABEL

    :access: Get/Set
    :type: :ref:`string <string>`

    Optional, defaults to "". Text to show on-screen at the midpoint of the vector.
    Note the font size the label is displayed in gets stretched when you
    change the :attr:`SCALE` or the :attr:`WIDTH` values.

.. attribute:: VecDraw:SCALE

    :access: Get/Set
    :type: :ref:`scalar <scalar>`

    Optional, defaults to 1.0. Scalar to multiply the VEC by, and the WIDTH,
    but not the START.

    .. versionchanged:: 0.19.0

        In previous versions, this also moved the start location, but most
        users found that useless and confusing and that has been
        changed as described above.

.. attribute:: VecDraw:SHOW

    :access: Get/Set
    :type: :ref:`boolean <boolean>`

    Set to true to make the arrow appear, false to hide it. Defaults to false until you're ready to set it to true.

.. attribute:: VecDraw:WIDTH

    :access: Get/Set
    :type: :ref:`scalar <scalar>`

    Define the width of the drawn line, in meters.  The deafult is 0.2 if
    left off.  Note, this also causes the font of the label to be enlarged
    to match if set to a value larger than 0.2.

    .. versionadded:: 0.19.0

        This parameter didn't exist before kOS 0.19.0.

.. attribute:: VecDraw:STARTUPDATER

    :access: Get/Set
    :type: :struct:`KosDelegate` with no parameters, returning a :struct:`Vector`

    This allows you to tell the VecDraw that you'd like it to update the START position
    of the vector regularly every update, according to your own scripted code.

    You create a :struct:`KosDelegate` that takes no parameters, and returns a vector,
    which the system will automatically assign to the :attr:`START` suffix every update.
    Be aware that this system does eat into the instructions available per update, so if
    you make this delegate do too much work, it will slow down your script's performance.

    To make the system stop calling your delegate, set this suffix to the magic
    keyword :global:`DONOTHING`.

    Example::

        // This example will bounce the arrow up and down over time for a few seconds,
        // moving the location of the vector's start according to a sine wave over time:
        set vd to vecdraw(v(0,0,0), ship:north:vector*5, green, "bouncing arrow", 1.0, true, 0.2).
        print "Moving the arrow up and down for a few seconds.".
        set vd:startupdater to { return ship:up:vector*3*sin(time:seconds*180). }.
        wait 5.
        print "Stopping the arrow movement.".
        set vd:startupdater to DONOTHING.
        wait 3.
        print "Removing the arrow.".
        set vd to 0.

    .. versionadded:: 1.1.0

        scripted Delegate callbacks such as this did not exist prior to kOS version 1.1.0

.. attribute:: VecDraw:VECUPDATER

    :access: Get/Set
    :type: :struct:`KosDelegate` with no parameters, returning a :struct:`Vector`

    This allows you to tell the VecDraw that you'd like it to update the ``VEC`` suffix
    of the vector regularly every update, according to your own scripted code.

    You create a :struct:`KosDelegate` that takes no parameters, and returns a vector,
    which the system will automatically assign to the :attr:`VEC` suffix every update.
    Be aware that this system does eat into the instructions available per update, so if
    you make this delegate do too much work, it will slow down your script's performance.

    To make the system stop calling your delegate, set this suffix to the magic
    keyword :global:`DONOTHING`.

    Example::

        // This example will spin the arrow around in a circle by leaving the start
        // where it is but moving the tip by trig functions:
        set vd to vecdraw(v(0,0,0), v(5,0,0), green, "spinning arrow", 1.0, true, 0.2).
        print "Moving the arrow in a circle for a few seconds.".
        set vd:vecupdater to {
           return ship:up:vector*5*sin(time:seconds*180) + ship:north:vector*5*cos(time:seconds*180). }.
        wait 5.
        print "Stopping the arrow movement.".
        set vd:vecupdater to DONOTHING.
        wait 3.
        print "Removing the arrow.".
        set vd to 0.


    .. versionadded:: 1.1.0

        scripted Delegate callbacks such as this did not exist prior to kOS version 1.1.0

.. attribute:: VecDraw:VECTORUPDATER

    This is just an alias for :attr:`VecDraw:VECUPDATER`.

.. attribute:: VecDraw:COLORUPDATER

    :access: Get/Set
    :type: :struct:`KosDelegate` with no parameters, returning a :struct:`Color`

    This allows you to tell the VecDraw that you'd like it to update the ``COLOR``/``COLOUR``
    suffix of the vector regularly every update, according to your own scripted code.

    You create a :struct:`KosDelegate` that takes no parameters, and returns a Color,
    which the system will automatically assign to the :attr:`COLOR` suffix every update.
    Be aware that this system does eat into the instructions available per update, so if
    you make this delegate do too much work, it will slow down your script's performance.

    To make the system stop calling your delegate, set this suffix to the magic
    keyword :global:`DONOTHING`.

    Example::

        // This example will change how opaque the arrow is over time by changing
        // the 'alpha' of its color:
        set vd to vecdraw(v(0,0,0), ship:north:vector*5, green, "fading arrow", 1.0, true, 0.2).
        print "Fading the arrow in and out for a few seconds.".
        set vd:colorupdater to { return RGBA(0,1,0,sin(time:seconds*180)). }.
        wait 5.
        print "Stopping the color change.".
        set vd:colorupdater to DONOTHING.
        wait 3.
        print "Removing the arrow.".
        set vd to 0.


    .. versionadded:: 1.1.0

        scripted Delegate callbacks such as this did not exist prior to kOS version 1.1.0

.. attribute:: VecDraw:COLOURUPDATER

    This is just an alias for :attr:`VecDraw:COLORUPDATER`.
