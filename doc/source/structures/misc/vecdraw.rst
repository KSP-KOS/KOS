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
          - string
          - Text to show next to vector
        * - :attr:`SCALE`
          - scalar number
          - Scale :attr:`VEC` and :attr:`WIDTH` but not :attr:`START`
        * - :attr:`SHOW`
          - boolean
          - True to enable display to screen
        * - :attr:`WIDTH`
          - scalar number
          - width of vector, default is 0.2




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
    :type: string

    Optional, defaults to "". Text to show on-screen at the midpoint of the vector.
    Note the font size the label is displayed in gets stretched when you
    change the :attr:`SCALE` or the :attr:`WIDTH` values.

.. attribute:: VecDraw:SCALE

    :access: Get/Set
    :type: Scalar number

    Optional, defaults to 1.0. Scalar to multiply the VEC by, and the WIDTH,
    but not the START.

    .. versionchanged:: 0.19.0

        In previous versions, this also moved the start location, but most
        users found that useless and confusing and that has been
        changed as described above.

.. attribute:: VecDraw:SHOW

    :access: Get/Set
    :type: boolean

    Set to true to make the arrow appear, false to hide it. Defaults to false until you're ready to set it to true.

.. attribute:: VecDraw:WIDTH

    :access: Get/Set
    :type: Scalar number

    Define the width of the drawn line, in meters.  The deafult is 0.2 if
    left off.  Note, this also causes the font of the label to be enlarged
    to match if set to a value larger than 0.2.

    .. versionadded:: 0.19.0

        This parameter didn't exist before kOS 0.19.0.
