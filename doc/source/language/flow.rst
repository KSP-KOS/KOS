.. _flow:

Flow Control
============

.. contents::
    :local:
    :depth: 1

.. index:: BREAK
.. _break:

``BREAK``
---------

Breaks out of a loop::

    SET X TO 1.
    UNTIL 0 {
        SET X TO X + 1.
        IF X > 10 { BREAK. } // Exits the loop when
                             // X is greater than 10
    }

.. index:: IF
.. _if:

``IF`` / ``ELSE``
-----------------

Checks if the expression supplied returns true. If it does, ``IF`` executes the following command block. Can also have an optional ``ELSE`` to execute when the ``IF`` condition is not true. ``ELSE`` can have another ``IF`` after it, to make a chain of ``IF``/``ELSE`` conditions::

    SET X TO 1.
    IF X = 1 { PRINT "X equals one.". }     // Prints "X equals one."
    IF X > 10 { PRINT "X is greater than ten.". }  // Does nothing

    // IF-ELSE structure:
    IF X > 10 { PRINT "X is large".  } ELSE { PRINT "X is small".  }

    // An if-else ladder:
    IF X = 0 {
        PRINT "zero".
    } ELSE IF X < 0 {
        PRINT "negative".
    } ELSE {
        PRINT "positive".
    }

.. note::
    The period (``.``) is optional after the end of a set of curly braces like so::

        // both of these lines are fine
        IF TRUE { PRINT "Hello". }
        IF TRUE { PRINT "Hello". }.

    In the case where you are using the ``ELSE`` keyword, you must *not* end the previous ``IF`` body with a period, as that terminates the ``IF`` command and causes the ``ELSE`` keyword to be without a matching ``IF``::

        // works:
        IF X > 10 { PRINT "Large". }  ELSE { PRINT "Small". }.

        // syntax error - ELSE without IF.
        IF X > 10 { PRINT "Large". }. ELSE { PRINT "Small". }.

.. index:: LOCK
.. _lock:

``LOCK``
--------

Locks an identifier to an expression. Each time the identifier is used in an expression, its value will be re-calculated on the fly::

    SET X TO 1.
    LOCK Y TO X + 2.
    PRINT Y.         // Outputs 3
    SET X TO 4.
    PRINT Y.         // Outputs 6

LOCK follows the same scoping rules as the SET command.  If the variable
name used already exists in local scope, then the lock command creates 
a lock function that only lasts as long as the current scope and then 
becomes unreachable after that.  If the variable name used does not exist
in local scope, then LOCK will create it as a global variable, unless
@NOLAZYGLOBAL is set to off, in which case it will be an error.

Note that a LOCK expression is extremely similar to a user function.
Every time you read the value of the "variable", it executes the expression
again.

.. note::
    If a ``LOCK`` expression is used with a flight control such as ``THROTTLE`` or ``STEERING``, then it will get continually evaluated in the background :ref:`each update tick <cpu hardware>`.

.. index:: UNLOCK
.. _unlock:

``UNLOCK``
----------

Releases a lock on a variable. See ``LOCK``::

    UNLOCK X.    // Releases a lock on variable X
    UNLOCK ALL.  // Releases ALL locks

.. index:: UNTIL
.. _until:

``UNTIL``
---------

Performs a loop until a certain condition is met::

    SET X to 1.
    UNTIL X > 10 {      // Prints the numbers 1-10
        PRINT X.
        SET X to X + 1.
    }

Note that if you are creating a loop in which you are watching a physical value that you expect to change each iteration, it's vital that you insert a small WAIT at the bottom of the loop like so::

    SET PREV_TIME to TIME:SECONDS.
    SET PREV_VEL to SHIP:VELOCITY.
    SET ACCEL to V(9999,9999,9999).
    PRINT "Waiting for accellerations to stop.".
    UNTIL ACCEL:MAG < 0.5 {
        SET ACCEL TO (SHIP:VELOCITY - PREV_VEL) / (TIME:SECONDS - PREV_TIME).
        SET PREV_TIME to TIME:SECONDS.
        SET PREV_VEL to SHIP:VELOCITY.

        WAIT 0.001.  // This line is Vitally Important.
    }

The full explanation why is :ref:`in the CPU hardware description
page <cpu hardware>`.

.. index:: FOR
.. _for:

``FOR``
-------

Loops over a list collection, letting you access one element at a time. Syntax::

    FOR variable1 IN variable2 { use variable1 here. }

Where:

- `variable1` is a variable to hold each element one at a time.
- `varaible2` is a LIST variable to iterate over.

Example::

    PRINT "Counting flamed out engines:".
    SET numOUT to 0.
    LIST ENGINES IN MyList.
    FOR eng IN MyList {
        IF ENG:FLAMEOUT {
            set numOUT to numOUT + 1.
        }
    }
    PRINT "There are " + numOut + "Flamed out engines.".

.. index:: WAIT
.. _wait:

``WAIT``
--------

Halts execution for a specified amount of time, or until a specific set of criteria are met. Note that running a ``WAIT UNTIL`` statement can hang the machine forever if the criteria are never met. Examples::

    WAIT 6.2.                     // Wait 6.2 seconds
    WAIT UNTIL X > 40.            // Wait until X is greater than 40
    WAIT UNTIL APOAPSIS > 150000. // You can see where this is going

Note that any ``WAIT`` statement, no matter what the actual expression is, will always result in a wait time that lasts at least :ref:`one physics tick <cpu hardware>`.

.. index:: WHEN
.. _when:

``WHEN`` / ``THEN``
-------------------

Executes a command when a certain criteria are met. Unlike ``WAIT``, ``WHEN``
does not halt execution. It starts a check in the background that will keep actively looking for the trigger condition while the rest of the code continues. When it triggers, the body after the ``THEN`` will execute exactly once, after which the trigger is removed unless the ``PRESERVE`` is used, in which case the trigger is not removed.

The body of a ``THEN`` or an ``ON`` statement interrupts the normal flow of a **kOS** program. When the event that triggers the body happens, the main **kOS** program is paused until the body of the ``THEN`` completes.

.. warning::
    With the advent of :ref:`local variable scoping <trigger_scope>` in kOS
    version 0.17 and above, it's important to note that the variables
    used within the expression of a WHEN or an ON statement should
    be GLOBAL variables or the results are unpredictable.  If local
    variables were used, the results could change depending on where
    you are within the execution at the time.  

.. warning::
    Do not make the body of a ``WHEN``/``THEN`` take a long time to execute. If you attempt to run code that lasts too long in the body of your ``WHEN``/``THEN`` statement, :ref:`it will cause an error <cpu hardware>`. Avoid looping during ``WHEN``/``THEN`` if you can. For details on how to deal with this, see the :ref:`tutorial on design patterns <designpatterns>`.

.. note::
    .. versionchanged:: 0.12
        **IMPORTANT BREAKING CHANGE:** In previous versions of **kOS**, the body of a ``WHEN``/``THEN`` would execute simultaneously in the background with the rest of the main program. This behavior has changed as of version *0.12* of **kOS**, as described above, and scripts that used to rely on this behavior will not work with version *0.12* of **kOS**

Example::

    WHEN BCount < 99 THEN PRINT BCount + " bottles of beer on the wall”.

    // Watch in the background for when the altitude is high enough.
    // Once it is, then turn on the solar panels and action group 1
    WHEN altitude > 70000 THEN {
        PRINT "ACTIVATING PANELS AND AG 1.".
        PANELS ON.
        AG1 ON.
    }

A ``WHEN``/``THEN`` trigger is removed when the program that created it exits, even if it has not occurred yet. The ``PRESERVE`` can be used inside the ``THEN`` clause of a ``WHEN`` statement. If you are going to make extensive use of ``WHEN``/``THEN`` triggers, it's important to understand more details of how they :ref:`work in the kOS CPU <cpu hardware>`.

.. index:: ON
.. _on_trigger:

``ON``
------

The ``ON`` command is almost identical to the ``WHEN``/``THEN`` command. ``ON`` sets up a trigger in the background that will run the selected command exactly once when the boolean variable changes state from true to false or from false to true. This command is best used to listen for action group activations.

Just like with the ``WHEN``/``THEN`` command, the ``PRESERVE`` command can be used inside the code block to cause the trigger to remain active and not go away.

.. warning::
    With the advent of :ref:`local variable scoping <scope>` in kOS
    version 0.17 and above, it's important to note that the variables
    used within the expression of a WHEN or an ON statement should
    be GLOBAL variables or the results are unpredictable.  If local
    variables were used, the results could change depending on where
    you are within the execution at the time.  

How does it differ from ``WHEN``/``THEN``? The ``WHEN``/``THEN`` triggers are executed whenever the conditional expression *becomes true*. ``ON`` triggers are executed whenever the boolean variable *changes state* either from false to true or from true to false.

The body of an ``ON`` statement can be a list of commands inside curly braces, just like for ``WHEN``/``THEN``. Also just like with ``WHEN``/``THEN``, the body of the ``ON`` interrupts all of **KSP** while it runs, so it should be designed to be a short and finish quickly without getting stuck in a long loop::

    ON AG3 {
       PRINT "Action Group 3 Activated!”.
    }
    ON SAS PRINT "SAS system has been toggled”.
    ON AG1 {
        PRINT "Action Group 1 activated.".
        PRESERVE.
    }

.. warning::
    DO NOT make the body of an ``ON`` statement take a long time to execute. If you attempt to run code that lasts too long in the body of your ``ON`` statement, :ref:`it will cause an error <cpu hardware>`. For general help on how to deal with this, see the :ref:`tutorial on design patterns <designpatterns>`.

Avoid looping during ``ON`` code blocks if you can. If you are going to make extensive use of ``ON`` triggers, it's important to understand more details of how they :ref:`work in the kOS CPU <cpu hardware>`.

.. index:: PRESERVE
.. _preserve:

``PRESERVE``
------------

``PRESERVE`` is a command keyword that is only valid inside of ``WHEN``/``THEN`` and ``ON`` code blocks.

When a ``WHEN``/``THEN`` or ``ON`` condition is triggered, the default behavior is to execute the code block body exactly once and only once, and then the trigger condition is removed and the trigger will never occur again.

To alter this, execute the ``PRESERVE`` command anywhere within the body of the code being executed and it tells the **kOS** computer to keep the trigger condition active. When it finishes executing the code block of the trigger, if ``PRESERVE`` has happened anywhere within that run of the block of code, it will not remove the trigger. Instead it will allow it to re-trigger, possibly as soon as the very next tick. If the ``PRESERVE`` keyword is executed again and again each time the trigger occurs, the trigger could remain active indefinitely.

The following example sets up a continuous background check to keep looking for if there's no fuel in the current stage, and if there is, then it activates the next stage, but no more often than once every half second. Once more than ``NUMSTAGES`` have happened, it allows the check to stop executing but it keeps the check alive until that happens::

    SET NUMSTAGES TO 5.
    SET COOLDOWN_START TO 0.

    WHEN (TIME:SECONDS > COOLDOWN_START + 0.5) AND STAGE:LIQUIDFUEL = 0 {
        SET COOLDOWN_START TO TIME:SECONDS.
        STAGE.
        SET NUMSTAGES TO NUMSTAGES - 1.
        IF NUMSTAGES > 0 {
            PRESERVE.
        }
    }

    // Continue to the rest of the code

.. index:: Boolean Operators
.. _booleans:

``DECLARE FUNCTION``
--------------------

Covered in more depth :ref:`elsewhere in the documentation <user_functions>`, 
the ``DECLARE FUNCTION`` statement creates a user-defined function that
you can then call elsewhere in the code.

Boolean Operators
-----------------

All conditional statements, like ``IF``, can make use of boolean operators. The order of operations is as follows:

- ``=`` ``<`` ``>`` ``<=`` ``>=`` ``<>``
- ``AND``
- ``OR``
- ``NOT``

Boolean is a type that can be stored in a variable and used that way as well. The constants ``True`` and ``False`` (case insensitive) may be used as values for boolean variables. If a number is used as if it was a Boolean variable, it will be interpreted in the standard way (zero means false, anything else means true)::

    IF X = 1 AND Y > 4 { PRINT "Both conditions are true". }
    IF X = 1 OR Y > 4 { PRINT "At least one condition is true". }
    IF NOT (X = 1 or Y > 4) { PRINT "Neither condition is true". }
    IF X <> 1 { PRINT "X is not 1". }
    SET MYCHECK TO NOT (X = 1 or Y > 4).
    IF MYCHECK { PRINT "mycheck is true." }
    LOCK CONTINUOUSCHECK TO X < 0.
    WHEN CONTINUOUSCHECK THEN { PRINT "X has just become negative.". }
    IF True { PRINT "This statement happens unconditionally." }
    IF False { PRINT "This statement never happens." }
    IF 1 { PRINT "This statement happens unconditionally." }
    IF 0 { PRINT "This statement never happens." }
    IF count { PRINT "count isn't zero.". }
