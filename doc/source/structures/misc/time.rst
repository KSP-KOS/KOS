.. _time:
.. _timestamp:

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

This allows you to use a :struct:`TimeSpan` such as is returned by the TIME special variable to make correct physics calculations.

Special variable TIME
---------------------

.. global:: TIME

    :access: Get only
    :type: :struct:`TimeSpan`

    The special variable **TIME** is used to get the current time.

    Any time you perform arithmetic on **TIME** you get a result back that is also a :struct:`TimeSpan`. In other words, TIME is a :struct:`TimeSpan`, but TIME + 100 is also a :struct:`TimeSpan`.

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

Using TIME to detect when the physics have been updated 'one tick'
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

kOS programs run however fast your computer's animation rate will allow, which can flow and change from one moment to the next depending on load. However, the physics of the universe get updated at a fixed rate according to your game settings (the default, as of KSP 0.25, is 25 physics updates per second)

You can use the TIME special variable to detect whether or not a real physics 'tic' has occurred yet, which can be important for scripts that need to take measurements from the simulated universe. If no physics tic has occurred, then TIME will still be exactly the same value.

.. warning::

    Please be aware that the kind of calendar :struct:`TimeSpan`'s use will depend on your KSP settings. The main KSP game supports both Kerbin time and Earth time and changing that setting will affect how :struct:`TimeSpan` works in kOS.

    The difference is whether 1 day = 6 hours or 1 day = 24 hours.

    You can access this setting from your script by using
    :attr:`Kuniverse:HOURSPERDAY`.

.. warning::

    Beware the pitfall of confusing the :attr:`TimeSpan:SECOND` (singular) suffix with the :attr:`TimeSpan:SECONDS` (plural) suffix.

    :attr:`TimeSpan:SECOND`

        This is the number of **remainder** seconds leftover after all whole-number minutes, hours, days, and years have been subtracted out, and it's never outside the range [0..60). It's essentially the 'seconds hand' on a clock.

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
          - string
          - "HH:MM:SS"
        * - :attr:`CALENDAR`
          - string
          - "Year YYYY, day DDD"
        * - :attr:`SECOND`
          - integer (0-59)
          - Second-hand number
        * - :attr:`MINUTE`
          - integer (0-59)
          - Minute-hand number
        * - :attr:`HOUR`
          - integer (0-5)
          - Hour-hand number
        * - :attr:`DAY`
          - integer (1-426)
          - Day-hand number
        * - :attr:`YEAR`
          - integer
          - Year-hand number
        * - :attr:`SECONDS`
          - Number (float)
          - Total Seconds since Epoch


.. note::

    This type is serializable.


.. attribute:: TimeSpan:CLOCK

    :access: Get only
    :type: string

    Time in (HH:MM:SS) format.

.. attribute:: TimeSpan:CALENDAR

    :access: Get only
    :type: string

    Day in "Year YYYY, day DDD" format. (Kerbals don't have 'months'.)

.. attribute:: TimeSpan:SECOND

    :access: Get only
    :type: integer (0-59)

    Second-hand number.

.. attribute:: TimeSpan:MINUTE

    :access: Get only
    :type: integer (0-59)

    Minute-hand number

.. attribute:: TimeSpan:HOUR

    :access: Get only
    :type: integer (0-5) or (0-23)

    Hour-hand number. Kerbin has six hours in its day.

.. attribute:: TimeSpan:DAY

    :access: Get only
    :type: integer (1-426) or (1-356)

    Day-hand number. Kerbin has 426 days in its year.

.. attribute:: TimeSpan:YEAR

    :access: Get only
    :type: integer

    Year-hand number

.. attribute:: TimeSpan:SECONDS

    :access: Get only
    :type: Number (float)

    Total Seconds since Epoch.  Epoch is defined as the moment your 
    current saved game's universe began (the point where you started
    your campaign).  Can be very precise.


