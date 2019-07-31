.. _time:
.. _timestamp:

(For the documentation on controlling time **warp**,
please see the :struct:`timewarp` page.  This page is the
documentation on the structure that holds an individual
timestamp representing some universal moment in time.)

Time Span
=========

In several places the game uses a :struct:`TimeSpan` format. This is a structure that gives the time in various formats. It also allows you to perform arithmetic on the time.

TimeSpan represents *SIMULATED* time
------------------------------------

When you are examining a :struct:`TimeSpan` you are looking at the
"in character" **simulated** time, not the "out of character" real
world time. This is a very important distinction to remember, as
the following points illustrate:

-  A :struct:`TimeSpan` does not count the time that was passing while the game was paused.
-  If you turn off your computer and don't play the game for several days, the :struct:`TimeSpan` does not count this time.
-  If your game lags and stutters such that the simulation is taking 2 seconds of real time to calculate 1 second of game time, then the number of seconds that have passed according to a :struct:`TimeSpan` will be fewer than the number of seconds that have passed in the real world.

This allows you to use a :struct:`TimeSpan` such as is returned by the :global:`TIME` special variable to make correct physics calculations.

Built-in function TIME
----------------------

.. function:: TIME(universal_time)

    :parameter universal_time: (:struct:`Scalar`)
    :return: A :struct`TimeSpan` of the time represented by the seconds timestamp passed in.
    :rtype: :struct:`TimeSpan`

    This creates a :struct:`TimeSpan` given a "universal time",
    which is a number of seconds since the current game began,
    IN GAMETIME.  example: ``TIME(3600)`` will give you a
    :struct:`TimeSpan` representing the moment exactly 1 hour
    (3600 seconds) since the current game first began.

    The parameter is OPTIONAL.  If you leave it off,
    and just call ``TIME()``, then you end up getting
    the current time, which is the same thing that :global:`TIME`
    gives you (without the parentheses).

Special variable TIME
---------------------

.. global:: TIME

    :access: Get only
    :type: :struct:`TimeSpan`

    The special variable :global:`TIME` is used to get the current time
    in the gameworld (not the real world where you're sitting in a chair
    playing Kerbal Space Program.)  It is the same thing as calling
    :func:`TIME` with empty parentheses.

Using a TimeSpan
----------------

    Any time you perform arithmetic on a :global:`TIMESPAN` you get a result back that is also a :struct:`TimeSpan`. In other words, :global:`TIME` is a :struct:`TimeSpan`, but ``TIME + 100`` is also a :struct:`TimeSpan`.

    Note that Kerbals do not have the concept of "months"::

        TIME                // Gets the current universal time
        TIME:CLOCK          // Universal time in H:M:S format(1:50:26)
        TIME:CALENDAR       // Year 1, day 134
        TIME:YEAR           // 1
        TIME:DAY            // 134 : changes depending on KUNIVERSE:HOURSPERDAY
        TIME:HOUR           // 1
        TIME:MINUTE         // 50
        TIME:SECOND         // 26
        TIME:SECONDS        // Total Seconds since campaign began

    Note that the notion of "how many hours in a day" and "how many days in a year"
    depends on the gameworld, not our real world.  Kerbin has a shorter day, and
    a longer year in days as a result, than Earth.  But there is an option in
    KSP's main settings screen that can toggle this notion, and kOS will use
    whatever option you set it to.

    Also note that the mods that alter the calendar for other solar systems,
    if they inject changes into KSP's main game, will cause these values to
    change too.

.. highlight:: kerboscript

Using TIME or TIME() to detect when the physics have been updated 'one tick'
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The game will make an effort to maintain regular physics updates at a fixed rate (defaulting to 25 updates per second), sacrificing animation rate as necessary.  When the game is unable to maintain regular updates at this rate, the clock time (in the upper left of the screen) will turn yellow or red instead of green.

You can use the time reported by :global:`TIME` to detect whether or not a real physics 'tic' has occurred yet, which can be important for scripts that need to take measurements from the simulated universe. If no physics tic has occurred, then :global:`TIME` will still be exactly the same value.

.. warning::

    Please be aware that the kind of calendar :struct:`TimeSpan`'s use will depend on your KSP settings. The main KSP game supports both Kerbin time and Earth time and changing that setting will affect how :struct:`TimeSpan` works in kOS.

    The difference is whether 1 day = 6 hours or 1 day = 24 hours.

    You can access this setting from your script by using
    :attr:`Kuniverse:HOURSPERDAY`.

.. warning::

    Beware the pitfall of confusing the :attr:`TimeSpan:SECOND` (singular) suffix with the :attr:`TimeSpan:SECONDS` (plural) suffix.

    :attr:`TimeSpan:SECOND`

        This is the **whole** number of **remainder** seconds leftover after all whole-number minutes, hours, days, and years have been subtracted out, and it's never outside the range [0..60). It's essentially the 'seconds hand' on a clock.

    :attr:`TimeSpan:SECONDS`

        This is the number of seconds total if you want to represent time as just a simple flat number without all the components. It's the total count of the number of seconds since the beginning of time (Epoch). Because it's a floating point number, it can store times less than 1 second. Note this is a measure of how much simulated Kerbal time has passed since the game began. People experienced at programming will be familiar with this concept. It's the Kerbal's version of "unix time".

        The epoch (time zero) in the KSP game is the time at which you first started the new campaign. All campaign games begin with the planets in precisely the same position and the clock set to zero years, zero days, zero hours, and so on.


.. structure:: TimeSpan

    .. list-table::
        :header-rows: 1
        :widths: 1 1 4

        * - Suffix
          - Type
          - Description


        * - :attr:`CLOCK`
          - :struct:`String`
          - "HH:MM:SS"
        * - :attr:`CALENDAR`
          - :struct:`String`
          - "Year YYYY, day DDD"
        * - :attr:`SECOND`
          - :struct:`Scalar` (0-59)
          - Second-hand number
        * - :attr:`MINUTE`
          - :struct:`Scalar` (0-59)
          - Minute-hand number
        * - :attr:`HOUR`
          - :struct:`Scalar` (0-5)
          - Hour-hand number
        * - :attr:`DAY`
          - :struct:`Scalar` (1-426)
          - Day-hand number
        * - :attr:`YEAR`
          - :struct:`Scalar`
          - Year-hand number
        * - :attr:`SECONDS`
          - :struct:`Scalar` (fractional)
          - Total Seconds since Epoch (includes fractional partial seconds)


.. note::

    This type is serializable.


.. attribute:: TimeSpan:CLOCK

    :access: Get only
    :type: :struct:`String`

    Time in (HH:MM:SS) format.

.. attribute:: TimeSpan:CALENDAR

    :access: Get only
    :type: :struct:`String`

    Day in "Year YYYY, day DDD" format. (Kerbals don't have 'months'.)

.. attribute:: TimeSpan:SECOND

    :access: Get only
    :type: :struct:`Scalar` (0-59)

    Second-hand number.

.. attribute:: TimeSpan:MINUTE

    :access: Get only
    :type: :struct:`Scalar` (0-59)

    Minute-hand number

.. attribute:: TimeSpan:HOUR

    :access: Get only
    :type: :struct:`Scalar` (0-5) or (0-23)

    Hour-hand number. Kerbin has six hours in its day.

.. attribute:: TimeSpan:DAY

    :access: Get only
    :type: :struct:`Scalar` (1-426) or (1-356)

    Day-hand number. Kerbin has 426 days in its year.

.. attribute:: TimeSpan:YEAR

    :access: Get only
    :type: :struct:`Scalar`

    Year-hand number

.. attribute:: TimeSpan:SECONDS

    :access: Get only
    :type: :struct:`Scalar` (float)

    Total Seconds since Epoch.  Epoch is defined as the moment your
    current saved game's universe began (the point where you started
    your campaign).  Can be very precise.
