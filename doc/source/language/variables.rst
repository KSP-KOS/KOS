Variables & Statements
======================

.. contents::
    :local:
    :depth: 2

.. _declare:

``DECLARE .. TO/IS``
--------------------

What it does:
:::::::::::::

Declares a variable, explicitly or implicitly defining what scope it
has, and gives it an initial value.

Allowed Syntax:
:::::::::::::::

All the following are legal "declare" statements:
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. _declare syntax:

The following alternate versions have identical meaning to each other:

  * ``DECLARE`` *identifier* ``TO`` *expression* *dot*
  * ``DECLARE`` *identifier* ``IS`` *expression* *dot*
  * ``DECLARE`` ``LOCAL`` *identifier* ``TO`` *expression* *dot*
  * ``DECLARE`` ``LOCAL`` *identifier* ``IS`` *expression* *dot*
  * ``LOCAL`` *identifier* ``TO`` *expression* *dot*
  * ``LOCAL`` *identifier* ``IS`` *expression* *dot*

The following alternate versions have identical meaning to each other:

  * ``DECLARE`` ``GLOBAL`` *identifier* ``TO`` *expression* *dot*
  * ``DECLARE`` ``GLOBAL`` *identifier* ``IS`` *expression* *dot*
  * ``GLOBAL`` *identifier* ``TO`` *expression* *dot*
  * ``GLOBAL`` *identifier* ``IS`` *expression* *dot*

.. warning::
    .. versionadded:: 0.17
        ***BREAKING CHANGE**
        The meaning, and syntax, of this statement changed considerably
        in this update.  Prior to this version, DECLARE always created
        global variables no matter where it appeared in the script.
        See 'initializer required' below.

Detailed Description of the syntax:
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

   * The statement must begin with either the word ``DECLARE``, ``LOCAL``,
     or ``GLOBAL``.  If it begins with the word ``DECLARE`` it may optionally
     also contain the word ``LOCAL`` or ``GLOBAL`` afterward.  *Note that if
     neither* ``GLOBAL`` *nor* ``LOCAL`` *is used, the behavior of* 
     ``LOCAL`` *will be assumed implicitly.  Therefore* ``DECLARE LOCAL``
     *and just* ``DECLARE`` *and just* ``LOCAL`` *mean the same thing.* 
   * After that it must contain an identifier.
   * After that it must contain either the word ``TO`` or the word ``IS``,
     which mean the same thing here.
   * After that it must contain some expression for the initial starting
     value of the variable.
   * After that it must contain a dot ("period"), like all commands in
     Kerboscript.
   
   ::

    // These all do the exact same thing - make a local variable:
    DECLARE X TO 1. // assumes local when unspecified.
    LOCAL X IS 1.
    DECLARE LOCAL X IS 1.

    // These do the exact same thing - make a global variable:
    GLOBAL X IS 1.
    DECLARE GLOBAL X IS 1.

If neither the scope word ``GLOBAL`` nor the scope word ``LOCAL``
appear, a declare statement assumes ``LOCAL`` by default.

Any variable declared with ``DECLARE``, ``DECLARE LOCAL``, or ``LOCAL``
will only exist inside the code block section it was created in.
After that code block is finished, the variable will no longer exist.

See Scoping:
::::::::::::

    If you don't know what the terms "global" or "local" mean, it's
    important to read the :ref:`section below about scoping. <scope>`

.. note::
    It is implied that the outermost scope of a program file is 
    the global scope.  Therefore if you make a LOCAL variable at
    the outermost nesting level of your program it really ends up
    being GLOBAL.  Note that GLOBAL variables are not only shared
    between functions of your script, but also can be seen by
    other programs you run from the current program, and visa
    versa.

Alternatively, a variable can be implicitly declared by any ``SET`` or
``LOCK`` statement, however doing so causes the variable to always have 
global scope.  **The only way to make a variable be local instead of
global is to declare it explicitly with one of these DECLARE statements**.

.. note::
    **Terminology: "declare statement"**: Note that the documentation
    will often refer to the phrase "declare statement" even when
    referring to a statement in which the optional keyword "declare"
    was left off.  A statement such as ``LOCAL X IS 1.`` Will still
    be referred to as a "declare statement", even though the word
    "declare" never explicitly appeared in it.

Initializer required in DECLARE
:::::::::::::::::::::::::::::::

.. versionadded:: 0.17

The syntax without the initializer, looking like so::

    DECLARE x. // no initializer like "TO 1."

is **no longer legal syntax**.

Kerboscript now requires the use of the initializer clause (the "TO"
keyword) after the identifier name so as to make it impossible for
there to exist any uninitialized variables in a script.

.. _declare parameter:

``DECLARE PARAMETER``
---------------------

If you put this statement in the main part of your script, it
declares variables to be used as a parameter that can be passed
in using the ``RUN`` command.

If you put this statement inside of a :ref:`Function body <user_functions>`,
then it declares variables to be used as a parameter that can
be passed in to that function when calling the function.

Just as with a :ref:`declare identifier statement <declare>`,
in a ``declare parameter`` statement, the actual keyword
``declare`` need not be used.  The word ``parameter`` may
be used alone and that is legal syntax.

Program 1::

    // This is the contents of program1:
    DECLARE PARAMETER X.
    PARAMETER Y. // omitting the word "DECLARE" - it still means the same thing.
    PRINT "X times Y is " + X*Y.

Program 2::

    // This is the contents of program2, which calls program1:
    SET A TO 7.
    RUN PROGRAM1( A, A+1 ).

The above example would give the output::

    X times Y is 56.

It is also possible to put more than one parameter into a single ``DECLARE PARAMETER`` statement, separated by commas, as shown below::

    DECLARE PARAMETER X, Y, CheckFlag.

    // Or you could leave "DECLARE" off like so:
    PARAMETER X, Y, CheckFlag.

Either of the above is exactly equivalent to::

    PARAMETER X.
    PARAMETER Y.
    PARAMETER CheckFlag.

Note: Unlike normal variables, Parameter variables are always local to the program. When program A calls program B and passes parameters to it, program B can alter their values without affecting the values of the variables in program A.

Caveat
    This is only true if the values are primitive singleton values like numbers or booleans. If the values are Structures like Vectors or Lists, then they do end up behaving as if they were passed by reference, in the usual way that should be familiar to people who have used languages like Java or C# before.


**Illegal to say** ``DECLARE GLOBAL PARAMETER`` : Because parameters
are always local to the location they were declared at, the keyword
``GLOBAL`` is illegal to use in a ``DECLARE PARAMETER`` statement.

The ``DECLARE PARAMETER`` statements can appear anywhere in a program as long as they are in the file at a point earlier than the point at which the parameter is being used. The order the arguments need to be passed in by the caller is the order the ``DECLARE PARAMETER`` statements appear in the program being called.

Optional Parameters (defaulted parameters)
::::::::::::::::::::::::::::::::::::::::::

If you wish, you may make some of the parameters of a program or a user
function optional by defaulting them to a starting value with the ``IS`` keyword, as follows:

    // Imagine this is a file called MYPROG

    DECLARE PARAMETER P1, P2, P3 is 0, P4 is "cheese".
    print P1 + ", " + P2 + ", " + P3 + ", " + P4.


    // Imagine this is a different file that runs it:

    run MYPROG(1,2).         // prints "1, 2, 0, cheese".
    run MYPROG(1,2,3).       // prints "1, 2, 3, cheese".
    run MYPROG(1,2,3,"hi").  // prints "1, 2, 3, hi".

Whenever arguments are missing, the system always makes up the difference by
using defaults for the lastmost parameters until the correct number have been
padded.  (So for example, if you call MYFUNC() above with 3 arguments, it's 
the last argument, P4, that gets defaulted, but P3 does not.  But if you call
it with 2 arguments, both P4 and P3 get defaulted.)

It is illegal to put mandatory (not defaulted) parameters after defaulted ones.

This will not work::

    DECLARE PARAMETER thisIsOptional is 0,
                      thisIsOptionalToo is 0.
                      thisIsMandatory.

Because the optional parameters didn't come at the end.

Default parameters follow short-circuit logic
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Remember that if you have an optional parameter with an initializer
expression, the expression will not get executed if the calling
function had an argument present in that position.  The expression
only gets executed if the system needed to pad a missing argument.

.. versionadded:: 0.18.3
   Optional Parameters were added as a new feature in kOS 0.18.3



.. note::

    **Pass By Value**

    The following paragraph is important for people familiar with other programming languages. If you are new to programming and don't understand what it is saying, that's okay you can ignore it.

    At the moment the only kind of parameter supported is a pass-by-value parameter, and pass-by reference parameters don't exist. Be aware, however, that due to the way kOS is implemented on top of a reference-using object-oriented language (CSharp), if you pass an argument which is a complex aggregate structure (i.e. a Vector, or a List - anything that kOS lets you use a colon suffix with), then the parameters will behave exactly like being passed by reference because all you're passing is the handle to the object rather than the object itself. This should be familiar behavior to anyone who has written software in Java or C# before.

.. _set:

``SET``
-------

Sets the value of a variable. Implicitly creates a global variable if it doesn’t already exist::

    SET X TO 1.
    SET X TO y*2 - 1.

This follows the :ref:`scoping rules explained below <scope>`.  If the 
variable can be found in the current local scope, or any scope higher
up, then it won't be created and instead the existing one will be used.

.. _defined:

``DEFINED``
-----------

::

    DEFINED identifier

Returns a boolean true or false according to whether or not an 
identifier is defined in such a way that you can use it from
this part of the program.  (i.e. is it declared and is it in scope
and visible right now)::

    // This part prints 'doesn't exist":
    if defined var1 {
      print "var1 exists". 
    } else {
      print "var1 doesn't exist."
    }

    local var1 is 0.

    // But now it prints that it does exist:
    if defined var1 {
      print "var1 exists". 
    } else {
      print "var1 doesn't exist."
    }

The DEFINED operator pays attention to all the normal scoping rules
described in the :ref:`scoping section below <scope>`.  If an identifier
does exist but is not usable from the current scope, it will return false.

Note that DEFINED does not work well on things that are not pure identifiers.
for example::

   print defined var1:suffix1.

is going to end up printing "False" because it's looking for pure identifiers,
not complex suffix chains, and there's no identifier called "var1:suffix1".


Difference between SET and DECLARE LOCAL and DECLARE GLOBAL
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

The following three examples look very similar and you might ask
why you'd pick one instead of the other::

    SET X TO 1.
    DECLARE LOCAL X TO 1.
    DECLARE GLOBAL X TO 1.

They are slightly different, as follows:

``SET X TO 1.`` Performs the following activity:

  1. Attempt to find an already existing local X.  If found, set it to 1.
  2. Try again for each scoping level outside the current one.
  3. If and only if it gets all the way out to global scope and it still
     hasn't found an X, then create a new X with value 1, and do so at
     global scope.  This behavior is called making a "lazy global".

``DECLARE LOCAL X TO 1.`` Performs the following activity:

  1. Immediately make a new X right here at the local-most scope.
     Set it to 1.

``DECLARE GLOBAL X TO 1.`` Performs the following activity:

  1. Ignore whether or not there are any existing X's in a local scope.
  2. Immediately go all the way to global scope and make a new X there.
     Set it to 1.

When to use GLOBAL
::::::::::::::::::

You should use a ``DECLARE GLOBAL`` statement only sparingly.  It 
mostly exists so that a function can store values "in the caller"
for the caller to get its hands on.  It's generally a "sloppy" design
pattern to use, and it's much better to keep everything local
and only pass back things to the caller as return values.


``LOCK``
--------

Declares that the identifier will refer to an expression that is always re-evaluated on the fly every time it is used (See also :ref:`Flow Control documentation <lock>`)::

    SET Y TO 1.
    LOCK X TO Y + 1.
    PRINT X.    // prints "2"
    SET Y TO 2.
    PRINT X.    // prints "3"

Note that because of how LOCK expressions are in fact implemented as mini
functions, they cannot have local scope.  A LOCK *always* has global scope.

By default a ``LOCK`` expression is ``GLOBAL`` when made.  This is 
necessary for backward compatibility with older scripts that use
LOCK STEERING from inside triggers, loops, etc, and expect it to 
affect the global steering value.

Calling a LOCK that was created in another file
:::::::::::::::::::::::::::::::::::::::::::::::

If you try to call a lock that is declared in another program
file you run, it does not work, and has never worked prior to kOS
0.17.0:

File1.ks::

    run File2.
    print "x's locked value is " + x.

File2.ks::

    lock x to "this is x".

But now with the Kerboscript of kOS 0.17.0, you can make it work
by inserting empty parentheses after the lock name to help give
the compiler the hint that you expected x to be a function call
(which is what a lock really is):

Change this line::

    print "x's locked value is " + x.

To this instead::

    print "x's locked value is " + x().

and it should work.

Local lock
::::::::::

You can explicitly make a ``LOCK`` statement be LOCAL with the ``LOCAL``
keyword, like so:

``LOCAL LOCK`` identifier ``TO`` expression.

But be aware that doing so with a cooked steering control such
as THROTTLE or STEERING will not actually affect your ship.  The
automated cooked steering control is only reading the GLOBAL locks
for these settings.

The purpose of making a LOCAL lock is if you only need to use the
value temporarily for the duration of a function call, loop, or
if-statement body, and then you don't care about it anymore after
that.

Why do I care about a local lock?
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

You care because in order to make a LOCK work even after the variables
it's using in its expression go out of scope (which is necessary
for LOCK STEERING or LOCK THROTTLE to work if done from inside
a user function call or trigger body), locks need to preserve
a thing called a "closure".
( http://en.wikipedia.org/wiki/Closure_(computer_programming)

When they do this, it means none of the local variables used
in the function body they were declared in truly "go away" from
memory.  They live on, taking up space until the lock disappears.
Making the lock be local tells the computer that it can make the lock
disappear when it goes out of scope, and thus it doesn't need to
hold that "closure" around forever.

The tl;dr version:  It's more efficient for memory.  If you know 
for sure that your lock isn't getting used after your current
section of code is over, make it a local lock.


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

Scoping terms
-------------

.. note::
    .. versionadded:: 0.17
        In prior versions of Kerboscript, all identifiers other than
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
    never use the DECLARE statement, then your variables in Kerboscript
    will all be in *global scope*.  For simple easy scripts used by
    beginners, this is often enough and you don't have to read the rest
    of this topic until you start advancing to more intermediate scripts.

Local Scope
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
    variables, can be a large unmanageable chore, especially with
    programs written by more than one person collaborating together.
    (2) Even if you can keep track of all that in your head, there's
    a certain programming technique known as recursion
    ( http://en.wikipedia.org/wiki/Recursion#In_computer_science )
    in which you actually NEED to have local variable scope for
    the technique to even work at all.

If you need to have variables that only have local scope, either just
to keep your code more manageable, or because you literally need
local scope to allow for recursive function calls, then you use the
``DECLARE LOCAL`` statement (or just ``LOCAL`` for short) to create
the variables.

Scoping syntax
--------------

Presumed defaults
:::::::::::::::::

The DECLARE keyword and the LOCK keyword have some default
presumed scoping behaviors:

``DECLARE`` Is assumed to always be LOCAL when not otherwise specified.

``FUNCTION`` Is assumed to always be LOCAL when not otherwise specified.

``PARAMETER`` Cannot be anything but LOCAL to the location it's mentioned.
It is an error to attempt to declare a parameter with the GLOBAL keyword.

``LOCK`` Is assumed to always be GLOBAL when not otherwise specified.
this is necessary to preserve backward compatibility with how cooked
controls such as LOCK STEERING and LOCK THROTTLE work.

Explicit scoping keywords
:::::::::::::::::::::::::

The ``DECLARE``, ``FUNCTION``, and ``LOCK`` commands can be given
explicit ``GLOBAL`` or ``LOCAL`` keywords to define their intended
scoping level::

    //
    // These are all synonymous with each other:
    //
    DECLARE X TO 1.
    DECLARE LOCAL X TO 1.
    LOCAL X TO 1. // 'declare' is implied and optional when scoping words are used
    LOCAL X IS 1. // 'declare' is implied and optional when scoping words are used
    //
    // These are all synonymous with each other:
    //
    DECLARE GLOBAL X TO 1.
    GLOBAL X TO 1. // 'declare' is implied and optional when scoping words are used
    GLOBAL X IS 1. // 'declare' is implied and optional when scoping words are used

Even when the word 'DECLARE' is left off, the statement can still be
referred to as a "declare statement".  The word "declare" is implied
by the use of LOCAL or GLOBAL and you are allowed to leave it off 
merely to reduce verbosity.

Explicit Scoping required for @lazyglobal off
:::::::::::::::::::::::::::::::::::::::::::::

Note that when operating under the :ref:`@LAZYGLOBAL OFF <lazyglobal>`
directive the keywords LOCAL and GLOBAL are no longer optional for
**declare identifier** statements, and are in fact required.  You
are not allowed to rely on these presumed defaults when you've
turned off LAZYGLOBAL.  (This only applies to trying to make
a variable with **declare identifier to value**, and not to
``declare parameter`` or ``declare function``.)


Locals stated at the global level are global
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Note that if you put a statement at the outermost scope
of the program, then there is effectively no difference
between a ``DECLARE LOCAL`` (or just ``LOCAL`` for short)
and a ``DECLARE GLOBAL`` (or just ``GLOBAL`` for short) statement.  
They are both going to make a variable at global scope because that's
the scope the program was in when the statement was encountered.


Examples::

    GLOBAL x IS 10. // X is now a global variable with value 10,
    SET y TO 20. // Y is now a global variable (implicitly) with value 20.
    LOCAL z IS 0.  // Z is now a global variable
                   // because even though this says LOCAL, it was
                   // stated at the outermost, global scope.

    SET sum to -1. // sum is now an implicitly made global variable, containing -1.

    // A function to return the mean average of all the items in the list
    // passed into it, under the assumption all the items in the list are
    // numbers of some sort:
    FUNCTION calcAverage {
      PARAMETER inputList.
      
      LOCAL sum IS 0. // sum is now local to this function's body.
      FOR val IN inputList {
        SET sum TO sum + val.
      }.
      print "Inside calcAverage, sum is " + sum.
      RETURN sum / inputList:LENGTH.
    }.

    SET testList TO LIST(5,10,15);
    print "average is " + calcAverage(testList).
    print "but out here where it's global, sum is still " + sum.

This example will print::

    Inside calcAverage, sum is 30
    average is 10
    but out here where it's global, sum is still -1
    
Thus proving that the variable called SUM inside the function is NOT the
same variable as the one called SUM out in the global main code.

Nesting
~~~~~~~

The scoping rules are nested as well.  If you attempt to use a
variable that doesn't exist in the local scope, the next scope "outside"
it will be used, and if it doesn't exist there, the next scope "outside"
that will be used and so on, all the way up to the global scope.  Only
if the variable isn't found at the global scope either will it be 
implicitly created.

.. _trigger_scope:

Scoping and Triggers:
:::::::::::::::::::::

Triggers such as:

  - WHEN <expression> { <statements> }.

and

  - ON <boolean variable> { <statements> }.

Do not work predictably when you use local variables in the <expression>
part of them.  They need to be designed to use global variables only,
because they outlive the duration of any particular scoping braces.
You can declare local variables within their <statements> in their bodies,
just don't use local variables in the trigger conditions.

.. _lazyglobal:

``@LAZYGLOBAL`` directive
:::::::::::::::::::::::::

Often the fact that you can get an implicit global variable declared
without intending to can lead to a lot of code maintenance headaches
down the road.  If you make a typo in a variable name, you end up
creating a new variable instead of generating an error.  Or you may just
forget to mark the variable as local when you intended to.  

If you wish to instruct Kerboscript to alter its behavior and
disable its normal implicit globals, and instead demand that all
variables MUST be explicitly declared and may not use implied
lazy scoping, the ``@LAZYGLOBAL`` compiler directive allows you to
do that.

If you place the words::

    @LAZYGLOBAL OFF.

At the start of your program, you will turn off the compiler's
lazy global feature and it will require you to explicitly mention
all variables you use in a declaration somewhere (with the 
exception of the built-in variables such as THROTTLE, STEERING,
SHIP, and so on.)

.. note::
    The @LAZYGLOBAL directive does not affect LOCK statements.
    LOCKS are a special case that define new pseudo-functions
    when encountered and don't quite work the same way as
    SET statements do. Thus even with @LAZYGLOBAL OFF, it's still
    possible to make a LOCK statement with a typo in the identifier
    name and it will still create the new typo'ed lock that way.

@LAZYGLOBAL Can only exist at the top of your code.
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The @LAZYGLOBAL compile directive is only allowed as the first
non-comment thing in the program file.  This is because it
instructs the compiler to change its default behavior for the
duration of the entire file's compile.

@LAZYGLOBAL Makes ``LOCAL`` and ``GLOBAL`` mandatory
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Normally the keywords ``local`` and ``global`` can be left off
as optional in declare **identifier** statements.  But when you
turn LAZYGLOBAL off, the compiler starts requiring them to be
explicitly stated for **declare identifier** statements, to
force yourself to be clear and explicit about the difference.

For example, this program, which is valid::

    function foo {print "foo ". }
    declare x is 1.

    print foo() + x.

Starts giving errors when you add @LAZYGLOBAL OFF to the top::

    @LAZYGLOBAL OFF.
    function foo {print "foo ". }
    declare x is 1.

    print foo() + x.

Which you fix by explicitly stating the local keyword, as follows::

    @LAZYGLOBAL OFF.
    function foo {print "foo ". }  // This does not need the 'local' keyword added
    declare local x is 1.          // But this does because it is a declare *identifier* statement.
                                   // you could have also just said:
                                   //     local x is 1.
                                   // without the 'declare' keyword.

    print foo() + x.

If you get in the habit of just writing your **declare identifier**
statements like ``local x is 1.`` or ``global x is 1.``, which is
probably nicer to read anyway, the issue won't come up.

Longer Example of use
~~~~~~~~~~~~~~~~~~~~~

Example::

    @LAZYGLOBAL off.
    global num TO 1.
    IF TRUE {
      LOCAL Y IS 2.
      SET num TO num + Y. // This is fine.  num exists already as a global and
                          // you're adding the local Y to it.
      SET nim TO 20. // This typo generates an error.  There is
                     // no such variable "nim" and @LAZYGLOBAL OFF
                     // says not to implicitly make it.
    }.

Why ``LAZYGLOBAL OFF``?
    The rationale behind ``LAZYGLOBAL OFF.`` is to primarily be used in 
    cases where you're writing a library of function calls you intend to
    use elsewhere, and want to be careful not to accidentally make
    them dependent on globals outside the function itself.

The ``@LAZYGLOBAL OFF.`` directive is meant to mimic Perl's ``use strict;``
directive.

~~~~~~

History:
    Kerboscript began its life as a language in which you never have to
    declare a variable if you don't want to.  You can just create any
    variable implicitly by just using it in a SET statement.

    There are a variety of programming languages that work like this,
    such as Perl, JavaScript, and Lua.  However, they all share one
    thing in common - once you want to allow the possibility of having
    local variables, you have to figure out how this should work with
    the implicit variable declaration feature.

    And all those languages went with the same solution, which 
    Kerboscript now follows as well.  Because implicit undeclared
    variables are intended to be a nice easy way for new users to
    ease into programming, they should always default to being 
    global so that people who wish to keep programming that way
    don't need to understand or deal with scope.

