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

.. _electricdrain:

Electric Drain
--------------

.. versionadded:: 0.19.0
    As of version 0.19.0, the electric charge drain varies depending
    on CPU % usage.  Prior to version 0.19.0, the CPU load made no
    difference and the electric drain was constant regardless of
    utilization.

Real world CPUs often have low power modes, and sleep modes, and these are
vital to long distance probes.  In these modes the computer deliberately
runs slowly in order to use less power, and then the program can tell it to
speed up to normal speed again when it needs to wake up and do something.

In kOS, this concept is simplified by just draining electric charge by
"micropayments" of charge per instruction executed.

To change this setting if you want to re-balance the system, see the
page about :ref:`kOSProcessor part config values <EcPerInstruction>`.

The shorthand version is this:  The more instructions per update
actually get executed, the more power is drained.  This can be reduced
by either lowering ``CONFIG:IPU`` or by making sure your main loop
has a ``WAIT`` statement in it.  (When encountering a ``WAIT`` statement,
the remainder of the instructions for that update are not used and end
up not counting against electric charge).

The system always costs at least 1 instruction of electric charge per
update no matter what the CPU is doing, unless it's powered down entirely,
because there's always at least 1 instruction just to check if it's time
to resume yet in a ``WAIT``.  The electric cost is never entirely zero
as long as it's turned on, but it can be very close to zero while it is
stuck on a wait.

If your program spins in a busy loop, never waiting, it can consume
quite a bit more power than it would if you explicitly throw in a
``WAIT 0.001.`` in the loop.  Even if the wait is very small, the
mere fact that it yields the remaining instructions still allowed
that update can make a big difference.

.. _triggers:

Triggers
--------

.. versionadded:: 0.19.3
    Note that as of version 0.19.3 and up, the entire way that triggers
    are dealt with by the underlying kOS CPU has been redesigned.  In
    previous versions it was not possible to have a trigger that lasts
    longer than one **physics tick**, leading to a lot of warnings in
    this section of the documentation.  Many of those warnings are now
    moot, which caused a re-write of most of this section of the
    documentation.

There are multiple things within kerboscript that run "in the background"
always updating, while the main script continues on. The way these work is
a bit like a real computer's interrupt handling system, but not *quite*.

Collectively all of these things are called "triggers".

Triggers come in these varieties:

.. _recurring_trigger:

- **Recurring triggers:** Triggers that once they are started keep getting
  called again and again on a regular basis, until they are made to stop.
  - LOCKS which are attached to flight controls (THROTTLE, STEERING,
    etc), but not other LOCKS.
  - User Delegates assigned to recurrently updating suffixes such as
    :attr:`VecDraw:VECUPDATER`.
  - WHEN and ON triggers:
    - ``WHEN condition THEN { some commands }``
    - ``ON condition { some commands }``

.. _callback_once_trigger:

- **CallbackOnce triggers:** Triggers that only happen once per event.  To
  make the trigger happen again, the event has to happen again:
  - Callback delegates you tell the system to call when the user
    performs GUI events (for example a button's ONCLICK).

These two types of trigger don't have the same priority level.
It is possible for a recurring trigger to interrupt a callback-once
trigger, but not the other way around.  Further information about
this is described in the :ref:`interrupt priority <interrupt_priority>`
documentation below.

All triggers work essentially like this:

The kOS CPU decides it's time to cause a call to the trigger.  (How it
does this is explained below in
:ref:`interrupt priority <interrupt_priority>`.)  Once it decides its
time to call the trigger, it does so by inserting a subroutine call
at the current moment that interrupts the normal program flow and
jumps to the trigger's subroutine *as if* the program itself had chosen
to call the subroutine.  It manipulates the call-stack in such a way
that the normal work of the ``Return`` instruction at the end of the
trigger routine will pop back to the current location of the program
flow.  This system works because all variables in kOS are on the
stack without any registers, and so popping back to where the
interruption happened puts everything back in the state it was in
before the interruption so the program can continue as if nothing
had happened.

.. _trigger_steering:

Triggers for Cooked Steering
~~~~~~~~~~~~~~~~~~~~~~~~~~~~

*This is a kind of :ref:`recurring trigger <recurring_trigger>`.*

The ``lock`` expressions associated with
:ref:`Cooked Control <commands/flight/cooked.html>`__,
meaning ``STEERING``, ``THROTTLE``, ``WHEELSTEERING``, and
``WHEELTHROTTLE``, have triggers associated with them.
kOS will keep calling these expressions repeatedly as frequently
as it can (once per **physics tick** if it can).  That is why
they are a kind of *recurring_trigger*.

Note, the ``LOCK`` command does not *normally* result in a trigger
that runs every **physics tick**.  It just does this when dealing with
one of these specific values, of ``STEERING``, ``THROTTLE``,
``WHEELSTEERING``, and ``WHEELTHROTTLE``.  The normal behaviour of
a lock expression is to only execute the expression when it's used
inside another expression.  It's just that in the case of these
special locks, the kOS system *itself* is repeatedly doing that.
To do this kOS needs to interrupt whatever your code was doing at the
time to perform this expression and it uses the trigger interrupt
system to do so.

.. _when_on_trigger:

Triggers for WHEN and ON statements
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

*This is a kind of :ref:`recurring trigger <recurring_trigger>`.*

Each of the ``ON`` and ``WHEN`` triggers also behave
much like a function, with a body like this::

   if (not conditional_expression)
       return true.  // premature quit.  preserve and try again next time.
   do_rest_of_trigger_body_here.

.. _when_on_conditional:

**WHEN and ON Triggers always interrupt to check the condition even when
the body doesn't happen yet.**

Even a trigger who's condition isn't true yet still needs to execute
the few instructions at the start of the trigger that *discover* that
its condition isn't true yet.  The trigger causes a subroutine call
once per **physics tick** (or less often if the system has too 
much trigger work to accomplish all the triggers in one tick).
This call gets at least far enough into the routine to
reach the conditional expression check and discover that it's not
time to run the rest of the body yet, so it returns.  An expensive
to calculate conditional expression can really starve the system of
instructions because the system is attempting to run it every
**physics tick** if it can.

*It's good practice to try to keep your trigger's conditional check
short and fast to execute.  If it consists of multiple clauses, try
to take advantage of :ref:`short circuit boolean <short_circuit>`
logic by putting the fastest part of the check first.*

.. _wait_in_trigger:

Wait in a Trigger
~~~~~~~~~~~~~~~~~

While ``WAIT`` is possible from inside a trigger and it won't crash
the script to use it, it's probably not a good design choice to use
``WAIT`` inside a trigger.  Triggers should be designed to execute
all the way through to the end in one fast pass, if possible.

Do Not Loop a Long Time in a Trigger Body!
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

For similar reasons to the explanation above about the ``WAIT`` command
used inside triggers, it's not really a good idea for a trigger to
have a long loop inside it that just keeps going and going.

The system does allow a trigger to take more than one **physics tick**
to finish.  There are cases where it is entirely legitimate to do so
if the trigger's body has too much work to do to get it all done in one
update.  However, all triggers should be designed to finish their tasks
in finite time and return.  What you should not do is design a trigger's
body to go into an infinite loop, or a long-lasting loop that you thought
would run in the background while the rest of the program continues on.

This is because while you are in a trigger, main-line code isn't being
executed, and other triggers of equal or lesser priority aren't being
executed.  A trigger that performs a long-running loop will starve the
rest of the code in your kerboscript program from being allowed to run.

But I Want a Loop!!
~~~~~~~~~~~~~~~~~~~

If you want a trigger body that is meant to loop a long time, the only
workable way to do it is to design it to execute just once, but
then make it return true (or use the ``preserve`` keyword, which is
basically the same thing) to keep the trigger around for the next
**physics tick**. Thus your trigger becomes a sort of "loop" that
executes one iteration per **physics tick**.

.. versionadded:: 1.1.6.0
    The multiple priorities of interruption described below (GUI callbacks
    being lower priority than recurring callbacks) were introduced in
    kOS v1.1.6.0

.. _interrupt_priority:

Trigger Interrupt Priority
--------------------------


TODO: THIS IS A LONG WRITEUP TO GO HERE, TALKING ABOUT TRIGGER PRIORITIES.


Wait!!!
-------

Any WAIT statement causes the kerboscript program to immediately stop executing the main program where it is, even if far fewer than :attr:`Config:IPU` instructions have been executed in this **physics tick**. It will not continue the execution until at least the next **physics tick**, when it will check to see if the WAIT condition is satisfied and it's time to wake up and continue.

Therefore ANY WAIT of any kind will guarantee that your program will allow at least one **physics tick** to have happened before continuing. If you attempt to::

    WAIT 0.001.

But the duration of the next physics tick is actually 0.09 seconds, then you will actually end up waiting at least 0.09 seconds. It is impossible to wait a unit of time smaller than one physics tick. Using a very small unit of time in a WAIT statement is an effective way to force the CPU to allow a physics tick to occur before continuing to the next line of code.
In fact, you can just tell it to wait "zero" seconds and it will still
really wait the full length of a **physics tick**.  For example::

    WAIT 0.

Ends up being effectively the same thing as ``WAIT 0.01.``
or ``WAIT 0.001.`` or ``WAIT 0.000001.``.  Since they all contain a
time less than a **physics tick**, they all "round up" to waiting a
full **physics tick**.

Similarly, if you just say::

    WAIT UNTIL TRUE.

Then even though the condition is immediately true, it will still wait one physics tick to discover this fact and continue.

.. _cpu_update_loop:

CPU Update Loop
---------------

.. note::

    As of version 0.17.0, The kOS CPU runs every *physics tick*, not
    every *update tick* as it did before.

.. versionadded:: 0.19.3
    As of version 0.19.3, the behaviour of triggers was changed
    dramatically to enable triggers that last longer than one
    *physics tick*, thereby causing the section of documentation
    that follows to be completely re-written.  If you were familiar
    with triggers before 0.19.3, you should read the next section
    carefully to be aware of what changed.

On each physics tick, each kOS CPU that's fully present "near" enough
to the player's current ship to be fully loaded, including the current
ship itself, wakes up and performs the following steps, in this order:

1. For each TRIGGER (see below) that is currently enabled,
   manipulate the call stack to make it look as if the program
   had just made a subroutine call to the trigger right now, and the
   current execution is now set to the start of the trigger's code.
   *Remeber that from the point of view of the CPU, triggers appear
   to be subroutines it just unconditionally calls whether or not
   their trigger condition is true yet.  The code to decide that
   it's not really time yet for the trigger to fire is contained
   inside the trigger subroutine itself.  The first thing the
   trigger routine does is return prematurely if its trigger
   condition hasn't been met.*
   If more than one such trigger is enabled and needs to be set up,
   then the calls to the triggers will end up looking like a list of
   nested subroutine calls on the stack had just begun, and the
   current instruction is the start of the innermost nested subroutine
   call.
2. Any TRIGGER which has just been set up thusly is temporarily removed
   from the list of enabled triggers, so it will be ignored in step (1)
   above should the *physics tick* expire before the trigger's code
   had its chance to go.
3. *(THE LOOP PART)*:
   The cpu now goes on and executes the next :attr:`Config:IPU` number of
   instructions, mostly not caring about whether those instructions are
   ordinary main-line code or instructions that are inside of a trigger.
   Step (1) above has caused each trigger to look like just a normal
   subroutine was called from main-line code.  When the nested subroutines
   all finish, the call stack has "popped" all the way back to where the
   mainline code left off, and so it just continues on from there.
   **Warning: Advanced sentence follows.  You can ignore it if you don't
   understand it:** *Because kOS is a pure stack computer with no
   temporary data held in "registers", this technique works because all
   relevant data must be on the stack, and thus will get returned to its
   original state once the interrupting triggers are done with their work
   and the stack has fully popped back to where it started from.*
4. While executing the instructions in Step(3) above, if any of those
   instructions are a ``WAIT`` command, the execution stops there for
   now and the full number of :attr:`Config:IPU` instructions won't be
   used this update.  This is true BOTH of wait's in main-line code and
   wait's in trigger code.  Although you *can* wait in a trigger, doing
   so also stops main line code until that trigger is done waiting.
5. One thing the CPU *does* keep track of while executing the instructions,
   though, is whether or not it got all the way back to executing mainline
   code again or not.  It's possible that it spent the entire
   :attr:`Config:IPU` inside triggers and never got back to mainline code.
   If it *has* gotten back to mainline code and executed at least one
   mainline instruction, then it re-enables all the triggers that wished
   to be re-enabled because they executed ``preserve.`` or did a
   ``return true``.   (They were temporarily disabled up in Step(2) above.)
   If it has *not* gotten back to mainline code yet, then that means
   it's about to finish a physics tick while still inside a trigger, and
   it shouldn't allow more triggers to re-fire yet until the main-line code
   has had a chance to go again.

Note that the number of instructions being executed (CONFIG:IPU) are NOT lines of code or kerboscript statements, but rather the smaller instruction opcodes that they are compiled into behind the scenes. A single kerboscript statement might become anywhere from one to ten or so instructions when compiled.


.. _frozen:

The Frozen Universe
-------------------

Each **physics** *tick*, the kOS mod wakes up and runs through all the currently loaded CPU parts that are in "physics range" (i.e. 2.5 km), and executes a batch of instructions from your script code that's on them. It is important to note that during the running of this batch of instructions, because no **physics ticks** are happening during it, none of the values that you might query from the KSP system will change. The clock time returned from the TIME variable will keep the same value throughout. The amount of fuel left will remain fixed throughout. The position and velocity of the vessel will remaining fixed throughout. It's not until the next physics tick occurs that those values will change to new numbers. It's typical that several lines of your kerboscript code will run during a single physics tick.

Effectively, as far as the *simulated* universe can tell, it's as if your script runs several instructions in literally zero amount of time, and then pauses for a fraction of a second, and then runs more instructions in literally zero amount of time, then pauses for a fraction of a second, and so on, rather than running the program in a smoothed out continuous way.

This is a vital difference between how a kOS CPU behaves versus how a real world computer behaves. In a real world computer, you would know for certain that time will pass, even if it's just a few picoseconds, between the execution of one statement and the next.
