.. _warp:

WARP
====

.. contents:: CONTENTS
    :local:
    :depth: 2

.. _timewarp:

TimeWarp structure
------------------

.. structure:: TimeWarp

    :struct:`TimeWarp` allows you to control and query the KSP game's
    time warping abilities.  You can obtain the ``TimeWarp`` structure
    by using the :attr:`TimeWarp <Kuniverse:TIMEWARP>` suffix of
    :struct:`kuniverse`.

    .. list-table:: Members and Methods
        :header-rows: 1
        :widths: 2 2 1 4

        * - Suffix
          - Type
          - Get/Set
          - Description

        * - :attr:`RATELIST`
          - :struct:`List` of :struct:`Scalar` values
          - Get
          - returns either :attr:`RAILSRATELIST` or :attr:`PHYSICSRATELIST` depending on which matches the current mode.
        * - :attr:`RAILSRATELIST`
          - :struct:`List` of :struct:`Scalar` values
          - Get
          - The list of allowed multiplier rates for on-rails-warp modes.
        * - :attr:`PHYSICSRATELIST`
          - :struct:`List` of :struct:`Scalar` values
          - Get
          - The list of allowed multiplier rates for physics-warp modes.
        * - :attr:`MODE`
          - :struct:`String`
          - Get/Set
          - Time warp mode.  values are "PHYSICS" or "RAILS".
        * - :attr:`WARP`
          - :struct:`Scalar`
          - Get/Set
          - Time warp integer index.  Values go 0,1,2,3, etc.
        * - :attr:`RATE`
          - :struct:`Scalar`
          - Get/Set
          - The current multiplier timescale rate (i.e. 100 for 100x warp)
        * - :meth:`WARPTO`
          - None
          - Callable method
          - Call to warp forward to a known timestamp.
        * - :meth:`CANCELWARP`
          - None
          - Callable method
          - Cancel current warping (including :meth:`WARPTO`)
        * - :attr:`PHYSICSDELTAT`
          - :struct:`SCALAR`
          - Get
          - Physics Delta-T.  How much time *should* pass between ticks.
        * - :attr:`ISSETTLED`
          - :struct:`Boolean`
          - Get
          - True once the actual rate finally arrives at the commanded rate.

Suffixes
--------

.. attribute:: TimeWarp:RATELIST

    :access: Get
    :type: :struct:`Scalar`

    If :attr:`MODE<TimeWarp:MODE>` is "PHYSICS", this will return :attr:`PHYSICSRATELIST<TimeWarp:PHYSICSRATELIST>`.
    If :attr:`MODE<TimeWarp:MODE>` is "RAILS", this will return :attr:`RAILSRATELIST<TimeWarp:RAILSRATELIST>`.

    It's always the list that goes with the current warping mode.

.. attribute:: TimeWarp:RAILSRATELIST

    :access: Get
    :type: :struct:`List` of :struct:`Scalar` values

    The list of the legal values that the game lets you set the warp rate
    to when in "on rails" warping mode ("normal" time warp).

    (As of this writing of the documents, the values come out like the table
    below, but the base KSP game could change these at any time.  The
    following table is not a guarantee.)

    .. table:: RAILS WARP RATE LIST

        ==== ====
        WARP RATE
        ==== ====
        0    1x
        1    5x
        2    10x
        3    50x
        4    100x
        5    1000x
        6    10000x
        7    100000x
        ==== ====

.. attribute:: TimeWarp:PHYSICSRATELIST

    :access: Get
    :type: :struct:`List` of :struct:`Scalar` values

    The list of the legal values that the game lets you set the warp rate
    to when in "physics warp" warping mode.

    (As of this writing of the documents, the values come out like the table
    below, but the base KSP game could change these at any time.  The
    following table is not a guarantee.)

    .. table:: PHYSICS WARP RATE LIST

        ==== ====
        WARP RATE
        ==== ====
        0    1x
        1    2x
        2    3x
        3    4x
        ==== ====

.. attribute:: TimeWarp:MODE

    :access: Get/Set
    :type: :struct:`String`

    The string value indicating whether we are in "PHYSICS" or "RAILS"
    warping mode right now.  You can set this value to change which
    warp mode the game will perform.

    (Any experienced player of KSP should be aware of what the difference
    between physics warp and "time warp" (rails) is.  In "physics" warp,
    all the normal things work, and the game simulates the entire physics
    engine with longer coarser delta-T time steps to achieve a faster
    simulation rate.  In "rails" warp, many of the calculations are not
    working, the vessel is not controllable, and the game calculates
    positions of objects based on the Keplerian elliptical parameters only.)

.. attribute:: TimeWarp:WARP

    :access: Get/Set
    :type: :struct:`Scalar`

    Time warp as an integer index.  In the tables listed above for
    :attr:`RAILSRATELIST<TimeWarp:RAILSRATELIST>` and :attr:`PHYSICSRATELIST<TimeWarp:PHYSICSRATELIST>`, this is the number
    on the lefthand side of those tables.  (i.e. if
    :attr:`MODE<TimeWarp:MODE>` is "RAILS" and :attr:`RATE<TimeWarp:RATE>` is 50, then that means
    :attr:`WARP<TimeWarp:WARP>` is 3.)

    If you set either :attr:`WARP<TimeWarp:WARP>` or :attr:`RATE<TimeWarp:RATE>`, the other will change
    along with it to agree with it.  (See the full explanation in
    :attr:`RATE<TimeWarp:RATE>` below).

.. attribute:: TimeWarp:RATE

    :access: Get/Set
    :type: :struct:`Scalar`

    The current multiplier timescale rate (i.e.  1000 if current rate
    is 1000x as much as normal, etc).

    If you have just changed the time warp, it takes a few moments for
    the game to "catch up" and achieve the desired warp rate.  You can
    query this value to find out what the current rate is the game is
    operating under during this physics tick.  It often takes almost
    a whole second of game time to finally arrive at the destination rate.

    When you ``set`` the ``:RATE`` equal to a new value, then
    instead of directly setting the rate to that value, kOS will
    set the :attr:`WARP<TimeWarp:WARP>` to whatever value it would need to have
    to end up with that rate.  The rate itself won't change right
    away.  For example, the following two commands are equivalent::

        // This will eventually give you a rate of 100, after several
        // update ticks have passed, but not right away:
        set kuniverse:timewarp:warp to 4.

        // This will *also* do the same thing, and not set the rate
        // to 100 right away, but instead tells kOS indirectly
        // to set the WARP to 4, so as to target a destination
        // rate of 100.
        set kuniverse:timewarp:rate to 100.

    If you set the rate to a value that isn't on the allowed list
    that the KSP game interface normally lets you pick, then kOS
    will pick whichever :attr:`WARP<TimeWarp:WARP>` value will get you closest
    to the requested rate.  For example::

        // If you do any of these, then the effect is the same:
        set kuniverse:timewarp:rate to 89.
        set kuniverse:timewarp:rate to 145.
        set kuniverse:timewarp:rate to 100.
        // Because the game only allows permanent rates of 1,5,10,50,100,1000, etc.
        // A rate of 100 was the closest match it could allow.

    Note, the game is actually capable of warping at arbitrary rates
    in between these values, and it does so temporarily when transitioning
    to a new warp rate, but it doesn't allow you to hold the rate at those
    in-between values indefintiely.


.. method:: TimeWarp:WARPTO(timestamp)

    :access: Method
    :parameter timestamp: :struct:`Scalar`
    :return: None

    Call this method to warp time forward to a universal time stamp.
    The argument you pass in should be a universal timestamp in seconds.
    Example: To warp 120 seconds into the future:
    ``kuniverse:timewarp:warpto(time:seconds + 120)``.

    Obviously this alters the values of :attr:`WARP<TimeWarp:WARP>` and :attr:`RATE<TimeWarp:RATE>` while
    the warping is happening.


.. method:: TimeWarp:CANCELWARP()

    :access: Method
    :return: None

    Call this method to cancel any active warp.  This will both interupt any
    current automated warp (such as one using :meth:`WARPTO<TimeWarp:WARPTO>`
    or the "Warp Here" user interface) and a manual warp setting (as if you had
    used the ``SET WARP TO 0.`` command).

.. attribute:: TimeWarp:PHYSICSDELTAT

    :access: Get
    :type: :struct:`Scalar`

    Physics Delta-T.  How much time *should* pass between ticks.  Note this is
    not the *actual* time that has passed.  For that you should query
    :attr:`time:seconds <timespan:seconds>` regularly and store the timestamps it
    returns, and compare those timestamps.  This value is just the "expected"
    physics delta-T that you *should* get if everything is running smoothly
    and your computer can keep up with everything the game is doing.

    This value changes depending on your physics warp.  Note, if you query it
    during on-rails warping, it can return some very strange values you
    shouldn't trust.

.. attribute:: TimeWarp:ISSETTLED

    :access: Get
    :type: :struct:`Boolean`

    When you have just changed the warp speed, the game takes time to
    "catch up" and achieve the new desired speed.  (i.e. if you change your
    rate from 100x up to 1000x, and you look at the screen, you will see
    the numbers in the display saying things like "Warp 123x" then "Warp 344x"
    then "Warp 432x", etc.  There are several "ticks" during which the warp
    hasn't yet achieved the desired 1000x level.)  This can take a "long"
    time in computer terms to happen.

    You can query this value to find out whether or not the actual warp
    rate has finally settled on the desired amount yet.

    For example::

        set kuniverse:timewarp:mode to "RAILS".
        set kuniverse:timewarp:rate to 1000.
        print "starting to change warp".
        until kuniverse:timewarp:issettled {
            print "rate = " + round(rate,1).
            wait 0.
        }
        print "warp is now 1000x".

        // The above would output something like this to the screen:
        starting to change warp.
        rate = 113.5
        rate = 143.2
        rate = 213.1
        rate = 233.2
        rate = 250.0
        rate = 264.1
        rate = 301.5
        rate = 320.5
        rate = 361.5
        rate = 391.3
        rate = 421.5
        rate = 430.0
        rate = 450.5
        rate = 471.5
        rate = 490.1
        rate = 501.5
        rate = 613.5
        rate = 643.2
        rate = 713.1
        rate = 733.2
        rate = 750.0
        rate = 764.1
        rate = 801.5
        rate = 820.5
        rate = 861.5
        rate = 891.3
        rate = 921.5
        rate = 930.0
        rate = 950.5
        rate = 971.5
        rate = 990.1
        rate = 1000
        warp is now 1000x.

Backward Compatible Time Warping
--------------------------------

    Time warping is accomplished using the :struct:`TimeWarp` structure
    described above on this page.

    But, for backward compatibility, the following shortcut bound names exist
    as aliases for the functionality in the :struct:`TimeWarp` structure.

.. global:: WARPMODE

    This is identical to :attr:`MODE<TimeWarp:MODE>` above.
    ::

        // These two do the same thing:
        SET WARPMODE TO "PHYSICS".
        SET KUNIVERSE:TIMEWARP:MODE TO "PHYSICS".

        // These two do the same thing:
        SET WARPMODE TO "RAILS".
        SET KUNIVERSE:TIMEWARP:MODE TO "RAILS".

.. global:: WARP

    This is identical to :attr:`WARP<TimeWarp:WARP>` above.
    ::

        // These do the same thing:
        SET WARP TO 3.
        SET KUNIVERSE:TIMEWARP:WARP to 3.

.. function:: WARPTO(timestamp)

    This is identical to :meth:`WARPTO<TimeWarp:WARPTO>` above.
    ::

        // These two do the same thing:
        WARPTO(time:seconds + 60*60). // warp 1 hour into the future.
        KUNIVERSE:TIMEWARP:WARPTO(time:seconds + 60*60).
