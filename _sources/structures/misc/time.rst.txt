.. _time:

(For the documentation on controlling time **warp**,
please see the :struct:`timewarp` page.  This page is the
documentation on the structure that holds an individual
timestamp representing some universal moment in time.)

Time
====

kOS provides two types you can use to work with game-time.  One is
the :struct:`TimeStamp` and the other is the :struct:`TimeSpan`.
This page describes both, and the differences between them.

.. _timestamp_timespan_diff:

**Similarities between ``TimeStamp`` and ``TimeSpan``**:
Both :struct:`TimeStamp` and :struct:`TimeSpan` store an amount
of time, and can show you that time divided up into years, days,
hours, minutes, and seconds.

**Differences between ``TimeStamp`` and ``TimeSpan``**:
The diference is that :struct:`TimeStamp` is for storing a single
point in time, while :struct:`TimeSpan` is for
storing an offset, or duration of time.  Where they differ is in
what it means to call something "year 1" or "day 1".  ``TimeStamp``
is like a calendar moment.  The first day of the year is called day
1, and the first year of the game is called year 1.  ``TimeSpan``,
because it measures a duration of time, counts starting at zero
years and zero days.  Had Kerbin had a more messy calendar like
Earth's Gregorian calendar with its dissimilar months some of which
are 30 days and some are 31 or occasionally 28 or 29, there would have
been even more differences between the two, but thankfully Kerbals don't
reckon things in months or weeks, so the only big difference is whether
you start counting at 1 or at 0 for years and days.

Mixing TimeStamp with TimeSpan
------------------------------

There are rules for how you can mix and match :struct:`TimeStamp` and 
:struct:`TimeSpan` in artithmetic and boolean comparisons.  (For example
if you add a :struct:`TimeSpan` to a :struct:`TimeStamp` you get a new
:struct:`TimeStamp`.)  The full rules for these operations are listed
in the :ref:`Time operators <time_operators>` section further down this page.

.. _universal_time:

Timestamps as Scalar Universal time (seconds since Epoch)
---------------------------------------------------------

There are a number of places in kOS where a function or suffix expects
a time to be expressed as a simple scalar rather than as a
:struct:`TimeStamp`.  When you come across these places what it means
is that you simply use the number of seconds since the game clock began.
(This is similar to the concept of "unix time" in real world computing,
if that's a term you've heard of.)  For example, if you mean
"Exactly 1 hour in the gameworld after the campaign began" you'd use
a value of 3600 since there's 3600 seconds in one hour.  If you mean
"Exactly 1 year and 20 seconds after the campaign began" you'd use
a value of 9201620, since there's 9201600 seconds in a Kerbal year
(9201600 = 60 * 60 * 6 * 426), plus 20 more seconds on top of that.

You may easily convert to/from this type of time and the type
:struct:`TimeStamp` as follows:

    * :attr:`TimeStamp:SECONDS` converts a :struct:`TimeStamp` into
      a :struct:`Scalar` universal time.
    * :func:`TimeStamp(universal_time)` converts a :struct:`Scalar`
      universal time into a :struct:`TimeStamp`.

.. _timestamp:

TimeStamp
=========

In several places the game uses a :struct:`TimeStamp` format. This is a
structure that gives the time in various formats. In combination with
:struct:`TimeSpan` it also allows you to perform arithmetic on the time.

TimeStamp represents *SIMULATED* time
-------------------------------------

When you are examining a :struct:`TimeSpan` you are looking at the
"in character" **simulated** time, not the "out of character" real
world time. This is a very important distinction to remember, as
the following points illustrate:

-  A :struct:`TimeSpan` does not count the time that was passing while the game was paused.
-  If you turn off your computer and don't play the game for several days, the :struct:`TimeSpan` does not count this time.
-  If your game lags and stutters such that the simulation is taking 2 seconds of real time to calculate 1 second of game time, then the number of seconds that have passed according to a :struct:`TimeSpan` will be fewer than the number of seconds that have passed in the real world.

This allows you to use a :struct:`TimeSpan` such as is returned by the :global:`TIME` special variable to make correct physics calculations.

Built-in function TIMESTAMP
---------------------------

.. function:: TIMESTAMP(universal_time)

    :parameter universal_time: (:struct:`Scalar`)
    :return: A :struct`TimeStamp` of the time represented by the seconds passed in.
    :rtype: :struct:`TimeStamp`

    This creates a :struct:`TimeStamp` given a "universal time",
    which is a number of seconds since the current game began,
    IN GAMETIME.  example: ``TIME(3600)`` will give you a
    :struct:`TimeSpan` representing the moment exactly 1 hour
    (3600 seconds) since the current game first began.

    The parameter is OPTIONAL.  If you leave it off,
    and just call ``TIMESTAMP()``, then you end up getting
    the current time, which is the same thing that :global:`TIME`
    gives you (without the parentheses).

.. function:: TIMESTAMP(year,day,hour,min,sec)

    :parameter year: (:struct:`Scalar`)
    :parameter day: (:struct:`Scalar`)
    :parameter hour: (:struct:`Scalar`) [optional]
    :parameter min: (:struct:`Scalar`) [optional]
    :parameter sec: (:struct:`Scalar`) [optional]
    :return: A :struct`TimeStamp` of the time represented by the values passed in.
    :rtype: :struct:`TimeStamp`

    This creates a :struct:`TimeStamp` given a year, day, hour-hand,
    minute-hand, and second-hand.

    Because a :struct:`TimeStamp` is a calendar reckoning, the values
    you use for the year and the day should start counting at 1, not
    at 0.  (The hour, minute, and second still start at zero).

    In other words::
    
      // Notice these are equal because year and day start at 1 not 0:
      set t1 to TIMESTAMP(0).
      set t2 to TIMESTAMP(1,1,0,0,0).
      print t1:full.
      print t2:full. // Prints same as above.

    Note that the year and day are mandatory, but the remaining
    parameters are optional and if you leave them off it assumes you
    meant them to be zero (meaning it will give you a timestamp at
    the very start of that date, right at midnight 0:00:00 O'clock).

.. function:: TIME(universal_time)

    :parameter universal_time: (:struct:`Scalar`)
    :return: A :struct`TimeStamp`
    :rtype: :struct:`TimeStamp`

    This is an alias that means the same thing as
    :func:`TIMESTAMP(universal_time)`.  It exists to support older scripts
    written before this was renamed to ``TIMESTAMP()``.


Special variable TIME
---------------------

.. global:: TIME

    :access: Get only
    :type: :struct:`TimeStamp`

    The special variable :global:`TIME` is used to get the current time
    in the gameworld (not the real world where you're sitting in a chair
    playing Kerbal Space Program.)  It is the same thing as calling
    :func:`TIME` with empty parentheses.

Kerbal Calendar Differs From Earth's
------------------------------------

    Note that the notion of "how many hours in a day" and "how many days in
    a year" depends on the gameworld, not our real world.  Kerbin has a
    shorter day (6 hours) than Earth, and 426 of these days make up a Kerbin
    year. But there is an option in KSP's main settings screen
    that can toggle whether the game counts with Kerbin days (6 hours per day)
    or Earth days (24 hours per day). kOS will use whatever
    option you set it to alter the meaning of the Day suffix of a
    :struct:`TIMESTAMP` and a :struct:`TIMESPAN`.  You can see what
    the length of a day in the calendar is set to by reading
    :attr:`Kuniverse:HOURSPERDAY`.

    Also note that the mods that alter the calendar for other solar systems,
    if they inject changes into KSP's main game, can cause these values to
    change too.

.. highlight:: kerboscript

Using TIME or TIME() to detect when the physics have been updated 'one tick'
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The game will make an effort to maintain regular physics updates at a fixed rate (defaulting to 50 updates per second), sacrificing animation rate as necessary.  When the game is unable to maintain regular updates at this rate, the clock time (in the upper left of the screen) will turn yellow or red instead of green.

You can use the time reported by :global:`TIME` to detect whether or not a real physics 'tic' has occurred yet, which can be important for scripts that need to take measurements from the simulated universe. If no physics tic has occurred, then :global:`TIME` will still be exactly the same value.

TimeStamp: Difference between SECOND and SECONDS
------------------------------------------------

.. warning::

    Beware the pitfall of confusing the :attr:`TimeStamp:SECOND` (singular) suffix with the :attr:`TimeStamp:SECONDS` (plural) suffix.

    :attr:`TimeStamp:SECOND`

        This is the **whole** number of **remainder** seconds leftover after all whole-number minutes, hours, days, and years have been subtracted out, and it's never outside the range [0..60). It's essentially the 'seconds hand' on a clock.

    :attr:`TimeStamp:SECONDS`

        This is the number of seconds total if you want to represent time as just a simple flat number without all the components. It's the total count of the number of seconds since the beginning of time (Epoch). Because it's a floating point number, it can store times less than 1 second. Note this is a measure of how much simulated Kerbal time has passed since the game began. People experienced at programming will be familiar with this concept. It's the Kerbal's version of "unix time".

        The epoch (time zero) in the KSP game is the time at which you first started the new campaign. All campaign games begin with the planets in precisely the same position and the clock set to zero years, zero days, zero hours, and so on.


.. structure:: TimeStamp

TimeStamp Structure
-------------------

    .. list-table::
        :header-rows: 1
        :widths: 1 1 4

        * - Suffix
          - Type
          - Description


        * - :attr:`FULL`
          - :struct:`String`
          - The full attr`:CALENDAR` and :attr`:CLOCK` as one string.
        * - :attr:`CLOCK`
          - :struct:`String`
          - "HH:MM:SS"
        * - :attr:`CALENDAR`
          - :struct:`String`
          - "__y__d" format (i.e. "2y31d")
        * - :attr:`YEAR`
          - :struct:`Scalar`
          - Year-hand number
        * - :attr:`DAY`
          - :struct:`Scalar` (range varies by universe)
          - Day-hand number
        * - :attr:`HOUR`
          - :struct:`Scalar` (0-5) or (0-23) depending
          - Hour-hand number
        * - :attr:`MINUTE`
          - :struct:`Scalar` (0-59)
          - Minute-hand number
        * - :attr:`SECOND`
          - :struct:`Scalar` (0-59)
          - Second-hand number
        * - :attr:`SECONDS`
          - :struct:`Scalar` (fractional)
          - Total Seconds since Epoch (includes fractional partial seconds)


.. note::

    This type is serializable.


.. attribute:: TimeStamp:FULL

    :access: Get only
    :type: :struct:`String`

    The full string for the timestamp. (Down to the second anyway.  Fractions of
    seconds not shown), including year, day, hour, minute, and second.
    The format is:

    ``Year XX Day XX HH:MM:SS``

.. attribute:: TimeStamp:CLOCK

    :access: Get only
    :type: :struct:`String`

    Time in (HH:MM:SS) format.  Does not show which year or day.

.. attribute:: TimeStamp:CALENDAR

    :access: Get only
    :type: :struct:`String`

    Date in ``Year XX Day XX`` format.

.. attribute:: TimeStamp:YEAR

    :access: Get only
    :type: :struct:`Scalar`

    Year-hand number.  Note that the first year of the game, at "epoch"
    time is actullay year 1, not year 0.

.. attribute:: TimeStamp:DAY

    :access: Get only
    :type: :struct:`Scalar` (range varies by universe)

    Day-hand number. Kerbin has 426 days in its year if using Kerbin's
    6 hour day (one fourth of that if :attr:`Kuniverse:HOURSPERDAY` is
    24 meaning the game is configured to show Earthlike days not Kerbin
    days.)

    Also note that with mods installed you might not be looking at
    the stock universe, which could change the range this field could
    be if it changes how long a year is in your solar system.

    Note that the first day of the year is actually day 1, not day 0.

.. attribute:: TimeStamp:HOUR

    :access: Get only
    :type: :struct:`Scalar` (0-5) or (0-23)

    Hour-hand number.  Note the setting :attr:`Kuniverse:HOURSPERDAY` affects
    whether this will be a number from 0 to 5 (6 hour day) or a number
    from 0 to 23 (24 hour day).

.. attribute:: TimeStamp:MINUTE

    :access: Get only
    :type: :struct:`Scalar` (0-59)

    Minute-hand number

.. attribute:: TimeStamp:SECOND

    :access: Get only
    :type: :struct:`Scalar` (0-59)

    Second-hand number.

.. attribute:: TimeStamp:SECONDS

    :access: Get only
    :type: :struct:`Scalar` (float)

    Total Seconds since Epoch.  Epoch is defined as the moment your
    current saved game's universe began (the point where you started
    your campaign).  Can be very precise to the current "physics tick"
    and store values less than one second.  (i.e. a typical value
    might be something like 50402.103019 seconds).  Please note
    that if you print this in a loop again and again it will appear
    to be "frozen" for a bit before moving in discrete jumps.  This
    reflects the fact that Kerbal Space Program is a computer simulated
    world where time advances in discrete chunks, not smoothly.

.. _timespan:

TimeSpan
========

A :struct:`TimeSpan` is like a :struct:`TimeStamp` except that it counts
years starting at zero and days starting at zero, because it represents
an offset of time rather than a fixed single point of time on the
calendar/clock. It has fairly similar suffixes to :struct:`TimeStamp`
but their meaning can be subtly different as should be carefully
examined below in the suffix descriptions.

Constructing 
============

A :struct:`TimeSpan` can be created using built-in functions similar to those
for :struct:`TimeStamp`:

.. function:: TIMESPAN(universal_time)

    :parameter universal_time: (:struct:`Scalar`)
    :return: A :struct`TimeSpan` of the time represented by the seconds passed in.
    :rtype: :struct:`TimeSpan`

    This creates a :struct:`TimeSpan` equal to the number of seconds
    passed in. Fractional seconds are allowed for more precise spans.

    The parameter is OPTIONAL.  If you leave it off, and just call
    ``TIMESPAN()``, then you end up getting a timespan of zero duration.

.. function:: TIMESPAN(year,day,hour,min,sec)

    :parameter year: (:struct:`Scalar`)
    :parameter day: (:struct:`Scalar`)
    :parameter hour: (:struct:`Scalar`) [optional]
    :parameter min: (:struct:`Scalar`) [optional]
    :parameter sec: (:struct:`Scalar`) [optional]
    :return: A :struct`TimeSpan` of the time represented by the values passed in.
    :rtype: :struct:`TimeSpan`

    This creates a :struct:`TimeSpan` that lasts this number of years
    plus this number of days plus this number of hours plus this number
    of minutes plus this number of seconds.

    Because a :struct:`TimeSpan` is NOT a calendar reckoning, but
    an actual duration, the values you use for the year and the day
    should start counting at 0, not at 1.

    In other words::
    
      // Notice these are equal because year and day start at 0 not 1:
      set span1 to TIMESPAN(0).
      set span2 to TIMESPAN(0,0,0,0,0).
      print span1:full.
      print span2:full. // Prints same as above.

    Note that the year and day are mandatory in this function, but the
    remaining parameters are optional and if you leave them off it
    assumes you meant them to be zero (meaning it will give you a
    timespan exactly equal to that many years and days, with no leftover
    hours or minutes or seconds.)

Offsetting TimeStamps with TimeSpans
------------------------------------

The main purpose of a :struct:`TimeSpan` is to be added or subtracted
from a :struct:`TimeStamp`.  The exact rules for these operations
are elsewhere on this page in the :ref:`Time operators <time_operators>`
section.


TimeSpan: Difference between SECOND and SECONDS
-----------------------------------------------

.. warning::

    Beware the pitfall of confusing the :attr:`TimeSpan:SECOND` (singular)
    suffix with the :attr:`TimeSpan:SECONDS` (plural) suffix.

    :attr:`TimeSpan:SECOND`

        This is the **whole** number of **remainder** seconds leftover after all whole-number minutes, hours, days, and years have been subtracted out, and it's never outside the range [0..60). It's essentially the 'seconds hand' on a clock.

    :attr:`TimeSpan:SECONDS`

        This is the number of seconds total if you want to represent the
        span of time as just a simple flat number without all the components.
        It's the total count of the number of seconds within the time span,
        and it can have a fractional component to represent times more precise
        than one second.

.. structure:: TimeSpan

TimeSpan Structure
------------------

    .. list-table::
        :header-rows: 1
        :widths: 1 1 4

        * - Suffix
          - Type
          - Description


        * - :attr:`FULL`
          - :struct:`String`
          - The full time duration split into fields for display.
        * - :attr:`CLOCK`
          - :struct:`String`
          - THIS DOES NOT EXIST
        * - :attr:`CALENDAR`
          - :struct:`String`
          - THIS DOES NOT EXIST
        * - :attr:`YEAR`
          - :struct:`Scalar`
          - Whole number of years in the span.
        * - :attr:`YEARS`
          - :struct:`Scalar`
          - *TOTAL* time in the span expressed in years.
        * - :attr:`DAY`
          - :struct:`Scalar` (range vaires by universe)
          - Whole number of days after the last whole year in the span.
        * - :attr:`DAYS`
          - :struct:`Scalar` 
          - *TOTAL* time in the span expressed in days.
        * - :attr:`HOUR`
          - :struct:`Scalar` (0-5) or (0-23)
          - Whole number of hours after the last whole day in the span.
        * - :attr:`HOURS`
          - :struct:`Scalar`
          - *TOTAL* time in the span expressed in hours.
        * - :attr:`MINUTE`
          - :struct:`Scalar` (0-59)
          - Whole number of minutes after the last whole hour in the span.
        * - :attr:`MINUTES`
          - :struct:`Scalar`
          - *TOTAL* time in the span expressed in minutes.
        * - :attr:`SECOND`
          - :struct:`Scalar` (0-59)
          - Whole number of seconds after the last whole minute in the span.
        * - :attr:`SECONDS`
          - :struct:`Scalar` (fractional)
          - *TOTAL* Seconds since Epoch (includes fractional partial seconds)


.. note::

    This type is serializable.


.. attribute:: TimeSpan:FULL

    :access: Get only
    :type: :struct:`String`

    The full string for the TimeSpan. (Down to the second anyway.  Fractions of
    seconds not shown), including year, day, hour, minute, and second.
    The format is:

    ``_y_d_h_m_s`` (where the underscores are numbers).

.. attribute:: TimeSpan:CLOCK

    :access: Get only
    :type: :struct:`String`

    **DOES NOT EXIST**
    ``TimeSpan`` has no such suffix as ``:CLOCK`` because it might miss the
    important fact that the ``TimeSpan`` is bigger than a day by not showing
    the year and day fields.  Why document this then?  To make it clear
    that this is a difference compared to :struct:`TimeStamp` in case you
    were looking for such a similar suffix for :struct:`TimeSpan`

.. attribute:: TimeSpan:CALENDAR

    :access: Get only
    :type: :struct:`String`

    **DOES NOT EXIST**
    ``TimeSpan`` has no such suffix as ``:CALENDAR` because it might miss the
    important fact that the ``TimeSpan`` has remainder time less than 1 day.
    in the hour, minute, and second fields.  Why document this then?  To make
    it clear that this is a difference compared to :struct:`TimeStamp` in
    case you were looking for such a similar suffix for :struct:`TimeSpan`

.. attribute:: TimeSpan:YEAR

    :access: Get only
    :type: :struct:`Scalar`

    Whole number of Years in the span.  Note that TimeSpan starts
    counting years at 0 not at 1.  This is a difference from how it
    works for :struct:`TimeStamp`

.. attribute:: TimeSpan:YEARS

    :access: Get only
    :type: :struct:`Scalar`

    *TOTAL* time in the span, expressed in units of years.  This is not
    the same as :attr:`TimeSpan:YEAR` because it includes a fractional
    part and is the *entire* span, not just the whole number of years.
    Example: If there are 426 days in a Year, and the Timespan is
    1 year and 213 days long, then this will return ``1.5`` rather than ``1``,
    as the *entire* span is one and a half years.  You can think of this
    as being :attr:`TimeSpan:SECONDS` divided by seconds per year.

.. attribute:: TimeSpan:DAY

    :access: Get only
    :type: :struct:`Scalar` (range varies by universe)

    Whole number of days remaining after the lst full year within the span.
    Kerbin has 426 days in a year if using Kerbin's
    6 hour day (one fourth as much if if :attr:`Kuniverse:HOURSPERDAY`
    is 24 meaning the game is configured to show Earthlike days not
    Kerbin days.

    The range of possible values could be different if you have mods
    installed that replace the stock solar system with a different
    solar system and thus alter how long your homeworld's year is.

    Note that for spans the first day of the year is the zero-th
    day, not the 1-th day.  This is a difference from how it
    works for :struct:`TimeStamp`

.. attribute:: TimeSpan:DAYS

    :access: Get only
    :type: :struct:`Scalar`

    *TOTAL* time in the span, expressed in units of days.  This is not
    the same as :attr:`TimeSpan:DAY` because it includes a fractional
    part and is the *entire* span, not just the whole number of days leftover
    in the last partial year.
    Example: If there are 426 days in a Year, and the Timespan is
    1 year and 213 days and 12 hours long, then this will return ``639.5``
    rather than ``213``, as the *entire* span is 639 and a half days.

.. attribute:: TimeSpan:HOUR

    :access: Get only
    :type: :struct:`Scalar` (0-5) or (0-23)

    Whole number of hours remaining after the last full day in the span.
    Note the setting :attr:`Kuniverse:HOURSPERDAY` affects
    whether this will be a number from 0 to 5 (6 hour day) or a number
    from 0 to 23 (24 hour day).

.. attribute:: TimeSpan:HOURS

    :access: Get only
    :type: :struct:`Scalar`

    *TOTAL* time in the span, expressed in units of hours.  This is not
    the same as :attr:`TimeSpan:HOUR` because it includes a fractional
    part and is the *entire* span, not just the whole number of hours
    leftover in the last partial day.
    Example: If the Timespan is 0 years, 2 days, 3 hours, and 20 minutes,
    and days are 6 hours long, then this will return 15.3333333 since
    the *entire* span is 2 days of 6 hours each, plus 3 more hours, plus 
    20 minutes which is one third of an hour.

.. attribute:: TimeSpan:MINUTE

    :access: Get only
    :type: :struct:`Scalar` (0-59)

    Whole number of minutes remaining after the last full hour in the span.

.. attribute:: TimeSpan:MINUTES

    :access: Get only
    :type: :struct:`Scalar`

    *TOTAL* time in the span, expressed in units of minutes.  This is not
    the same as :attr:`TimeSpan:MINUTE` because it includes a fractional
    part and is the *entire* span, not just the whole number of minutes
    leftover in the last partial hour.
    Example: If the Timespan is 0 years, 0 days, 3 hours, 20 minutes, and
    30 seconds, then this will return ``200.5`` as that is the *entire*
    span: 3 60-minute hours is 180, plus 20 more minutes is 200, plus 30
    seconds which is half a minute gives 200.5.

.. attribute:: TimeSpan:SECOND

    :access: Get only
    :type: :struct:`Scalar` (0-59)

    Whole number of seconds remaining after the last full minute in the span.
    Please note the difference between this and :attr:`TimeSpan:SECONDS`.

.. attribute:: TimeSpan:SECONDS

    :access: Get only
    :type: :struct:`Scalar` (float)

    *TOTAL* Seconds in the TimeSpan, including fractonal part.  Note
    this is NOT the same as :attr:`TimeSpan:SECOND` (singular),
    because this is the total span of time expressed in seconds,
    and not just the leftover seconds in the last minute of the span.

.. _time_operators:

Time Operators
==============

It is possible to mix and match :struct:`TimeStamp` with :struct:`TimeSpan`
operands and :struct:`Scalar` operands in math and comparison operations.

Time Operators - arithmetic
---------------------------

You may subtract (*but NOT add*) two TimeStamps, which
gives a TimeSpan for the interval between the two:

   * a :struct:`TimeStamp` - a :struct:`TimeStamp` = a :struct:`TimeSpan`
   * a :struct:`TimeStamp` + a :struct:`TimeStamp` = Illegal operation.

Example::

  // This sets A = year 1, day 3, hour 1:
  set A to TIMESTAMP(1,3,1,0,0).
  // This sets B = year 1, day 3, hour 2, minute 20:
  set B to TIMESTAMP(1,3,2,20,0).
  // This sets C to the difference between A and B, which is
  // 0 years, 0 days, 1 hour, 20 minutes, 0 seconds
  set C to B - A.
  print C:full. // should print 0y0d1h20m0s

You may add or subtract a TimeSpan and a TimeStamp, which gives a TimeStamp:

   * a :struct:`TimeStamp` + a :struct:`TimeSpan` = a :struct:`TimeStamp`
   * a :struct:`TimeStamp` - a :struct:`TimeSpan` = a :struct:`TimeStamp`

Example::

  // This sets A = right now:
  set A to TIMESTAMP().
  // This sets B = 1 hour and 30 minutes from right now:
  set B to A + TIMESPAN(0,0,1,30,0).
  // This sets C = 1 minute and 45 seconds before B:
  set C to B - TIMESPAN(0,0,0,1,45).

You may add or subtract a Scalar to either TimeStamps or TimeSpans. In either
case, *when adding or subtracting it assumes a Scalar is a number of seconds*:

   * a :struct:`TimeStamp` + a :struct:`Scalar` = a :struct:`TimeStamp`
   * a :struct:`TimeStamp` - a :struct:`Scalar` = a :struct:`TimeStamp`
   * a :struct:`TimeSpan` + a :struct:`Scalar` = a :struct:`TimeSpan`
   * a :struct:`TimeSpan` - a :struct:`Scalar` = a :struct:`TimeSpan`

Example::

  // This sets A = right now:
  set A to TIMESTAMP().
  // This sets B = 3600 seconds from now:
  set B to A + 3600.
  // This sets C = half a second before B:
  set C to B - 0.5.

  // This sets D = a span of 3 minutes:
  set D to TIMESPAN(0,0,0,3,0).
  // This sets D = 5 seconds longer than it was before:
  set D to D + 5.

You may add or subtract two TimeSpans to get a new longer or shorter
TimeSpan:

   * a :struct:`TimeSpan` + a :struct:`TimeSpan` = a :struct:`TimeSpan`
   * a :struct:`TimeSpan` - a :struct:`TimeSpan` = a :struct:`TimeSpan`

Example::

  // This sets A = 30 minutes:
  set A to TIMESPAN(0,0,0,30,0).
  // This sets B = 10 minutes:
  set B to TIMESPAN(0,0,0,10,0).
  // This sets C = 40 minutes:
  set C to A + B.
  // This sets D = 20 minutes:
  set D to A - B.

You may divide or multiply a TimeSpan (*but NOT a TimeStamp*) by a scalar.
*When using scalars this way, they are interpreted as unit-less
coefficients and NOT as seconds like they are when adding or subtracting*.
This gives you a larger or smaller time interval.  Note that if multiplying,
the order does not matter, but if dividing, then you *may not* put the
TimeStamp in the denominator.

   * a :struct:`TimeSpan` * a :struct:`Scalar` = a :struct:`TimeSpan`
   * a :struct:`TimeSpan` / a :struct:`Scalar` = a :struct:`TimeSpan`
   * a :struct:`Scalar` * a :struct:`TimeSpan` = a :struct:`TimeSpan`
   * a :struct:`Scalar` / a :struct:`TimeSpan` = Illegal Operation
   * a :struct:`TimeSpan` / a :struct:`Scalar` = a :struct:`TimeSpan`
   * a :struct:`TimeStamp` * a :struct:`Scalar` = Illegal Operation
   * a :struct:`TimeStamp` / a :struct:`Scalar` = Illegal Operation
   * a :struct:`TimeStamp` * a :struct:`TimeSpan` = Illegal Operation
   * a :struct:`TimeStamp` / a :struct:`TimeSpan` = Illegal Operation
   * a :struct:`Scalar` * a :struct:`TimeStamp` = Illegal Operation
   * a :struct:`Scalar` / a :struct:`TimeStamp` = Illegal Operation
   * a :struct:`Scalar` * a :struct:`TimeStamp` = Illegal Operation
   * a :struct:`Scalar` / a :struct:`TimeStamp` = Illegal Operation

Example::

  // This sets A = 45 minutes:
  set A to TIMESPAN(0,0,0,45,0).
  // This sets B to 1 hour 30 minutes (2 * 45 minutes = 90 minutes)
  set B to 2 * A.
  // This sets C to 22 minutes 30 seconds (half of 45 minutes):
  set C to A / 2.

Time Opertators - comparisons
-----------------------------

You may check if two TimeStamps are equal, greater, or lesser.

   * (a :struct:`TimeStamp` = a :struct:`TimeStamp`) is true if they are the same time
   * (a :struct:`TimeStamp` <> a :struct:`TimeStamp`) is true if they are not the same time
   * (a :struct:`TimeStamp` < a :struct:`TimeStamp`) is true if the time on the left is sooner than the one on the right
   * (a :struct:`TimeStamp` > a :struct:`TimeStamp`) is true if the time on the left is later than the one on the right
   * (a :struct:`TimeStamp` <= a :struct:`TimeStamp`) works as expected, given the above.
   * (a :struct:`TimeStamp` >= a :struct:`TimeStamp`) works as expected, given the above.

Example::

  // Run the loop until 3 seconds have passed:
  local end_time is TIMESTAMP() + 3. // Now plus 3 seconds.
  until TIMESTAMP() > end_time { // Note this is a TimeStamp > TimeStamp comparison
    print "3 seconds aren't up yet...".
    wait 0.2.
  }
  print "3 seconds have passed.".

You may check if two TimeSpans are equal, greater, or lesser.

   * (a :struct:`TimeSpan` = a :struct:`TimeSpan`) is true if they are the same length
   * (a :struct:`TimeSpan` <> a :struct:`TimeSpan`) is true if they are not the same length
   * (a :struct:`TimeSpan` < a :struct:`TimeSpan`) is true if the span on the left is shorter than the one on the right
   * (a :struct:`TimeSpan` > a :struct:`TimeSpan`) is true if the span on the left is longer than the one on the right
   * (a :struct:`TimeSpan` <= a :struct:`TimeSpan`) works as expected, given the above.
   * (a :struct:`TimeSpan` >= a :struct:`TimeSpan`) works as expected, given the above.

Example::

  local short_span is TIMESPAN(0,0,0,0,30). // 30 seconds
  local long_span is TIMESPAN(0,0,0,5,0). // 5 minutes
  if short_span < long_span {
    print "I guess 30 seconds is shorter than 5 minutes.".
  }

You may compare TimeStamps with Scalars, or TimeSpans with Scalars.  In all such
cases, the Scalar is interpreted as a number of seconds.  In the case of comparing
a TimeStamp with a Scalar, the Scalar is taken as a Universal Time expressed
in seconds-since-epoch.  In the case of comparing a TimeSpan to a Scalar,
the Scalar is just a duration of that many seconds.

   * (a :struct:`TimeStamp` = a :struct:`Scalar`) Works as described above.
   * (a :struct:`TimeStamp` <> a :struct:`Scalar`) Works as described above.
   * (a :struct:`TimeStamp` < a :struct:`Scalar`) Works as described above.
   * (a :struct:`TimeStamp` > a :struct:`Scalar`) Works as described above.
   * (a :struct:`TimeStamp` <= a :struct:`Scalar`) Works as described above.
   * (a :struct:`TimeStamp` >= a :struct:`Scalar`) Works as described above.
   * (a :struct:`TimeSpan` = a :struct:`Scalar`) Works as described above.
   * (a :struct:`TimeSpan` <> a :struct:`Scalar`) Works as described above.
   * (a :struct:`TimeSpan` < a :struct:`Scalar`) Works as described above.
   * (a :struct:`TimeSpan` > a :struct:`Scalar`) Works as described above.
   * (a :struct:`TimeSpan` <= a :struct:`Scalar`) Works as described above.
   * (a :struct:`TimeSpan` >= a :struct:`Scalar`) Works as described above.

Example::

  local how_many_seconds_in_3_hours is 3 * 3600.
  if TIMESTAMP() > how_many_seconds_in_3_hours {
    print "This campaign universe has existed for at least 3 hours of game time.".
  }
  if TIMESPAN(1,0,0,0,0) > 1000000 {
    print "One year is more than 1000000 seconds.".
  }

You *may NOT* compare TimeStamps with TimeSpans.  All the following are illegal:

   * (a :struct:`TimeStamp` = a :struct:`TimeSpan`) is an Illegal Comparison
   * (a :struct:`TimeStamp` <> a :struct:`TimeSpan`) is an Illegal Comparison
   * (a :struct:`TimeStamp` < a :struct:`TimeSpan`) is an Illegal Comparison
   * (a :struct:`TimeStamp` > a :struct:`TimeSpan`) is an Illegal Comparison
   * (a :struct:`TimeStamp` <= a :struct:`TimeSpan`) is an Illegal Comparison
   * (a :struct:`TimeStamp` >= a :struct:`TimeSpan`) is an Illegal Comparison
   * (a :struct:`TimeSpan` = a :struct:`TimeStamp`) is an Illegal Comparison
   * (a :struct:`TimeSpan` <> a :struct:`TimeStamp`) is an Illegal Comparison
   * (a :struct:`TimeSpan` < a :struct:`TimeStamp`) is an Illegal Comparison
   * (a :struct:`TimeSpan` > a :struct:`TimeStamp`) is an Illegal Comparison
   * (a :struct:`TimeSpan` <= a :struct:`TimeStamp`) is an Illegal Comparison
   * (a :struct:`TimeSpan` >= a :struct:`TimeStamp`) is an Illegal Comparison

