.. _rcs:

RCS
======

Some of the Parts returned by :ref:`LIST PARTS <list command>` will be of type RCS. It is also possible to get just the RCS parts by executing ``LIST RCS``, for example::

    LIST RCS IN myVariable.
    FOR rcs IN myVariable {
        print "An rcs thruster exists with ISP = " + rcs:ISP.
    }.

.. structure:: RCS

    .. list-table::
        :header-rows: 1
        :widths: 1 1 2

        * - Suffix
          - Type (units)
          - Description

        * - All suffixes of :struct:`Part`
          -
          - :struct:`RCS` objects are a type of :struct:`Part`


        * - :attr:`ENABLED`
          -
          - Is this thruster enabled.
        * - :attr:`YAWENABLED`
          -
          - Is yaw control enabled for this thruster.
        * - :attr:`PITCHENABLED`
          -
          - Is pitch control enabled for this thruster.
        * - :attr:`ROLLENABLED`
          -
          - Is roll control enabled for this thruster.
        * - :attr:`FOREENABLED`
          -
          - Is fore/aft control enabled for this thruster.
        * - :attr:`STARBOARDENABLED`
          -
          - Is port/starboard control enabled for this thruster.
        * - :attr:`TOPENABLED`
          -
          - Is dorsal/ventral control enabled for this thruster.
        * - :attr:`FOREBYTHROTTLE`
          -
          - Does this thruster apply fore thrust when the ship throttled up.
        * - :attr:`FULLTHRUST`
          -
          - Does this thruster always apply full thrust.
        * - :attr:`THRUSTLIMIT`
          - :ref:`scalar <scalar>` (%)
          - Tweaked thrust limit.
        * - :attr:`DEADBAND`
          - :ref:`scalar <scalar>`
          - The game's built-in RCS input null zone for this RCS thruster.
        * - :attr:`MAXTHRUST`
          - :ref:`scalar <scalar>` (kN)
          - Untweaked thrust limit.
        * - :meth:`MAXTHRUSTAT(pressure)`
          - :ref:`scalar <scalar>` (kN)
          - Max thrust at the specified pressure (in standard Kerbin atmospheres).
        * - :attr:`AVAILABLETHRUST`
          - :ref:`scalar <scalar>` (kN)
          - Available thrust at full throttle accounting for thrust limiter.
        * - :meth:`AVAILABLETHRUSTAT(pressure)`
          - :ref:`scalar <scalar>` (kN)
          - Available thrust at the specified pressure (in standard Kerbin atmospheres).
        * - :attr:`MAXFUELFLOW`
          - :ref:`scalar <scalar>` (unit/s)
          - Untweaked maximum volumetric flow rate of fuel at full throttle.
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
          - Check if no more fuel.
        * - :attr:`THRUSTVECTORS`
          - :struct:`List`
          - List of thrust :struct:`Vectors <Vector>` for this RCS module.
        * - :attr:`CONSUMEDRESOURCES`
          - :struct:`Lexicon`
          - Lexicon of resources consumed by this thruster, keyed by resource name.


.. note::

    A :struct:`RCS` is a type of :struct:`Part`, and therefore can use all the suffixes of :struct:`Part`.

.. attribute:: RCS:ENABLED

    :access: Get/Set
    :type: :ref:`Boolean <boolean>`
    
    Is this rcs thruster enabled.
    
.. attribute:: RCS:YAWENABLED

    :access: Get/Set
    :type: :ref:`Boolean <boolean>`
        
    Is yaw control enabled for this rcs thruster.
    
.. attribute:: RCS:PITCHENABLED

    :access: Get/Set
    :type: :ref:`Boolean <boolean>`
        
    Is pitch control enabled for this rcs thruster.
    
.. attribute:: RCS:ROLLENABLED

    :access: Get/Set
    :type: :ref:`Boolean <boolean>`
        
    Is roll control enabled for this rcs thruster.
    
.. attribute:: RCS:FOREENABLED

    :access: Get/Set
    :type: :ref:`Boolean <boolean>`
        
    Is fore/aft control enabled for this rcs thruster.
    
.. attribute:: RCS:STARBOARDENABLED

    :access: Get/Set
    :type: :ref:`Boolean <boolean>`
        
    Is port/starboard control enabled for this rcs thruster.
    
.. attribute:: RCS:TOPENABLED

    :access: Get/Set
    :type: :ref:`Boolean <boolean>`
        
    Is dorsal/ventral control enabled for this rcs thruster.
    
.. attribute:: RCS:FOREBYTHROTTLE

    :access: Get/Set
    :type: :ref:`Boolean <boolean>`
        
    Does this thruster apply fore thrust when the ship throttled up.
    
.. attribute:: RCS:FULLTHRUST

    :access: Get/Set
    :type: :ref:`Boolean <boolean>`
        
    Does this thruster always apply full thrust.
    
.. attribute:: RCS:THRUSTLIMIT

    :access: Get/Set
    :type: :ref:`scalar <scalar>` (%)

    If this is a thruster with a thrust limiter (tweakable) enabled, what
    percentage is it limited to?  Note that this is expressed as a
    percentage, not a simple 0..1 coefficient.  e.g. To set thrustlimit
    to half, you use a value of 50.0, not 0.5.

    This value is not allowed to go outside the range [0..100].  If you
    attempt to do so, it will be clamped down into the allowed range.

    Note that although a kerboscript is allowed to set the value to a
    very precise number (for example 10.5123), the stock in-game display
    widget that pops up when you right-click the rcs will automatically
    round it to the nearest 0.5 whenever you open the panel.  So if you
    do something like ``set ship:part[20]:thrustlimit to 10.5123.`` in
    your script, then look at the rightclick menu for the rcs, the very
    act of just looking at the menu will cause it to become 10.5 instead
    of 10.5123.  There isn't much that kOS can do to change this.  It's a
    user interface decision baked into the stock game.

.. attribute:: RCS:DEADBAND

    :access: Get/Set (but Note the Warning on SET below)
    :type: :ref:`scalar <scalar>`

    Default: 0.05.

    **Please note the warning below before you try to SET this.**

    The stock game imposes a large dead zone on RCS thrusters.  By
    default they will not respond to any inputs less than this value.
    For example, at the default value of 0.05, the RCS thruster
    will ignore this statement::
    
        set ship:control:yaw to 0.049.

    but it will respond to this statement::

        set ship:control:yaw to 0.051.

    The reason this limit exists is apparently (this is speculation,
    warning) that it's how the stock game prevents SAS from spending
    a lot of monopropellant when it wiggles the controls small amounts.
    When control inputs are smaller than this value, then the RCS
    thrusters ignore them and only the reaction wheels and engine
    gimbals respond.  Despite the fact that this is really only a
    problem with SAS, the game appears to have solved the problem by
    imposing this null zone physically on the RCS parts themselves so
    the limit affects everything that uses them, including kOS
    autopiloting and user manual control.

    The best way to deal with this, if you have a script that wants
    the RCS thrusters to operate at a value less than this, is
    to pulse the input intermittently on and off at 0.05 to achieve
    amounts smaller than 0.05, rather than trying to solve it by
    setting this value.  (Remember that in the real world, thrusters
    have a minimum thrust they can't go below so it's not entirely
    unrealistic for this deadband to exist in the game.)

.. warning::

    **BEWARE if you want to Set this:**
    Although this can be set and changing it works okay in the current
    version of KSP as of this writing (KSP 1.10.1), it is exactly the
    sort of thing that seems could break in future versions of KSP.

    Be aware of that when deciding to change it. (If you are a programmer,
    you might understand the next sentence and get a clear picture of
    why this warning is here:  The value in the KSP class that this
    affects is marked ``private`` and kOS is using "Reflection" to bypass
    that access rule.)

    If you are tempted to change this value, please first consider
    coming up with a solution where your script pulses the input
    on and off between 0.05 and 0 to simulate inputs less than 0.05.

.. warning::

    **BEWARE if you want to Set this:**
    Setting this value too small on your RCS thrusters will cause the
    stock SAS to wastefully spend RCS propellant wiggling the controls
    when it tries to hold position.  (If you ever played KSP back in
    its alpha pre-release days you might remember SAS behaving like 
    this in the old days.)

.. _rcs_MAXTHRUST:

.. attribute:: RCS:MAXTHRUST

    :access: Get only
    :type: :ref:`scalar <scalar>` (kN)

    How much thrust would this rcs thruster give at its current atmospheric pressure if one of the control axes that activates it (yaw, pitch, roll, fore, aft, or top) was maxxed, and the thrust limiter was max at 100%.  Note this might not be the thruster's actual max thrust it could have under other air pressure conditions.  Some thrusters have a very different value for MAXTHRUST in vacuum as opposed to at sea level pressure.

.. _rcs_MAXTHRUSTAT:

.. method:: RCS:MAXTHRUSTAT(pressure)

    :parameter pressure: atmospheric pressure (in standard Kerbin atmospheres)
    :type: :ref:`scalar <scalar>` (kN)

    How much thrust would this rcs thruster give if one of the control axes that activated it (yaw, pitch, roll, fore, aft, or top) was maxxed and thrust limiter was max at the given atmospheric pressure.  Use a pressure of 0.0 for vacuum, and 1.0 for sea level (on Kerbin) (or more than 1 for thicker atmospheres like on Eve).
    (Pressure must be greater than or equal to zero.  If you pass in a
    negative value, it will be treated as if you had given a zero instead.)

.. attribute:: RCS:THRUST

    :access: Get only
    :type: :ref:`scalar <scalar>` (kN)

    How much thrust is this rcs thruster is giving at this very moment.

.. _rcs_AVAILABLETHRUST:

.. attribute:: RCS:AVAILABLETHRUST

    :access: Get only
    :type: :ref:`scalar <scalar>` (kN)

    Taking into account the thrust limiter tweakable setting, how much thrust would this rcs thruster give at its current thrust limit setting and atmospheric pressure conditions, if one of the control axes that activated it (yaw, pitch, roll, fore, aft, or top) was maxxed .

.. _rcs_AVAILABLETHRUSTAT:

.. method:: RCS:AVAILABLETHRUSTAT(pressure)

    :parameter pressure: atmospheric pressure (in standard Kerbin atmospheres)
    :type: :ref:`scalar <scalar>` (kN)

    Taking into account the thrust limiter tweakable setting, how much thrust at the given atmospheric pressure would this rcs thruster give at its current thrust limit setting if one of the control axes that activated it (yaw, pitch, roll, fore, aft, or top) was maxxed.   The pressure is measured in ATMs, meaning 0.0 is a vacuum, 1.0 is sea level at Kerbin.
    (Pressure must be greater than or equal to zero.  If you pass in a
    negative value, it will be treated as if you had given a zero instead.)

.. attribute:: RCS:MAXFUELFLOW

    :access: Get only
    :type: :ref:`scalar <scalar>` (units/s)

    How much fuel volume would this rcs thruster consume at standard pressure and velocity if one of the control axes that activated it (yaw, pitch, roll, fore, aft, or top) was maxxed, and the thrust limiter was max at 100%.  Note this might not be the engine's actual max fuel flow it could have under other air pressure conditions.

.. attribute:: RCS:MAXMASSFLOW

    :access: Get only
    :type: :ref:`scalar <scalar>` (Mg/s)

    How much fuel mass would this rcs thruster consume at standard pressure and velocity if one of the control axes that activated it (yaw, pitch, roll, fore, aft, or top) was maxxed, and the thrust limiter was max at 100%.  Note this might not be the engine's actual max fuel flow it could have under other air pressure conditions.

.. attribute:: RCS:ISP

    :access: Get only
    :type: :ref:`scalar <scalar>`

    `Specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_

.. method:: RCS:ISPAT(pressure)

    :parameter pressure: atmospheric pressure (in standard Kerbin atmospheres)
    :type: :ref:`scalar <scalar>`

    `Specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_ at the given atmospheric pressure.  Use a pressure of 0 for vacuum, and 1 for sea level (on Kerbin).
    (Pressure must be greater than or equal to zero.  If you pass in a
    negative value, it will be treated as if you had given a zero instead.)

.. attribute:: RCS:VACUUMISP

    :access: Get only
    :type: :ref:`scalar <scalar>`

    Vacuum `specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_

.. attribute:: RCS:VISP

    :access: Get only
    :type: :ref:`scalar <scalar>`

    Synonym for :VACUUMISP

.. attribute:: RCS:SEALEVELISP

    :access: Get only
    :type: :ref:`scalar <scalar>`

    `Specific impulse <http://wiki.kerbalspaceprogram.com/wiki/Specific_impulse>`_ at Kerbin sealevel.

.. attribute:: RCS:SLISP

    :access: Get only
    :type: :ref:`scalar <scalar>`

    Synonym for :SEALEVELISP

.. attribute:: RCS:FLAMEOUT

    :access: Get only
    :type: :ref:`Boolean <boolean>`

    Is this rcs thruster failed because it is starved of a resource (monopropellant)?

.. attribute:: RCS:THRUSTVECTORS

    :access: Get only
    :type: :struct:`List` of :struct:`Vectors <Vector>`

    This gives a list of all the vectors that this RCS module can thrust along. Vectors returned are of unit length.  The vectors are returned in Ship-Raw coordinates, rather than relative to the ship.  (i.e. if it thrusts along the ship's fore axis, and the ship's current ``ship:facing:forevector`` is ``V(0.7071, 0.7071, 0)``, then the value this returns would be ``V(0.7071, 0.7071, 0)``, not ``V(0,0,1)``).

.. attribute:: RCS:CONSUMEDRESOURCES

    :access: Get only
    :type: :struct:`Lexicon` of :struct:`CONSUMEDRESOURCERCS`

    This gives a lexicon of all the resources this rcs thruster consumes, keyed by resource name.

