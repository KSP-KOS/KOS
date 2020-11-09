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

TimeSpan has two modes: "one" mode and "zero" mode
--------------------------------------------------

By default, a :struct:`TimeSpan` will use a convention where the
year starts at 1, and the day starts at 1, such there there is
no such thing as Year 0, and Day 0.  This is how the Kerbal game
itself shows times, and it matches how our Gregorian Calendar works
(which was invented when Europe hadn't adapted the number zero yet).

However, if you would prefer it to calculate times starting at
year 0, and day 0, which is often mathematically cleaner, you
can do so by setting its boolean suffix :attr:`zeroMode` to true.

The exact same timespan will print out the clock time differently
depending on how this flag is set, and it will change the meaning
of how you set it when setting its :attr:`year` and :attr:`day`
suffixes.  The :attr:`hour`, :attr:`minute`, and :attr:`second`
suffixes are unaffected by the flag, as they always start from
zero anyway, even in the stock game.

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

Built-in function TIMESPAN
--------------------------

.. function:: TIMESPAN(year, day, hour, minute, second, zeroMode)

    :parameter year: the year portion of the time.  *Meaning changes
    depending on zeroMode*
    :parameter day: the day portion of the time.  *Meaning changes
    depending on zeroMode*
    :parameter hour: the hour portion of the time.
    :parameter minute: the minute portion of the time.
    :parameter second: the second portion of the time.
    :parameter zeroMode: boolean flag - true if you want this to use
    zero-based time.
    :return: A new :struct:`TimeSpan` of the time represented by the
    values passed in.
    :rtype: :struct:`TimeSpan`

    This is a way to create a :struct:`TimeSpan` by passing in all the
    values individually for year, day, hour, minute, second.

    **Important** The meaning of the ``year`` and ``day`` parameters is
    *different* depending on what you pass in for the ``zeroMode`` boolean
    value.  If you pass in ``zeroMode`` set to ``false``, then the timespan
    created will assume you are giving it the year and the day both 
    in a notation that starts counting at 1.  If you pass in ``zeroMode`` set
    to ``true``, then the timespan will assume you are giving it the year
    and the day both in a notation that starts counting at 0.  In other
    words, ``TIMESPAN(5,4,0,0,0,false)``, which starts counting years and
    days at 1, is equal to ``TIMESPAN(4,3,0,0,0,true)``, which starts counting
    years and days at zero.  If you print out their :attr:``seconds``
    values to see their true time, you'll see they're the same thing.

    The other three parameters, ``hour``, ``minute``, and ``second``, always
    count from zero and are unaffected by the ``zeroMode`` flag.

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
        :widths: 1 1 1 4

        * - Suffix
          - Type
          - Access
          - Description


        * - :attr:`CLOCK`
          - :struct:`String`
          - Get Only
          - A string formatted like so: "HH:MM:SS"
        * - :attr:`CALENDAR`
          - :struct:`String`
          - Get Only
          - A string formatted like so: "Year Y, day D"
        * - :attr:`SECOND`
          - :struct:`Scalar` (0-59)
          - Get/Set
          - Second-hand number
        * - :attr:`MINUTE`
          - :struct:`Scalar` (0-59)
          - Get/Set
          - Minute-hand number
        * - :attr:`HOUR`
          - :struct:`Scalar` (0-5)
          - Get/Set
          - Hour-hand number
        * - :attr:`DAY`
          - :struct:`Scalar`
          - Get/Set
          - Day number - affected by :attr:`zeroMode`
        * - :attr:`YEAR`
          - :struct:`Scalar`
          - Get/Set
          - Year number - affected by :attr:`zeroMode`
        * - :attr:`SECONDS`
          - :struct:`Scalar` (fractional)
          - Get/Set
          - Total Seconds since Epoch (includes fractional partial seconds)
        * - :attr:`ZEROMODE`
          - :struct:`Boolean`
          - Get/Set
          - Set True to make :attr:`YEAR` and :attr:`DAY` count from 0, not 1.


.. note::

    This type is serializable.


.. attribute:: TimeSpan:CLOCK

    :access: Get only
    :type: :struct:`String`

    Time in (HH:MM:SS) format.

.. attribute:: TimeSpan:CALENDAR

    :access: Get only
    :type: :struct:`String`

    Day in "Year Y, day D" format. (Kerbals don't have 'months'.)
    Note that the meaning of this is *different depending on how
    you set :attr:`zeroMode`.*

.. attribute:: TimeSpan:SECOND

    :access: Get/Set
    :type: :struct:`Scalar` (0-59)

    Second-hand number. If you SET this, it is like you are affecting
    ONLY the second-hand of the time, leaving the year, day, hour,
    and minute as they are.  (In reality the :struct:`TimeSpan` only
    really remembers times as a single timestamp in seconds since
    the game started, but what setting this will do is perform the
    arithmentic behind the scenes to behave as if you only moved
    the second hand.)

    Unaffected by :attr:`zeroMode`. It always counts from zero no
    matter what.

.. attribute:: TimeSpan:MINUTE

    :access: Get/Set
    :type: :struct:`Scalar` (0-59)

    Minute-hand number. If you SET this, it is like you are affecting
    ONLY the minute-hand of the time, leaving the year, day, hour,
    and second as they are.  (In reality the :struct:`TimeSpan` only
    really remembers times as a single timestamp in seconds since
    the game started, but what setting this will do is perform the
    arithmentic behind the scenes to behave as if you only moved
    the minute hand.)

    Unaffected by :attr:`zeroMode`. It always counts from zero no
    matter what.

.. attribute:: TimeSpan:HOUR

    :access: Get/Set
    :type: :struct:`Scalar` (0-5) or (0-23)

    Hour-hand number. Kerbin has six hours in its day, but
    you may be using a 24-hour clock anyway if your game
    settings are set that way.

    If you set this, it is like you are affecting
    ONLY the hour-hand of the time, leaving the year, day, minute
    and second as they are.  (In reality the :struct:`TimeSpan` only
    really remembers times as a single timestamp in seconds since
    the game started, but what setting this will do is perform the
    arithmentic behind the scenes to behave as if you only moved
    the hour hand.)

    Unaffected by :attr:`zeroMode`. It always counts from zero no
    matter what.

.. attribute:: TimeSpan:DAY

    :access: Get/Set
    :type: :struct:`Scalar` (1-426) or (1-356)

    Day-hand number. Kerbin has 426 days in its year.

    *Affected by :attr:`zeroMode`.*  If :attr:`zeroMode` is true,
    then this uses a reckoning where the day starts at zero,
    otherwise it starts at one.

    If you set this, You are affecting ONLY the day, leaving
    the year, hour, minute, and second as they are.
    (In reality the :struct:`TimeSpan` only
    really remembers times as a single timestamp in seconds since
    the game started, but what setting this will do is perform the
    arithmentic behind the scenes to behave as if you only moved
    the day of the year, leaving the rest as it is.)

.. attribute:: TimeSpan:YEAR

    :access: Get only
    :type: :struct:`Scalar`

    Year-hand number

    *Affected by :attr:`zeroMode`.*  If :attr:`zeroMode` is true,
    then this uses a reckoning where the year starts at zero,
    otherwise it starts at one.  (In reality the :struct:`TimeSpan` only
    really remembers times as a single timestamp in seconds since
    the game started, but what setting this will do is perform the
    arithmentic behind the scenes to behave as if you only shifted
    the year, to the same day, hour, minute, and second in a diferent
    year.)

.. attribute:: TimeSpan:SECONDS

    :access: Get/Set
    :type: :struct:`Scalar` (float)

    Total Seconds since Epoch.  Epoch is defined as the moment your
    current saved game's universe began (the point where you started
    your campaign).  Can be very precise.

    Not affected by :attr:`zeroMode`.

.. attribute:: TimeSpan:ZEROMODE

    :access: Get/Set
    :type: :struct:`Boolean`

    *Changes the meaning of the :attr:`Year` and :attr:`Day` suffixes,
    and changes the string returned by :attr:`Clock` and :attr:`Calendar`
    too.*

    In Kerbal Space Program, as in the human world, the people in
    ancient history who invented the calendar in use today didn't
    use a numbering system that had a concept of "zero".  Therefore
    the calandar starts with "year 1" and "day 1", even though
    that's mathematically icky.  You can see this in the game when
    you start a brand new career and the space center shows a time
    of "year 1, day 1, 00:00:00" on the game screen, rather than
    the somewhat more mathematically clean "year 0, day 0, 00:00:00".

    The kOS :struct:`TimeSpan` type uses that same start-at-one
    convention by default, in order to match what the game shows.

    But you may prefer to see the year and the day starting at zero,
    just like the hour, minute, and second do.  If you set this
    suffix on a :struct:`TimeSpan` to true, you cause that
    :struct:`TimeSpan` to switch to a zero-based convention for
    year and day.

    This means the following two are actually the exact
    same timespan, just with different conventions::

      set t1 to time(0).
      set t1:zeroMode to false. // year and day start with 1.
      set t1:year to 5.
      set t1:day to 10.
      set t1:hour to 3.
      set t1:minute to 0.
      set t1:second to 0.
      print "t1's calendar date, counting from 1 is: " + t1:calendar.
      print "t1's seconds since epoch is: " + t1:seconds.

      set t2 to time(0).
      set t2:zeroMode to true. // year and day start with 0.
      set t2:year to 4.
      set t2:day to 9.
      set t2:hour to 3.
      set t2:minute to 0.
      set t2:second to 0.
      print "t2's calendar date, counting from 0 is: " + t2:calendar.
      print "t2's seconds since epoch is " + t2:seconds.

      print "Note that t1 and t2 are really the same time,".
      print "Which becomes apparent when you look at their".
      print "Seconds timestamps.".
    
