Variables & Statements
======================

.. contents::
    :local:
    :depth: 2

.. _declare:

``DECLARE``
-----------

.. note::
    .. versionadded:: 0.17
        The meaning of this statement changed considerably in this update.
	Prior to this version, DECLARE always created global variables
	no matter where it appeared in the script.

Declares a variable that is limited in scope to the code block it appears in::

    DECLARE X.

(A "code block" is any section of statements that begins with a
left-curly-brace "{" and ends with its closing right-curly-brace "}".)

Any variable declared with DECLARE will only exist inside the code block
section it was created in.  After that code block is finished, the variable
will no longer exist.

Alternatively, a variable can be implicitly declared by any ``SET`` or
``LOCK`` statement, however doing so causes the variable to always have 
global scope.  **The only way to make a variable be local instead of
global is to declare it explicitly with DECLARE**.

``DECLARE ... TO``
------------------

An optional syntax for the DECLARE statement is to combine a DECLARE
with a SET into the same statement as follows::

    DECLARE countdown TO 10.

Which would have exactly the same effect as::

    DECLARE countdown.
    SET countdown TO 10.

.. _declare parameter:

``DECLARE PARAMETER``
---------------------

If you put this statement in the main part of your script, it
declares variables to be used as a parameter that can be passed
in using the ``RUN`` command.

If you put this statement inside of a :ref:`Function body <user_functions>`,
then it declares variables to be used as a parameter that can
be passed in to that function when calling the function.

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

Sets the value of a variable. Implicitly creates a global variable if it doesn’t already exist::

    SET X TO 1.

This follows the :ref:`scoping rules explained below <scope>`.  If the 
variable can be found in the current local scope, or any scope higher
up, then it won't be created and instead the existing one will be used.


``LOCK``
--------

Declares that the idenifier will refer to an expression that is always re-evaluated on the fly every time it is used (See also :ref:`Flow Control documentation <lock>`)::

    SET Y TO 1.
    LOCK X TO Y + 1.
    PRINT X.    // prints "2"
    SET Y TO 2.
    PRINT X.    // prints "3"

Note that because of how LOCK expressions are in fact implemented as mini
functions, they cannot have local scope.  A LOCK *always* has global scope.

.. _toggle:

``TOGGLE``
----------

Toggles a variable between ``TRUE`` or ``FALSE``. If the variable in question starts out as a number, it will be converted to a boolean and then toggled. This is useful for setting action groups, which are activated whenever their values are inverted::

    TOGGLE AG1. // Fires action group 1.
    TOGGLE SAS. // Toggles SAS on or off.

This follows the same rules as :ref:`SET <set>`, in that if the variable in
question doesn't already exist, it will end up creating it as a global 
variable.

.. _on:

``ON``
------

Sets a variable to ``TRUE``. This is useful for the ``RCS`` and ``SAS`` bindings::

    RCS ON.  // Turns on the RCS


This follows the same rules as :ref:`SET <set>`, in that if the variable in
question doesn't already exist, it will end up creating it as a global 
variable.

.. _off:

``OFF``
-------

Sets a variable to ``FALSE``. This is useful for the ``RCS`` and ``SAS`` bindings::

    RCS OFF.  // Turns off the RCS

This follows the same rules as :ref:`SET <set>`, in that if the variable in
question doesn't already exist, it will end up creating it as a global 
variable.

.. _scope:

Scoping rules
-------------

.. note::
    .. versionadded:: 0.17
        In prior versions of kerboscript, all identifiers other than
	DECLARE PARAMETER identifiers were always global variables no
	matter what, even if you used the DECLARE statement to make them.

What is Scope?
    The term *Scope* simply refers to asking the question "where in the
    code can this variable be used, and how long does it last before it
    goes away?"  The *scope* of a variable is the section of the program's
    code that it "works" within.  Any section of the program's code
    from which the variable cannot be seen is said to be "out of that
    variable's scope".

Global scope
    The simplest scope is called "global".  Global scope simply means
    "this variable can be used from anywhere in the program".  If you
    never use the DECLARE statement, then your variables in kerboscript
    will all be in *global scope*.  For simple easy scripts used by
    beginners, this is often enough and you don't have to read the rest
    of this topic until you start advancing to more intermediate scripts.

If you need to have variables that only have local scope, either just
to keep your code more manageable, or because you literally need
local scope to allow for recursive function calls, then you use the
DECLARE statement to create the variables.

DECLARE statements are in block scope
    Kerboscript uses block scoping to keep track of local variable
    scope.  This means you can have variables that are not only
    local to a function, but are in fact actually local to JUST
    the current curly-brace block of statements, even if that block
    of statements is, say, the body of an IF check, or the body of
    an UNTIL loop.

Why limit scope?
    You might be wondering why it's useful to limit the scope of a
    variable.  Wouldn't it be easier just to make all variables
    global?  The answer is twofold: (1) Once a program becomes large
    enough, trying to remember the name of every variable in the
    program, and having to keep coming up with new names for new
    variables, can be a large unmanagable chore, especially with
    programs written by more than one person collaborating together.
    (2) Even if you can keep track of all that in your head, there's
    a certain programming technique known as recursion (TODO - wiki
    link) in which you actually NEED to have local variable scope for
    the technique to even work at all.

Examples::

    DECLARE x TO 10. // X is now a global variable with value 10.
    SET y TO 20. // Y is now a global variable (implicitly) with value 20.
    DECLARE z. // Z is now a global variable, with no particular value yet.

    SET sum to -1. // sum is now an implicitly made global variable, containing -1.

    // A function to return the mean average of all the items in the list
    // passed into it, under the assumption all the items in the list are
    // numbers of some sort:
    DECLARE FUNCTION calcAverage {
      DECLARE PARAMETER inputList.
      
      DECLARE sum TO 0. // sum is now local to this function's body.
      FOR val IN inputList {
        SET sum TO sum + val.
      }.
      print "Inside calcAverage, sum is " + sum.
      RETURN sum / inputList:LENGTH.
    }.

    SET testList TO LIST();
    testList:ADD(5).
    testList:ADD(10).
    testList:ADD(15).
    print "average is " + calcAverage(testList).
    print "but out here where it's global, sum is still " + sum.

This example will print::

    Inside calcAverage, sum is 30
    average is 10
    but out here where it's global, sum is still -1
    
Thus proving that the variable called SUM inside the function is NOT the
same variable as the one called SUM out in the global main code.

Nesting:
  The scoping rules are nested as well.  If you attempt to use a
  variable that doesn't exist in the local scope, the next scope "outside"
  it wil be used, and if it doesn't exist there, the next scope "outside"
  that will be used and so on, all the way up to the global scope.  Only
  if the variable isn't found at the global scope either will it be 
  implicitly created.

.. _nolazyglobal:

``NOLAZYGLOBAL``
::::::::::::::::

Often the fact that you can get an implicit global variable declared
without intending to can lead to a lot of code maintenence headaches
down the road.  If you make a typo in a variable name, you end up
creating a new variable instead of generating an error.  Or you may just
forget to mark the variable as local when you intended to.  

If you wish to instruct kerboscript to alter its behavior and
disable its normal implicit globals, and instead demand that all
variables MUST be mentioned in a DECLARE statement, you can do so
using the ``NOLAZYGLOBAL`` syntax.  Everything that occurs inside
a NOLAZYGLOBAL code block will use the rule that varibles MUST already
exist before being encountered.  SET will no longer automatically create
variables for you when inside this section.

Example::

    NOLAZYGLOBAL {
      SET num TO 1.
      IF TRUE {
        DECLARE Y TO 2.
        SET num TO num + Y. // This is fine.  num exists already as a global and
                            // you're adding the local Y to it.
        SET nim TO 20. // This typo generates an error.  There is
                       // no such variable "nim" and NOLAZYGLOBAL
                       // says not to implicitly make it.
      }.
    }.

    SET nim TO 20.  // HERE, on the other hand, this doesn't generate an
                    // error.  When outside the NOLAZYGLOBAL section,
                    // it just makes a new varible called nim

Why NOLAZYGLOBAL?
    The rationale behind NOLAZYGLOBAL is to primarily be used in cases
    where you're writing a libary of function calls you intend to
    use elsewhere, and want to be careful not to accidentally make
    them dependant on globals outside the function itself.

~~~~~~

History:
    Kerboscript began its life as a language in which you never have to
    declare a variable if you don't want to.  You can just create any
    variable implicitly by just using it in a SET statement.

    There are a variety of programming langauges that work like this,
    such as Perl, Javascript, and Lua.  However, they all share one
    thing in common - once you want to allow the possiblity of having
    local variables, you have to figure out how this should work with
    the implicit variable declaration feature.

    And all those languages went with the same solution, which 
    kerboscript now follows as well.  Because implicit undeclared
    variables are intended to be a nice easy way for new users to
    ease into programming, they should always default to being 
    global so that people who wish to keep programming that way
    don't need to understand or deal with scope.

    The NOLAZYGLOBAL keyword is meant to mimic Perl's ``use strict;``
    directive.
