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

.. index:: CHOOSE
.. _choose:

CHOOSE (Ternary operator)
-------------------------

An expression that evalualtes to one of two choices depending on a
conditional check:

   CHOOSE expression1 IF condition ELSE expression2

Note this is NOT a statement.  This is an expression that can be embedded
inside other statements, like so:

   SET X TO CHOOSE expression1 IF condition ELSE expression2.
   PRINT CHOOSE "High" IF altitude > 20000 ELSE "Low".

The reason to use the ``CHOOSE`` operator instead of an
IF/ELSE statement is that IF/ELSE won't return a value, while
this does, and thus this can be embedded inside other expressions.

(This is similar to the ``?`` operator in languages like "C" and its
derivatives, except it puts the "true" choice first, then the
conditional check, then the "false" choice.)


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
``@LAZYGLOBAL`` is set to off, in which case it will be an error.

Note that a LOCK expression is extremely similar to a user function.
Every time you read the value of the "variable", it executes the expression
again.

.. note::
    If a ``LOCK`` expression is used with a flight control such as ``THROTTLE`` or ``STEERING``, then it will get repeatedly evaluated in :ref:`each physics tick <physics tick>`.
    When used with a flight control variable, a ``LOCK`` actually
    becomes a :ref:`trigger <triggers>`.

.. index:: UNLOCK
.. _unlock:

``UNLOCK``
----------

Releases a lock on a variable. See ``LOCK``::

    UNLOCK X.    // Releases a lock on variable X
    UNLOCK ALL.  // Releases ALL locks

.. index:: UNTIL
.. _until:

``UNTIL`` loop
--------------

Performs a loop until a certain condition is met::

    SET X to 1.
    UNTIL X > 10 {      // Prints the numbers 1-10
        PRINT X.
        SET X to X + 1.
    }

.. note::
    If you are writing an ``UNTIL`` loop that looks much like the
    example above, consider the possibility of writing it as a
    :ref:`FROM <from>` loop instead.

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

``FOR`` loop
------------

Loops over a list collection, letting you access one element at a time. Syntax::

    FOR variable1 IN variable2 { use variable1 here. }

Where:

- `variable1` is a variable to hold each element one at a time.
- `variable2` is a LIST variable to iterate over.

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

.. note::
    If you are an experienced programmer looking for something more
    like the for-loop from C, with its 3-part clauses of init,
    check, and increment in the header, see the :ref:`FROM <from>` loop
    description.  The kerboscript 'for' loop is more like a
    'foreach' loop from other modern languages like C#.

.. index:: FROM
.. _from:

``FROM`` loop
-------------

Identical to the :ref:`UNTIL <until>` loop, except that it also contains
an explicit initializer and incrementer section in the header.

Syntax:
~~~~~~~

  ``FROM`` { one or more statements } ``UNTIL`` Boolean_expression
  ``STEP`` { one or more statements } ``DO`` one statement or a block of statements inside braces '{}'

Quick Example::

    print "Countdown initiated:".
    FROM {local x is 10.} UNTIL x = 0 STEP {set x to x-1.} DO {
      print "T -" + x.
    }

.. note::
    If you are an experienced programmer, you can think of the ``FROM``
    loop as just being Kerboscript's version of the generic 3-part
    for-loop ``for( int x=10; x > 0; --x ) {...}`` that first appeared
    in C and is now so common to many programming languages, except
    that its Boolean check uses the reverse of that logic because it's
    based on UNTIL loops instead of WHILE loops.

What the parts mean
~~~~~~~~~~~~~~~~~~~

- ``FROM`` { one or more statements }

  - Perform these statements at the beginning before starting the first
    pass through the loop.  They may contain local declarations of new
    variables.  If they do, then the variables will be local to the body
    of the loop and won't be visible outside the loop.  In this case the
    braces ``{`` and ``}`` are mandatory even when there is only one
    statement present.  To create a a null FROM clause, give it an empty
    set of braces.

- ``UNTIL`` expression

  - Exactly like the :ref:`UNTIL <until>` loop.  The loop will run this
    expression at the start of each pass through the loop body, and if
    it's true, it will abort and stop running the loop.  It checks before
    the initial first pass of the loop as well, so it's possible for the
    check to prevent the loop body from even executing once.  Braces
    ``{``..``}`` are not used here because this is not technically a
    complete statement.  It is just an expression that evaluates to a
    value.

- ``STEP`` { one or more statements }

  - Perform these statements at the bottom of each loop pass.  The purpose
    is typically to increment or decrement the variable you declared in
    your ``FROM`` clause to get it ready for the next loop pass.  In this
    case the braces ``{`` and ``}`` are mandatory even when there is
    only one statement present.  To create a null FROM clause, give
    it an empty set of braces.

- ``DO`` one statement or a block of statements inside braxes ``{``..``}``:

  - This is where the loop body gets put.  Much like with the UNTIL and FOR
    loops, these braces are not mandatory when there is only exactly one
    statement in the body, but are a very good idea to have anyway.

Why some braces are mandatory
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Some braces are mandatory (for the ``FROM`` and ``STEP`` clauses) even
when there is only one statement inside them, because the period that
ends a single statement would look like it's terminating the entire
FROM loop if it was open and bare.  Wrapping it inside braces makes it
more visually obvious that it's not the end of the FROM loop.

Why ``DO`` is mandatory
~~~~~~~~~~~~~~~~~~~~~~~

Other loop types don't require a keyword to begin the loop body.  You
can just start in with the opening left-brace ``{``.  The reason the
additional ``DO`` keyword exists in the FROM loop is because otherwise
you'd have two back-to-back brace sections (The  end of the ``STEP``
clause would abut against the start of the loop body) without any
punctuation between them, and that would look too much like it was
starting a brand new thing from scratch.

Other formatting examples
~~~~~~~~~~~~~~~~~~~~~~~~~

::

    // prints a count from 1 to 10:
    FROM {local x is 1.} UNTIL x > 10 STEP {set x to x+1.} DO { print x.}

    // Entire header in one line, body indented:
    // --------------------------------------------
    FROM {local x is 1.} UNTIL x > 10 STEP {set x to x+1.} DO {
      print x.
    }

    // Each header part on its own line, body indented:
    // --------------------------------------------
    FROM {local x is 1.}
    UNTIL x > 10
    STEP {set x to x+1.}
    DO {
      print x.
    }

    // Fully exploded out: Each header part on its own line,
    //  each clause indented separately:
    // --------------------------------------------
    FROM
    {
      local x is 1.  // x will count upward from 1.
      local y is 10. // while y is counting downward from 10.
    }
    UNTIL
      x > 10 or y = 0
    STEP
    {
      set x to x+1.
      set y to y-1.
    }
    DO
    {
      print "x is " + x + ", y is " + y.
    }

    // ETC.

Any such combination of indenting styles, or mix and match of them, is
understood by the compiler.  The compiler ignores the spacing and
indenting.  It is recommended that you pick just two of them and stick
with them - one compact one to use for short headers, and one longer exploded
one to use for more wordy headers when you have to split it up across lines.

The literal meaning of ``FROM``
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

If you have a ``FROM`` loop, it ends up being exactly identical to an
:ref:`UNTIL <until>` loop written as follows:

If we assume that AAAA, BBBB, CCCC, and DDDD are placeholders referring
to the actual script syntax, then in the generic case, the following
is how all FROM loops work:

``FROM`` loop::

    FROM { AAAA } UNTIL BBBB STEP { CCCC } DO { DDDD }

Is exactly the same as doing this::

    { // start a brace to keep the scope of AAAA local to the loop.
        AAAA
        UNTIL BBBB {
            DDDD

            CCCC
        }
    } // end a brace to throw away the local scope of AAAA


An example of why the FROM loop is useful
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Given that the ``FROM`` loop is really just an alternate way to write a
certain format of UNTIL loop, you might ask why bother having it.
The reason is that in the long run it makes your script easier to
edit and maintain.  It makes things more self-contained and cut-and-pasteable:

Above, in the documentation for :ref:`UNTIL <until>` loops, this example was
given::

    SET X to 1.
    UNTIL X > 10 {      // Prints the numbers 1-10
        PRINT X.
        SET X to X + 1.
    }

The same example, expressed as a ``FROM`` loop is this::

    FROM {SET X to 1.} UNTIL X > 10 {SET X to X + 1.} DO {
        PRINT X.
    }

Kerboscript ``FROM`` loop provides a way to place those sections in the
loop header so they are declared up front and let people see the layout
of how the loop iterates, leaving the body to just contain the statements
to be done for that iteration.

If you are editing your script and need to cut a loop section and move it
elsewhere, the FROM loop makes it more visually obvious how to cut
that loop and move it.  It makes the important parts of the loop be self
contained in the header, so you don't leave the initializer behind when
moving the loop.


.. index:: WAIT
.. _wait:


``WAIT``
--------

Halts execution for a specified amount of time, or until a specific set of criteria are met. Note that running a ``WAIT UNTIL`` statement can hang the machine forever if the criteria are never met. Examples::

    WAIT 6.2.                     // Wait 6.2 seconds
    WAIT UNTIL X > 40.            // Wait until X is greater than 40
    WAIT UNTIL APOAPSIS > 150000. // You can see where this is going

Note that any ``WAIT`` statement, no matter what the actual expression is, will always result in a wait time that lasts at least :ref:`one physics tick <physics tick>`.

.. _wait_mainline_trigger:

Difference between wait in mainline code and trigger code
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

When called from your mainline code, the :ref:`WAIT <wait>`
command causes mainline code to be suspended, but does
not stop :ref:`triggers <triggers>` from interrupting
this waiting period.  Triggers will continue to fire off
during the time that mainline code is stuck on a wait.

But when a ``WAIT`` is used in a trigger's body
(A "trigger" is any ``WHEN``, or ``ON`` statement,
or the expression in a steering control lock like
``lock throttle to mythrottlefunction().``), it actually
causes all execution except for other triggers that are
of higher priority to get stuck until the wait is done.
Because of this, while it is allowed, it is
:ref:`usually a bad idea to use WAIT inside a trigger <wait_in_trigger>`.


.. index:: Boolean Operators
.. _booleans:

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


``DECLARE FUNCTION``
--------------------

Covered in more depth :ref:`elsewhere in the documentation <user_functions>`,
the ``DECLARE FUNCTION`` statement creates a user-defined function that
you can then call elsewhere in the code.

``RETURN``
----------

Covered in more depth :ref:`elsewhere in the documentation <user_functions>`,
the ``RETURN`` statement causes a user function, or a trigger body, to
end, and chooses what the calling part of the program will see if it
reads the value of the function.

.. index:: WHEN
.. _when:
.. index:: ON
.. _on_trigger:

``WHEN`` / ``THEN`` statements, and ``ON`` statements
-----------------------------------------------------

.. note::

    Before going too far into this explanation, be aware that the
    ``WHEN`` and ``ON`` statements are rather advanced topics for a
    new programmer and if you're just getting a feel for how
    programming works, and are using kOS as a first gentle introduction
    to writing programs, you might want to avoid using them until
    you're more comfortable with the other features of kOS first.

*The WHEN and the ON statement are very similar to each other, and so
they are documented together here.*

.. seealso::

    :ref:`general_guidlines`
        Before you continue, be aware that there is
        also a page in the tutorials section describing the best practices
        to use with these statements, including :ref:`minimizing how long
        trigger bodies take <minimize_trigger_bodies>`. and :ref:`minimizing
        how many trigger conditions are active <minimize_trigger_conditions>`.
        It would be a good idea to read that documentation after reading this
        section.

``WHEN`` and ``ON`` both begin checking in the background for
a condition that will cause some code to execute some statements
later on.  They do NOT cause the code to necessarily get run right
now.  The check will occur at regular fast intervals in the
background, and the code will trigger whenever kOS next notices that
the check happens to be true.

kOS has a feature known as a :ref:`trigger <triggers>`, and a
``WHEN`` or an ``ON`` statement are two of the ways to create one.
Any time you make a section of program that is meant to repeatedly
run a check in the background while the main program continues on,
that is called a ``trigger`` in kOS terminology.  You may see the
term ``trigger`` mentioned in many places in this documentation.

Syntax examples:

.. list-table:: When and On side by side
    :header-rows: 1
    :widths: 1 1

    * - WHEN .. THEN syntax
      - ON syntax
    * - | WHEN *boolean_expression* THEN {
        |
        |   *statements go here*
        |
        | }
      - | ON *any_expression* {
        |
        |   *statements go here*
        |
        | }
    * - WHEN *boolean_expression* THEN *single_statement*.
      - ON *any_expression* *single_statement*.

For historical reasons, the ``THEN`` keyword is needed for ``WHEN``
statements but not for ``ON`` statements.

Here is the difference between them:

- ``WHEN`` statement:  When kOS checks it in the background, if it
  notices the condition is true, the trigger fires and it performs
  the statements. The condition to check for must be a boolean
  expression.
- ``ON`` statement: When kOS checks it in the backround, if it
  notices the expression *is now different from what it was the last
  time it checked*, the trigger fires and it performs the statements.
  The condition to check for can be any expression for which it
  is possible to test equality.  It can be a boolean, a scalar, etc.
  All that matters is that kOS needs to be able to check if its
  new value is equal to its previous value or not.

Other than that, the two work the same way, and follow the same rules.

``WHEN`` example::

    // This example will eventually print the message
    // once enough time has passed:

    SET tenSecondsLater to TIME:SECONDS + 10.
    WHEN TIME:SECONDS > tenSecondsLater THEN {
      PRINT "Ten seconds have passed.".
    }

    PRINT "now checking in the background to see if 10 seconds have passed yet.".

    WAIT UNTIL FALSE. // Wait forever.  You have to end with Control-C
                      // The trigger will interrupt this waiting when it
                      // notices it should.

``ON`` example.  This style is frequently used with action groups in kOS.
KSP's action groups actually *toggle* from true to false or from false to
true each time you press the key::

    // This example will print a message whenever you toggle
    // the lights, or press the '1' key.

    ON AG1 {
      PRINT "You pressed '1', causing action group 1 to toggle.".
      PRINT "Action group 1 is now " + AG1.
      PRINT "No longer paying attention.".
    }

    WAIT UNTIL FALSE. // Wait forever.  You have to end with Control-C
                      // The trigger will interrupt this waiting when it
                      // notices it should.

For either ``WHEN`` or ``ON`` triggers, the check to see if it's
time to trigger, and the subsequent run of the statements if they
do trigger, interrupts the normal flow of the program.  The normal
program flow will continue from where it left off, after the trigger
finishes its work.

In a sense, a trigger is a bit like a user function you created and
then asked the kOS system to please keep running it again and again
in the background until it finally says that it fired off.  In fact,
it *is* implemented much like your own user functions.

If you run the above examples, you will see that they actually only
happen once, and then stop happening again.  In the ON AG1 example,
it will only fire off once, no matter how many times you press the
'1' key.  More will be covered about how to change that further down.

.. warning::
    Do not make the body of a ``WHEN``/``THEN`` take a long time to
    execute. If you attempt to run code that lasts too long in the body
    of your ``WHEN``/``THEN`` statement, it will cause the main
    line code, and all other triggers (WHEN, ON, and cooked steering
    locks) to be stuck unable to continue until it finishes.  You also probably
    should not make the system execute a ``WAIT`` command when inside the
    body of a WHEN/THEN statement.

kOS has a mechanism in place that allows triggers to interrupt mainline
code that is stuck in a wait.  It does not have a mechanism to go the
other way around and have a trigger get interrupted.  Triggers are
meant to run quickly and finish so the system can get back to the
mainline code.

Don't let triggers bog down the code
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

If you are going to make extensive use of ``WHEN``/``THEN``
triggers, it's important to understand more details of how they
:ref:`work in the kOS CPU <triggers>`.

Most importantly, be aware that since they get checked again and
again in the background, having too many triggers that are
"too expensive" can starve your main code of its use of the
CPU, and thus slow down your program's rate of running.

By default triggers only run once, but this can be changed
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The original intention of the ``WHEN`` and ``ON`` triggers was that 
although they check the condition repeatedly, once the condition is
found to be true, they execute the body just once and then stop
checking the condition.

They were intended for things like only running a piece of code when
you break a threshold altitude, or detect that you've landed, etc.

So the default way they behave is that once the body of the trigger
happens the first time, the trigger will never be checked again, and
is now effectively dead for the rest of the program.

Obviously, that's probably not the behavior you always want.  Sometimes
you will want them to keep repeatedly happening, as a frequent
background check.  One obvious example comes from the ``ON AG1``
example above.  You probably want a program that can keep re-checking
to see if the action group button has been hit again and again, not just
notice it once and then quit looking for it.

There are two ways to do this - the new (better) way with
the ``return`` statement, and the older way, kept around for
backward compatibility, of using the ``preserve`` keyword.

.. _trigger_return:

Preserving with ``return``
::::::::::::::::::::::::::

Triggers are essentialy functions that don't quite look like functions.
They are frequently called, but they're not called *by you*.  They're
called by the Kerbal Operating System itself.  So you can tell the
Kerbal Operating System what your intentions were by simply deciding
to return either a false or a true boolean value from the body of the
trigger.  This tells kOS if you wanted to keep the trigger around or let
it get deleted.

- ``return true.`` to tell kOS to preserve the trigger and keep checking
  it again next time.
- ``return false.`` to tell kOS to disable the trigger after this check,
  and never use it again.

Therefore, if you want to have the ``ON AG1`` example always respond to
the keypress from now on, then change this::

    ON AG1 {
      PRINT "You pressed '1', causing action group 1 to toggle.".
      PRINT "Action group 1 is now " + AG1.
      PRINT "No longer paying attention.".
    }

To this instead::

    ON AG1 {
      PRINT "You pressed '1', causing action group 1 to toggle.".
      PRINT "Action group 1 is now " + AG1.
      RETURN true.
    }

Or, for a more complex example, if you want it to only respond to
the first 5 times you press the key and then stop after that,
you can conditionally decide what return value to use, like so::

    SET count TO 5.
    ON AG1 {
      PRINT "You pressed '1', causing action group 1 to toggle.".
      PRINT "Action group 1 is now " + AG1.
      SET count TO count - 1.
      PRINT "I will only pay attention " + count + " more times.".
      if count > 0
        RETURN true. // will keep the trigger alive.
      else
        RETURN false. // will let the trigger die.
    }

There is an alternate, older syntax you can use to do the same thing,
called the :ref:`preserve keyword <preserve>`.  You may see it used in a lot of
older scripts, but the new way using the ``return`` keyword is
cleaner.

If you never mention either a true or a false return value, the default
is to behave as if you had returned false, and delete the trigger.
This works because of the sort-of-secret fact that in kOS, all
functions return zero if you don't mention the return value explicitly.


They don't last past the end of the program
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

A ``WHEN``/``THEN`` or ``ON`` trigger gets removed when the
program that created it exits, even if it has not occurred yet.


.. index:: PRESERVE
.. _preserve:

``PRESERVE``
------------

``PRESERVE`` is a command keyword that is only valid inside of ``WHEN``/``THEN`` and ``ON`` code blocks.

When a ``WHEN``/``THEN`` or ``ON`` condition is triggered, the default behavior is to execute the code block body exactly once and only once, and then the trigger condition is removed and the trigger will never occur again.

To alter this, a new ability was added in kOS 0.19.3 and above to
have triggers simply :ref:`return a true or false value <trigger_return>`
to determine if they wish to be preserved.

But prior to kOS 0.19.3, the only way to do it in kerboscript was
with the ``PRESERVE`` keyword, which will likely remain in
kerboscript for quite some time because it has a lot of backward
compatibility legacy.

If you execute the ``PRESERVE`` command anywhere within the body
of a trigger, it tells kOS that you wish the trigger to remain
present and not get deleted.  Choosing not to execute it, and
just letting the execution fall through to the bottom of the
body, has the default behavior of causing the trigger to get
deleted.

For example, this::

    ON AG1 {
      PRINT "You pressed '1', causing action group 1 to toggle.".
      PRINT "Action group 1 is now " + AG1.
      RETURN true.
    }

could also be expressed this way::

    ON AG1 {
      PRINT "You pressed '1', causing action group 1 to toggle.".
      PRINT "Action group 1 is now " + AG1.
      PRESERVE.
    }

And this::

    SET count TO 5.
    ON AG1 {
      PRINT "You pressed '1', causing action group 1 to toggle.".
      PRINT "Action group 1 is now " + AG1.
      SET count TO count - 1.
      PRINT "I will only pay attention " + count + " more times.".
      if count > 0
        RETURN true. // will keep the trigger alive.
      else
        RETURN false. // will let the trigger die.
    }

could also be expressed this way::

    SET count TO 5.
    ON AG1 {
      PRINT "You pressed '1', causing action group 1 to toggle.".
      PRINT "Action group 1 is now " + AG1.
      SET count TO count - 1.
      PRINT "I will only pay attention " + count + " more times.".
      if count > 0
        PRESERVE.
    }

Also note that unlike using ``RETURN``, the ``PRESERVE`` statement
doesn't actually cause the trigger to abort and return at that point.
It just sets a flag for what the intended return value will be, without
actually returning yet.  Therefore it doesn't actually matter where
within the block of code it happens, it has the same effect.

this::

    ON AG1 {
      PRINT "You pressed '1', causing action group 1 to toggle.".
      PRINT "Action group 1 is now " + AG1.
      PRESERVE.
    }

has the same effect as this::

    ON AG1 {
      PRESERVE. // <-- Doesn't matter where you PRESERVE within the body.
      PRINT "You pressed '1', causing action group 1 to toggle.".
      PRINT "Action group 1 is now " + AG1.
    }

(If you attempt to BOTH execute ``PRESERVE.`` *and* provide a ``RETURN false.``
statement that contradicts it, the ``RETURN`` statement will end up
overriding the effect of the ``PRESERVE``.)
