.. _colors:
.. _color:

Colors
======

Any place you need to specify a color in the game (at the moment this is
just with :ref:`VECDRAW <vecdraw>`, :ref:`HIGHLIGHT <highlight>`. and HUDTEXT) You do so with a
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

.. structure:: RGBA

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :R or :RED
          - :ref:`scalar <scalar>`
          - the red component of the color
        * - :G or :GREEN
          - :ref:`scalar <scalar>`
          - the green component of the color
        * - :B or :BLUE
          - :ref:`scalar <scalar>`
          - the blue component of the color
        * - :A or :ALPHA
          - :ref:`scalar <scalar>`
          - the alpha (how opaque: 1 = opaque, 0 = transparent) component of the color
        * - :HTML or :HEX
          - string
          - the color rendered into a HTML tag string i.e. "#ff0000".  This format ignores the alpha channel and treats all colors as opaque.

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

    PRINT GREEN:HTML. // prints #00ff00

.. _hsv:

.. function:: HSV(h,s,v)

    This global function creates a color from hue, saturation and value::

        SET myColor TO HSV(h,s,v).
                
        `More Information about HSV <http://en.wikipedia.org/wiki/HSL_and_HSV>`_,

    where:

    ``h``
        A floating point number from 0.0 to 1.0 for the hue component.
    ``s``
        A floating point number from 0.0 to 1.0 for the saturation component.
    ``v``
        A floating point number from 0.0 to 1.0 for the value component.

.. function:: HSVA(h,s,v,a)

    Same as :func:`HSV()` but with an alpha (transparency) channel::

        SET myColor TO HSVA(h,s,v,a).

    ``h, s, v`` are the same as above.

    ``a``
        A floating point number from 0.0 to 1.0 for the alpha component. (1.0 means opaque, 0.0 means invisibly transparent).

.. structure:: HSVA

    The HSVA structure contains all of the suffixes from the RGBA structure in addition to these

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :H or :HUE
          - :ref:`scalar <scalar>`
          - the hue component of the color. It is a value from 0.0 to 360.0
        * - :S or :SATURATION
          - :ref:`scalar <scalar>`
          - the saturation component of the color. It has a value from 0.0 to 1.0
        * - :V or :VALUE
          - :ref:`scalar <scalar>`
          - the value component of the color. It has a value from 0.0 to 1.0
                  

Examples::

    SET myarrow TO VECDRAW.
    SET myarrow:VEC to V(10,10,10).
    SET myarrow:COLOR to HSV(60,1,1). // Yellow
    SET myarrow:COLOR:S to 0.5. // Light yellow
    SET myarrow:COLOR:H to 0. // pink
