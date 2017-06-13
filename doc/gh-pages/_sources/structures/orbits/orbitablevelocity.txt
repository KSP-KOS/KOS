.. _orbitablevelocity:

OrbitableVelocity
=================

When any :struct:`Orbitable` object returns its :attr:`VELOCITY <Orbitable:VELOCITY>` suffix, it returns it as a structure containing a pair of both its orbit-frame velocity and its surface-frame velocity at the same instant of time. To obtain its velocity as a vector you must pick whether you want the oribtal or surface velocities by giving a further suffix:

.. structure:: OrbitableVelocity::

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1

        * - Suffix
          - Type
        * - :attr:`ORBIT`
          - :struct:`Vector`
        * - :attr:`SURFACE`
          - :struct:`Vector`

.. attribute:: OrbitableVelocity:ORBIT

    :type: :struct:`Vector`
    :access: Get only

    Returns the orbital velocity.

.. attribute:: OrbitableVelocity:SURFACE

    :type: :struct:`Vector`
    :access: Get only

    Returns the surface-frame velocity. Note that this is the surface velocity relative to the surface of the SOI body, not the orbiting object itself. (i.e. Mun:VELOCITY:SURFACE returns the Mun's velocity relative to the surface of its SOI body, Kerbin).

    .. note::
        **Special case instance, the Sun:** Because the Sun has no parent
        SoI body that it orbits around (Kerbal Space Program does not
        simulate the existence of anything outside the one solar system),
        that means that the Sun's surface velocity is just hardcoded to
        be the same thing as its orbital velocity.  This may or may not be
        entirely correct, but the "correct" answer to the question, "What is
        sun:velocity:surface?" would technically be "I refuse to answer.
        That's an invalid question." Rather than crash or throw an exception,
        kOS just returns the same as the orbital velocity in this case.

Examples::

    SET VORB TO SHIP:VELOCITY:ORBIT
    SET VSRF TO SHIP:VELOCITY:SURFACE
    SET MUNORB TO MUN:VELOCITY:ORBIT
    SET MUNSRF TO MUN:VELOCITY:SURFACE

.. note::

    At first glance it may seem that ``Mun:VELOCITY:SURFACE`` is wrong because it creates a vector in the opposite direction from ``Mun:VELOCITY:ORBIT``, but this is actually correct. Kerbin's surface rotates once every 6 hours, and the Mun takes a lot longer than 6 hours to orbit Kerbin. Therefore, relative to Kerbin's surface, the Mun is going backward.
