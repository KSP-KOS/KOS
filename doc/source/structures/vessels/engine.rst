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
          - :ref:`scalar <scalar>` (%)
          - Tweaked thrust limit
        * - :attr:`MAXTHRUST`
          - :ref:`scalar <scalar>` (kN)
          - Untweaked thrust limit. Zero if engine disabled.
        * - :meth:`MAXTHRUSTAT(pressure)`
          - :ref:`scalar <scalar>` (kN)
          - Max thrust at the specified pressure (in standard Kerbin atmospheres). Zero if engine disabled.
        * - :attr:`THRUST`
          - :ref:`scalar <scalar>` (kN)
          - Current thrust. Zero if engine disabled.
        * - :attr:`AVAILABLETHRUST`
          - :ref:`scalar <scalar>` (kN)
          - Available thrust at full throttle accounting for thrust limiter. Zero if engine disabled.
        * - :meth:`AVAILABLETHRUSTAT(pressure)`
          - :ref:`scalar <scalar>` (kN)
          - Available thrust at the specified pressure (in standard Kerbin atmospheres). Zero if engine disabled.
        * - :attr:`POSSIBLETHRUST`
          - :ref:`scalar <scalar>` (kN)
          - Possible thrust at full throttle accounting for thrust limiter, when the engine is enabled.
        * - :meth:`POSSIBLETHRUSTAT(pressure)`
          - :ref:`scalar <scalar>` (kN)
          - Possible thrust at the specified pressure (in standard Kerbin atmospheres), when the engine is enabled.
        * - :attr:`FUELFLOW`
          - :ref:`scalar <scalar>` (unit/s)
          - Current volumetric flow rate of fuel.
        * - :attr:`MAXFUELFLOW`
          - :ref:`scalar <scalar>` (unit/s)
          - Untweaked maximum volumetric flow rate of fuel at full throttle.
        * - :attr:`MASSFLOW`
          - :ref:`scalar <scalar>` (Mg/s)
          - Current mass flow rate of fuel.
        * - :attr:`MAXMASSFLOW`
          - :ref:`scalar <scalar>` (Mg/s)
          - Untweaked maximum mass flow rate of fuel at full throttle.
        * - :attr:`ISP`
          - :ref:`scalar <scalar>`
          - `Specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_
        * - :meth:`ISPAT(pressure)`
          - :ref:`scalar <scalar>`
          - `Specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_ at the given pressure (in standard Kerbin atmospheres).
        * - :attr:`VACUUMISP`
          - :ref:`scalar <scalar>`
          - Vacuum `specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_
        * - :attr:`VISP`
          - :ref:`scalar <scalar>`
          - Synonym for VACUUMISP
        * - :attr:`SEALEVELISP`
          - :ref:`scalar <scalar>`
          - `Specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_ at Kerbin sealevel
        * - :attr:`SLISP`
          - :ref:`scalar <scalar>`
          - Synonym for SEALEVELISP
        * - :attr:`FLAMEOUT`
          - :ref:`Boolean <boolean>`
          - Check if no more fuel
        * - :attr:`IGNITION`
          - :ref:`Boolean <boolean>`
          - Check if engine is active
        * - :attr:`ALLOWRESTART`
          - :ref:`Boolean <boolean>`
          - Check if engine can be reactivated
        * - :attr:`ALLOWSHUTDOWN`
          - :ref:`Boolean <boolean>`
          - Check if engine can be shutdown
        * - :attr:`THROTTLELOCK`
          - :ref:`Boolean <boolean>`
          - Check if throttle can not be changed
        * - :attr:`MULTIMODE`
          - :ref:`Boolean <boolean>`
          - Check if engine has multiple modes
        * - :attr:`MODES`
          - :struct:`List`
          - List (string) of the engine modes
        * - :attr:`MODE`
          - :ref:`string <string>`
          - Name of the current mode (only if multiple)
        * - :attr:`TOGGLEMODE`
          - 
          - Switch to another mode (only if multiple)
        * - :attr:`PRIMARYMODE`
          - :ref:`Boolean <boolean>`
          - Is the engine in primary mode? (only if multiple)
        * - :attr:`AUTOSWITCH`
          - :ref:`Boolean <boolean>`
          - Can the engine switch modes automatically? (only if multiple)
        * - :attr:`HASGIMBAL`
          - :ref:`Boolean <boolean>`
          - Check if engine has gimbal
        * - :attr:`GIMBAL`
          - :struct:`Gimbal`
          - Gimbal of this engine (only if available)
        * - :attr:`ULLAGE`
          - :ref:`Boolean <boolean>`
          - Does this engine need ullage (for RealFuels)
        * - :attr:`FUELSTABILITY`
          - :ref:`scalar <scalar>`
          - How stable is the fuel for this engine (for RealFuels)
        * - :attr:`PRESSUREFED`
          - :ref:`Boolean <boolean>`
          - Is this engine pressure fed? (for RealFuels)
        * - :attr:`IGNITIONS`
          - :ref:`scalar <scalar>`
          - Number of ignitions remaining for this engine (for RealFuels)
        * - :attr:`MINTHROTTLE`
          - :ref:`scalar <scalar>`
          - The minimum throttle setting for this engine (for RealFuels)
        * - :attr:`CONFIG`
          - :struct:`String`
          - Engine configuration name (for RealFuels)
        * - :attr:`CONSUMEDRESOURCES`
          - :struct:`Lexicon` of :struct:`ConsumedResource`
          - The fuel resources this engine consumes, and in what ratios


.. note::

    :struct:`Engine` is a type of :struct:`Part`, and therefore can use all the suffixes of :struct:`Part`. Shown below are only the suffixes that are unique to :struct:`Engine`.



.. method:: Engine:ACTIVATE

    Call to make the engine turn on.

.. method:: Engine:SHUTDOWN

    Call to make the engine turn off.

.. attribute:: Engine:THRUSTLIMIT

    :access: Get/Set
    :type: :ref:`scalar <scalar>` (%)

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
    of 10.5123.  There isn't much that kOS can do to change this.  It's a
    user interface decision baked into the stock game.

.. _engine_MAXTHRUST:

.. attribute:: Engine:MAXTHRUST

    :access: Get only
    :type: :ref:`scalar <scalar>` (kN)

    How much thrust would this engine give at its current atmospheric pressure and velocity if the throttle was max at 1.0, and the thrust limiter was max at 100%.  Note this might not be the engine's actual max thrust it could have under other air pressure conditions.  Some engines have a very different value for MAXTHRUST in vacuum as opposed to at sea level pressure.  Also, some jet engines have a very different value for MAXTHRUST depending on how fast they are currently being rammed through the air. Also note that this will read zero if the engine is currently disabled.

.. _engine_MAXTHRUSTAT:

.. method:: Engine:MAXTHRUSTAT(pressure)

    :parameter pressure: atmospheric pressure (in standard Kerbin atmospheres)
    :type: :ref:`scalar <scalar>` (kN)

    How much thrust would this engine give if both the throttle and thrust limtier was max at the current velocity, and at the given atmospheric pressure.  Use a pressure of 0.0 for vacuum, and 1.0 for sea level (on Kerbin) (or more than 1 for thicker atmospheres like on Eve). Note that this will read zero if the engine is currently disabled.
    (Pressure must be greater than or equal to zero.  If you pass in a
    negative value, it will be treated as if you had given a zero instead.)

.. _engine_THRUST:

.. attribute:: Engine:THRUST

    :access: Get only
    :type: :ref:`scalar <scalar>` (kN)

    How much thrust is this engine giving at this very moment.

.. _engine_AVAILABLETHRUST:

.. attribute:: Engine:AVAILABLETHRUST

    :access: Get only
    :type: :ref:`scalar <scalar>` (kN)

    Taking into account the thrust limiter tweakable setting, how much thrust would this engine give if the throttle was max at its current thrust limit setting and atmospheric pressure and velocity conditions. Note that this will read zero if the engine is currently disabled.

.. _engine_AVAILABLETHRUSTAT:

.. method:: Engine:AVAILABLETHRUSTAT(pressure)

    :parameter pressure: atmospheric pressure (in standard Kerbin atmospheres)
    :type: :ref:`scalar <scalar>` (kN)

    Taking into account the thrust limiter tweakable setting, how much thrust would this engine give if the throttle was max at its current thrust limit setting and velocity, but at a different atmospheric pressure you pass into it.  The pressure is measured in ATM's, meaning 0.0 is a vacuum, 1.0 is sea level at Kerbin.  Note that this will read zero if the engine is currently disabled.
    (Pressure must be greater than or equal to zero.  If you pass in a
    negative value, it will be treated as if you had given a zero instead.)

.. _engine_POSSIBLETHRUST:

.. attribute:: Engine:POSSIBLETHRUST

    :access: Get only
    :type: :ref:`scalar <scalar>` (kN)

    Taking into account the thrust limiter tweakable setting, how much thrust would this engine give if the throttle was max at its current thrust limit setting and atmospheric pressure and velocity conditions. This will give the correct value even if the engine is currently disabled.

.. _engine_POSSIBLETHRUSTAT:

.. method:: Engine:POSSIBLETHRUSTAT(pressure)

    :parameter pressure: atmospheric pressure (in standard Kerbin atmospheres)
    :type: :ref:`scalar <scalar>` (kN)

    Taking into account the thrust limiter tweakable setting, how much thrust would this engine give if the throttle was max at its current thrust limit setting and velocity, but at a different atmospheric pressure you pass into it.  The pressure is measured in ATM's, meaning 0.0 is a vacuum, 1.0 is sea level at Kerbin.  This will give the correct value even if the engine is currently disabled.
    (Pressure must be greater than or equal to zero.  If you pass in a
    negative value, it will be treated as if you had given a zero instead.)

.. attribute:: Engine:FUELFLOW

    :access: Get only
    :type: :ref:`scalar <scalar>` (units/s)

    How much fuel volume is this engine consuming at this very moment.

.. attribute:: Engine:MAXFUELFLOW

    :access: Get only
    :type: :ref:`scalar <scalar>` (units/s)

    How much fuel volume would this engine consume at standard pressure and velocity if the throttle was max at 1.0, and the thrust limiter was max at 100%.  Note this might not be the engine's actual max fuel flow it could have under other air pressure conditions.  Some jet engines have a very different fuel consumption depending on how fast they are currently being rammed through the air.
    
.. attribute:: Engine:MASSFLOW

    :access: Get only
    :type: :ref:`scalar <scalar>` (Mg/s)

    How much fuel mass is this engine consuming at this very moment.

.. attribute:: Engine:MAXMASSFLOW

    :access: Get only
    :type: :ref:`scalar <scalar>` (Mg/s)

    How much fuel mass would this engine consume at standard pressure and velocity if the throttle was max at 1.0, and the thrust limiter was max at 100%.  Note this might not be the engine's actual max fuel flow it could have under other air pressure conditions.  Some jet engines have a very different fuel consumption depending on how fast they are currently being rammed through the air.

.. attribute:: Engine:ISP

    :access: Get only
    :type: :ref:`scalar <scalar>`

    `Specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_

.. method:: Engine:ISPAT(pressure)

    :parameter pressure: atmospheric pressure (in standard Kerbin atmospheres)
    :type: :ref:`scalar <scalar>`

    `Specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_ at the given atmospheric pressure.  Use a pressure of 0 for vacuum, and 1 for sea level (on Kerbin).
    (Pressure must be greater than or equal to zero.  If you pass in a
    negative value, it will be treated as if you had given a zero instead.)

.. attribute:: Engine:VACUUMISP

    :access: Get only
    :type: :ref:`scalar <scalar>`

    Vacuum `specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_

.. attribute:: Engine:VISP

    :access: Get only
    :type: :ref:`scalar <scalar>`

    Synonym for :VACUUMISP

.. attribute:: Engine:SEALEVELISP

    :access: Get only
    :type: :ref:`scalar <scalar>`

    `Specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_ at Kerbin sealevel.

.. attribute:: Engine:SLISP

    :access: Get only
    :type: :ref:`scalar <scalar>`

    Synonym for :SEALEVELISP

.. attribute:: Engine:FLAMEOUT

    :access: Get only
    :type: :ref:`Boolean <boolean>`

    Is this engine failed because it is starved of a resource (liquidfuel, oxidizer, oxygen)?

.. attribute:: Engine:IGNITION

    :access: Get only
    :type: :ref:`Boolean <boolean>`

    Has this engine been ignited? If both :attr:`Engine:IGNITION` and :attr:`Engine:FLAMEOUT` are true, that means the engine could start up again immediately if more resources were made available to it.

.. attribute:: Engine:ALLOWRESTART

    :access: Get only
    :type: :ref:`Boolean <boolean>`

    Is this an engine that can be started again? Usually True, but false for solid boosters.

.. attribute:: Engine:ALLOWSHUTDOWN

    :access: Get only
    :type: :ref:`Boolean <boolean>`

    Is this an engine that can be shut off once started? Usually True, but false for solid boosters.

.. attribute:: Engine:THROTTLELOCK

    :access: Get only
    :type: :ref:`Boolean <boolean>`

    Is this an engine that is stuck at a fixed throttle? (i.e. solid boosters)

.. attribute:: Engine:MULTIMODE

    :access: Get only
    :type: :ref:`Boolean <boolean>`

    Does this engine have multiple modes (i.e. RAPIER)? Check this before calling multi-mode specific suffixes.   
    
.. attribute:: Engine:MODES

    :access: Get only
    :type: :struct:`List` of strings

    Lists names of modes of this engine if multimode, returns a list of 1 string "Single mode" otherwise.   

.. attribute:: Engine:MODE

    :access: Get only
    :type: :ref:`string <string>`

    Name of the current mode. Only assessible for multi-mode engines.

.. method:: Engine:TOGGLEMODE

    Call to switch to another mode. Only assessible for multi-mode engines.  

.. attribute:: Engine:PRIMARYMODE

    :access: Get/Set
    :type: :ref:`Boolean <boolean>`

    True for primary mode, false for secondary. Setting to other value equals toggling the mode. Only assessible for multi-mode engines. 

.. attribute:: Engine:AUTOSWITCH

    :access: Get/Set
    :type: :ref:`Boolean <boolean>`

    Is automatic switching enabled? Can set to switch between manual and automatic switching. Only assessible for multi-mode engines. 

.. attribute:: Engine:HASGIMBAL

    :access: Get only
    :type: :ref:`Boolean <boolean>`

    Does this engine have a gimbal enabled?

.. attribute:: Engine:GIMBAL

    :access: Get only
    :type: :struct:`Gimbal`

    Returns the :struct:`Gimbal` attached to this engine. Only accessible if the gimbal is present (Use :attr:`Engine:HASGIMBAL` to check if available).
    
.. attribute:: Engine:ULLAGE

    :access: Get only
    :type: :ref:`Boolean <boolean>`
    
    If RealFuels is installed, returns true if this engine is a type of engine that requires ullage, otherwise returns false.
    Note: this is a static property of the engine, for current fuel status, check `FUELSTABILITY`.

.. attribute:: Engine:FUELSTABILITY

    :access: Get only
    :type: :ref:`scalar <scalar>`
    
    If RealFuels is installed, returns the fuel stability of this engine as a value between 0 and 1 (where 1 is fullly stable), otherwise returns 1.
    Engines that don't require ullage will always return 1, unless they are pressure fed and the feed pressure is too low.

.. attribute:: Engine:PRESSUREFED

    :access: Get only
    :type: :ref:`Boolean <boolean>`
    
    If RealFuels is installed, returns true if this engine is pressure fed, otherwise returns false.

.. attribute:: Engine:IGNITIONS

    :access: Get only
    :type: :ref:`scalar <scalar>`
    
    If RealFuels is installed, returns the number of ignitions remaining, or -1 if it is unlimited, otherwise returns -1.

.. attribute:: Engine:MINTHROTTLE

    :access: Get only
    :type: :ref:`scalar <scalar>`
    
    If RealFuels is installed, returns the minimum throttle setting as a value between 0 and 1, otherwise returns 0.

.. attribute:: Engine:CONFIG

    :access: Get only
    :type: :struct:`String`
    
    If RealFuels is installed, returns the configuration name of this engine if applicable, otherwise returns the part title.

.. attribute:: Engine:CONSUMEDRESOURCES

    :access: Get only
    :type: :struct:`Lexicon` of :struct:`ConsumedResource`

    The fuel resources this engine consumes, and in what ratios.

.. _isp: http://en.wikipedia.org/wiki/Specific_impulse
