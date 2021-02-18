.. _reactionwheel:

Reaction Wheel
======

Some of the Parts returned by :ref:`LIST PARTS <list command>` will be a Reaction Wheel. It is also possible to get just the Reaction Wheel parts by executing ``LIST ReactionWheels``, for example::

    LIST ReactionWheels IN myVariable.
    FOR wheel IN myVariable {
        print "A reaction wheel exists that is currently " + wheel:WHEELSTATE.
    }.

.. structure:: Reaction Wheel

    .. list-table::
        :header-rows: 1
        :widths: 1 1 2

        * - Suffix
          - Type (units)
          - Description

        * - All suffixes of :struct:`Part`
          -
          - :struct:`RCS` objects are a type of :struct:`Part`


        * - :attr:`AUTHORITYLIMITER`
          - :ref:`scalar <scalar>` (%)
          - The authority limit for the reaction wheel.
        * - :attr:`MAXTORQUE`
          - :ref:`Vector <vector>` (kN)
          - A vector representing the maximum amount of force that can be applied over the Pitch, Yaw and Roll axis.
        * - :attr:`WHEELSTATE`
          - :ref:`String <string>`
          - The status of the reaction wheel: ACTIVE, DISABLED or BROKEN.
        * - :attr:`TORQUERESPONSESPEED`
          - :ref:`scalar <scalar>`
          - 

.. note::

    Reaction wheels always apply their torque at the ships center of mass. The center of mass of a ship can be found using the :struct:`Vessel <Vessel>`:position parameter.

.. note::

    A :struct:`ReactionWheel` is a type of :struct:`Part`, and therefore can use all the suffixes of :struct:`Part`.

.. attribute:: ReactionWheel:AUTHORITYLIMITER

    :access: Get/Set
    :type: :ref:`scalar <scalar>` (%)
    
    The authority limit of the reaction wheel.
    
.. attribute:: ReactionWheel:MAXTORQUE

    :access: Get
    :type: :ref:`Vector <vector>`
        
    A vector representing the amount of force that can be applied over the three axis: V(maxPitchTorque, maxYawTorque, maxRollTorque).
    
.. attribute:: ReactionWheel:WHEELSTATE

    :access: Get
    :type: :ref:`String <string>`
        
    The status of the reaction wheel. One of: ACTIVE, DISABLED or BROKEN.
    
.. attribute:: ReactionWheel:TORQUERESPONSESPEED

    :access: Get
    :type: :ref:`scalar <scalar>`
        
    Unknown
    
