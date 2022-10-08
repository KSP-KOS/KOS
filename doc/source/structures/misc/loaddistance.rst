.. _loaddistance:

Vessel Load Distance
====================


.. structure:: LoadDistance

    **(The "on rails" settings)**

    :struct:`LoadDistance` describes the set of distances at which the game
    causes vessels to unload, and the distances that cause a vessel to
    become "packed". This requires some explanation.

    Before entering into that explanation, first here's a list of
    example cases where you might want to use this feature to change
    these values:

    - Trying to have one airplane follow another.
    - Trying to have two rockets fly in formation into orbit together.
    - Trying to have a rover race to a flag, with several rovers seeing who gets there first.

    Basically, any time you might have more than just the current
    active vessel running a kOS script, this is a setting you probably
    will want to understand and tweak.

    **The explanation:**

    Most players of KSP eventually discover a concept called being
    "on rails".  This is a term used by the player community to
    describe the fact that vessels that are far away from the active
    vessel aren't being micro-managed under the full physics engine.  At that
    distance, changes in movement due to things like the atmosphere, are not
    applied.  The vessel's physics are calculated based only on orbital motion,
    much like the effects of using timewarp (not physics warp).

    But the actual behavior in the game is a bit more complex than that,
    and understanding it is necessary to use this structure.

    The term "on rails" actually refers to two entirely different things
    that are controlled by separate settings, as described below:

    **loaded** : A vessel is LOADED when all its parts are being
    rendered by the graphics engine and it's possible to actually see
    what it looks like.  A vessel that is UNLOADED doesn't even
    have its parts in memory and is just a single dot in space
    with no dimensions. An unloaded vessel is literally impossible
    to see on your screen no matter how much you squint because
    it's not even being rendered on camera at all.  Unloaded vessels
    only exist as a marker icon in space, with a possible label text.

    **packed** : A vessel is PACKED when it is close enough to be
    *loaded* (see above), but still far enough away that its full
    capabilities aren't enabled.  A vessel that is *loaded*, but
    still *packed* will be unable to have its parts interact, and
    the vessel will appear stuck in the same location, unmoving.
    You can *see* a vessel that is loaded but packed, but the vessel
    won't actually be able to *do* anything.  In this state, the
    game is still treating the entire object as if it was one single
    part.  It's added all the graphic models of all the parts together
    into one conglomerate "object" (thus the term "packed") that exists
    purely so you can look at it, even though it doesn't actually work
    until you get closer and it becomes *unpacked*.

    **THE NEXT SENTENCE IS VERY IMPORTANT AND VITAL:**

    *The kOS processor is able to run scripts any time that a vessel is loaded,
    but the script is not guaranteed access to all features when a ship is
    LOADED but not UNPACKED.*  KSP limits some features (like throttle control)
    to only vessels that are unpacked.  You may check the `UNPACKED` suffix of
    the `SHIP` variable to determine if these features are available.

    This structure allows you to read or change the stock KSP game's
    distance settings for how far a vessel has to get from the active
    vessel in order for it to trigger its UNLOAD or PACK states.

    The distance settings are different for different vessel situations.
    It's important to first read the existing values before changing them,
    to see what the stock game thought were reasonable for them.

    Some distances are very short.  For example, the fact that the
    pack distance for a landed vessel is short is what allows landers
    to stay "parked" in place without tipping over when you leave them
    on a long distance EVA.

    Each of these suffixes returns a :struct:`SituationLoadDistance`,
    which is a tuple of values for the loading and packing distances in
    that situation.

    **Wait between LOAD and PACK changes!**

    Due to a strange way the game behaves, it is unsafe to change
    both the load/unload distance and the unpack/pack distance at
    the same time in the same physics tick.  If you are
    going to increase both, then increase the load/unload distances
    first, followed by a ``WAIT 0.001.`` to force a new physics tick
    and let the change take effect, then increase the unpack/pack
    distances after the wait is over.

    **Beware the space kraken when changing these:**

    There's a reason the stock game has these distance limitations.  Setting
    them very large can degrade your performance, and can cause buggy
    inaccuracies in the position and velocity calculations that cause the
    game to think things have collided together when they haven't.  This
    is the classic "space kraken" that KSP players talk about a lot.  Computer
    floating point numbers get less precise the farther from zero they are.
    So allowing the game to try to perform microcalculations on tiny time
    scales using floating point numbers that have imprecision because they are
    large in magnitude (i.e. the positions of parts that are many kilometers
    away from you), can cause phantom collisions, which make the game
    explode things for "no reason".

    These distance limits were put in place by SQUAD specifically for
    the purpose of trying to avoid the space kraken.  If you set them
    too large again, you can risk invoking the Kraken again.  They
    typically CAN be enlarged some, because the settings are very low
    to provide a overly large safety margin, but be careful with it.
    Don't go overboard and set the ranges to several thousand kilometers.

    Also, don't set the PACK distance to be higher than the LOAD distance,
    as that is undefined behavior in the main game.  Always keep the LOAD
    distance higher than or equal to the PACK distance.

    .. list-table:: Members and Methods
        :header-rows: 1
        :widths: 2 2 1 4

        * - Suffix
          - Type
          - Get/Set
          - Description

        * - :attr:`ESCAPING`
          - :struct:`SituationLoadDistance`
          - Get
          - Load and pack Distances while escaping the current body
        * - :attr:`FLYING`
          - :struct:`SituationLoadDistance`
          - Get
          - Load and pack Distances while flying in atmosphere
        * - :attr:`LANDED`
          - :struct:`SituationLoadDistance`
          - Get
          - Load and pack Distances while landed on the surface
        * - :attr:`ORBIT`
          - :struct:`SituationLoadDistance`
          - Get
          - Load and pack Distances while in orbit
        * - :attr:`PRELAUNCH`
          - :struct:`SituationLoadDistance`
          - Get
          - Load and pack Distances while on the launch pad or runway
        * - :attr:`SPLASHED`
          - :struct:`SituationLoadDistance`
          - Get
          - Load and pack Distances while splashed in water
        * - :attr:`SUBORBITAL`
          - :struct:`SituationLoadDistance`
          - Get
          - Load and pack Distances while on a suborbital trajectory

Situation Load Distance
=======================

Each of the above

.. structure:: SituationLoadDistance

  :struct:`SituationLoadDistance` is what is returned by each of the
  above suffixes mentioned in the LoadDistance suffix list above.

  **Order Matters.** - Becuse of the protections in place to prevent
  you from setting some values bigger than others (see the descriptions
  below), sometimes the order in which you change the values matters
  and you have to be careful to change them in the correct order, or
  else  the attempt to change them will be denied.

  .. list-table:: Members and Methods
      :header-rows: 1
      :widths: 2 1 1 4

      * - Suffix
        - Type
        - Get/Set
        - Description

      * - :attr:`LOAD`
        - :struct:`Scalar` (m)
        - Get/Set
        - The load distance
      * - :attr:`UNLOAD`
        - :struct:`Scalar` (m)
        - Get/Set
        - The unload distance
      * - :attr:`UNPACK`
        - :struct:`Scalar` (m)
        - Get/Set
        - The unpack distance
      * - :attr:`PACK`
        - :struct:`Scalar` (m)
        - Get/Set
        - The pack distance

.. attribute:: SituationLoadDistance:LOAD

    :access: Get/Set
    :type: :struct:`Scalar` (m)

    Get or set the load distance.  When another vessel is getting closer
    to you, because you are moving toward it or it is moving toward you,
    when that vessel becomes this distance *or closer* to the active
    vessel, it will transition from being *unloaded* to being *loaded*.
    See the description above for what it means for a vessel to be *loaded*.

    This value must be less than :attr:`UNLOAD`, and will automatically
    be adjusted accordingly.

.. attribute:: SituationLoadDistance:UNLOAD

    :access: Get/Set
    :type: :struct:`Scalar` (m)

    Get or set the unload distance.  When another vessel is becoming more
    distant as you move away from it, or it moves away from you,
    when that vessel becomes this distance *or greater* from the active
    vessel, it will transition from being *loaded* to being *unloaded*.
    See the description above for what it means for a vessel to be *loaded*.

    This value must be greater than :attr:`LOAD`, and will automatically
    be adjusted accordingly.

.. attribute:: SituationLoadDistance:UNPACK

    :access: Get/Set
    :type: :struct:`Scalar` (m)

    Get or set the unpack distance.  When another vessel is getting closer
    to you, because you are moving toward it or it is moving toward you,
    when that vessel becomes this distance *or closer* to the active
    vessel, it will transition from being *packed* to being *unpacked*.
    See the description above for what it means for a vessel to be *packed*.

    This value must be less than :attr:`PACK`, and will automatically be adjusted accordingly.

.. attribute:: SituationLoadDistance:PACK

    :access: Get/Set
    :type: :struct:`Scalar` (m)

    Get or set the pack distance.  When another vessel is getting farther
    away from you, because you are moving away from it or it is moving
    away from you, when that vessel becomes this distance *or greater*
    from the active vessel, it will transition from being *unpacked* to
    being *packed*.  See the description above for what it means for
    a vessel to be *packed*.

    This value must be greater than :attr:`UNPACK`, and will automatically be adjusted accordingly.


===Examples===

Print out all the current settings::

    SET distances TO KUNIVERSE:DEFAULTLOADDISTANCE.

    PRINT "escaping distances:".
    print "    load: " + distances:ESCAPING:LOAD + "m".
    print "  unload: " + distances:ESCAPING:UNLOAD + "m".
    print "  unpack: " + distances:ESCAPING:UNPACK + "m".
    print "    pack: " + distances:ESCAPING:PACK + "m".
    PRINT "flying distances:".
    print "    load: " + distances:FLYING:LOAD + "m".
    print "  unload: " + distances:FLYING:UNLOAD + "m".
    print "  unpack: " + distances:FLYING:UNPACK + "m".
    print "    pack: " + distances:FLYING:PACK + "m".
    PRINT "landed distances:".
    print "    load: " + distances:LANDED:LOAD + "m".
    print "  unload: " + distances:LANDED:UNLOAD + "m".
    print "  unpack: " + distances:LANDED:UNPACK + "m".
    print "    pack: " + distances:LANDED:PACK + "m".
    PRINT "orbit distances:".
    print "    load: " + distances:ORBIT:LOAD + "m".
    print "  unload: " + distances:ORBIT:UNLOAD + "m".
    print "  unpack: " + distances:ORBIT:UNPACK + "m".
    print "    pack: " + distances:ORBIT:PACK + "m".
    PRINT "prelaunch distances:".
    print "    load: " + distances:PRELAUNCH:LOAD + "m".
    print "  unload: " + distances:PRELAUNCH:UNLOAD + "m".
    print "  unpack: " + distances:PRELAUNCH:UNPACK + "m".
    print "    pack: " + distances:PRELAUNCH:PACK + "m".
    PRINT "splashed distances:".
    print "    load: " + distances:SPLASHED:LOAD + "m".
    print "  unload: " + distances:SPLASHED:UNLOAD + "m".
    print "  unpack: " + distances:SPLASHED:UNPACK + "m".
    print "    pack: " + distances:SPLASHED:PACK + "m".
    PRINT "suborbital distances:".
    print "    load: " + distances:SUBORBITAL:LOAD + "m".
    print "  unload: " + distances:SUBORBITAL:UNLOAD + "m".
    print "  unpack: " + distances:SUBORBITAL:UNPACK + "m".
    print "    pack: " + distances:SUBORBITAL:PACK + "m".

Change the settings while flying or landed or splashed or
on launchpad or runway,
For the purpose of allowing more vessels to fly around the
Kerbal Space Center at a greater distances from each other::

    // 30 km for in-flight
    // Note the order is important.  set UNLOAD BEFORE LOAD,
    // and PACK before UNPACK.  Otherwise the protections in
    // place to prevent invalid values will deny your attempt
    // to change some of the values:
    SET KUNIVERSE:DEFAULTLOADDISTANCE:FLYING:UNLOAD TO 30000.
    SET KUNIVERSE:DEFAULTLOADDISTANCE:FLYING:LOAD TO 29500.
    WAIT 0.001. // See paragraph above: "wait between load and pack changes"
    SET KUNIVERSE:DEFAULTLOADDISTANCE:FLYING:PACK TO 29999.
    SET KUNIVERSE:DEFAULTLOADDISTANCE:FLYING:UNPACK TO 29000.
    WAIT 0.001. // See paragraph above: "wait between load and pack changes"

    // 30 km for parked on the ground:
    // Note the order is important.  set UNLOAD BEFORE LOAD,
    // and PACK before UNPACK.  Otherwise the protections in
    // place to prevent invalid values will deny your attempt
    // to change some of the values:
    SET KUNIVERSE:DEFAULTLOADDISTANCE:LANDED:UNLOAD TO 30000.
    SET KUNIVERSE:DEFAULTLOADDISTANCE:LANDED:LOAD TO 29500.
    WAIT 0.001. // See paragraph above: "wait between load and pack changes"
    SET KUNIVERSE:DEFAULTLOADDISTANCE:LANDED:PACK TO 39999.
    SET KUNIVERSE:DEFAULTLOADDISTANCE:LANDED:UNPACK TO 29000.
    WAIT 0.001. // See paragraph above: "wait between load and pack changes"

    // 30 km for parked in the sea:
    // Note the order is important.  set UNLOAD BEFORE LOAD,
    // and PACK before UNPACK.  Otherwise the protections in
    // place to prevent invalid values will deny your attempt
    // to change some of the values:
    SET KUNIVERSE:DEFAULTLOADDISTANCE:SPLASHED:UNLOAD TO 30000.
    SET KUNIVERSE:DEFAULTLOADDISTANCE:SPLASHED:LOAD TO 29500.
    WAIT 0.001. // See paragraph above: "wait between load and pack changes"
    SET KUNIVERSE:DEFAULTLOADDISTANCE:SPLASHED:PACK TO 29999.
    SET KUNIVERSE:DEFAULTLOADDISTANCE:SPLASHED:UNPACK TO 29000.
    WAIT 0.001. // See paragraph above: "wait between load and pack changes"

    // 30 km for being on the launchpad or runway
    // Note the order is important.  set UNLOAD BEFORE LOAD,
    // and PACK before UNPACK.  Otherwise the protections in
    // place to prevent invalid values will deny your attempt
    // to change some of the values:
    SET KUNIVERSE:DEFAULTLOADDISTANCE:PRELAUNCH:UNLOAD TO 30000.
    SET KUNIVERSE:DEFAULTLOADDISTANCE:PRELAUNCH:LOAD TO 29500.
    WAIT 0.001. // See paragraph above: "wait between load and pack changes"
    SET KUNIVERSE:DEFAULTLOADDISTANCE:PRELAUNCH:PACK TO 29999.
    SET KUNIVERSE:DEFAULTLOADDISTANCE:PRELAUNCH:UNPACK TO 29000.
    WAIT 0.001. // See paragraph above: "wait between load and pack changes"
