.. _engine:

Engine
======

Some of the Parts returned by :ref:`LIST PARTS <list command>` will be of type Engine. It is also possible to get just the Engine parts by executing ``LIST ENGINES``, for example::

    LIST ENGINES IN myVariable.
    FOR eng IN myVariable {
        print "An engine exists with ISP = " + eng:ISP.
    }.

.. structure:: Engine

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 2

        * - Suffix
          - Type (units)
          - Description

        * - All suffixes of :struct:`Part`
          -
          -
        * - :meth:`ACTIVATE`
          -
          - Turn engine on
        * - :meth:`SHUTDOWN`
          -
          - Turn engine off
        * - :attr:`THRUSTLIMIT`
          - scalar (%)
          - Tweaked thrust limit
        * - :attr:`MAXTHRUST`
          - scalar (kN)
          - Untweaked thrust limit
        * - :meth:`MAXTHRUSTAT(pressure)`
          - scalar (kN)
          - Max thrust at the specified pressure (in standard Kerbin atmospheres).
        * - :attr:`THRUST`
          - scalar (kN)
          - Current thrust
        * - :attr:`AVAILABLETHRUST`
          - scalar (kN)
          - Available thrust at full throttle accounting for thrust limiter
        * - :meth:`AVAILABLETHRUSTAT(pressure)`
          - scalar (kN)
          - Available thrust at the specified pressure (in standard Kerbin atmospheres).
        * - :attr:`FUELFLOW`
          - scalar (l/s maybe)
          - Rate of fuel burn
        * - :attr:`ISP`
          - scalar
          - `Specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_
        * - :meth:`ISPAT(pressure)`
          - scalar
          - `Specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_ at the given pressure (in standard Kerbin atmospheres).
        * - :attr:`VACUUMISP`
          - scalar
          - Vacuum `specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_
        * - :attr:`VISP`
          - scalar
          - Synonym for VACUUMISP
        * - :attr:`SEALEVELISP`
          - scalar
          - `Specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_ at Kerbin sealevel
        * - :attr:`SLISP`
          - scalar
          - Synonym for SEALEVELISP
        * - :attr:`FLAMEOUT`
          - boolean
          - Check if no more fuel
        * - :attr:`IGNITION`
          - boolean
          - Check if engine is active
        * - :attr:`ALLOWRESTART`
          - boolean
          - Check if engine can be reactivated
        * - :attr:`ALLOWSHUTDOWN`
          - boolean
          - Check if engine can be shutdown
        * - :attr:`THROTTLELOCK`
          - boolean
          - Check if throttle can not be changed


.. note::

    :struct:`Engine` is a type of :struct:`Part`, and therefore can use all the suffixes of :struct:`Part`. Shown below are only the suffixes that are unique to :struct:`Engine`.



.. method:: Engine:ACTIVATE

    Call to make the engine turn on.

.. method:: Engine:SHUTDOWN

    Call to make the engine turn off.

.. attribute:: Engine:THRUSTLIMIT

    :access: Get/Set
    :type: scalar (%)

    If this an engine with a thrust limiter (tweakable) enabled, what
    percentage is it limited to?  Note that this is expressed as a 
    percentage, not a simple 0..1 coefficient.  e.g. To set thrustlimit
    to half, you use a value of 50.0, not 0.5.

    This value is not allowed to go outside the range [0..100].  If you
    attempt to do so, it will be clamped down into the allowed range.

    Note that although a kerboscript is allowed to set the value to a
    very precise number (for example 10.5123), the stock in-game display
    widget that pops up when you right-click the engine will automatically
    round it to the nearest 0.5 whenever you open the panel.  So if you
    do something like ``set ship:part[20]:thrustlimit to 10.5123.`` in
    your script, then look at the rightclick menu for the engine, the very
    act of just looking at the menu will cause it to become 10.5 instead 
    of 10.5123.  There isn't much that kOS can to to change this.  It's a
    user interface decision baked into the stock game.

.. _engine_MAXTHRUST:

.. attribute:: Engine:MAXTHRUST

    :access: Get only
    :type: scalar (kN)

    How much thrust would this engine give at its current atmospheric pressure and velocity if the throttle was max at 1.0, and the thrust limiter was max at 100%.  Note this might not be the engine's actual max thrust it could have under other air pressure conditions.  Some engines have a very different value for MAXTHRUST in vacuum as opposed to at sea level pressure.  Also, some jet engines have a very different value for MAXTHRUST depending on how fast they are currently being rammed through the air.

.. _engine_MAXTHRUSTAT:

.. method:: Engine:MAXTHRUSTAT(pressure)

    :parameter pressure: atmospheric pressure (in standard Kerbin atmospheres)
    :type: scalar (kN)

    How much thrust would this engine give if both the throttle and thrust limtier was max at the current velocity, and at the given atmospheric pressure.  Use a pressure of 0.0 for vacuum, and 1.0 for sea level (on Kerbin) (or more than 1 for thicker atmospheres like on Eve).

.. attribute:: Engine:THRUST

    :access: Get only
    :type: scalar (kN)

    How much thrust is this engine giving at this very moment.

.. _engine_AVAILABLETHRUST:

.. attribute:: Engine:AVAILABLETHRUST

    :access: Get only
    :type: scalar (kN)

    Taking into account the thrust limiter tweakable setting, how much thrust would this engine give if the throttle was max at its current thrust limit setting and atmospheric pressure and velocity conditions.

.. _engine_AVAILABLETHRUSTAT:

.. method:: Engine:AVAILABLETHRUSTAT(pressure)

    :parameter pressure: atmospheric pressure (in standard Kerbin atmospheres)
    :type: scalar (kN)

    Taking into account the thrust limiter tweakable setting, how much thrust would this engine give if the throttle was max at its current thrust limit setting and velocity, but at a different atmospheric pressure you pass into it.  The pressure is measured in ATM's, meaning 0.0 is a vacuum, 1.0 is seal level at Kerbin.

.. attribute:: Engine:FUELFLOW

    :access: Get only
    :type: scalar (Liters/s? maybe)

    Rate at which fuel is being burned. Not sure what the units are.

.. attribute:: Engine:ISP

    :access: Get only
    :type: scalar

    `Specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_

.. method:: Engine:ISPAT(pressure)

    :parameter pressure: atmospheric pressure (in standard Kerbin atmospheres)
    :type: scalar

    `Specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_ at the given atmospheric pressure.  Use a pressure of 0 for vacuum, and 1 for sea level (on Kerbin).

.. attribute:: Engine:VACUUMISP

    :access: Get only
    :type: scalar

    Vacuum `specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_

.. attribute:: Engine:VISP

    :access: Get only
    :type: scalar

    Synonym for :VACUUMISP

.. attribute:: Engine:SEALEVELISP

    :access: Get only
    :type: scalar

    `Specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_ at Kerbin sealevel.

.. attribute:: Engine:SLISP

    :access: Get only
    :type: scalar

    Synonym for :SEALEVELISP

.. attribute:: Engine:FLAMEOUT

    :access: Get only
    :type: boolean

    Is this engine failed because it is starved of a resource (liquidfuel, oxidizer, oxygen)?

.. attribute:: Engine:IGNITION

    :access: Get only
    :type: boolean

    Has this engine been ignited? If both :attr:`Engine:IGNITION` and :attr:`Engine:FLAMEOUT` are true, that means the engine could start up again immediately if more resources were made available to it.

.. attribute:: Engine:ALLOWRESTART

    :access: Get only
    :type: boolean

    Is this an engine that can be started again? Usually True, but false for solid boosters.

.. attribute:: Engine:ALLOWSHUTDOWN

    :access: Get only
    :type: boolean

    Is this an engine that can be shut off once started? Usually True, but false for solid boosters.

.. attribute:: Engine:THROTTLELOCK

    :access: Get only
    :type: boolean

    Is this an engine that is stuck at a fixed throttle? (i.e. solid boosters)

.. _isp: http://en.wikipedia.org/wiki/Specific_impulse
