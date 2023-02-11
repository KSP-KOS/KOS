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

    **Radians vs Degrees**

    Some of the parameters listed below come directly from KSP's API and there is a bit of inconsistency with whether it uses radians or degrees for angles. As much as possible we have tried to present everything in kOS as degrees for consistency, but some of these may have slipped through. If you see any of these being reported in radians, please make a bug report.


Creation
--------

It is possible to make an :struct`Orbit` object without it coming from
either a :struct:`Vessel` or a :struct:`Body`.  This could be useful when
you want to be able to get information about a hypothetical orbit that
an object may someday end up in, even when its not in that orbit now.  One
case where this may be useful is when trying to place a satellite into a
desired final orbit (say to fufill a contract).  You may wish to see some
information about that destination orbit even though no particular objects
are in that orbit at the moment.  To do this you can create an :struct`Orbit`
object using the ``CREATEORBIT()`` function described below.  You pass in
the Keplerian parameters that define the orbit (which are typically the
values you will see on a contract parameter for putting satellites into
desired orbits), and the orbit object you make can then be queried for things
like its apoapsis, periapsis, etc.

.. function:: CREATEORBIT(inc, e, sma, lan, argPe, mEp, t, body)

    :parameter inc: (:struct:`Scalar`) inclination, in degrees.
    :parameter e: (:struct:`Scalar`) eccentricity
    :parameter sma: (:struct:`Scalar`) semi-major axis
    :parameter lan: (:struct:`Scalar`) longitude of ascending node, in degrees.
    :parameter argPe: (:struct:`Scalar`) argument of periapsis
    :parameter mEp: (:struct:`Scalar`) mean anomaly at epoch, in degrees.
    :parameter t: (:struct:`Scalar`) epoch
    :parameter body: (:struct:`Body`) body to orbit around
    :return: :struct:`Orbit`

    This creates a new orbit around the Mun::

        SET myOrbit TO CREATEORBIT(0, 0, 270000, 0, 0, 0, 0, Mun).

It is also possible to create an orbit from a position and a velocity using the ``CREATEORBIT()`` function described below:

.. function:: CREATEORBIT(pos, vel, body, ut)

    :parameter pos: (:struct:`Vector`) position (relative to center of body, NOT the usual relative to current ship most positions in kOS use.  Remember to offset a kOS position from the body's position when calculating what to pass in here.)
    :parameter vel: (:struct:`Vector`) velocity
    :parameter body: (:struct:`Body`) body to orbit around
    :parameter ut: (:struct:`Scalar`) time (universal)
    :return: :struct:`Orbit`

    This creates a new orbit around Kerbin::

        SET myOrbit TO CREATEORBIT(V(2295.5, 0, 0), V(0, 0, 70000 + Kerbin:RADIUS), Kerbin, 0).

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
          - :struct:`String`
          - name of this orbit
        * - :attr:`APOAPSIS`
          - :struct:`Scalar` (m)
          - Maximum altitude
        * - :attr:`PERIAPSIS`
          - :struct:`Scalar` (m)
          - Minimum altitude
        * - :attr:`BODY`
          - :struct:`Body`
          - Focal body of orbit
        * - :attr:`PERIOD`
          - :struct:`Scalar` (s)
          - `orbital period`_
        * - :attr:`INCLINATION`
          - :struct:`Scalar` (deg)
          - `orbital inclination`_
        * - :attr:`ECCENTRICITY`
          - :struct:`Scalar`
          - `orbital eccentricity`_
        * - :attr:`SEMIMAJORAXIS`
          - :struct:`Scalar` (m)
          - `semi-major axis`_
        * - :attr:`SEMIMINORAXIS`
          - :struct:`Scalar` (m)
          - `semi-minor axis`_
        * - :attr:`LAN`
          - :struct:`Scalar` (deg)
          - Same as :attr:`LONGITUDEOFASCENDINGNODE`
        * - :attr:`LONGITUDEOFASCENDINGNODE`
          - :struct:`Scalar` (deg)
          - Longitude of the ascending node
        * - :attr:`ARGUMENTOFPERIAPSIS`
          - :struct:`Scalar`
          - `argument of periapsis`_
        * - :attr:`TRUEANOMALY`
          - :struct:`Scalar`
          - `true anomaly`_ in degrees (not radians)
        * - :attr:`MEANANOMALYATEPOCH`
          - :struct:`Scalar`
          - `mean anomaly`_ in degrees (not radians) at a specific fixed time called :attr:`EPOCH`
        * - :attr:`EPOCH`
          - :struct:`Scalar`
          - The universal timestamp at which :attr:`MEANANOMALYATEPOCH` is measured.
        * - :attr:`TRANSITION`
          - :struct:`String`
          - :ref:`Transition from this orbit <transitions>`
        * - :attr:`POSITION`
          - :struct:`Vector`
          - The current position
        * - :attr:`VELOCITY`
          - :struct:`OrbitableVelocity`
          - The current velocity
        * - :attr:`NEXTPATCH`
          - :struct:`Orbit`
          - Next :struct:`Orbit` (building upgrade needed)
        * - :attr:`NEXTPATCHETA`
          - :struct:`Scalar`
          - ETA to next :struct:`Orbit` (building upgrade needed)
        * - :attr:`ETA`
          - :struct:`ORBITETA`
          - ETA object showing time to Pe, Ap, and transition. (building upgrade needed)
        * - :attr:`HASNEXTPATCH`
          - :struct:`Boolean`
          - Has a next :struct:`Orbit` (building upgrade needed)

.. attribute:: Orbit:NAME

    :type: :struct:`String`
    :access: Get only

    a name for this orbit.

.. attribute:: Orbit:APOAPSIS

    :type: :struct:`Scalar` (m)
    :access: Get only

    The max altitude expected to be reached.

.. attribute:: Orbit:PERIAPSIS

    :type: :struct:`Scalar` (m)
    :access: Get only

    The min altitude expected to be reached.

.. attribute:: Orbit:BODY

    :type: :struct:`Body`
    :access: Get only

    The celestial body this orbit is orbiting.

.. attribute:: Orbit:PERIOD

    :type: :struct:`Scalar` (seconds)
    :access: Get only

    `orbital period`_

.. attribute:: Orbit:INCLINATION

    :type: :struct:`Scalar` (degree)
    :access: Get only

    `orbital inclination`_

.. attribute:: Orbit:ECCENTRICITY

    :type: :struct:`Scalar`
    :access: Get only

    `orbital eccentricity`_

.. attribute:: Orbit:SEMIMAJORAXIS

    :type: :struct:`Scalar` (m)
    :access: Get only

    `semi-major axis`_

.. attribute:: Orbit:SEMIMINORAXIS

    :type: :struct:`Scalar` (m)
    :access: Get only

    `semi-minor axis`_

.. attribute:: Orbit:LAN

    Same as :attr:`Orbit:LONGITUDEOFASCENDINGNODE`.

.. attribute:: Orbit:LONGITUDEOFASCENDINGNODE

    :type: :struct:`Scalar` (deg)
    :access: Get only

    The Longitude of the ascening node is the "celestial longitude" where
    the orbit crosses the body's equator from its southern hemisphere to
    its northern hemisphere

    Note that the "celestial longitude" in this case is NOT the planetary
    longitude of the orbit body.  "Celestial longitudes" are expressed
    as the angle from the :ref:`Solar Prime Vector <solarprimevector>`,
    not from the body's longitude.  In order to find out where it is
    relative to the body's longitude, you will have to take into account
    ``body:rotationangle``, and take into account that the body will
    rotate by the time you get there.

.. attribute:: Orbit:ARGUMENTOFPERIAPSIS

    :type: :struct:`Scalar`
    :access: Get only

    `argument of periapsis`_

.. attribute:: Orbit:TRUEANOMALY

    :type: :struct:`Scalar`
    :access: Get only

    `true anomaly`_ in degrees.  Even though orbital parameters are
    traditionally done in radians, in keeping with the kOS standard
    of making everything into degrees, they are given as degrees by
    kOS.

    **Closed versus Open orbits clamp this differently:** The range of
    possible values this can have differs depending on if the orbit
    is "closed" (elliptical, eccentricity < 1.0) versus "open" (parabolic
    or hyperbolic, eccentricity >= 1.0).  If the orbit is closed, then
    this value will be in the range [0..360), where values larger than
    180 represent positions in the orbit where it is "coming down"
    from apoapsis to periapsis.  But if the orbit is open, then this
    value will be in the range (-180..180), where negative values are
    used to represent the positions in the orbit where it is "coming down"
    to the periapsis.  The difference is because it does not make sense
    to speak of the orbit looping all the way around 360 degrees in
    the case of an open orbit where it does not come back down.

    Note that the above switch between 0..360 versus -180..180 happens
    when the orbit is *mathematically* shown to be an escaping orbit,
    NOT when it's still an ellipse but the apoapsis happens to be higher
    than the body's sphere of influence so the game will let it escape
    anyway.  Both conditions look similar on the game map so it may
    be hard to tell them apart without actually querying the eccentricity
    to find out which it is.

.. attribute:: Orbit:MEANANOMALYATEPOCH

    :type: :struct:`Scalar` degrees
    :access: Get only

    `mean anomaly`_  in degrees. Even though orbital parameters are
    traditionally done in radians, in keeping with the kOS standard
    of making everything into degrees, they are given as degrees by
    kOS.

    Internally, KSP tracks orbit position using :attr:`MEANANOMALYATEPOCH`
    and :attr:`EPOCH`.  "Epoch" is an arbitrary timestamp expressed in
    universal time (gameworld seconds from game start, same as ``TIME:SECONDS``
    uses) at which the mean anomaly of the orbit would be :attr:`MEANANOMALYATEPOCH`.

    Given the mean anomaly at epoch, and the epoch time, and the current time,
    and the orbital period, it's possible to find out the current mean anomaly.
    Kerbal Space Program uses this internally to track orbit positions while under
    time warp without using the full physics system.

    **Closed versus Open orbits clamp this differently:** The range of
    possible values this can have differs depending on if the orbit
    is "closed" (elliptical, eccentricity < 1.0) versus "open" (parabolic
    or hyperbolic, eccentricity >= 1.0).  If the orbit is closed, then
    this value will be in the range [0..360), where values larger than
    180 represent positions in the orbit where it is "coming back down"
    from apoapsis to periapsis.  But if the orbit is open, then this value
    doesn't have any limits, and furthermore negative values are
    used to represent the portion of the orbit that is "coming down"
    to the periapsis, rather than using values > 180 for this.

    Note that the above switch between MEANANOMALY behaving in the "closed"
    versus "open" way depends on the orbit being *mathematically* shown
    to be an escaping orbit, NOT merely "escaping" because it has an
    apoapsis higher than the body's sphere of influence.  If the orbit's
    mathematical parameters show it to be an ellipse, but its apoapsis is
    higher than the body's sphere of influence, then the game will let it
    escape anyway despite it still being an elliptical orbit.  (It's just
    an elliptical orbit with the top "cut off".)  The MEANANOMALY
    measurement will treat such elliptical-but-escaping-anyway scenarios
    as "closed" even though they don't look like it on the map.

.. attribute:: Orbit:EPOCH

    :type: :struct:`Scalar` universal timestamp (seconds)
    :access: Get only

    Internally, KSP tracks orbit position using :attr:`MEANANOMALYATEPOCH`
    and :attr:`EPOCH`.  "Epoch" is an arbitrary timestamp expressed in
    universal time (gameworld seconds from game start, same as ``TIME:SECONDS``
    uses) at which the mean anomaly of the orbit would be :attr:`MEANANOMALYATEPOCH`.

    Beware, if you are an experienced programmer, you may be aware of the
    word "Epoch" being used to mean a fixed point in time that never
    ever changes throughout an entire system.  For example, the Unix
    timestamp system refers to Jan 1, 1970 as the "epoch".  This is *NOT*
    how the word is used in KSP's orbit system.  In Kerbal Space Program,
    the "epoch" is not a true "epoch", in that it often moves and you have to
    re-check what it is.  It's not a hardcoded constant.

    (The epoch timestamp seems to change when you go on or off from time warp.)

.. attribute:: Orbit:TRANSITION

    :type: :struct:`String`
    :access: Get only

    Describes the way in which this orbit will end and become a different orbit, with a value taken :ref:`from this list <transitions>`.

.. attribute:: Orbit:POSITION

    :type: :struct:`Vector`
    :access: Get only

    The current position of whatever the object is that is in this orbit.

.. attribute:: Orbit:VELOCITY

    :type: :struct:`OrbitableVelocity`
    :access: Get only

    The current velocity of whatever the object is that is in this orbit.  Be aware
    that this is not just a velocity vector, but a structure containing both the
    orbital and surface velocity vectors as a pair.  (See :struct:`OrbitableVelocity`).

.. attribute:: Orbit:NEXTPATCH

    :type: :struct:`Orbit`
    :access: Get only

    *In career this requires a building upgrade* - In career mode where
    buildings are not upgraded at the start, this suffix won't be allowed
    until your tracking station is upgraded a level.

    When this orbit has a transition to another orbit coming up, this suffix returns the next Orbit patch after this one. For example, when escaping from a Mun orbit into a Kerbin orbit from which you will escape and hit a Solar orbit, then the current orbit's :attr:`:NEXTPATCH <Orbit:NEXTPATCH>` will show the Kerbin orbit, and ``:NEXTPATCH:NEXTPATCH`` will show the solar orbit. The number of patches into the future that you can peek depends on your conic patches setting in your **Kerbal Space Program** Settings.cfg file.

.. attribute:: Orbit:NEXTPATCHETA

    :type: :struct:`Scalar`
    :access: Get only

    *In career this requires a building upgrade* - In career mode where
    buildings are not upgraded at the start, this suffix won't be allowed
    until your tracking station is upgraded a level.

    When this orbit has a transition to another orbit coming up, this suffix
    returns the eta to that transition.  This is different from the value
    provided by the :attr:`ETA:TRANSITION` suffix as it is not limited
    to the patch following the current orbit, but rather may be chained to
    multiple patch transitions.  The number of patches depends on your conic
    patches setting in your **Kerbal Space Program** Settings.cfg file.

.. attribute:: Orbit:ETA

    :type: :struct:`OrbitEta`
    :access: Get only

    Returns the :struct:`OrbitEta` object that lets you access the number of
    seconds to important events in this orbit (periapsis, apoapsis, and transition
    to next orbit).

.. attribute:: Orbit:HASNEXTPATCH

    :type: :struct:`Boolean`
    :access: Get only

    *In career this requires a building upgrade* - In career mode where
    buildings are not upgraded at the start, this suffix won't be allowed
    until your tracking station is upgraded a level.

    If :attr:`:NEXTPATCH <Orbit:NEXTPATCH>` will return a valid patch, this is true. If :attr:`:NEXTPATCH <Orbit:NEXTPATCH>` will not return a valid patch because there are no transitions occurring in the future, then :attr:`HASNEXTPATCH <Orbit:HASNEXTPATCH` will be false.



.. _orbital period: http://en.wikipedia.org/wiki/Orbital_period
.. _orbital inclination: http://en.wikipedia.org/wiki/Orbital_inclination
.. _orbital eccentricity: http://en.wikipedia.org/wiki/Orbital_eccentricity
.. _semi-major axis: http://en.wikipedia.org/wiki/Semi-major_axis
.. _semi-minor axis: http://en.wikipedia.org/wiki/Semi-minor_axis
.. _argument of periapsis: http://en.wikipedia.org/wiki/Argument_of_periapsis
.. _true anomaly: http://en.wikipedia.org/wiki/True_anomaly
.. _mean anomaly: http://en.wikipedia.org/wiki/Mean_anomaly

Both :attr:`NEXTPATCH <Orbit:NEXTPATCH>` and :attr:`HASNEXTPATCH <Orbit:HASNEXTPATCH>` both only operate on the **current** momentum of the object, and do **not** take into account any potential changes planned with maneuver nodes. To see the possible new path you would have if a maneuver node gets executed exactly as planned, you need to first get the orbit that follows the manuever node, by looking at the maneuver node's :attr:`:ORBIT <ManeuverNode:ORBIT>` suffix, and then look at **its** :attr:`:NEXTPATCH <Orbit:NEXTPATCH>` and :attr:`:HASNEXTPATCH <Orbit:HASNEXTPATCH>`.

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
