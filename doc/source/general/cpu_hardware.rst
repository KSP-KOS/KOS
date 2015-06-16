.. _cpu hardware:

The kOS CPU hardware
====================

While it's possible to write some software without knowing anything
about the underlying computer hardware, and there are good design
principles that state one should never make assumptions about the
computer hardware when writing software, there are still some basic
things about how computers work in general that a good programmer
needs to be aware of to write good code. Along those lines, the KSP
player writing a Kerboscript program needs to know a few basic things
about how the simulated kOS CPU operates in order to be able to write
more advanced scripts. This page contains that type of information.

.. contents::
    :local:
    :depth: 2

.. _physics tick:

Update Ticks and Physics Ticks
------------------------------

.. note::
    .. versionadded:: 0.17
        Previous versions of kOS used to execute program code during the
	Update phase, rather than the more correct Physics Update phase.

Kerbal Space Program simulates the universe by running the universe in
small incremental time intervals that for the purpose of this
document, we will call "**physics ticks**". The exact length of time
for a physics tick varies as the program runs. One physics tick might
take 0.09 seconds while the next one might take 0.085 seconds. (The
default setting for the rate of physics ticks is 25 ticks per second,
just to give a ballpark figure, but you **must not** write any scripts
that depend on this assumption because it's a setting the user can
change, and it can also vary a bit during play depending on system
load. The setting is a target goal for the game to try to achieve, not
a guarantee. If it's a fast computer with a speedy animation frame
rate, it will try to run physics ticks less often than it runs
animation frame updates, to try to make the physics tick rate match
this setting. On the other hand, If it's a slow computer, it will try
to sacrifice animation frame rate to archive this number (meaning
physics get calculated faster than you can see the effects.)

When calculating physics formulas, you need to actually measure
elapsed time in the TIME:SECONDS variable in your scripts.

The entire simulated universe is utterly frozen during the duration of
a physics tick. For example, if one physics tick occurs at timestamp
10.51 seconds, and the next physics tick occurs 0.08 seconds later at
timestamp 10.59 seconds, then during the entire intervening time, at
timestamp 10.52 seconds, 10.53 seconds, and so on, nothing moves. The
clock is frozen at 10.51 seconds, and the fuel isn't being consumed,
and the vessel is at the same position. On the next physics tick at
10.59 seconds, then all the numbers are updated.  The full details of
the physics ticks system are more complex than that, but that quick
description is enough to describe what you need to know about how
kOS's CPU works.

There is another kind of time tick called an **Update tick**. It is
similar to, but different from, a **physics tick**. *Update ticks*
often occur a bit more often than *physics ticks*. Update ticks are
exactly the same thing as your game's Frame Rate. Each time your game
renders another animation frame, it performs another Update tick. On a
good gaming computer with fast speed and a good graphics card, It is
typical to have about 2 or even 3 *Update ticks* happen within the
time it takes to have one *physics tick* happen. On a slower computer,
it is also possible to go the other way and have *Update ticks*
happening *less* frequently than *physics tics*. Basically, look at
your frame rate. Is it higher than 25 fps? If so, then your *update
ticks* happen faster than your *physics ticks*, otherwise its the
other way around.

.. note::

    As of version 0.17.0, The kOS CPU runs every *physics tick*.

On each physics tick, each kOS CPU that's within physics range (i.e. 2.5 km),
wakes up and *attempts to* perform the following steps, in this order:

1. For each *trigger*, run the trigger's test condition, followed by its
   body if the condition is true. (Triggers are explained in the next
   section below).
2. If there's a pending WAIT statement, do none of the main program's 
   instructions.
3. Use whatever remaining instructions are left in this update tick's
   alloted running time to run statements from the main program.

However, it only *attempts* to perform all those steps.  It's possible
for it to decide that performing all 3 steps to completion is taking too
long and it is in danger of stealing time away from the rest of the KSP
game.  If it comes to the decision that the above steps are taking too
long (usually because of the amount of code being executed in the triggers
in step 1), then it will stop partway through step 1 and mark where it was
and come back to continue from there in the next *physics tick*.  When this
happens it means your main program will be slowed down by the trigger code.

There is a facility in place to ensure that triggers can't steal ALL the time
away from mainline code, and that at least an opportunity for a few mainline
instructions to run must exist before it will check for triggers again, but it
is possible for you to write very long triggers that will steal MOST of the
time away from your main program.  Be wary of doing this.

Note that the number of instructions being executed (CONFIG:IPU) are NOT lines of code or kerboscript statements, but rather the smaller instruction opcodes that they are compiled into behind the scenes. A single kerboscript statement might become anywhere from one to ten or so instructions when compiled.

Triggers
--------

There are multiple things within kerboscript that run "in the background"
always updating, while the main script continues on. The way these work is
a bit like a real computer's multithreading, but not *quite*.  The kOS
computer doesn't truly multithread, but it does allow mainline code to
be interrupted by triggers. (Triggers on the other hand, can't be
interrupted by other triggers).

These things that occur in the background are called "triggers" and
include all of the following:

-  LOCKS which are attached to flight controls (THROTTLE, STEERING,
   etc), but not other LOCKS.
-  ON condition { some commands }.
-  WHEN condition THEN { some commands }.

.. note::

    The :ref:`WAIT <wait>` command only causes mainline code
    to be suspended.  Trigger code such as WHEN, ON, LOCK STEERING,
    and LOCK THROTTLE, will continue executing while your program
    is sitting still on the WAIT command.


The way these work is that once per **physics tick**, all the LOCK
expressions which directly affect flight control are re-executed, and
then each conditional trigger's condition is checked, and if true,
then the entire body of the trigger is executed all the way to the
bottom \*before any more instructions of the main body are executed\*.
This means that execution of a trigger never gets interleaved with the
main code. Once a trigger happens, the entire trigger occurs all in one
go before the rest of the main body continues.

Do Not Loop a Long Time in a Trigger Body!
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Triggers are meant to be ways you can write small pieces of code that
watch for conditions in the background that interrupt your main code,
do some small thing, then let your main code continue where it left off.

Using a trigger to constantly run a loop is not generally a good idea,
because triggers can steal a lot of CPU time away from your main code,
and because triggers don't interrupt other triggers, so while your
trigger is running, other triggers can't fire off until it finishes.

As of version kOS 0.17.4, Triggers are now capable of taking longer than
one update to execute.  They simply starve the main code of executing 
time while they do so.  The previous versions of kOS had a restriction
to force all triggers to fit in one small update.  This limitation no
longer exists, but it's still a good idea to keep triggers small for good
performance.  Now you can let a trigger take several *physics ticks* to
finish, but be careful not to let it starve your mainline code by setting
up a long background loop in a trigger.


But I Want a Loop!!
~~~~~~~~~~~~~~~~~~~

If you want a trigger body that is meant to loop, but don't want it to
steal time away from the mainline code, one useful way to do it is to
design it to execute just once, but then use the PRESERVE keyword to
keep the trigger around to be checked again and again. Thus your trigger
becomes a sort of "loop" that executes roughly one iteration per
**physics tick** (less often if you have other triggers that are taking
long to execute and you can't fit all your trigger code in one **physics
tick**).

Because triggers aren't interruptable by other triggers, WAIT statements
are disallowed inside a trigger. This is because, while it's possible to
have a trigger wait to execute the rest of its code later, it will be
preventing the mainline program from executing while it does so, and this
is bad form.

Wait!!!
~~~~~~~

Any WAIT statement causes the kerboscript program to immediately stop executing the main program where it is, even if far fewer than :attr:`Config:IPU` instructions have been executed in this **physics tick**. It will not continue the execution until at least the next **physics tick**, when it will check to see if the WAIT condition is satisfied and it's time to wake up and continue.

Therefore ANY WAIT of any kind will guarantee that your program will allow at least one **physics tick** to have happened before continuing. If you attempt to::

    WAIT 0.001.

But the duration of the next physics tick is actually 0.09 seconds, then you will actually end up waiting at least 0.09 seconds. It is impossible to wait a unit of time smaller than one physics tick. Using a very small unit of time in a WAIT statement is an effective way to force the CPU to allow a physics tick to occur before continuing to the next line of code. Similarly, if you just say::

    WAIT UNTIL TRUE.

Then even though the condition is immediately true, it will still wait one physics tick to discover this fact and continue.

.. note::

    The :ref:`WAIT <wait>` command only causes mainline code
    to be suspended.  If you execute a WAIT command from
    your mainline code, then the Trigger code such as WHEN,
    ON, LOCK STEERING, and LOCK THROTTLE, will continue
    executing while your program is sitting still on the
    main program's WAIT command.  This is deliberate, as the
    intent is to allow triggers to fire if their conditions
    become true while you are waiting.


The Frozen Universe
-------------------

Each **physics** *tick*, the kOS mod wakes up and runs through all the currently loaded CPU parts that are in "physics range" (i.e. 2.5 km), and executes a batch of instructions from your script code that's on them. It is important to note that during the running of this batch of instructions, because no **physics ticks** are happening during it, none of the values that you might query from the KSP system will change. The clock time returned from the TIME variable will keep the same value throughout. The amount of fuel left will remain fixed throughout. The position and velocity of the vessel will remaining fixed throughout. It's not until the next physics tick occurs that those values will change to new numbers. It's typical that several lines of your kerboscript code will run during a single physics tick.

Effectively, as far as the *simulated* universe can tell, it's as if your script runs several instructions in literally zero amount of time, and then pauses for a fraction of a second, and then runs more instructions in literally zero amount of time, then pauses for a fraction of a second, and so on, rather than running the program in a smoothed out continuous way.

This is a vital difference between how a kOS CPU behaves versus how a real world computer behaves. In a real world computer, you would know for certain that time will pass, even if it's just a few picoseconds, between the execution of one statement and the next.

