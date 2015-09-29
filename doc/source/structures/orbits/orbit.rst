.. _orbit:

Orbit
=====

.. contents:: Contents
    :local:
    :depth: 1

Variables of type :struct:`Orbit` hold descriptive information about the elliptical shape of a predicted orbit. Whenever there are multiple patches of orbit ellipses strung together, for example, when an encounter with a body is expected to alter the path, or when a maneuver node is planned, then each individual patch of the path is represented by one :struct:`Orbit` object.

Each :struct:`Orbitable` item such as a :struct:`Vessel` or celestial :struct:`Body` has an ``:ORBIT`` suffix that can be used to obtain its current :struct:`Orbit`.

Whenever you get the :struct:`Orbit` of a :struct:`Vessel`, be aware that its just the current :struct:`Orbit` patch that doesn't take into account any planetary encounters (slingshots) or maneuver nodes that may occur. For example, your vessel might never reach ``SHIP:ORBIT:APOAPSIS`` if you're going to intersect the Mun and be flung by it into a new orbit.

.. warning::

    Some of the parameters listed here come directly from KSP's API and there is a bit of inconsistency with whether it uses radians or degrees for angles. As much as possible we have tried to present everything in kOS as degrees for consistency, but some of these may have slipped through. If you see any of these being reported in radians, please make a bug report.

Structure
---------

.. structure:: Orbit

    .. list-table:: **Members**
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type (units)
          - Description

        * - :attr:`NAME`
          - string
          - name of this orbit
        * - :attr:`APOAPSIS`
          - scalar (m)
          - Maximum altitude
        * - :attr:`PERIAPSIS`
          - scalar (m)
          - Minimum altitude
        * - :attr:`BODY`
          - :struct:`Body`
          - Focal body of orbit
        * - :attr:`PERIOD`
          - scalar (s)
          - `orbital period`_
        * - :attr:`INCLINATION`
          - scalar (deg)
          - `orbital inclination`_
        * - :attr:`ECCENTRICITY`
          - scalar
          - `orbital eccentricity`_
        * - :attr:`SEMIMAJORAXIS`
          - scalar (m)
          - `semi-major axis`_
        * - :attr:`SEMIMINORAXIS`
          - scalar (m)
          - `semi-minor axis`_
        * - :attr:`LAN`
          - scalar (deg)
          - Same as :attr:`LONGITUDEOFASCENDINGNODE`
        * - :attr:`LONGITUDEOFASCENDINGNODE`
          - scalar (deg)
          - Longitude of the ascending node
        * - :attr:`ARGUMENTOFPERIAPSIS`
          - scalar
          - `argument of periapsis`_
        * - :attr:`TRUEANOMALY`
          - scalar
          - `true anomaly`_ in degrees (not radians)
        * - :attr:`MEANANOMALYATEPOCH`
          - scalar
          - `mean anomaly`_ in degrees (not radians)
        * - :attr:`TRANSITION`
          - string
          - :ref:`Transition from this orbit <transitions>`
        * - :attr:`POSITION`
          - :struct:`Vector`
          - The current position
        * - :attr:`VELOCITY`
          - :struct:`Vector`
          - The current velocity
        * - :attr:`NEXTPATCH`
          - :struct:`Orbit`
          - Next :struct:`Orbit`
        * - :attr:`HASNEXTPATCH`
          - boolean
          - Has a next :struct:`Orbit`




.. attribute:: Orbit:NAME

    :type: string
    :access: Get only

    a name for this orbit.

.. attribute:: Orbit:APOAPSIS

    :type: scalar (m)
    :access: Get only

    The max altitude expected to be reached.

.. attribute:: Orbit:PERIAPSIS

    :type: scalar (m)
    :access: Get only

    The min altitude expected to be reached.

.. attribute:: Orbit:BODY

    :type: :struct:`Body`
    :access: Get only

    The celestial body this orbit is orbiting.

.. attribute:: Orbit:PERIOD

    :type: scalar (seconds)
    :access: Get only

    `orbital period`_

.. attribute:: Orbit:INCLINATION

    :type: scalar (degree)
    :access: Get only

    `orbital inclination`_

.. attribute:: Orbit:ECCENTRICITY

    :type: scalar
    :access: Get only

    `orbital eccentricity`_

.. attribute:: Orbit:SEMIMAJORAXIS

    :type: scalar (m)
    :access: Get only

    `semi-major axis`_

.. attribute:: Orbit:SEMIMINORAXIS

    :type: scalar (m)
    :access: Get only

    `semi-minor axis`_

.. attribute:: Orbit:LAN

    Same as :attr:`Orbit:LONGITUDEOFASCENDINGNODE`.

.. attribute:: Orbit:LONGITUDEOFASCENDINGNODE

    :type: scalar (deg)
    :access: Get only

    The Longitude of the ascening node is the "celestial longitude" where
    the orbit crosses the body's equator from its southern hemisphere to
    its northern hemisphere

    Note that the "celestial longitude" in this case is NOT the planetary
    longitude of the orbit body.  "Celestial longitudes" are expressed
    as the angle from the :ref:`universal reference vector <referencevector`,
    not from the body's longitude.  In order to find out where it is
    relative to the body's longitude, you will have to take into account
    ``body:rotationangle``, and take into account that the body will
    rotate by the time you get there.

.. attribute:: Orbit:ARGUMENTOFPERIAPSIS

    :type: scalar
    :access: Get only

    `argument of periapsis`_

.. attribute:: Orbit:TRUEANOMALY

    :type: scalar
    :access: Get only

    `true anomaly`_ in degrees.  Even though orbital parameters are
    traditionally done in radians, in keeping with the kOS standard
    of making everything into degrees, they are given as degrees by
    kOS.

.. attribute:: Orbit:MEANANOMALYATEPOCH

    :type: scalar
    :access: Get only

    `mean anomaly`_  in degrees. Even though orbital parameters are
    traditionally done in radians, in keeping with the kOS standard
    of making everything into degrees, they are given as degrees by
    kOS.


.. attribute:: Orbit:TRANSITION

    :type: string
    :access: Get only

    Describes the way in which this orbit will end and become a different orbit, with a value taken :ref:`from this list <transitions>`.

.. attribute:: Orbit:POSITION

    :type: :struct:`Vector`
    :access: Get only

    The current position of whatever the object is that is in this orbit.

.. attribute:: Orbit:VELOCITY

    :type: :struct:`Vector`
    :access: Get only

    The current velocity of whatever the object is that is in this orbit.

.. attribute:: Orbit:NEXTPATCH

    :type: :struct:`Orbit`
    :access: Get only

    When this orbit has a transition to another orbit coming up, this suffix returns the next Orbit patch after this one. For example, when escaping from a Mun orbit into a Kerbin orbit from which you will escape and hit a Solar orbit, then the current orbit's ``:NEXTPATCH`` will show the Kerbin orbit, and ``:NEXTPATCH:NEXTPATCH`` will show the solar orbit. The number of patches into the future that you can peek depends on your conic patches setting in your **Kerbal Space Program** Settings.cfg file.

.. attribute:: Orbit:HASNEXTPATCH

    boolean
    :access: Get only

    If :attr:`:NEXTPATCH <Orbit:NEXTPATCH>` will return a valid patch, this is true. If :attr:`:NEXTPATCH <Orbit:NEXTPATCH>` will not return a valid patch because there are no transitions occurring in the future, then ``HASNEXTPATCH`` will be false.



.. _orbital period: http://en.wikipedia.org/wiki/Orbital_period
.. _orbital inclination: http://en.wikipedia.org/wiki/Orbital_inclination
.. _orbital eccentricity: http://en.wikipedia.org/wiki/Orbital_eccentricity
.. _semi-major axis: http://en.wikipedia.org/wiki/Semi-major_axis
.. _semi-minor axis: http://en.wikipedia.org/wiki/Semi-minor_axis
.. _argument of periapsis: http://en.wikipedia.org/wiki/Argument_of_periapsis
.. _true anomaly: http://en.wikipedia.org/wiki/True_anomaly
.. _mean anomaly: http://en.wikipedia.org/wiki/Mean_anomaly

Both ``:NEXTPATCH`` and ``:HASNEXTPATCH`` both only operate on the **current** momentum of the object, and do **not** take into account any potential changes planned with maneuver nodes. To see the possible new path you would have if a maneuver node gets executed exactly as planned, you need to first get the orbit that follows the manuever node, by looking at the maneuver node's :ORBIT suffix <node>, and then look at **it's** ``:NEXTPATCH` and ``:HASNEXTPATCH``.

Deprecated Suffix
-----------------

.. attribute:: Orbit:PATCHES

    :type: :struct:`List` of :struct:`Orbit` Objects
    :access: Get only

    .. note::
    
        .. deprecated:: 0.15
        
            To get the same functionality, you must use :attr:`Vessel:PATCHES`  which is a suffix of the :struct:`Vessel` itself.

.. _transitions:

Transition Names
----------------

INITIAL
    Refers to the pure of a new orbit, which is a value you will never see from the :attr:`Orbit:TRANSITION` suffix (it refers to the start of the orbit patch, and :attr:`Orbit:TRANSITION` only refers to the end of the patch.

FINAL
    Means that no transition to a new orbit is expected. It this orbit is the orbit that will remain forever.

ENCOUNTER
    Means that this orbit will enter a new SOI of another orbital body that is smaller in scope and is "inside" the current one. (example: currently in Sun orbit, will enter Duna Orbit.)

ESCAPE
    Means that this orbit will enter a new SOI of another orbital body that is larger in scope and is "outside" the current one. (example: currently in Kerbin orbit, will enter Sun Orbit.)

MANEUVER
    Means that this orbit will end due to a manuever node that starts a new orbit?

