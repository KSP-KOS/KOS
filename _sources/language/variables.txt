.. _variables:

Variables & Statements
======================

.. contents::
    :local:
    :depth: 1
    
.. _declare:

``DECLARE``
-----------

Declares a global variable. Alternatively, a variable can be implicitly declared by any ``SET`` or ``LOCK`` statement::

    DECLARE X.

.. _declare parameter:

``DECLARE PARAMETER``
---------------------

Declares variables to be used as a parameter that can be passed in using the ``RUN`` command.

Program 1::

    // This is the contents of program1:
    DECLARE PARAMETER X.
    DECLARE PARAMETER Y.
    PRINT "X times Y is " + X*Y.

Program 2::

    // This is the contents of program2, which calls program1:
    SET A TO 7.
    RUN PROGRAM1( A, A+1 ).

The above example would give the output::

    X times Y is 56.

It is also possible to put more than one parameter into a single ``DECLARE PARAMETER`` statement, separated by commas, as shown below::

    DECLARE PARAMETER X, Y, CheckFlag.

This is exactly equivalent to::

    DECLARE PARAMETER X.
    DECLARE PARAMETER Y.
    DECLARE PARAMETER CheckFlag.

Note: Unlike normal variables, Parameter variables are local to the program. When program A calls program B and passes parameters to it, program B can alter their values without affecting the values of the variables in program A.

Caveat
    This is only true if the values are primitive singleton values like numbers or booleans. If the values are Structures like Vectors or Lists, then they do end up behaving as if they were passed by reference, in the usual way that should be familiar to people who have used languages like Java or C# before.

The ``DECLARE PARAMETER`` statements can appear anywhere in a program as long as they are in the file at a point earlier than the point at which the parameter is being used. The order the arguments need to be passed in by the caller is the order the ``DECLARE PARAMETER`` statements appear in the program being called.

.. note::

    **Pass By Value**

    The following paragraph is important for people familiar with other programming languages. If you are new to programming and don't understand what it is saying, that's okay you can ignore it.
    
    At the moment the only kind of parameter supported is a pass-by-value parameter, and pass-by reference parameters don't exist. Be aware, however, that due to the way kOS is implemented on top of a reference-using object-oriented language (CSharp), if you pass an argument which is a complex aggregate structure (i.e. a Vector, or a List - anything that kOS lets you use a colon suffix with), then the parameters will behave exactly like being passed by reference because all you're passing is the handle to the object rather than the object itself. This should be familiar behavior to anyone who has written software in Java or C# before.

.. _set:

``SET``
-------

Sets the value of a variable. Declares a global variable if it doesn’t already exist::

    SET X TO 1.

.. _lock:

``LOCK``
--------

Declares that the idenifier will refer to an expression that is always re-evaluated on the fly every time it is used::

    SET Y TO 1.
    LOCK X TO Y + 1.
    PRINT X.    // prints "2"
    SET Y TO 2.
    PRINT X.    // prints "3"

.. _toggle:

``TOGGLE``
----------

Toggles a variable between ``TRUE`` or ``FALSE``. If the variable in question starts out as a number, it will be converted to a boolean and then toggled. This is useful for setting action groups, which are activated whenever their values are inverted::

    TOGGLE AG1. // Fires action group 1.
    TOGGLE SAS. // Toggles SAS on or off.

.. _on:

``ON``
------

Sets a variable to ``TRUE``. This is useful for the ``RCS`` and ``SAS`` bindings::

    RCS ON.  // Turns on the RCS

.. _off:

``OFF``
-------

Sets a variable to ``FALSE``. This is useful for the ``RCS`` and ``SAS`` bindings::

    RCS OFF.  // Turns off the RCS

