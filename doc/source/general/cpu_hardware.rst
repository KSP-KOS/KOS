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

On each physics tick, each kOS CPU that's within physics range (i.e. 2.5 km), wakes up and performs the following steps, in this order:

1. Run the conditional checks of all TRIGGERS (see below)
2. For any TRIGGERS who's conditional checks are true, execute the entire body of the trigger.
3. If there's a pending WAIT statement, check if it's done. If so wake up.
4. If awake, then execute the next :attr:`Config:IPU` number of instructions of the main program.

Note that the number of instructions being executed (CONFIG:IPU) are NOT lines of code or kerboscript statements, but rather the smaller instruction opcodes that they are compiled into behind the scenes. A single kerboscript statement might become anywhere from one to ten or so instructions when compiled.


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

Triggers
--------

There are multiple things within kerboscript that run "in the background" always updating, while the main script continues on. The way these work is a bit like a real computer's multithreading, but not *quite*. Collectively all of these things are called "triggers".

Triggers are all of the following:

-  LOCKS which are attached to flight controls (THROTTLE, STEERING,
   etc), but not other LOCKS.
-  ON condition { some commands }.
-  WHEN condition THEN { some commands }.

.. note::

    The :ref:`WAIT <wait>` command only causes mainline code
    to be suspended.  Trigger code such as WHEN, ON, LOCK STEERING,
    and LOCK THROTTLE, will continue executing while your program
    is sitting still on the WAIT command.


The way these work is that once per **physics tick**, all the LOCK expressions which directly affect flight control are re-executed, and then each conditional trigger's condition is checked, and if true, then the entire body of the trigger is executed all the way to the bottom \*before any more instructions of the main body are executed\*. This means that execution of a trigger never gets interleaved with the main code. Once a trigger happens, the entire trigger occurs all in one go before the rest of the main body continues.

Do Not Loop a Long Time in a Trigger Body!
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Because the entire body of a trigger will execute all the way to the bottom on *within a single* **physics tick**, *before* any other code continues, it is vital that you not write code in a trigger body that takes a long time to execute. The body of a trigger must be kept quick. An infinite loop in a trigger body could literally freeze all of KSP, because the kOS mod will never finish executing its update.

*As of kOS version 0.14 and higher, this condition is now being checked for* and the script will be **terminated with a runtime error** if the triggers like WHEN/THEN and ON take more than :attr:`Config:IPU` instructions to execute. The sum total of all the code within your WHEN/THEN and ON code blocks MUST be designed to complete within one physicd tick.

**This may seem harsh**. Ideally, kOS would only generate a runtime error if it thought your script was stuck in an **infinite loop**, and allow it to exceed the :attr:`Config:IPU` number of instructions if it was going to finish and just needed a little longer to to finish its work. But, because of a well known problem in computer science called `the halting problem <http://en.wikipedia.org/wiki/Halting_problem>`__, it's literally impossible for kOS, or any other software for that matter, to detect the difference between another program's infinite loop versus another program's loop that will end soon. kOS only knows how long your triggers have taken so far, not how long they're going to take before they're done, or even if they'll be done.

If you suspect that your trigger body would have ended if it was allowed to run a little longer, try setting your :attr:`Config:IPU` setting a bit higher and see if that makes the error go away.

If it does not make the error go away, then you will need to redesign your script to not depend on running a long-lasting amount of code inside triggers.

But I Want a Loop!!
~~~~~~~~~~~~~~~~~~~

If you want a trigger body that is meant to loop, the only acceptable way to do it is to design it to execute just once, but then use the PRESERVE keyword to keep the trigger around for the next physics update. Thus your trigger becomes a sort of "loop" that executes one iteration per **physics tick**.

It is also important to consider the way triggers execute for performance reasons too. Every time you write an expression for a trigger, you are creating a bit of code that gets executed fully to the end before your main body will continue, once each **physics tick**. A complex expression in a trigger condition, which in turn calls other complex LOCK expressions, which call other complex LOCK expressions, and so on, may cause kOS to bog itself down during each physics tick. (And as of version 0.14, it may cause kOS to stop your program and issue a runtime error if it's taking too long.)

Because of how WAIT works, you cannot put a WAIT statement inside a trigger. If you try, it will have no effect. This is because WAIT requires the ability of the program to go to sleep and then in a later physics tick, continue from where it left off. Because triggers run to the bottom entirely within one physics tick, they can't do that.

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
    to be suspended.  Trigger code such as WHEN, ON, LOCK STEERING,
    and LOCK THROTTLE, will continue executing while your program
    is sitting still on the WAIT command.


The Frozen Universe
-------------------

Each **physics** *tick*, the kOS mod wakes up and runs through all the currently loaded CPU parts that are in "physics range" (i.e. 2.5 km), and executes a batch of instructions from your script code that's on them. It is important to note that during the running of this batch of instructions, because no **physics ticks** are happening during it, none of the values that you might query from the KSP system will change. The clock time returned from the TIME variable will keep the same value throughout. The amount of fuel left will remain fixed throughout. The position and velocity of the vessel will remaining fixed throughout. It's not until the next physics tick occurs that those values will change to new numbers. It's typical that several lines of your kerboscript code will run during a single physics tick.

Effectively, as far as the *simulated* universe can tell, it's as if your script runs several instructions in literally zero amount of time, and then pauses for a fraction of a second, and then runs more instructions in literally zero amount of time, then pauses for a fraction of a second, and so on, rather than running the program in a smoothed out continuous way.

This is a vital difference between how a kOS CPU behaves versus how a real world computer behaves. In a real world computer, you would know for certain that time will pass, even if it's just a few picoseconds, between the execution of one statement and the next.

