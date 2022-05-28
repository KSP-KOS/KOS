.. _deltav:

DeltaV
======

A :struct:`DeltaV` contains information about the delta-V
numbers the stock KSP game calculates for you in the
user interface (in the staging list display).  There are in
fact two different kinds of DeltaV structures in kOS - one
that refers to the whole veseel, or one that refers to just
one stage of the vessel.

You can obtain a DeltaV about the sum of the whole vessel
with ``SHIP:DELTAV``.  For example::

    PRINT "Vessel's total dV number is " + SHIP:DELTAV:CURRENT.

You can obtain a DeltaV of just one stage with ``SHIP:STAGEDELTAV(num)``.
For Example::

    PRINT "The Current Stage dV number is " + SHIP:STAGEDELTAV(SHIP:STAGENUM):CURRENT.
    PRINT "The Next Stage dV number is " + SHIP:STAGEDELTAV(SHIP:STAGENUM-1):CURRENT.
    PRINT "The Next Stage dV number is " + SHIP:STAGEDELTAV(SHIP:STAGENUM-2):CURRENT.
    //
    // etc..  Stages number downward from SHIP:STAGENUM to 0, just like
    //        in the user interface stage list display.

The shortcut ``STAGE:DELTAV`` can be used as a shorthand for ``SHIP:STAGEDELTAV(SHIP:STAGENUM)``
(the currently active stage's deltaV).

Warning - stock numbers aren't totally reliable
-----------------------------------------------

The stock DeltaV system was designed for human eyeballs to read, not for
computer code to make reliable decisions from it.  As such it does have
some less-than-accurate traps you could fall into if you try to use it
for script decisions.  Be aware of these:

* The values take a few update ticks to "settle in" after a staging event.
* The values naturally fluctuate a bit between update ticks.  The human
  watching the game doesn't see this because the display gets rounded off,
  but a script reading the values will notice the slight variations.
* There are complex staging techniques that the stock deltaV calculations
  don't quite understand.  If you stage tanks away, asparagus-style, it
  might not be reliable.
* The delta V values are part of the user interface about the currently
  active vessel.  They will probably be wrong for vessels that are not
  currently active.  (i.e. if you have two vessels in loading distance,
  and are running scripts on both of them, the delta-V values might only
  be right for the one you are looking at, and wrong for the other one.)

DeltaV Structure
----------------

.. structure:: DeltaV

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 1 2

        * - Suffix
          - Type (units)
          - Access
          - Description

        * - :attr:`CURRENT`
          - :struct:`Scalar`
          - Get only
          - How much DeltaV (meters/second) at curent atmospheric conditions?
        * - :attr:`ASL`
          - :struct:`Scalar`
          - Get only
          - How much DeltaV (meters/second) at sea level conditions?
        * - :attr:`VACUUM`
          - :struct:`Scalar`
          - Get only
          - How much DeltaV (meters/second) at vacuum conditions?
        * - :attr:`DURATION`
          - :struct:`Scalar`
          - Get only
          - How many seconds at maxthrust will emit this deltaV?
        * - :meth:`FORCECALC()`
          - (none)
          - method call
          - Call to force KSP to re-run its simulator to recalc deltaV.

.. attribute:: DeltaV:CURRENT

    :access: Get only
    :type: :struct:`Scalar` in m/s

    Returns stock KSP's notion of how much deltaV there is.  The stock deltaV
    calculation assumes all engine burns take place at the current atmospheric
    pressure the ship is in.  This should match the value seen in the staging
    list during flight, (Including the sometimes incorrect values that stock
    KSP gives in its DeltaV calculations, unfortunately.)

    Heed the warning above: Stock delta-V values might not be getting
    calculated if this vessel is not the currently active vessel.

.. attribute:: DeltaV:ASL

    :access: Get only
    :type: :struct:`Scalar` in m/s

    Returns stock KSP's notion of how much deltaV there *would be* if all the
    burns took place at 1 ATM (sea level atmosphere).  This should match
    the value seen in the staging list during construction if you had the
    delta-V readouts in sea level mode., (Including the sometimes
    incorrect values that stock KSP gives in its DeltaV calculations,
    unfortunately.)

    Heed the warning above: Stock delta-V values might not be getting
    calculated if this vessel is not the currently active vessel.

.. attribute:: DeltaV:VACUUM

    :access: Get only
    :type: :struct:`Scalar` in m/s

    Returns stock KSP's notion of how much deltaV there *would be* if all the
    burns took place in a vacuum.  This should match
    the value seen in the staging list during construction if you had the
    delta-V readouts in vacuum mode., (Including the sometimes
    incorrect values that stock KSP gives in its DeltaV calculations,
    unfortunately.)

    Heed the warning above: Stock delta-V values might not be getting
    calculated if this vessel is not the currently active vessel.

.. attribute:: DeltaV:DURATION

    :access: Get only
    :type: :struct:`Scalar` in seconds

    Returns stock KSP's notion of how long it will take to cause this deltaV.
    (How much time it takes for the engine(s) to burn up the fuel if they
    are at MAXTHRUST).  This should match the value seen in the staging list.
    (Including the sometimes incorrect values that stock KSP gives in its
    DeltaV calculations, unfortunately.)

    Heed the warning above: Stock delta-V values might not be getting
    calculated if this vessel is not the currently active vessel.

.. method:: DeltaV:FORCECALC()

    :type: none (void)

    The stock delta-V calculations are made by constantly running a small
    simulation in its head during flight that takes a few update ticks
    to arrive at the answer.  Calling this method will force the stock game
    to mark its current delta-V values "dirty" and make it want to re-run
    the calculation.  **DO NOT CALL THIS REPEATEDLY IN A LOOP OR A
    TRIGGER**.  Calling this causes the game to lag if you keep doing it
    all the time (it generates "garbage" for the game to "collect".)

    After calling FORCECALC(), the deltaV values you see will be quite
    wrong for a few update ticks, while the game calculates the new values.
    Unfortunately, there isn't a good way for a kerobscript to find out
    when the answer is final and the recalculation is over.  (Sorry, we
    tried, but couldn't find the API call in KSP that would tell us this.)

    The only real way to decide the recalculation is over is to examine
    the output of the :ASL or :VACUUM suffixes and see when they seem
    to have settled on a number and stayed there a while.  (don't use
    :CURRENT for this, as it will naturally change if you are ascending
    or descending in atmosphere.)  Be aware that a *small* fluctuation
    in the value is in fact expected, as the simulation KSP runs in its
    head is subject to floating point errors (i.e. while you see a value
    like "2345 m/s" in the User display, under the hood that value could
    actually be varying between 2345.11112 to 2345.11135 to 2345.11102,
    etc.)
