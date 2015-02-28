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
        * - :attr:`THRUST`
          - scalar (kN)
          - Current thrust
        * - :attr:`FUELFLOW`
          - scalar (l/s maybe)
          - Rate of fuel burn
        * - :attr:`ISP`
          - scalar
          - `Specific impulse <isp>`_
        * - :attr:`VACUUMISP`
          - scalar
          - `Vacuum Specific impulse <isp>`_
        * - :attr:`VISP`
          - scalar
          - `Synonym for VACUUMISP`_
        * - :attr:`SEALEVELISP`
          - scalar
          - `Specific impulse at Kerbin sealevel <isp>`_
        * - :attr:`SLISP`
          - scalar
          - `Synonym for SEALEVELISP`_
        * - :attr:`ISP`
          - scalar
          - `Specific impulse <isp>`_
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

    If this an engine with a thrust limiter (tweakable) enabled, what percentage is it limited to?

.. attribute:: Engine:MAXTHRUST

    :access: Get only
    :type: scalar (kN)

    How much thrust would this engine give if the throttle was max and conditions were ideal.

.. attribute:: Engine:THRUST

    :access: Get only
    :type: scalar (kN)

    How much thrust is this engine giving at this very moment.

.. attribute:: Engine:FUELFLOW

    :access: Get only
    :type: scalar (Liters/s? maybe)

    Rate at which fuel is being burned. Not sure what the units are.

.. attribute:: Engine:ISP

    :access: Get only
    :type: scalar

    `Specific impulse <isp>`_

.. attribute:: Engine:VACUUMISP

    :access: Get only
    :type: scalar

    `Vacuum Specific impulse <isp>`_

.. attribute:: Engine:VISP

    :access: Get only
    :type: scalar

    `Synonym for :VACUUMISP`_

.. attribute:: Engine:SEALEVELISP

    :access: Get only
    :type: scalar

    `Specific impulse at Kerbin sealevel <isp>`_

.. attribute:: Engine:SLISP

    :access: Get only
    :type: scalar

    `Synonym for :SEALEVELISP`_

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
