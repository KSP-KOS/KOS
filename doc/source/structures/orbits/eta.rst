.. _eta:

OrbitEta
========

OrbitEta is a special object that exists just to help you get the
times from now to certain events in an orbit's future.  It operates
on an Orbit, and can be obtained one of two ways:

* Any :struct:`Orbit` contains an ``ETA`` suffix that will give
  you the ETA values for that orbit patch, for example::

    print SHIP:OBT:ETA:APOAPSIS.
    print SHIP:OBT:NEXTPATCH:ETA:APOAPSIS.

* With the ``ETA`` keyword:  Just saying ``ETA`` gives the same
  thing as saying ``SHIP:OBT:ETA``.  Therefore this::

    print ETA:APOAPSIS.

  Is a shorthand for this::

    print SHIP:OBT:ETA:APOAPSIS

  .. structure:: OrbitEta

  .. list-table::

     :header-rows: 1
     :widths: 2 1 4

     * - Suffix
       - Type
       - Description

     * - :attr:`APOAPSIS`
       - :ref:`scalar <scalar>`, seconds
       - Seconds from now until apoapsis.

     * - :attr:`PERIAPSIS`
       - :ref:`scalar <scalar>`, seconds
       - Seconds from now until periapsis.

     * - :attr:`NEXTNODE`
       - :ref:`scalar <scalar>`, seconds
       - Seconds from now until the next maneuver node.

     * - :attr:`TRANSITION`
       - :ref:`scalar <scalar>`, seconds
       - Seconds from now until the next orbit patch starts.
		  
.. attribute:: ETA:APOAPSIS

    :type: :ref:`scalar <scalar>`, seconds
    :access: Get-only

    Seconds until the object in this orbit patch would hit its apoapsis.
    Note, that even hypothetical orbits created by :func:`CREATEORBIT`
    which have no "object in orbit" right now, still have a hypothetical
    imaginary point along that orbit that represents where the "object" is
    "now".
    
    If the object is on an escape trajectory (hyperbolic orbit) such that
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

    Seconds until the object in this orbit hits its periapsis.  If the
    ship is on an intersect with the ground, such that you'll hit the
    ground first before you'd get to periapsis, it will still return the
    hypothetical number of seconds it would have taken to get to periapsis
    if you had the magical ability to pass through the ground as if it
    wasn't there.

    Note that in hyperbolic orbits (escape trajectories), if you are
    past the Periapsis, then you'll never come back down to it.  Rather
    than returning the Very Big Number (``3.402823E+38``) in this case
    to represent infinity, it will instead count time "backward" and show
    you a negative number, for how many seconds it's been since periapsis.

.. attribute:: ETA:NEXTNODE

    :type: :ref:`scalar <scalar>`, seconds
    :access: Get only

    Seconds until the next manuever node's timestamp.  NOTE this is the
    time shown on the navball for the maneuver node, and does not
    take into account the lead time shown on the navball.
    
    This should give the exact same value as ``NEXTNODE:ETA`` with one
    important difference:  ``NEXTNODE:ETA`` will throw an error if
    there is no next node, while this (``ETA:NEXTNODE``) will simply
    return a **very big number** representing the biggest floating
    point value (32-bit).  (For various reasons, kOS does not allow
    the value "Infinity" in its Scalars, so "a really big number"
    is used in its place.)

.. attribute:: ETA:TRANSITION

    :type: :ref:`scalar <scalar>`, seconds
    :access: Get only

    Seconds until the transition from this orbit patch to the next one.
    This ignores the effect of any intervening manuever nodes it might
    hit before it gets there. (This will be the path you would follow
    if you never execute any of those manuever nodes.)

    If there *is* no next transition (you are on a closed loop that
    will not exit the current sphere of influence), this will
    return a **very big number** representing the biggest floating
    point value (32-bit).  (For various reasons, kOS does not allow
    the value "Infinity" in its Scalars, so "a really big number"
    is used in its place.)

