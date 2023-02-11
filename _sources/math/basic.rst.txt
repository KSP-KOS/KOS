.. _basic math:

Basic Math Functions
====================

These functions are built-in for performing basic math operations in kOS.

.. _constants:
.. index:: Fundamental Constants

Fundamental Constants
---------------------

There is a bound variable called `CONSTANT` which contains some basic fundamental
constants about the universe that you may find handy in your math operations.  Prior to kOS version 0.18, `CONSTANT` was implemented as a function call, so values were accessed as `CONSTANT():PI` and the like.  This functionality has been retained for backward compatibility, but new code should instead call `CONSTANT:PI`.

.. list-table::
    :header-rows: 1
    :widths: 1 4

    * - Identifier
      - Description

    * - :global:`G`
      - Newton's Gravitational Constant.
    * - :global:`g0`
      - gravity acceleration (m/s^2) at sea level on Earth.
    * - :global:`E`
      - Base of the natural log (Euler's number)
    * - :global:`PI`
      - :math:`\pi`
    * - :global:`c`
      - Speed of light in a vacuum, in m/s.
    * - :global:`AtmToKPa`
      - Conversion constant: Atmospheres to kiloPascals.
    * - :global:`KPaToAtm`
      - Conversion constant: kiloPascals to Atmospheres.
    * - :global:`DegToRad`
      - Conversion constant: Degrees to Radians.
    * - :global:`RadToDeg`
      - Conversion constant: Radians to Degrees.
    * - :global:`Avogadro`
      - Avogadro's Constant
    * - :global:`Boltzmann`
      - Boltzmann's Constant
    * - :global:`IdealGas`
      - The Ideal Gas Constant


.. global:: Constant:G

    Newton's Gravitational Constant that the game's planetary
    bodies are implying in their configuration data.
    (6.67384E-11 as of the last update to these documents).

    Note, the stock KSP game never technically records a value
    for G in its data.  kOS derives this value by calculating it
    based on the Sun's Mass and its Gravitational Parameter.  It
    is possible for a mod (or perhaps a future release of KSP, if
    mistakes were made) to define a universe in which Newton's
    Gravitational Constant, G, isn't actually constant at all
    within that game universe, and instead varies from one sphere
    of influence to the next.  Such a universe would be breaking
    some laws of physics by a lot, but it is technically possible
    in the game's data model.  Due to this strange feature in
    the game's data model, it is probably safer to always have
    your scripts use the body's Mu in your formulas instead of
    explicitly doing mass*G to derive it.

    Do NOT confuse this with ``Constant:g0`` below.

    Example::

        PRINT "Gravitational parameter of Kerbin, calculated:".
        PRINT constant:G * Kerbin:Mass.
        PRINT "Gravitational parameter of Kerbin, hardcoded:".
        PRINT Kerbin:Mu.
        PRINT "The above two numbers had *better* agree.".
        PRINT "If they do not, then your solar system is badly configured.".

.. global:: Constant:g0

    Standard value the game uses for acceleration due to
    gravity at sea level on Earth.  (9.80655 m/s^2 as
    of the last update to these documents).

    Do NOT confuse this with ``Constant:G`` above.

    The place where this matters the most is in ISP
    calculations.  The rocket equation using ISP 
    contains an inherent conversion from mass to weight
    that basically means, "what would this mass of fuel
    have weighed at g0?".  Some kind of official standard
    value of g0 is needed to use ISP properly to predict
    how much fuel will be burned in a scenario.

    In pretty much any other calculation you do in your kOS
    scripts, other than when using ISP in the Rocketry Equation,
    you should probably not use g0 and instead calculate your
    local gravity more precisely based on your actual radius to
    the body center.  Not only because this is more accurate, but
    because the g0 you see here is NOT the g0 you would actually
    have on Kerbin's sea level.  It's the g0 on Earth, which is
    what the game's ISP numbers are using.  Kerbin's sea level
    g0 is ever so slightly different from Earth's g0 (but not
    by much.)

    ::

        PRINT "Gravitational parameter of Kerbin is:".
        PRINT constant:G * Kerbin:Mass.

.. global:: Constant:E

    Natural Log base "e"::

        PRINT "e^2 is:".
        PRINT constant:e ^ 2.

.. global:: Constant:PI

    Ratio of circumference of a circle to its diameter, 3.14159265...
    
    ::

        SET diameter to 10.
        PRINT "circumference is:".
        PRINT constant:pi * diameter.

.. global:: Constant:C

    Speed of light in a vacuum, in meters per second.
    
    ::

        SET speed to SHIP:VELOCITY:ORBIT:MAG.
        SET percentOfLight to (speed / constant:c) * 100.
        PRINT "We're going " + percentOfLight + "% of lightspeed!".

    .. note::
        In Kerbal Space Program, all physics motion is purely Newtonian.
        You can go faster than the speed of light provided you have enough
        delta-V, and no time dilation effects will occur.  The universe
        will behave entirely linearly even at speeds near *c*.

    This constant is provided mainly for the benefit of people who are
    playing with the mod "RemoteTech" installed, who may want to perform
    calculations about signal delays to hypothetical probes.  (Note that
    if the probe already has a connection, you can
    :ref:`ask Remotetech directly <remotetech>` what the signal delay is.

.. global:: Constant:AtmToKPa

    A conversion constant.

    If you have a pressure measurement expressed in atmospheres of pressure,
    you can multiply it by this to get the equivalent in kiloPascals
    (kiloNewtons per square meter).
    
    ::

        PRINT "1 atm is:".
        PRINT 1 * constant:AtmToKPa + " kPa.".

.. global:: Constant:KPaToATM

    A conversion constant.

    If you have a pressure measurement expressed in kiloPascals (kiloNewtons
    per square meter), you can multiply it by this to get the equivalent
    in atmospheres.

    ::

        PRINT "100 kPa is:".
        PRINT 100 * constant:KPaToATM + " atmospheres".

.. global:: Constant:DegToRad

    A conversion constant.

    If you have an angle measured in degrees, you can multiply it by
    this to get the equivalent measure in radians.  It is exactly
    the same thing as saying ``constant:pi / 180``, except the result is
    pre-recorded as a constant number and thus no division is performed
    at runtime.

    ::

        PRINT "A right angle is:".
        PRINT 90 * constant:DegToRad + " radians".

.. global:: Constant:RadToDeg

    A conversion constant.

    If you have an angle measured in radians, you can multiply it by
    this to get the equivalent measure in degrees.  It is exactly
    the same thing as saying ``180 / constant:pi``, except the result is
    pre-recorded as a constant number and thus no division is performed
    at runtime.

    ::

        PRINT "A radian is:".
        PRINT 1 * constant:RadToDeg + " degrees".

.. global:: Constant:Avogadro

    Avogadro's Constant.

    This value can be used in calculating atmospheric properties for drag purposes,
    which can be a rather advanced topic.
    `(Avogadro's constant Wikipedia Page) <https://en.wikipedia.org/wiki/Avogadro_constant>`_.

.. global:: Constant:Boltzmann

    Boltzmann Constant.

    This value can be used in calculating atmospheric properties for drag purposes,
    which can be a rather advanced topic.
    `(Boltzmann constant Wikipedia Page) <https://en.wikipedia.org/wiki/Boltzmann_constant>`_.

.. global:: Constant:IdealGas

    Ideal Gas Constant.

    This value can be used in calculating atmospheric properties for drag purposes,
    which can be a rather advanced topic.
    `(Ideal Gas Constant Wikipedia Page) <https://en.wikipedia.org/wiki/Gas_constant>`_.

.. _math functions:
.. index:: Mathematical Functions

Mathematical Functions
----------------------

==================== ===================================================
 Function            Description
==================== ===================================================
:func:`ABS(a)`       absolute value
:func:`CEILING(a)`   round up
:func:`CEILING(a,b)` round up to nearest place
:func:`FLOOR(a)`     round down
:func:`FLOOR(a,b)`   round down to nearest place
:func:`LN(a)`        natural log
:func:`LOG10(a)`     log base 10
:func:`MOD(a,b)`     modulus
:func:`MIN(a,b)`     return a or b, whichever is lesser.
:func:`MAX(a,b)`     return a or b, whichever is greater.
:func:`RANDOM()`     random fractional number between 0 and 1.
:func:`RANDOMSEED()` Start a new random sequence with a seed.
:func:`ROUND(a)`     round to whole number
:func:`ROUND(a,b)`   round to nearest place
:func:`SQRT(a)`      square root
:func:`CHAR(a)`      character from unicode
:func:`UNCHAR(a)`    unicode from character
==================== ===================================================

.. function:: ABS(a)

    Returns absolute value of input::

        PRINT ABS(-1). // prints 1

.. function:: CEILING(a)

    Rounds up to the nearest whole number::

        PRINT CEILING(1.887). // prints 2

.. function:: CEILING(a,b)

    Rounds up to the nearest place value::

        PRINT CEILING(1.887,2). // prints 1.89

.. function:: FLOOR(a)

    Rounds down to the nearest whole number::

        PRINT FLOOR(1.887). // prints 1

.. function:: FLOOR(a,b)

    Rounds down to the nearest place value::

        PRINT CEILING(1.887,2). // prints 1.88

.. function:: LN(a)

    Gives the natural log of the provided number::

        PRINT LN(2). // prints 0.6931471805599453

.. function:: LOG10(a)

    Gives the log base 10 of the provided number::

        PRINT LOG10(2). // prints 0.30102999566398114

.. function:: MOD(a,b)

    Returns remainder from integer division.
    Keep in mind that it's not a traditional mathematical Euclidean division where the result is always positive. The result has the same absolute value as mathematical modulo operation but the sign is the same as the sign of dividend::

        PRINT MOD(21,6). // prints 3
        PRINT MOD(-21,6). // prints -3

.. function:: MIN(a,b)

    Returns The lower of the two values::

        PRINT MIN(0,100). // prints 0

.. function:: MAX(a,b)

    Returns The higher of the two values::

        PRINT MAX(0,100). // prints 100

.. function:: RANDOM(key) // parameter 'key' is optional.

    Returns the next random floating point number from a random
    number sequence.  The result is always in the range [0..1]

    This uses what is called a `pseudo-random number generator
    <https://en.wikipedia.org/wiki/Pseudorandom_number_generator>`_.

    For basic usage you can leave the ``key`` parameter off and it
    works fine, like so:

    Example, basic usage::

        PRINT RANDOM(). //prints a random number
        PRINT "Let's roll a 6-sided die 10 times:".
        FOR n in range(0,10) {

          // To make RANDOM give you an integer in the range [0..n-1], you do this:
          // floor(n*RANDOM()).

          // So for example : a die giving values from 1 to 6 is like this:
          print (1 + floor(6*RANDOM())).
        }

    The parameter ``key`` is a string, and it's used when you want
    to track separate psuedo-random number sequences by name and 
    have them be deterministically repeatable. *Like other
    string keys in kOS, this key is case-insensitive.*

    * If you leave the parameter ``key`` off, you get the next number
      from a default unnamed random number sequencer.
    * If you supply the parameter ``key``, you get the next number
      from a named random number sequencer.  You can invent however
      many keys you like and each one is a new random number sequencer.
      Supplying a key probably only means something if you have
      previously used :func:`RANDOMSEED(key, seed)`.

    The following example is more complex and shows the repeatability
    of the "random" sequence using seeds.  For most simple uses you
    probably don't need to bother with this.  If words like "random
    number seed" are confusing, you can probably skip this part and 
    get by just fine with the basic usage shown above.  (Explaining
    how pseudorandom number generators work is a bit beyond this
    page - check the wikipedia link above to learn more.)

    Example, deterministic usage::

        // create two different random number sequencers, both starting
        // with seed 12345 so they should have the same exact values.
        RANDOMSEED("sequence1",12345).
        RANDOMSEED("sequence2",12345).

        PRINT "5 coin flips from SEQUENCE 1:".
        FOR n in range(0,5) {
          print choose "heads" if RANDOM("sequence1") < 0.5 else "tails".
        }

        PRINT "5 coin flips from SEQUENCE 2, which should be the same:".
        FOR n in range(0,5) {
          print choose "heads" if RANDOM("sequence2") < 0.5 else "tails".
        }

        PRINT "5 more coin flips from SEQUENCE 1:".
        FOR n in range(0,5) {
          print choose "heads" if RANDOM("sequence1") < 0.5 else "tails".
        }

        PRINT "5 more coin flips from SEQUENCE 2, which should be the same:".
        FOR n in range(0,5) {
          print choose "heads" if RANDOM("sequence2") < 0.5 else "tails".
        }


.. function:: RANDOMSEED(key, seed)

    No Return Value.

    Initializes a new random number sequence from a seed, giving it a
    key name you can use to refer to it in future calls to :func:`RANDOM(key)`

    Using this you can make psuedo-random number sequences that can be
    re-run using the same seed to get the same result a second time.

    Parameter ``key`` is a string - a name you can use to refer to this
    random series later.  Calls to ``RANDOMSEED`` that use different
    keys actually cause different new random number sequences to be
    created that are tracked separately from each other. *Like other
    string keys in kOS, this key is case-insensitive.*

    Parameter ``seed`` is an integer - an initial value to cause a
    deterministic series of random numbers to come out of the random
    function.

    Whenever you call ``RANDOMSEED(key, seed)``, it starts a new
    random number sequence using the integer seed you give it, and names
    that sequence with a string key you can use later to retrive
    values from that random number sequence.

    Example::

      RANDOMSEED("generator A",1000).
      RANDOMSEED("generator B",1000).
      PRINT "Generators A and B should emit identical ".
      PRINT "sequences because they both started at seed 1000.".
      PRINT "3 numbers from Generator A:".
      PRINT floor(RANDOM("generator A")*100).
      PRINT floor(RANDOM("generator A")*100).
      PRINT floor(RANDOM("generator A")*100).
      PRINT "3 numbers from Generator B - they should ".
      PRINT "be the same as above:".
      PRINT floor(RANDOM("generator B")*100).
      PRINT floor(RANDOM("generator B")*100).
      PRINT floor(RANDOM("generator B")*100).

      PRINT "Resetting generator A but not Generator B:".
      RANDOMSEED("generator A",1000).

      PRINT "3 more numbers from Generator A which got reset".
      PRINT "so they should match the first ones again:".
      PRINT floor(RANDOM("generator A")*100).
      PRINT floor(RANDOM("generator A")*100).
      PRINT floor(RANDOM("generator A")*100).
      PRINT "3 numbers from Generator B, which didn't get reset:".
      PRINT floor(RANDOM("generator B")*100).
      PRINT floor(RANDOM("generator B")*100).
      PRINT floor(RANDOM("generator B")*100).

    
    If you call ``RANDOMSEED`` using the same key as a key you already used
    before, it just forgets the previous random number sequence and starts
    a new one using the new seed.  You can use this to reset the sequence.

.. function:: ROUND(a)

    Rounds to the nearest whole number::

        PRINT ROUND(1.887). // prints 2

.. function:: ROUND(a,b)

    Rounds to the nearest place value::

        PRINT ROUND(1.887,2). // prints 1.89

.. function:: SQRT(a)

    Returns square root::

        PRINT SQRT(7.89). // prints 2.80891438103763

.. function:: CHAR(a)

    :parameter a: (number)
    :return: (string) single-character string containing the unicode character specified

    ::

        PRINT CHAR(34) + "Apples" + CHAR(34). // prints "Apples"

.. function:: UNCHAR(a)

    :parameter a: (string)
    :return: (number) unicode number representing the character specified

    ::

        PRINT UNCHAR("A"). // prints 65

.. _trig:
.. index:: Trigonometric Functions

Trigonometric Functions
~~~~~~~~~~~~~~~~~~~~~~~

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
    * - :func:`ARCTAN2(y,x)`

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

    :parameter x: (:struct:`Scalar`)
    :return: (deg) angle whose sine is x

    ::

        PRINT ARCSIN(0.67). // prints 42.0670648

.. function:: ARCCOS(x)

    :parameter x: (:struct:`Scalar`)
    :return: (deg) angle whose cosine is x

    ::

        PRINT ARCCOS(0.67). // prints 47.9329352

.. function:: ARCTAN(x)

    :parameter x: (:struct:`Scalar`)
    :return: (deg) angle whose tangent is x

    ::

        PRINT ARCTAN(0.67). // prints 33.8220852

.. function:: ARCTAN2(y,x)

    :parameter y: (:struct:`Scalar`)
    :parameter x: (:struct:`Scalar`)
    :return: (deg) angle whose tangent is :math:`\frac{y}{x}`

    ::

        PRINT ARCTAN2(0.67, 0.89). // prints 36.9727625

    The two parameters resolve ambiguities when taking the arctangent. See the `wikipedia page about atan2 <http://en.wikipedia.org/wiki/Atan2>`_ for more details.
