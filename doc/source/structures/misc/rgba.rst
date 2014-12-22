.. _colors:

Colors
======

Any place you need to specify a color in the game (at the moment this is
just with :ref:`VECDRAW <vecdraw>`.) You do so with a
rgba color structure defined as follows:

Method 1: Use one of these pre-arranged named colors:

- .. global:: RED
- .. global:: GREEN
- .. global:: BLUE
- .. global:: YELLOW
- .. global:: CYAN
- .. global:: MAGENTA

    - .. global:: PURPLE

        (Alias of :global:`MAGENTA`)

- .. global:: WHITE
- .. global:: BLACK


.. function:: RGB(r,g,b)

    This global function creates a color from red green and blue values::

        SET myColor TO RGB(r,g,b).

    where:

    ``r``
        A floating point number from 0.0 to 1.0 for the red component.
    ``g``
        A floating point number from 0.0 to 1.0 for the green component.
    ``b``
        A floating point number from 0.0 to 1.0 for the blue component.

.. function:: RGBA(r,g,b,a)

    Same as :func:`RGB()` but with an alpha (transparency) channel::

        SET myColor TO RGBA(r,g,b,a).

    ``r, g, b`` are the same as above.

    ``a``
        A floating point number from 0.0 to 1.0 for the alpha component. (1.0 means opaque, 0.0 means invisibly transparent).

Examples::

    SET myarrow TO VECDRAW.
    SET myarrow:VEC to V(10,10,10).
    SET myarrow:COLOR to YELLOW.
    SET mycolor TO YELLOW.
    SET myarrow:COLOR to mycolor.
    SET myarrow:COLOR to RGB(1.0,1.0,0.0).

    // COLOUR spelling works too
    SET myarrow:COLOUR to RGB(1.0,1.0,0.0).

    // half transparent yellow.
    SET myarrow:COLOR to RGBA(1.0,1.0,0.0,0.5).

