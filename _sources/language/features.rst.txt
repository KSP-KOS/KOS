.. _features:

General Features of the **KerboScript** Language
================================================

.. contents::
    :local:
    :depth: 2

Case Insensitivity
------------------

Everything in **KerboScript** is case-insensitive, including your own variable names and filenames.
This extends to string comparison as well. (``"Hello"="HELLO"`` will return true.)

Expressions
-----------

KerboScript uses an expression evaluation system that allows you to perform math operations on variables. Some variables are defined by you. Others are defined by the system. There are four basic types:

Numbers (Scalars)
~~~~~~~~~~~~~~~~~

You can use mathematical operations on numbers, like this::

    SET X TO 4 + 2.5.
    PRINT X.             // Outputs 6.5

The system follows the usual mathematical order of operations.

Throughout the documentation, numbers like this are referred to
as :struct:`Scalars <scalar>` to distinguish them from the many
places where the mod works with :struct:`Vector <vector>` values
instead.

Strings
~~~~~~~

:struct:`Strings <string>` are pieces of text that are generally
meant to be printed to the screen. For example::

    PRINT "Hello World!".

To concatenate strings, you can use the + operator. This works with mixtures of numbers and strings as well::

    PRINT "4 plus 3 is: " + (4+3).

Booleans
~~~~~~~~

:struct:`Booleans <boolean>` are values that can either be ``True``
or ``False`` and can be used to store the result of conditional checks::

    set myValue to (x >= 10 and x <= 99).
    if myValue {
      print "x is a two digit number.".
    }

.. _features structures:

Structures
~~~~~~~~~~

Structures are variables that contain more than one piece of information. For example, a Vector has an X, a Y, and a Z component. Structures can be used with SET.. TO just like any other variable. To access the sub-elements of a structure, you use the colon operator (":"). Here are some examples::

    PRINT "The Mun's periapsis altitude is: " + MUN:PERIAPSIS.
    PRINT "The ship's surface velocity is: " + SHIP:VELOCITY:SURFACE.

Many structures also let you set a specific component of them, for example::

    SET VEC TO V(10,10,10).  // A vector with x,y,z components
                             // all set to 10.
    SET VEC:X to VEC:X * 4.  // multiply just the X part of VEC by 4.
    PRINT VEC.               // Results in V(40,10,10).

.. _features methods:

Structure Methods
~~~~~~~~~~~~~~~~~

Structures also often contain methods. A method is a suffix of a structure that actually performs an activity when you mention it, and can sometimes take parameters. The following are examples of calling methods of a structure::

    SET PLIST TO SHIP:PARTSDUBBED("my engines"). // calling a suffix
                                                 // method with one
                                                 // argument that
                                                 // returns a list.
    PLIST:REMOVE(0). // calling a suffix method with one argument that
                     // doesn't return anything.
    PRINT PLIST:SUBLIST(0,4). // calling a suffix method with 2
                              // arguments that returns a list.

For more information, see the :ref:`Structures Section <language structures>`. A full list of structure types can be found on the :ref:`Structures <structures>` page. For a more detailed breakdown of the language, see the :ref:`Language Syntax Constructs <syntax>` page.


.. _short_circuit:

Short-circuiting booleans
-------------------------

Further reading: https://en.wikipedia.org/wiki/Short-circuit_evaluation

When performing any boolean operation involving the use of the AND or the OR
operator, kerboscript will short-circuit the boolean check.  What this means
is that if it gets to a point in the expression where it already knows the
result is a forgone conclusion, it doesn't bother calculating the rest of
the expression and just quits there.

Example::

    set x to true.
    if x or y+2 > 10 {
        print "yes".
    } else {
        print "no".
    }.

In this case, the fact that x is true means that when evaluating
the boolean expression ``x or y+2 > 10`` it never even bothers trying
to add y and 2 to find out if it's greater than 10.  It already knew
as soon as it got to the ``x or whatever`` that given that x is true,
the *whatever* doesn't matter one bit.  Once one side of an OR is true,
the other side can either be true or false and it won't change the fact
that the whole expression will be true anyway.

A similar short circuiting happens with AND.  Once the left side of the
AND operator is false, then the entire AND expression is guaranteed
to be false regardless of what's on the right side, so kerboscript
doesn't bother calculating the righthand side once the lefthand side is false.

Read the link above for implications of why this matters in programming.

Late Typing
-----------

Kerboscript is a language in which there is only one type of variable
and it just generically holds any sort of object of any kind.  If
you attempt to assign, for example, a string into a variable that is
currently holding an integer, this does not generate an error.  It
simply causes the variable to change its type and no longer be an
integer, becoming a string now.

In other words, the type of a variable changes dynamically at
runtime depending on what you assign into it.

Lazy Globals (variable declarations optional)
---------------------------------------------

Kerboscript is a language in which variables need not be declared ahead
of time.  If you simply set a variable to a value, that just "magically"
makes the variable exist if it didn't already.  When you do this,
the variable will necessarily be *global* in scope.  kerboscript refers
to these variables created implicitly this way as "lazy globals".
It's a system designed to make kerboscript easy to use for people new to
programming.

But if you are an experienced programmer you might not like this
behavior, and there are good arguments for why you might want to
disable it.  If you wish to do so, a syntax exists to do so called
:ref:`@LAZYGLOBAL OFF <lazyglobal>`.

.. _feature functions:

User Functions
--------------

.. note::
    .. versionadded:: 0.17
        This feature did not exist in prior versions of kerboscript.

Kerboscript supports user functions which you can write yourself
and call from your own scripts.  *These are not* :ref:`structure
methods <features methods>` *(which as of this writing are a feature which
only works for the built-in kOS types, and are not yet supported
by the kerboscript language for user functions you write yourself).*

Example::

    DECLARE FUNCTION DEGREES_TO_RADIANS {
      DECLARE PARAMETER DEG.

      RETURN CONSTANT():PI * DEG/180.
    }.

    SET ALPHA TO 45.
    PRINT ALPHA + " degrees is " + DEGREES_TO_RADIANS(ALPHA) + " radians.".

For a more detailed description of how to declare your own user functions,
see the :ref:`Language Syntax Constructs, User Functions <syntax functions>`
section.

.. _language structures:

Structures
----------

Structures, :ref:`introduced above <features structures>`, are variable *types* that contain more than one piece of information. All structures contain sub-values or :ref:`methods <features methods>` that can be accessed with a colon (``:``) operator. Multiple structures can be chained together with more than one colon (``:``) operator::

    SET myCraft TO SHIP.
    SET myMass TO myCraft:MASS.
    SET myVel TO myCraft:VELOCITY:ORBIT.

These terms are referred to as "suffixes". For example ``Velocity`` is a suffix of ``Vessel``. It is possible to **set** some suffixes as well. The second line in the following example sets the ``ETA`` of a ``NODE`` 500 seconds into the future::

    SET n TO Node( TIME:SECONDS + 60, 0, 10, 10).
    SET n:ETA to 500.

The full list of available suffixes for each type :ref:`can be found here <structures>`.

.. _feature triggers:

Triggers
--------

One useful feature of kerboscript (but a potentialy confusing one for 
people new to the language, so we don't recommend you use it at
first) is the "trigger". Triggers are small sections of your program
that can interrupt your normal program flow when certain things
happen, then run a small patch of code, and return to wherever you
were in your program as if nothing happened.  They let you set up
hardware interrupts that will trigger based on your own conditions.
Example::

    // When the altitude eventually goes above 50,000 at some point later,
    // interrupt whatever is going on to set off action group 1:
    WHEN ship:altitude > 50000 then { ag1 on. }

This type of trigger is created using the :ref:`when <when>` or
:ref:`on <on_trigger>` statement.  It's a complex enough topic that you
should read the documentation for those keywords carefully to understand
them before you use them.
