.. _basic math:

.. _constants:
.. index:: Fundamental Constants

Fundamental Constants
=====================

A few fundamental constants can be obtained by using the expression ``CONSTANT()`` followed by a colon (``:``) followed by one of the following:

.. list-table::
    :header-rows: 1
    :widths: 1 4

    * - Identifier
      - Description

    * - :global:`G`
      - Newton's Gravitational Constant
    * - :global:`E`
      - Natural Log
    * - :global:`PI`
      - :math:`π`


.. global:: Constant():G

    Newton's Gravitational Constant, 6.67384E-11::

        PRINT "Gravitational parameter of Kerbin is:".
        PRINT constant():G * Kerbin:Mass.

.. global:: Constant():E

    Natural Log base "e"::

        PRINT "e^2 is:".
        PRINT constant():e ^ 2.

.. global:: Constant():PI

    Ratio of circumference of a circle to its diameter

.. _math functions:
.. index:: Mathematical Functions

Mathematical Functions
======================

==================== ===================================================
 Function             Description
==================== ===================================================
 :func:`ABS(a)`       absolute value
 :func:`CEILING(a)`   round up
 :func:`FLOOR(a)`     round down
 :func:`LN(a)`        natural log
 :func:`LOG10(a)`     log base 10
 :func:`MOD(a,b)`     modulus
 :func:`MIN(a,b)`     minimum
 :func:`MAX(a,b)`     maximum
 :func:`RANDOM()`     random number
 :func:`ROUND(a)`     round to whole number
 :func:`ROUND(a,b)`   round to nearest place
 :func:`SQRT(a)`      square root
==================== ===================================================

.. function:: ABS(a)

    Returns absolute value of input::

        PRINT ABS(-1). // prints 1

.. function:: CEILING(a)

    Rounds up to the nearest whole number::

        PRINT CEILING(1.887). // prints 2

.. function:: FLOOR(a)

    Rounds down to the nearest whole number::

        PRINT FLOOR(1.887). // prints 1

.. function:: LN(a)

    Gives the natural log of the provided number::

        PRINT LN(2). // prints 0.6931471805599453

.. function:: LOG10(a)

    Gives the log base 10 of the provided number::

        PRINT LOG10(2). // prints 0.30102999566398114

.. function:: MOD(a,b)

    Returns remainder from integer division::

        PRINT MOD(21,6). // prints 3

.. function:: MIN(a,b)

    Returns The lower of the two values::

        PRINT MIN(0,100). // prints 0

.. function:: MAX(a,b)

    Returns The higher of the two values::

        PRINT MAX(0,100). // prints 100

.. function:: RANDOM()
    
    Returns a random floating point number in the range [0,1]::
    
        PRINT RANDOM(). //prints a random number
        
.. function:: ROUND(a)

    Rounds to the nearest whole number::

        PRINT ROUND(1.887). // prints 2

.. function:: ROUND(a,b)

    Rounds to the nearest place value::

        PRINT ROUND(1.887,2). // prints 1.89

.. function:: SQRT(a)

    Returns square root::

        PRINT SQRT(7.89). // prints 2.80891438103763

.. _trig:
.. index:: Trigonometric Functions

Trigonometric Functions
-----------------------

.. list-table::
    :header-rows: 1
    :widths: 1

    * - Function
    * - :func:`SIN(a)`
    * - :func:`COS(a)`
    * - :func:`TAN(a)`
    * - :func:`ARCSIN(x)`
    * - :func:`ARCCOS(x)`
    * - :func:`ARCTAN(x)`
    * - :func:`ARCTAN2(x,y)`

.. function:: SIN(a)

    :parameter a: (deg) angle
    :return: sine of the angle

    ::

        PRINT SIN(6). // prints 0.10452846326

.. function:: COS(a)

    :parameter a: (deg) angle
    :return: cosine of the angle

    ::

        PRINT COS(6). // prints 0.99452189536

.. function:: TAN(a)

    :parameter a: (deg) angle
    :return: tangent of the angle

    ::

        PRINT TAN(6). // prints 0.10510423526

.. function:: ARCSIN(x)

    :parameter x: (scalar)
    :return: (deg) angle whose sine is x

    ::

        PRINT ARCSIN(0.67). // prints 42.0670648

.. function:: ARCCOS(x)

    :parameter x: (scalar)
    :return: (deg) angle whose cosine is x

    ::

        PRINT ARCCOS(0.67). // prints 47.9329352

.. function:: ARCTAN(x)

    :parameter x: (scalar)
    :return: (deg) angle whose tangent is x

    ::

        PRINT ARCTAN(0.67). // prints 33.8220852

.. function:: ARCTAN2(y,x)

    :parameter y: (scalar)
    :parameter x: (scalar)
    :return: (deg) angle whose tangent is :math:`\frac{y}{x}`

    ::

        PRINT ARCTAN2(0.67, 0.89). // prints 36.9727625

    The two parameters resolve ambiguities when taking the arctangent. See the `wikipedia page about atan2 <http://en.wikipedia.org/wiki/Atan2>`_ for more details.

