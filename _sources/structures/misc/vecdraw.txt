.. _vecdraw:

Drawing Vectors on the Screen
=============================

.. function:: VECDRAW()

    Build the suffix fields one at a time using the :struct:`VecDraw` empty construction function. This creates an empty :struct:`VecDraw` with nothing populated yet. You have to follow it up with calls to the suffixes as shown here::

        SET anArrow TO VECDRAW().
        SET anArrow:VEC TO V(a,b,c).
        SET anArrow:SHOW TO true.
        // At this point you have done the minimal necessary to make the arrow appear
        // and it shows up on the scren immediately.

        // Further options can also be set:
        SET anArrow:START TO V(0,0,0).
        SET anArrow:COLOR TO RGB(1,0,0).
        SET anArrow:LABEL TO "See the arrow?".
        SET anArrow:SCALE TO 5.0.

.. function:: VECDRAWARGS(start, vec, color, label, scale, show)

    This builds the :struct:`VecDraw` all at once with the :func:`VECDRAWARGS()` construction function. :func:`VECDRAWARGS()` lets you specify all of the attributes in a list of arguments at once::

        SET anArrow TO VECDRAWARGS(
            V(0,0,0),
            V(a,b,c),
            RGB(1,0,0),
            "See the arrow?",
            5.0,
            TRUE                  ).

The above two examples make the same thing. The arrow should be visible on both the map view and the in-flight view, but on the map view it will have to be a long arrow to be visible. :struct:`VecDraw`'s do not auto-update for changes in the vector like a LOCK would, but if you repeatedly SET the :VEC suffix in a loop, it will adjust the arrow picture to match as you do so::

    set xAxis to VECDRAWARGS( V(0,0,0), V(1,0,0), RGB(1.0,0.5,0.5), "X axis", 5, TRUE ).
    set yAxis to VECDRAWARGS( V(0,0,0), V(0,1,0), RGB(0.5,1.0,0.5), "Y axis", 5, TRUE ).
    set zAxis to VECDRAWARGS( V(0,0,0), V(0,0,1), RGB(0.5,0.5,1.0), "Z axis", 5, TRUE ).

To make a :struct:`VecDraw` disappear, you can either set its :attr:`VecDraw:SHOW` to false or just UNSET the variable, or re-assign it. An example using :struct:`VecDraw` can be seen in the documentation for :func:`POSITIONAT()`.




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
          - integer
          - Scale :attr:`START` and :attr:`VEC`
        * - :attr:`SHOW`
          - boolean
          - Draw vector to screen




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

.. attribute:: VecDraw:SCALE

    :access: Get/Set
    :type: integer

    Optional, defauls to 1. Scalar to multiply by both the START and the VEC

.. attribute:: VecDraw:SHOW

    :access: Get/Set
    :type: boolean

    Set to true to make the arrow appear, false to hide it. Defaults to false until you're ready to set it to true.


