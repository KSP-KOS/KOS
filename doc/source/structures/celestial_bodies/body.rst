.. _body:

Body
====

This is any sort of planet or moon. To get a variable referring to a Body, you can do this::

    // "name" is the name of the body,
    // like "Mun" for example.
    SET MY_VAR TO BODY("name").

Also, all bodies have hard-coded variable names as well. You can use the variable ``Mun`` to mean the same thing as ``BODY("Mun")``.

.. note::
    .. versionchanged:: 0.13
        A Celestial Body is now also an :ref:`Orbitable <orbitable>`, and can use all the terms described for these objects too.

Predefined Celestial Bodies
---------------------------

All of the main celestial bodies in the game are reserved variable names. The following two lines do the exactly the same thing::

    SET the_mun TO Mun.
    SET the_mun TO Body("Mun").

* Sun
* Moho
* Eve
    * Gilly
* Kerbin
    * Mun
    * Minmus
* Duna
    * Ike
* Jool
    * Laythe
    * Vall
    * Tylo
    * Bop
    * Pol
* Eeloo

.. structure:: Body

    ================================ ============
    Suffix                           Type (units)
    ================================ ============
         Every Suffix of :struct:`Orbitable`
    ---------------------------------------------
    :attr:`NAME`                     string
    :attr:`DESCRIPTION`              string
    :attr:`MASS`                     scalar (kg)
    :attr:`ALTITUDE`                 scalar (m)
    :attr:`ROTATIONPERIOD`           scalar (s)
    :attr:`RADIUS`                   scalar (m)
    :attr:`MU`                       scalar (:math:`m^3 s^{−2}`)
    :attr:`ATM`                      :struct:`Atmosphere`
    :attr:`ANGULARVEL`               :struct:`Direction` in :ref:`SHIP-RAW <ship-raw>`
    :attr:`GEOPOSITIONOF`            :struct:`GeoCoordinates` in :ref:`SHIP-RAW <ship-raw>`
    :attr:`ALTITUDEOF`               scalar (m)
    ================================ ============

.. attribute:: Body:NAME

    The name of the body. Example: "Mun".

.. attribute:: Body:DESCRIPTION

    Longer description of the body, often just a duplicate of the name.

.. attribute:: Body:MASS

    The mass of the body in kilograms.

.. attribute:: Body:ALTITUDE

    The altitude of this body above the sea level surface of its parent body. I.e. the altitude of Mun above Kerbin.

.. attribute:: Body:ROTATIONPERIOD

    The length of the body's day in seconds. I.e. how long it takes for it to make one rotation.

.. attribute:: Body:RADIUS

    The radius from the body's center to its sea level.

.. attribute:: Body:MU

    The `Gravitational Parameter`_ of the body.

.. _Gravitational Parameter: http://en.wikipedia.org/wiki/Standard_gravitational_parameter

.. attribute:: Body:ATM

    A variable that describes the atmosphere of this body.

.. attribute:: Body:ANGULARVEL

    Despite the name, this is technically not a velocity. It only tells you the axis of rotation, not the speed of rotation around that axis.

.. attribute:: Body:GEOPOSITIONOF

    The geoposition underneath the given vector position.  SHIP:BODY:GEOPOSITIONOF(SHIP:POSITION) should, in principle, give the same thing as SHIP:GEOPOSITION, while SHIP:BODY:GEOPOSITIONOF(SHIP:POSITION + 1000*SHIP:NORTH) would give you the lat/lng of the position 1 kilometer north of you.  Be careful not to confuse this with :GEOPOSITION (no "OF" in the name), which is also a suffix of Body by virtue of the fact that Body is an Orbitable, but it doesn't mean the same thing.

.. attribute:: Body:ALTITUDEOF

    The altitude of the given vector position, above this body's 'sea level'.  SHIP:BODY:ALTITUDEOF(SHIP:POSITION) should, in principle, give the same thing as SHIP:ALTITUDE.  Example: Eve:ALTITUDEOF(GILLY:POSITION) gives the altitude of gilly's current position above Eve, even if you're not actually anywhere near the SOI of Eve at the time.  Be careful not to confuse this with :ALTITUDE (no "OF" in the name), which is also a suffix of Body by virtue of the fact that Body is an Orbitable, but it doesn't mean the same thing.

