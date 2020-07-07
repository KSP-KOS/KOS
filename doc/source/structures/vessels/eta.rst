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

    Seconds until SHIP hits its apoapsis.
    
    If the ship is on an escape trajectory (hyperbolic orbit) such that
    you will never reach apoapsis, it will return the Very Big Number
    ``3.402823E+38``.  (Largest non-infinity number that can be
    represented in a single precision float value, if you care why it's
    that number.) A reasonable script test could simply be
    ``if eta:apoapsis > 100000000000000`` you can assume it's actually
    infinite, as that's much bigger than any real elliptical orbit in
    the game would give you.  (But a much better test for hyperbolic
    orbits is to look for the Apoapsis height being negative.)

    Also be aware that in the stock KSP game (things may be different
    if you install a mod like Principia that changes the orbital
    calculation model) ``ETA:APOAPSIS`` can be decieving when looking at
    some large orbits.  kOS will only return the fake bignum
    ``3.402823E+38`` for those orbits that are mathematically *actual*
    hyperbolic escape tragectories, not the orbits that are elliptical
    but the game still lets them escape anyway because of the limits of the
    Sphere of Influence model.

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

