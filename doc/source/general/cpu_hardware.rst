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

Kerbal Space Program simulates the universe by running the universe in
small incremental time intervals that for the purpose of this
document, we will call "**physics ticks**". The exact length of time
for a physics tick varies as the program runs. One physics tick might
take 0.02 seconds while the next one might take 0.021 seconds and maybe
the next one takes 0.019 seconds.

The game *tries* to simulate the universe using 50 physics ticks per
second (0.02 seconds per tick), but there is no guarantee it succeeds
at this.  There is a lot of variation depending on how fast your
computer is, and how heavily you are loading it with large rockets or
complex mods.

If the KSP game is unable to execute *physics ticks* fast enough to
keep up the 50-per-second rate, that's when you see the time display
in the upper-left of the Kerbal Space Program screen turn red as a
warning that simulation is getting coarse-grain and might start
getting error-prone because of it.

Note that the game may also resort to slowing down the presentation
of the simulated world in order to make the simulated time still
be fine-grained at 0.02 seconds per physics tick even though the
computer can't keep up with it.  In this state it is showing
the game in slow motion.  This is what it means when the clock in
the upper-left corner of the screen is yellow.

The relevant take-away from that is this: When calculating physics
formulas, never assume elapsed time moves in constant amounts.  It
is *typically* about 0.02 seconds per physics tick, but not reliably
so.  You need to actually measure elapsed time in the TIME:SECONDS
variable in any formulas that depend on delta time.

The entire simulated universe is utterly frozen during the duration of
a physics tick. For example, if one physics tick occurs at timestamp
10.50 seconds, and the next physics tick occurs 0.02 seconds later at
timestamp 10.52 seconds, then during all the intervening times, such
as at timestamp 10.505 seconds, 10.51 seconds, and 10.515 seconds
nothing has moved. ``TIME:SECONDS`` will claim the time is still 10.50
seconds during that whole time, and the fuel isn't being consumed, and
the vessel is at the same position. On the next physics tick at 10.52
seconds, then all the numbers are updated.  The full details of the
physics ticks system are more complex than that, but that quick
description is enough to describe what you need to know about how kOS's
CPU works.

**Physics ticks are NOT your FPS:**
There is another kind of time tick called an **Update tick**. It is
similar to, but different from, a **physics tick**. *Update ticks*
often occur a bit more often than *physics ticks*. Update ticks are
exactly the same thing as your game's Frame Rate. Each time your game
renders another animation frame, it performs another Update tick. 
Essentially, *physics ticks* get the first dibs on execution time,
while *update ticks* use up whatever time is leftover after that.
If your computer is super fast so there's a lot of leftover time
after *physics ticks* are satisfied, it just uses that time to make
more *update ticks*, not to make more *physics ticks*.  A fast
computer might have 2 or 3 *update ticks* per *physics tick*.  A slow
computer might only be able to manage 1 *update tick* per *physics
tick*, or in extreme cases, less than 1 so animation is in fact
painting the picture at a slower frame rate than the frame rate that
the physical world is actually being simulated under the hood.

It is important to note that versions of kOS prior to v0.17 executed
program code during these *update ticks* so they were tied to your 
animation FPS.  But versions more recent than that started executing
code on *physics ticks*, as is more proper for the simulation, and
to make script behvaior more consistent across different computers with
different frame rates.

.. _electricdrain:

Electric Drain
--------------

Real world CPUs often have low power modes, and sleep modes, and these are
vital to long distance probes.  In these modes the computer deliberately
runs slowly in order to use less power, and then the program can tell it to
speed up to normal speed again when it needs to wake up and do something.

Older versions of kOS implemented this concept with a constant electric drain regardless of CPU load.  As of version 0.19.0, this concept is simplified by just draining electric charge by "micropayments" of charge per instruction executed.

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

There are multiple things within kerboscript that run "in the background"
always updating, while the main script continues on. The way these work is
a bit like a real computer's interrupt handling system, but not *quite*.

Collectively all of these things are called "triggers".

Triggers come in these varieties:

.. _recurring_trigger:

* **Recurring triggers:** Triggers that once they are started keep getting
  called again and again on a regular basis, until they are made to stop.

  * LOCKS which are attached to flight controls (THROTTLE, STEERING,
    etc), but not other LOCKS.
  * User Delegates assigned to recurrently updating suffixes such as
    :attr:`VecDraw:VECUPDATER`.
  * WHEN and ON triggers:

    * ``WHEN condition THEN { some commands }``
    * ``ON condition { some commands }``

.. _callback_once_trigger:

* **CallbackOnce triggers:** Triggers that only happen once per event.  To
  make the trigger happen again, the event has to happen again:

  * Callback delegates you tell the system to call when the user
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

Prior to kOS 0.19.3, this section was quite different but large changes to how triggers work required a re-write of this whole page. Any old kOS scripts you find that were written prior to kOS 0.19.3 that used triggers might have different behaviour because of this.

.. _trigger_steering:

Triggers for Cooked Steering
~~~~~~~~~~~~~~~~~~~~~~~~~~~~

*This is a kind of* :ref:`recurring trigger <recurring_trigger>`.

The ``lock`` expressions associated with :ref:`Cooked Control <cooked>`,
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

*This is a kind of* :ref:`recurring trigger <recurring_trigger>`.

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
to take advantage of* :ref:`short circuit boolean <short_circuit>`
*logic by putting the fastest part of the check first.*

Triggers for GUI callbacks
~~~~~~~~~~~~~~~~~~~~~~~~~~

Another type of trigger is the callback delegates that you can
write for the :ref:`GUI system <gui>` when using the
:ref:`Callback technique <gui_callback_technique>`.  (For example,
using :attr:`Button:ONCLICK`, :attr:`Slider:ONCHANGE`, and so on.)

When you give a GUI a callback hook to call, the CPU will implement
that as a trigger as well.  When you click the button or move the
slider, etc, then kOS will interrupt your program at the next available
opportunity (usually the start of the next IPU's worth of instructions),
to call your callback delegate.

.. _wait_in_trigger:

Wait in a Trigger
~~~~~~~~~~~~~~~~~

While ``WAIT`` is possible from inside a trigger and it won't crash
the script to use it, it's probably not a good design choice to use
``WAIT`` inside a trigger.  Triggers should be designed to execute
all the way through to the end in one fast pass, if possible.

Exception: If you are careful, there is a built-in function you
can call that will have your trigger willingly relinquish its priority
increase, reducing it back down to whatever the priority was before
it rudely interrupted things. Doing that can allow other triggers of
equal priority to itself to interrupt it again.  To see how this works,
look at :func:`DROPPRIORITY()`, explained below on this page.  In general,
however, it's a better idea not to use this unless you fully understand
how the prioriy system here works.

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

Exception: If you are careful, there is a built-in function you
can call that will have your trigger willingly relinquish its priority
increase, reducing it back down to whatever the priority was before
it rudely interrupted things. Doing that can allow other triggers of
equal priority to itself to interrupt it again.  To see how this works,
look at :func:`DROPPRIORITY()`, explained below on this page.  In general,
however, it's a better idea not to use this unless you fully understand
how the prioriy system here works.

But I Want a Loop!!
~~~~~~~~~~~~~~~~~~~

If you want a trigger body that is meant to loop a long time, the only
workable way to do it is to design it to execute just once, but
then make it return true (or use the ``preserve`` keyword, which is
basically the same thing) to keep the trigger around for the next
**physics tick**. Thus your trigger becomes a sort of "loop" that
executes one iteration per **physics tick**.

.. _interrupt_priority:

Trigger Interrupt Priority
--------------------------

.. versionadded:: 1.1.6.0
    The multiple priorities of interruption described below (GUI callbacks
    being lower priority than recurring callbacks) were introduced in
    kOS v1.1.6.0

When the CPU wants to interrupt the normal program flow and redirect it
into a trigger, there are some priority rules for which kind of trigger
is allowed to interrupt the program flow depending on what the program
is doing right now.  This is accomplished by having a few priority
levels, shown in this list:

* Priority 30: :ref:`Cooked control Interrupts <trigger_steering>` (i.e. LOCK STEERING)
* Priority 20: :ref:`Recurring Interrupts <recurring_trigger>` (i.e. WHEN or ON)
* Priority 10: :ref:`Callback-Once Interrupts <callback_once_trigger>` (i.e. GUI callbacks)
* Priority 0: Normal (non-interrupting) code.

**A Trigger will only interrupt something of lower priority than itself**.

If the CPU is currently running normal non-interrupting) code, then any
trigger is allowed to interrupt it.  But if it is currently already in
the middle of running a trigger, and another trigger of equal priority
wants to interrupt it, the second trigger will wait until the first
trigger is over and the CPU has dropped back down to normal code
before the second trigger will be allowed to happen.

The reason the priorities are laid out the way they are is that
the assumption is that recurring interrupts need to be the
highest priority because they're often time sensitive and need
to happen again and again with speed, while the callback-once
interrupts are probably not as time-sensitive since they respond
to one-shot events like user clicks.

**most triggers cannot interrupt *themselves* if they're still running**.

When you have recurring triggers that keep re-running themselves
again and again, the way they work is that they wait till the previous
instance of themselves has finished running before a new instance will
happen.  Thus a recurring trigger will *not* run every single **physics
tick** if the trigger takes longer than 1 tick to finish.  Instead it
will wait for the start of the next **physics tick** *after* the current
execution of the trigger is over.  (This is to prevent it from queuing
up calls faster than they get dispatched, which would make a backlog.)

These priorities are subject to change in later future versions of
kOS.  Right now they're pretty coarse-grain, which is why they count
by 10's - so there is room to split them up and make them more
fine-grained if that becomes necessary later.  Never write code that
is too dependant on the priorities being exactly this way.  (This is
why these numbers aren't even exposed to the script at the moment,
to avoid that design pattern.)


.. _drop_priority:

Deliberately reducing your priority in long running triggers
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Normally if you did something like this::

    local done is false.

    set Gwin to GUI(200).
    set b1 to Gwin:addbutton("beep").
    set b1:onclick to { getvoice(0):play(note(300,0.2)). }.
    set b2 to GWin:addbutton("count").
    set b2:onclick to count@.
    set b3 to Gwin:addbutton("quit").
    set b3:onclick to { set done to true. }.

    GWin:show().
    wait until done.
    GWin:Dispose().

    function count {
      local i is 5.
      until i = 0 {
        print "Counting.. " + i.
        set i to i - 1.
        wait 1.
      }
    }

It would mean that while you press the "count" button, and it prints the
countdown from 5 to 1, the other buttons, including "beep" and "quit"
would have no effect until the countdown is done.  Because ``count()``
is the callback for a GUI button, it runs at a higher than normal priority,
which means it won't let itself get interrupted by other GUI callbacks.
Instead those other GUI callbacks will be delayed until count() is done.

If you wish, you can cause your trigger, or callback, to deliberately
relinquish its hold on other interrupts, allowing them to interrupt it
despite the fact that it is itself in the middle of an interrupt.
You do this by  deliberately reducing your current priority level
back down a step to whatever it was prior to being incresed by the
interrrupt, which is what this special built-in function does:

.. function:: DROPPRIORITY()

    After this built-in function is executed by a trigger's body,
    the current interrupt priority is dropped back down to whatever the
    priority of the code you interrupted was.  This is your trigger's
    way of saying "I don't actually want to block interrupts anymore.
    Please let me be interrupted just as much as whatever *I*
    interrupted was allowed to be interrupted."

    SO, for example, if Priority 0 code (normal code) got interrupted
    by priority 10 code (GUI callback code), and the GUI callback
    code executed ``DROPPRIORITY``, then it would now be running at
    priority 0 instead of 10, because priority 0 is what got interrupted,
    and thus allow other GUI code to interrupt it again.

    On the other hand, if GUI callback code (priority 10) got 
    interrupted by WHEN-THEN code (priority 20), and the WHEN-THEN
    code had called DROPPRIORITY(), then the priority level of 
    that pass through the WHEN-THEN would only be dropped down to
    10, NOT all the way to 0, because it was interrupting priority 10
    code.
    
    The reason it works this way (instead of just dropping it all the
    way down to normal (0) priority directly) is that, effectively,
    it means a trigger only has the authority to undo its own
    priority increase that it caused itself.  It can't force the
    priority down to something less than the code that got interrupted
    had to begin with.  Had it been allowed to do that, it could have
    been a back-door to circumventing the priority of the thing
    that it interrupted.

    Be aware that once you ``DROPPRIORITY()``, you also are making it
    so that the SAME trigger you are currently inside of could fire off
    again too.  It may be a good idea to protect yourself against that,
    if it is not desired, by setting a flag variable to record the fact
    that you are inside the trigger at the time and should not re-run it,
    and then test this flag variable at the top of your trigger code,
    skipping the body if it's set.

So in the above GUI example, if you added ``DROPPRIORITY`` as shown
in the edited version of the example, below, then the other buttons
like the "beep" button, would work while the count() is happening::

    local done is false.

    set Gwin to GUI(200).
    set b1 to Gwin:addbutton("beep").
    set b1:onclick to { getvoice(0):play(note(300,0.2)). }.
    set b2 to GWin:addbutton("count").
    set b2:onclick to count@.
    set b3 to Gwin:addbutton("quit").
    set b3:onclick to { set done to true. }.

    GWin:show().
    wait until done.
    GWin:Dispose().

    function count {

      DROPPRIORITY(). // <--- NEW LINE ADDED HERE

      local i is 5.
      until i = 0 {
        print "Counting.. " + i.
        set i to i - 1.
        wait 1.
      }
    }

Once you call ``DROPPRIORITY()``, then from then on, you are effectively no
longer a trigger, as far as the interruption system is concerned.

BE CAREFUL - if you do this then it is possible for the same trigger or
callback to interrupt *itself* again.  In the above example where
DROPPRIORITY() was added, you could press the "count" button twice in
quick succession and one press would interrupt the other.  It's up to you,
if you use ``DROPPRIORITY()`` to deal with this problem and stop it from
happening if it's a bad thing for your program.  You can do this by
setting a flag that checks if your trigger is already running and if so,
skips it, like so::

    local count_is_running is false.
    function count {

      if not(count_is_running) {
        set count_is_running to true.
        DROPPRIORITY().

        local i is 5.
        until i = 0 {
          print "Counting.. " + i.
          set i to i - 1.
          wait 1.
        }
        set count_is_running to false.
      }
    }

Again, using ``DROPPRIORITY()`` is an advanced topic that should be avoided
until after you understand what you've read here.  Even then, it's usually
simpler and better to just avoid using it and instead design your script in
such a way that it's unnecessary to use it.  (It's only necessary to use it
if you have interrupt triggers that run a long time instead of finishing
quickly like they should.)

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

.. versionadded:: 1.1.6.0
    As of version 1.1.6.0, the entire layout of the CPU update loop
    was re-written to handle the new trigger priority system.


The guts behind the kOS emulated CPU is the main loop explained below
that runs once per **physics tick**.  (A "FixedUpdate" in Unity3d terms).

* 1. instructionsExecuted = 0
* 2. how_many_instructions_this_time = config:IPU plus or minus one. (It
  wavers slightly because doing so can help prevent edge cases where
  the interrupt triggers syhnc up perfectly with the end of an update
  and thus starve main code.)
  TODO: THIS +/- 1 thing ISN'T TRUE IN THE CODE YET.  I'm WRITING THIS
  DOCUMENT BEFORE I'M IMPLEMENTING THIS.  COME BACK AND REMOVe THIS
  TODO WHEN I ACTUALLY IMPLEMENT THIS.
* 3. while instructionsExecuted < how_many_instructions_this_time do this:

  * 3.1 Execute one instruction.  It will move the instruction pointer +1
    to the next opcode in the program, or in the case of a jump opcode, by
    some other number than +1.
  * 3.2 Break out early from this loop if instruction was a WAIT or if program
    is over or errored out.
  * 3.3 Check if there's enabled triggers with priority allowing an interrupt.

     * 3.3.1 - If so then insert a "faked" subroutine call right now that jumps
       to trigger's code, with the stack arranged so it will return back to
       the current instruction pointer when it's done.

  * 3.4 increment instructionsExecuted.

* 4. Any trigger that wanted to interrupt but was waiting for the next
  **physics tick** boundary before it did so (recurring triggers are
  usually like this), gets moved from the "pending" trigger queue to
  the "active" queue so it will get executed next time on step 3.3 above).

How an interrupt works
~~~~~~~~~~~~~~~~~~~~~~

Whenever the CPU decides to cause an interrupt in step 3.3 above, it does
so by simulating how a subroutine call normally works in the system.  It
does the following:

* Create a subroutine context record which has its "came from" instruction
  pointer set to the current instruction pointer, and its "came from"
  priority level set to the current priority level.
* Push that subroutine context record on the callstack just like a normal
  subroutine call would do.
* Set the instruction pointer to the first instruction of the trigger's
  code.
* Change the CPU priority to match the new priority of the interrupt.

Now if it just lets the CPU loop run as normal after that, it will be
inside the trigger code, and when it reaches the ``Return`` instruction at
the end of the trigger code, it will pop the context record off the call
stack and end up back where it was now before the interruption happened.
Not only does ``Return`` go back to the instruction the call came from,
but it also drops back down to the priority level the call came from.

Because the kOS CPU is a pure stack machine, with all variables and
scopes stored on the stack, this ensures everything will be just like
it was before the interruption, and the main code can continue on,
unaware that it was even interrupted.

Interrupts that happen at the same time
:::::::::::::::::::::::::::::::::::::::

When more than one trigger of the same priority are in the queue and both
try to interrupt at the same time before either one has started running,
then what happens is this:  The first trigger gets its interrupt to occur,
but the second trigger, because the first trigger raised the priority
level of the CPU, will refuse to interrupt the first one... UNTIL
the first one gets to the bottom and does its ``Return``.  Then before
executing the next normal priority instruction, the CPU hits point 3.3 in
the loop above again with the priority level now reduced back to normal
because the first trigger has returned, and right away it notices the
second trigger still in the queue, and inserts a call to it before the
main code can continue.

Thus the two interrupts happen back to back before normal code continues.


Note that the number of instructions being executed (CONFIG:IPU) are NOT lines of code or kerboscript statements, but rather the smaller instruction opcodes that they are compiled into behind the scenes. A single kerboscript statement might become anywhere from one to ten or so instructions when compiled.

.. _frozen:

The Frozen Universe
-------------------

Each **physics** *tick*, the kOS mod wakes up and runs through all the currently loaded CPU parts that are in "physics range" (i.e. 2.5 km), and executes a batch of instructions from your script code that's on them. It is important to note that during the running of this batch of instructions, because no **physics ticks** are happening during it, none of the values that you might query from the KSP system will change. The clock time returned from the TIME variable will keep the same value throughout. The amount of fuel left will remain fixed throughout. The position and velocity of the vessel will remaining fixed throughout. It's not until the next physics tick occurs that those values will change to new numbers. It's typical that several lines of your kerboscript code will run during a single physics tick.

Effectively, as far as the *simulated* universe can tell, it's as if your script runs several instructions in literally zero amount of time, and then pauses for a fraction of a second, and then runs more instructions in literally zero amount of time, then pauses for a fraction of a second, and so on, rather than running the program in a smoothed out continuous way.

This is a vital difference between how a kOS CPU behaves versus how a real world computer behaves. In a real world computer, you would know for certain that time will pass, even if it's just a few picoseconds, between the execution of one statement and the next.
