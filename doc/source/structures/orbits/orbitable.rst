.. _orbitable:

Orbitable (Vessels and Bodies)
==============================

All objects that can move in orbit around other objects share some similar structure. To help keep things as consistent as possible, that similar structure is defined here. Everything you see here works for both :struct:`Vessels <vessel>` and :struct:`Bodies <body>`.

.. note::

    **SOI Body**

    Every where you see the term **SOI Body** in the descriptions below, it refers to the body at the center of the orbit of this object - the body in who's sphere of influence this object is located. It is important to make the distinction that if this object is itself a Body, the **SOI body** is the body being orbited, not the body doing the orbiting. I.e. When talking about the Mun, the **SOI body** means "Kerbin". When talking about Kerbin, the **SOI body** means "Sun".

.. structure:: Orbitable

    **These terms are all read-only.**

    ======================= ==============
     Suffix                  Type (units)
    ======================= ==============
     :attr:`NAME`            :struct:`String`
     :attr:`BODY`            :struct:`Body`
     :attr:`HASBODY`         :struct:`boolean`
     :attr:`HASORBIT`        :struct:`boolean`
     :attr:`HASOBT`          :struct:`boolean`
     :attr:`OBT`             :struct:`Orbit`
     :attr:`ORBIT`           :struct:`Orbit`
     :attr:`UP`              :struct:`Direction`
     :attr:`NORTH`           :struct:`Direction`
     :attr:`PROGRADE`        :struct:`Direction`
     :attr:`SRFPROGRADE`     :struct:`Direction`
     :attr:`RETROGRADE`      :struct:`Direction`
     :attr:`SRFRETROGRADE`   :struct:`Direction`
     :attr:`POSITION`        :struct:`Vector`
     :attr:`VELOCITY`        :struct:`OrbitableVelocity`
     :attr:`DISTANCE`        :ref:`scalar <scalar>` (m)
     :attr:`DIRECTION`       :struct:`Direction`
     :attr:`LATITUDE`        :ref:`scalar <scalar>` (deg)
     :attr:`LONGITUDE`       :ref:`scalar <scalar>` (deg)
     :attr:`ALTITUDE`        :ref:`scalar <scalar>` (m)
     :attr:`GEOPOSITION`     :struct:`GeoCoordinates`
     :attr:`PATCHES`         :struct:`List` of :struct:`Orbits <Orbit>`
    ----------------------- --------------
     The Following are deprecated (use apoapsis and periapsis on :attr:`OBT`)
    --------------------------------------
     :attr:`APOAPSIS`        :ref:`scalar <scalar>` (m)
     :attr:`PERIAPSIS`       :ref:`scalar <scalar>` (m)
    ======================= ==============


.. attribute:: Orbitable:NAME

    :type: :ref:`string <string>`
    :access: Get only

    Name of this vessel or body.

.. attribute:: Orbitable:HASBODY

    :type: :ref:`boolean <boolean>`
    :access: Get only

    True if this object has a body it orbits (false only when this object is the Sun, pretty much).

.. attribute:: Orbitable:HASORBIT

    :type: :ref:`boolean <boolean>`
    :access: Get only

    Alias for HASBODY.

.. attribute:: Orbitable:HASOBT

    :type: :ref:`boolean <boolean>`
    :access: Get only

    Alias for HASBODY.

.. attribute:: Orbitable:BODY

    :type: :struct:`Body`
    :access: Get only

    The :struct:`Body` that this object is orbiting. I.e. ``Mun:BODY`` returns ``Kerbin``.

.. attribute:: Orbitable:OBT

    :type: :struct:`Orbit`
    :access: Get only

    The current single orbit "patch" that this object is on (not the future orbits it might be expected to achieve after maneuver nodes or encounter transitions, but what the current orbit would be if nothing changed and no encounters perturbed the orbit.

.. attribute:: Orbitable:ORBIT

    :type: :struct:`Orbit`
    :access: Get only

    This is an alias for OBT, as described above.

.. attribute:: Orbitable:UP

    :type: :struct:`Direction`
    :access: Get only

    pointing straight up away from the SOI body.

.. attribute:: Orbitable:NORTH

    :type: :struct:`Direction`
    :access: Get only

    pointing straight north on the SOI body, parallel to the surface of the SOI body.

.. attribute:: Orbitable:PROGRADE

    :type: :struct:`Direction`
    :access: Get only

    pointing in the direction of this object's **orbitable-frame** velocity

.. attribute:: Orbitable:SRFPROGRADE

    :type: :struct:`Direction`
    :access: Get only

    pointing in the direction of this object's **surface-frame** velocity. Note that if this Orbitable is itself a body, remember that this is relative to the surface of the SOI body, not this body.

.. attribute:: Orbitable:RETROGRADE

    :type: :struct:`Direction`
    :access: Get only

    pointing in the opposite of the direction of this object's **orbitable-frame** velocity

.. attribute:: Orbitable:SRFRETROGRADE

    :type: :struct:`Direction`
    :access: Get only

    pointing in the opposite of the direction of this object's **surface-frame** velocity. Note that this is relative to the surface of the SOI body.

.. attribute:: Orbitable:POSITION

    :type: :struct:`Vector`
    :access: Get only

    The position of this object in the :ref:`SHIP-RAW reference frame <ship-raw>`

.. attribute:: Orbitable:VELOCITY

    :type: :struct:`OrbitableVelocity`
    :access: Get only

    The :struct:`orbitable velocity <OrbitableVelocity>` of this object in the :ref:`SHIP-RAW reference frame <ship-raw>`

.. attribute:: Orbitable:DISTANCE

    :type: :ref:`scalar <scalar>` (m)
    :access: Get only

    The :ref:`scalar <scalar>` distance between this object and the center of :struct:`SHIP`.

.. attribute:: Orbitable:DIRECTION

    :type: :struct:`Direction`
    :access: Get only

    pointing in the direction of this object from :struct:`SHIP`.

.. attribute:: Orbitable:LATITUDE

    :type: :ref:`scalar <scalar>` (deg)
    :access: Get only

    The latitude in degrees of the spot on the surface of the SOI body directly under this object.

.. attribute:: Orbitable:LONGITUDE

    :type: :ref:`scalar <scalar>` (deg)
    :access: Get only

    The longitude in degrees of the spot on the surface of the SOI body directly under this object. Longitude returned will always be normalized to be in the range [-180,180].

.. attribute:: Orbitable:ALTITUDE

    :type: :ref:`scalar <scalar>` (m)
    :access: Get only

    The altitude in meters above the *sea level* surface of the SOI body (not the center of the SOI body. To get the true radius of the orbit for proper math calculations remember to add altitude to the SOI body's radius.)

.. attribute:: Orbitable:GEOPOSITION

    :type: :struct:`GeoCoordinates`
    :access: Get only

    A combined structure of the latitude and longitude numbers.

.. attribute:: Orbitable:PATCHES

    :type: :struct:`List` of :struct:`Orbit` "patches"
    :access: Get only

    The list of all the orbit patches that this object will transition to, not taking into account maneuver nodes. The zero-th patch of the list is the current orbit.

.. attribute:: Orbitable:APOAPSIS

    :type: :ref:`scalar <scalar>` (deg)
    :access: Get only

    .. deprecated:: 0.15
    
        Use :attr:`OBT:APOAPSIS <Orbit:APOAPSIS>` instead.

.. attribute:: Orbitable:PERIAPSIS

    :type: :ref:`scalar <scalar>` (deg)
    :access: Get only

    .. deprecated:: 0.15

        Use :attr:`OBT:PERIAPSIS <Orbit:PERIAPSIS>` instead.

