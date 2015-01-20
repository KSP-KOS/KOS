.. _cpu hardware:

The kOS CPU hardware
====================

While it's possible to write some software without knowing anything about the underlying computer hardware, and there are good design principles that state one should never make assumptions about the computer hardware when writing software, there are still some basic things about how computers work in general that a good programmer needs to be aware of to write good code. Along those lines, the KSP player writing a Kerboscript program needs to know a few basic things about how the simulated kOS CPU operates in order to be able to write more advanced scripts. This page contains that type of information.

.. contents::
    :local:
    :depth: 2

Update Ticks and Physics Ticks
------------------------------

Kerbal Space Program simulates the universe by running the universe in small incremental time intervals that for the purpose of this document, we will call "**physics ticks**\ ". The exact length of time for a physics tick varies as the program runs. One physics tick might take 0.09 seconds while the next one might take 0.085 seconds. (The default setting for the rate of physics ticks is 25 ticks per second, just to give a ballpark figure, but you **must not** write any scripts that depend on this assumption because it's a setting the user can change, and it can also vary a bit during play depending on system load. The setting is a target goal for the game to try to achieve, not a guarantee. If it's a fast computer with a speedy animation frame rate, it will try to run physics ticks less often than it runs animation frame updates, to try to make the physics tick rate match this setting. On the other hand, If it's a slow computer, it will try to sacrifice animation frame rate to archive this number (meaning physics get calculated faster than you can see the effects.) The game will try as hard as it can to keep the physics rate matched to the setting, not faster and not slower, because when the physics rate isn't steady, the simulation breaks down and starts making erroneous things happen, like miscalculating forces on part joints and breaking ships. But however hard the game tries to stick to the setting, it can't do it 100% the same every single moment, thus the need to actually measure elapsed time in the TIME variable in your scripts.

The entire simulated universe is utterly frozen during the duration of a physics tick. For example, if one physics tick occurs at timestamp 10.51 seconds, and the next physics tick occurs 0.08 seconds later at timestamp 10.59 seconds, then during the entire intervening time, at timestamp 10.52 seconds, 10.53 seconds, and so on, nothing moves. The clock is frozen at 10.51 seconds, and the fuel isn't being consumed, and the vessel is at the same position. On the next physics tick at 10.59 seconds, then all the numbers are updated.  The full details of the physics ticks system are more complex than that, but that quick description is enough to describe what you need to know about how kOS's CPU works.

There is another kind of time tick called an **Update tick**. It is similar to, but different from, a **physics tick**. *Update ticks* often occur a bit more often than *physics ticks*. Update ticks are exactly the same thing as your game's Frame Rate. Each time your game renders another animation frame, it performs another Update tick. On a good gaming computer with fast speed and a good graphics card, It is typical to have about 2 or even 3 *Update ticks* happen within the time it takes to have one *physics tick* happen. On a slower computer, it is also possible to go the other way and have *Update ticks* happening *less* frequently than *physics tics*. Basically, look at your frame rate. Is it higher than 25 fps? If so, then your *update ticks* happen faster than your *physics ticks*, otherwise its the other way around.

.. note::

    The kOS CPU runs every **update tick** rather than every **physics tick**.

On each update tick, each kOS CPU that's within physics range (i.e. 2.5 km), wakes up and performs the following steps, in this order:

1. Run the conditional checks of all TRIGGERS (see below)
2. For any TRIGGERS who's conditional checks are true, execute the entire body of the trigger.
3. If there's a pending WAIT statement, check if it's done. If so wake up.
4. If awake, then execute the next :attr:`Config:IPU` number of instructions of the main program.

Note that the number of instructions being executed (CONFIG:IPU) are NOT lines of code or kerboscript statements, but rather the smaller instruction opcodes that they are compiled into behind the scenes. A single kerboscript statement might become anywhere from one to ten or so instructions when compiled.

Triggers
--------

There are multiple things within kerboscript that run "in the background" always updating, while the main script continues on. The way these work is a bit like a real computer's multithreading, but not *quite*. Collectively all of these things are called "triggers".

Triggers are all of the following:

-  LOCKS which are attached to flight controls (THROTTLE, STEERING,
   etc), but not other LOCKS.
-  ON condition { some commands }.
-  WHEN condition THEN { some commands }.

The way these work is that once per **update tick**, all the LOCK expressions which directly affect flight control are re-executed, and then each conditional trigger's condition is checked, and if true, then the entire body of the trigger is executed all the way to the bottom \*before any more instructions of the main body are executed\*. This means that execution of a trigger never gets interleaved with the main code. Once a trigger happens, the entire trigger occurs all in one go before the rest of the main body continues.

Do Not Loop a Long Time in a Trigger Body!
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Because the entire body of a trigger will execute all the way to the bottom on *within a single* **update tick**, *before* any other code continues, it is vital that you not write code in a trigger body that takes a long time to execute. The body of a trigger must be kept quick. An infinite loop in a trigger body could literally freeze all of KSP, because the kOS mod will never finish executing its update.

*As of kOS version 0.14 and higher, this condition is now being checked for* and the script will be **terminated with a runtime error** if the triggers like WHEN/THEN and ON take more than :attr:`Config:IPU` instructions to execute. The sum total of all the code within your WHEN/THEN and ON code blocks MUST be designed to complete within one update tick.

**This may seem harsh**. Ideally, kOS would only generate a runtime error if it thought your script was stuck in an **infinite loop**, and allow it to exceed the :attr:`Config:IPU` number of instructions if it was going to finish and just needed a little longer to to finish its work. But, because of a well known problem in computer science called `the halting problem <http://en.wikipedia.org/wiki/Halting_problem>`__, it's literally impossible for kOS, or any other software for that matter, to detect the difference between another program's infinite loop versus another program's loop that will end soon. kOS only knows how long your triggers have taken so far, not how long they're going to take before they're done, or even if they'll be done.

If you suspect that your trigger body would have ended if it was allowed to run a little longer, try setting your :attr:`Config:IPU` setting a bit higher and see if that makes the error go away.

If it does not make the error go away, then you will need to redesign your script to not depend on running a long-lasting amount of code inside triggers.

But I Want a Loop!!
~~~~~~~~~~~~~~~~~~~

If you want a trigger body that is meant to loop, the only acceptable way to do it is to design it to execute just once, but then use the PRESERVE keyword to keep the trigger around for the next update. Thus your trigger becomes a sort of "loop" that executes one iteration per **update tick**.

It is also important to consider the way triggers execute for performance reasons too. Every time you write an expression for a trigger, you are creating a bit of code that gets executed fully to the end before your main body will continue, once each **update tick**. A complex expression in a trigger condition, which in turn calls other complex LOCK expressions, which call other complex LOCK expressions, and so on, may cause kOS to bog itself down during each update. (And as of version 0.14, it may cause kOS to stop your program and issue a runtime error if it's taking too long.)

Because of how WAIT works, you cannot put a WAIT statement inside a trigger. If you try, it will have no effect. This is because WAIT requires the ability of the program to go to sleep and then in a later update tick, continue from where it left off. Because triggers run to the bottom entirely within one update tick, they can't do that.

Wait!!!
~~~~~~~

Any WAIT statement causes the kerboscript program to immediately stop executing the main program where it is, even if far fewer than :attr:`Config:IPU` instructions have been executed in this **update tick**. It will not continue the execution until at least the next **update tick**, when it will check to see if the WAIT condition is satisfied and it's time to wake up and continue.

Therefore ANY WAIT of any kind will guarantee that your program will allow at least one **update tick** to have happened before continuing. If you attempt to::

    WAIT 0.001.

But the duration of the next update tick is actually 0.09 seconds, then you will actually end up waiting at least 0.09 seconds. It is impossible to wait a unit of time smaller than one update tick. Using a very small unit of time in a WAIT statement is an effective way to force the CPU to allow a update tick to occur before continuing to the next line of code. Similarly, if you just say::

    WAIT UNTIL TRUE.

Then even though the condition is immediately true, it will still wait one update tick to discover this fact and continue.

The Frozen Universe
-------------------

Each **update** *tick*, the kOS mod wakes up and runs through all the currently loaded CPU parts that are in "physics range" (i.e. 2.5 km), and executes a batch of instructions from your script code that's on them. It is important to note that during the running of this batch of instructions, because no **physics ticks** are happening during it, none of the values that you might query from the KSP system will change. The clock time returned from the TIME variable will keep the same value throughout. The amount of fuel left will remain fixed throughout. The position and velocity of the vessel will remaining fixed throughout. It's not until the next physics tick occurs that those values will change to new numbers. It's typical that several lines of your kerboscript code will run during a single update tick.

Effectively, as far as the *simulated* universe can tell, it's as if your script runs several instructions in literally zero amount of time, and then pauses for a fraction of a second, and then runs more instructions in literally zero amount of time, then pauses for a fraction of a second, and so on, rather than running the program in a smoothed out continuous way.

If your animation rate is slow enough, it gets even weirder. If your animation *update ticks* occur less often than your *physics ticks*, then it's as if your program spends the majority of the time paused, and only occasionally wakes up to execute a short burst of instructions.

Because of the difference between *update ticks* and *physics ticks*, it's entirely possible that your kOS script runs multiple updates in a row while the universe is still frozen, or it's possible to go the other way around and have the universe move more than one physics tick before your program has time to notice and react. A well written kOS script should be able to handle both cases.

This is a vital difference between how a kOS CPU behaves versus how a real world computer behaves. In a real world computer, you would know for certain that time will pass, even if it's just a few picoseconds, between the execution of one statement and the next.

So Why Does This Matter?
~~~~~~~~~~~~~~~~~~~~~~~~

The reason this matters is because of code that tries to do things like this: Imagine something like this inside a script designed to hover in place::

    PRINT "Waiting until altitude is".
    PRINT "holding stable within 0.1 meters.".

    SET PREV_ALT TO -99999. // bogus start value
    UNTIL ABS( PREV_ALT - SHIP:ALTITUDE ) < 0.1 {

      SET PREV_ALT TO SHIP:ALTITUDE.

      // Assume there's fancy PID controller
      // commands here, omitted for this example.

    }

This bit of code, if you assume you've written a nice bit of code where the comment is, looks like it would make sense at first. It looks like it should work. It records the previous altitude at the start of the loop body, and if the altitude hasn't changed by much by the start of the next loop, it assumes the altitude has become stable and it stops.

BUT, due to the frozen nature of the measurements during a **physics tick**, it's entirely possible, and quite likely, that the loop would exit prematurely because no simulation time has passed between the two altitude measurements. The previous altitude and the current altitude are the same. Not because the vessel has no vertical motion, but because the loop is executing fast enough to finish more than one iteration within the same **physics tick**. The two altitude measurements are the same because no time has passed in the simulated universe.

The Fix: Wait for Time to Change
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

If you are executing a loop like the one above in which it is absolutely vital that the next iteration of the loop must occur in a *different* **physics tick** than the previous one, so that it can take *new* measurements that are different, the solution is to use a WAIT statement that will delay until there's evidence that the physics clock has moved a tick.

The most effective way to do that is to check the :ref:`time` and see if it's different than it was before. As long as you are still within the same *physics tick*, the TIME will not move::

    PRINT "Waiting until altitude is holding stable within 0.1 meters.".

    SET PREV_ALT TO -99999. // bogus start value
    UNTIL ABS( PREV_ALT - SHIP:ALTITUDE ) < 0.1 {

      SET PREV_ALT TO SHIP:ALTITUDE.

      // Assume there's fancy PID controller
      // commands here, omitted for this example.

      SET TIMESTAMP TO TIME:SECONDS.
      WAIT UNTIL TIME:SECONDS > TIMESTAMP. // clock will not move
                                           // until we are in a new
                                           // physics tick.
    }

A More Elegant Solution
~~~~~~~~~~~~~~~~~~~~~~~

Thanks to user *Cairan*, who suggested this very good idea in the
forums. You may put this code up near the top of your script::

    // force it to trigger immediately the first time through
    SET LASTPHYS TO -99999.

    LOCK PHYSICS TO MIN(1,FLOOR((TIME:SECONDS-LASTPHYS) / 0.04 )).

    WHEN PHYSICS THEN {
      SET LASTPHYS TO TIME:SECONDS.

      // Store your measurements from
      // the physical world here during
      // the body of this WHEN

      PRESERVE.
    }

An Even Better Solution
~~~~~~~~~~~~~~~~~~~~~~~

There has been talk of instituting a special command: WAIT UNTIL PHYSICS that will sleep until there has been a physics update, and it's a good idea but it hasn't been implemented yet.
