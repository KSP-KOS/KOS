.. _eta:

ETA
===

ETA is a special object that exists just to help you get the
times from now to certain events in a vessel's future.  It 
always presumes you're operating on the current SHIP vessel:


.. structure:: ETA

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`APOAPSIS`
          - :ref:`scalar <scalar>`, seconds
          - Seconds until SHIP hits its apoapsis.

        * - :attr:`PERIAPSIS`
          - :ref:`scalar <scalar>`, seconds
          - Seconds until SHIP hits its periapsis.

        * - :attr:`TRANSITION`
          - :ref:`scalar <scalar>`, seconds
          - Seconds until SHIP hits the next orbit patch.
		  
.. attribute:: ETA:APOAPSIS

    :type: :ref:`scalar <scalar>`, seconds
    :access: Get only

    Seconds until SHIP hits its apoapsis.  If the ship is on an escape
    trajectory (hyperbolic orbit) such that you will never reach apoapsis,
    it will return zero.

.. attribute:: ETA:PERIAPSIS

    :type: :ref:`scalar <scalar>`, seconds
    :access: Get only

    Seconds until SHIP hits its periapsis.  If the ship is on an intersect
    with the ground, such that you'll hit the ground first before you'd
    get to periapsis, it will still return the hypothetical number of 
    seconds it would have taken to get to periapsis if you had the magical
    ability to pass through the ground as if it wasn't there.

.. attribute:: ETA:TRANSITION

    :type: :ref:`scalar <scalar>`, seconds
    :access: Get only

    Seconds until SHIP hits its the end of its current orbit patch and
    transitions into another one, ignoring the effect of any intervening
    manuever nodes it might hit before it gets there.  

