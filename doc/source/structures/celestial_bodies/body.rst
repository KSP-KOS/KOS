.. _body:

Body
====

.. function:: BODY(name)

This is any sort of planet or moon. To get a variable referring to a Body, you can do this::

    // "name" is the name of the body,
    // like "Mun" for example.
    SET MY_VAR TO BODY("name").

Bodies are also :struct:`Orbitable`, and as such have all the associated suffixes.

Bodies' names are added to the kerboscript language as variable names as well.
This means you can use the variable ``Mun`` to mean the same thing as ``BODY("Mun")``,
and the variable ``Kerbin`` to mean the same thing as ``BODY("Kerbin")``, and so on.

.. note::
    Exception: If you are using a mod that replaces the stock game's planets
    and moons with new bodies with new names, then there is a chance a body's
    name will match an existing bound variable name in kOS and we cannot
    control this.  Therefore if this happens, that body name will NOT become a
    variable name, so you can only refer to that body with the expression
    ``BODY(name)``.  (For example, this occurred when Galileo Planet Pack had
    a planet called "Eta" which has the same name as the bound variable "ETA").

    .. versionchanged:: 1.0.2
        This behavior was only added in kOS 1.0.2.
        Using a version of kOS prior to 1.0.2 will cause a name clash and
        broken behavior if a planet or moon exists that overrides a keyword name.

.. function:: BODYEXISTS(name)

To check whether a Body exists, you can use this boolean function::

    SET MUN_EXISTS TO BODYEXISTS("Mun").
    IF MUN_EXISTS PRINT "Mun Exists." ELSE PRINT "Mun does not exist.".



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
    :attr:`NAME`                     :struct:`String`
    :attr:`DESCRIPTION`              :struct:`String`
    :attr:`MASS`                     :struct:`Scalar` (kg)
    :attr:`HASOCEAN`                 :struct:`Boolean`
    :attr:`HASSOLIDSURFACE`          :struct:`Boolean`
    :attr:`ORBITINGCHILDREN`         :struct:`List`
    :attr:`ALTITUDE`                 :struct:`Scalar` (m)
    :attr:`ROTATIONPERIOD`           :struct:`Scalar` (s)
    :attr:`RADIUS`                   :struct:`Scalar` (m)
    :attr:`MU`                       :struct:`Scalar` (:math:`m^3 s^{−2}`)
    :attr:`ATM`                      :struct:`Atmosphere`
    :attr:`ANGULARVEL`               :struct:`Vector` in :ref:`SHIP-RAW <ship-raw>`
    :meth:`GEOPOSITIONOF`            :struct:`GeoCoordinates` given :ref:`SHIP-RAW <ship-raw>` position vector
    :meth:`GEOPOSITIONLATLNG`        :struct:`GeoCoordinates` given latitude and longitude values
    :attr:`ALTITUDEOF`               :struct:`Scalar` (m)
    :attr:`SOIRADIUS`                :struct:`Scalar` (m)
    :attr:`ROTATIONANGLE`            :struct:`Scalar` (deg)
    ================================ ============

.. note::

    This type is serializable.

.. attribute:: Body:NAME

    The name of the body. Example: "Mun".

.. attribute:: Body:DESCRIPTION

    Longer description of the body, often just a duplicate of the name.

.. attribute:: Body:MASS

    The mass of the body in kilograms.

.. attribute:: Body:HASOCEAN

    True if this body has an ocean.  Example: In the stock solar system,
    this is True for Kerbin and False for Mun.

.. attribute:: Body:HASSOLIDSURFACE

    True if this body has a solid surface.  Example: In the stock solar system,
    this is True for Kerbin and False for Jool.

.. attribute:: Body:ORBITINGCHILDREN

    A list of the bodies orbiting this body.  Example: In the stock solar system,
    Kerbin:orbitingchildren is a list two things: Mun and Minmus.

.. attribute:: Body:ALTITUDE

    The altitude of this body above the sea level surface of its parent body. I.e. the altitude of Mun above Kerbin.

.. attribute:: Body:ROTATIONPERIOD

    The number of seconds it takes the body to rotate around its own axis.
    This is the sidereal rotation period which can differ from the length
    of a day due to the fact that the body moves a bit further around the
    Sun while it's rotating around its own axis.

.. attribute:: Body:RADIUS

    The radius from the body's center to its sea level.

.. attribute:: Body:MU

    The `Gravitational Parameter`_ of the body.

.. _Gravitational Parameter: http://en.wikipedia.org/wiki/Standard_gravitational_parameter

.. attribute:: Body:ATM

    A variable that describes the atmosphere of this body.

.. attribute:: Body:ANGULARVEL

    Angular velocity of the body's rotation about its axis (its
    sidereal day) expressed as a vector.

    The direction the angular velocity points is in Ship-Raw orientation,
    and represents the axis of rotation.  Remember that everything in
    Kerbal Space Program uses a *left-handed coordinate system*, which
    affects which way the angular velocity vector will point.  If you
    curl the fingers of your **left** hand in the direction of the rotation,
    and stick out your thumb, the thumb's direction is the way the
    angular velocity vector will point.

    The magnitude of the vector is the speed of the rotation, *in radians*.

    Note, unlike many of the other parts of kOS, the rotation speed is
    expressed in radians rather than degrees.  This is to make it
    congruent with how VESSEL:ANGULARMOMENTUM is expressed, and for
    backward compatibility with older kOS scripts.

.. method:: Body:GEOPOSITIONOF(vectorPos)

    :parameter vectorPos: :struct:`Vector` input position in XYZ space.
    :type: :struct:`GeoCoordinates`

    The geoposition underneath the given vector position.  SHIP:BODY:GEOPOSITIONOF(SHIP:POSITION) should, in principle, give the same thing as SHIP:GEOPOSITION, while SHIP:BODY:GEOPOSITIONOF(SHIP:POSITION + 1000*SHIP:NORTH) would give you the lat/lng of the position 1 kilometer north of you.  Be careful not to confuse this with :GEOPOSITION (no "OF" in the name), which is also a suffix of Body by virtue of the fact that Body is an Orbitable, but it doesn't mean the same thing.

    (Not to be confused with the :attr:`Orbitable:GEOPOSITION` suffix, which ``Body`` inherits
    from :struct:`Orbitable`, and which gives the position that this body is directly above
    on the surface *of its parent body*.)

.. method:: Body:GEOPOSITIONLATLNG(latitude, longitude)

    :parameter latitude: :struct:`Scalar` input latitude
    :parameter longitude: :struct:`Scalar` input longitude
    :type: :struct:`GeoCoordinates`

    Given a latitude and longitude, this returns a :struct:`GeoCoordinates` structure
    for that position on this body.

    (Not to be confused with the :attr:`Orbitable:GEOPOSITION` suffix, which ``Body`` inherits
    from :struct:`Orbitable`, and which gives the position that this body is directly above
    on the surface *of its parent body*.)

.. attribute:: Body:ALTITUDEOF

    The altitude of the given vector position, above this body's 'sea level'.  SHIP:BODY:ALTITUDEOF(SHIP:POSITION) should, in principle, give the same thing as SHIP:ALTITUDE.  Example: Eve:ALTITUDEOF(GILLY:POSITION) gives the altitude of gilly's current position above Eve, even if you're not actually anywhere near the SOI of Eve at the time.  Be careful not to confuse this with :ALTITUDE (no "OF" in the name), which is also a suffix of Body by virtue of the fact that Body is an Orbitable, but it doesn't mean the same thing.

.. attribute:: Body:SOIRADIUS

    The radius of the body's sphere of influence. Measured from the body's center.

.. attribute:: Body:ROTATIONANGLE

    The rotation angle is the number of degrees between the
    :ref:`Solar Prime Vector <solarprimevector>` and the
    current position of the body's prime meridian (body longitude
    of zero).

    The value is in constant motion, and once per body's rotation
    period ("sidereal day"), its ``:rotationangle`` will wrap
    around through a full 360 degrees.
