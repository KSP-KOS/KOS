.. _user_functions:

**KerboScript** User Functions
==============================

.. contents::
    :local:
    :depth: 2

This page covers functions created by you, the user of kerboscript,
rather than the built-in functions provided by kOS.

Help for the new user - What is a Function?
-------------------------------------------

    In programming terminology, there is a commonly used feature of
    many programming languages that works as follows:

    - 1. Create a chunk of program instructions that you don't intend to execute YET.
    - 2. Later, when executing other parts of the program, do the following:

        - A. Remember the current location in the program.
        - B. Jump to the previously created chunk of code from (1) above.
        - C. Run the instructions there.
        - D. Return to where you remembered from (2.A) and continue from there.

    This feature goes by many different names, with slightly different
    precise meanings: *Subroutines*, *Procedures*, *Functions*, etc.
    For the purposes of kerboscript, we will refer to all uses of this
    feature with the term *Function*, whether it *technically* fits the
    mathematical definition of a "function" or not.

.. warning::
    **Functions created in programs can only be called from programs,
    not from the interpreter terminal prompt**.

    If you attempt to create a function in a program, and then call it
    from the interactive prompt in the interpreter instead of calling
    it from inside a program, it will definitely not work properly,
    but the exact error you get will depend on several "random"
    factors.  This may be fixed later by a later release, but for
    now, don't do it.  For further explanation, see the section entitled
    :ref:`Functions and the interpreter terminal <interpreter functions>`.

.. _declare function:

DECLARE FUNCTION
----------------

In kerboscript, you can make your own user functions using the
DECLARE FUNCTION command, which has syntax as follows:

  [``declare``] [``local``|``global``] ``function`` *identifier* ``{`` *statements* ``}`` *optional dot (.)*

The statement is called a "declare function" statement even when the optional
word "declare" was left off.

::

    declare function hi { print "hello". }
    declare local function hi { print "hello". }
    declare global function hi { print "hello". }
    local function hi { print "hello". }
    global function hi { print "hello". }
    function hi { print "hello". }

Example::

    // Print the string you pass in, in one of the 4 corners
    // of the terminal:
    //   mode = 1 for upper-left, 2 for upper-right, 3
    //          for lower-left, and 4 for lower-right:
    //
    function print_corner {
      parameter mode.
      parameter text.

      local row is 0.
      local col is 0.

      if mode = 2 or mode = 4 {
        set col to terminal:width - text:length.
      }.
      if mode = 3 or mode = 4 {
        set row to terminal:height - 1.
      }.

      print text at (col, row).
    }.

    // An example of calling it:

    print_corner(4,"That's me in the corner").

Default varies when local or global are omitted
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

When the explicit ``local`` or ``global`` scope keyword is missing,
the default that is chosen depends on where the function is declared.
If the function was declared in the outermost file scope, it will be
assumed to be global.  If it was declared inside some braces, it will
be assumed to be local to those braces::

    // f1 behaves as if it was declared global
    // because this is the outermost file scope:
    function f1 {
       print "I am in f1".
    }
    if true {
      // f2 behaves as if it was declared local
      // because this is inside some braces ("{","}"):
      function f2 {
       print "I am in f2".
      }
      f2(). // can be called from here.
    }
    f2(). // This throws an error because f2 was local to the "if true" braces.
    
A declare function command can appear anywhere in a kerboscript program,
and once its been "parsed" by the compiler, the function can be called
from anywhere in the program that is able to see the function according
to the scoping rules.

The best design pattern is probably to create your library of function
calls as one or more separate .ks files that contain just function
definitions and not much else in them.  Then when you "run" the file
containing the functions, what you're really doing is just loading
the function definitions into memory so they can be called by other
programs.  At the top of your main script you can then "run" the
other scripts containing the library of functions to get them
compiled into memory.

Using RUN ONCE or RUNONCEPATH
-----------------------------

If you want to load a library of functions that ALSO perform some
initialization mainline code, but you only want the mainline code
to execute once when the library is first loaded, rather than
every time a subprogram runs your library, then use the 'once'
keyword with the RUN command, or the RUNONCEPATH command, as
follows::

    // This will run mylib1 3 times, re-running the mainline code in it:`
    run mylib1.
    run mylib1.
    runpath("mylib1"). // just the same thing as 'run mylib1', really.

    // This will run mylib2 only one time, ignoring the additional
    // instances:
    run once mylib2.
    run once mylib2. // mylib2 was already run, will not be run again.
    runoncepath("mylib2"). // mylib2 was already run, will not be run again.

Example:  Let's say you want to have a library that keeps a counter
and always returns the next number up every time it's called.  You
want it initialized to start with, but not get re-initialized every time
another sub-program includes the library in its code.  So you have this:

**prog1, which calls counterlib:** ::

    // prog1
    run once counterlib.

    // Get some unique IDs:
    print "prog1:      next counter ID = " + counter_next().
    print "prog1:      next counter ID = " + counter_next().
    print "prog1:      next counter ID = " + counter_next().

    run subprogram.

**subprogram, which ALSO calls counterlib:** ::

    // subprogram
    runoncepath("counterlib"). // same as 'run once counterlib.'

    print "subprogram: next counter ID = " + counter_next().
    print "subprogram: next counter ID = " + counter_next().
    print "subprogram: next counter ID = " + counter_next().


**counterlib** ::

    // init code:
    global current_num is 0.

    // counter_next()
    function counter_next {
       set current_num to current_num + 1.
       return current_num.
    }

.. highlight:: none

The above example prints this::

    prog1:      next counter ID = 1
    prog1:      next counter ID = 2
    prog1:      next counter ID = 3
    subprogram: next counter ID = 4
    subprogram: next counter ID = 5
    subprogram: next counter ID = 6

whereas, had you used just ``run counterlib.`` instead of
``run once counterlib.``, then it would have printed this::

    prog1:      next counter ID = 1
    prog1:      next counter ID = 2
    prog1:      next counter ID = 3
    subprogram: next counter ID = 1
    subprogram: next counter ID = 2
    subprogram: next counter ID = 3

.. highlight:: kerboscript

because ``subprogram`` would have run the mainline code
``global current_num is 0`` again when it was run inside
``subprogram``.


DECLARE PARAMETER
-----------------

If your function expects to have parameters passed into it, you can
use the :ref:`DECLARE PARAMETER <declare parameter>` command to do
so.  This is the same command as is used to declare parameters for
running a whole script.  By putting a DECLARE PARAMETER statement
inside a function, you tell the kerboscript compiler that you want
the parameter to be for that function, not for the whole script.

An example of using ``declare parameter`` can be seen in the example
above, where it is used for the ``mode`` and ``text`` parameters.

(Again, even when the word 'declare' is missing, we still call them
'declare parameter' commands.)

Calling a function
------------------

To call a function you created, you call it the same way you
call a built-in function, by putting a pair of parentheses
to the right of it, as shown here::

    function example_function {
      print "hello, this is my example.".
    }

    example_function().

If the function takes parameters, then you put them in the parentheses
just like when running a program.  You can see an example of this above
in the previous example where it said::

    print_corner(4,"That's me in the corner").

.. _default_parameters:

Optional Parameters (parameter defaults)
----------------------------------------

If you wish, you may make some of the parameters of a user function optional
by defaulting them to a starting value with the ``IS`` keyword, as follows:

example 1::

    FUNCTION MYFUNC {
      DECLARE PARAMETER P1, P2, P3 is 0, P4 is "cheese".
      print P1 + ", " + P2 + ", " + P3 + ", " + P4.
    }

example 2::

    FUNCTION MYFUNC {
      PARAMETER P1, P2, P3 is 0, P4 is "cheese".

      print P1 + ", " + P2 + ", " + P3 + ", " + P4.
    }

example 3::

    FUNCTION MYFUNC {
      PARAMETER P1.
      PARAMETER P2.
      PARAMETER P3 is 0.
      PARAMETER P4 is "cheese".

      print P1 + ", " + P2 + ", " + P3 + ", " + P4.
    }

In the above examples, all of which are the same, if you call the function
with parameter 3 or 4 missing, kOS will assign it the default value mentioned
in the ``PARAMETER`` statement, like in the examples below::

    MYFUNC(1,2).         // prints "1, 2, 0, cheese".
    MYFUNC(1,2,3).       // prints "1, 2, 3, cheese".
    MYFUNC(1,2,3,"hi").  // prints "1, 2, 3, hi".

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

.. _interpreter functions:

Functions and the terminal interpreter
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

You **cannot** call functions from the interpreter interactive
command line if they were declared inside of script programs.
If you do, you will get seemingly "random" errors.  The reasons
for this are complex, but the short version is because the
memory the script files' pseudo-machine language instructions
live in and the memory the interpreter's pseudo-machine
language instructions live in are two different things.

The effect you may see if you attempt this is merely
an "Unknown Identifer" error, or worse yet, it may end
up jumping into random parts of your code that have nothing
to do with the actual function call you're trying to
make.

As a rule of thumb, make sure you only use
functions from inside script programs.  Don't try to call
them interactively from the interpreter prompt.  You will
get very strange and (seemingly) inexplicable errors.

In the future we may find a way to fix this problem,
but for right now, just don't do it.

Calling a function without parentheses (please don't)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

In some cases it is possible to call a function with the
parentheses off, as shown below, but this is not recommended::

    function example_function {
      print "hello, this is my example.".
    }

    example_function. // please don't do this, even if it works.

This is a holdover from the fact that functions and locks are
really the same thing, and you need to be able to call a lock
without the parentheses for old scripts written prior to kOS
version 0.17.0 to continue working.

Omitting parentheses only works in the same file
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

One reason to avoid the above technique (of leaving the parentheses
off) is that it really only works when you try to call a function
that was declared in the same file.  If you want to call a *library*
function (a function you made for yourself in another file) then it
does not work, for complex reason involving the compiler and late-time
binding.

LOCAL .. TO
-----------

(aka: **local variables**)

Syntax:

* ``DECLARE`` *identifier* ``TO`` *expression* *dot*
* ``LOCAL`` *identifier* ``IS`` *expression* *dot*
* ``DECLARE LOCAL`` *identifier* ``IS`` *expression* *dot*

The above are all the same, although the version that
just says ``LOCAL identifier IS expr.`` is preferred.

Examples::

    declare x to 5.
    local y is 2*x - 1.
    declare local halfSpeed to SHIP:VELOCITY:ORBIT:MAG / 2.

If your function needs to make a local variable, it can do so using
the :ref:`DECLARE <declare>` command.  Whenever the DECLARE command is
seen inside a function, the compiler assumes the variable is meant to
be local to that function's block.  This also works with recursion.
If you recursively call a function again and again, there will be
new copies stacked up of all the local variables made with DECLARE,
but not of the variables implicitly made global without DECLARE.

An example of using ``local`` for a local variable can be seen in
the example above, where it is used for the ``row`` and ``col`` variables.

A more in-depth explanation of kerboscript's scoping rules and how they
work is found :ref:`on another page <scope>`.

Initializers are now mandatory for the DECLARE statement
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

This is now **illegal** syntax::

    declare x.  // no initial value for x given.

.. warning::
  .. versionadded:: 0.17
    **Breaking Change:** The kerboscript from prior versions
    of kOS did allow you to make ``declare`` statements
    without any initializers in them (and in fact you couldn't
    provide an initializer for them in prior versions even if
    you wanted to).	

In order to avoid the issue of having uninitialized variables in
kerboscript, any declare statement *requires* the use of the
initializer clause.

  *This is especially important as kerboscript is a late typing
  language in which it is impossible for the compiler to choose
  some implied default initial value for the variable from some
  language spec.  This is because until a value has been assigned
  into it, the compiler wouldn't even know what type of default to
  use - a string, an integer, a floating point number, etc.*

Difference between declare and set
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

You may think that::

    local x is 5.

is identical to just not using a declare local statement
at all, and just performing ``set x to 5.`` alone, but
it is not.  With ``declare local`` (or just ``declare`` or just ``local``),
a NEW variable called ``x`` will be made at the current local scope,
temporarily hiding any existing ``x`` variables that may otherwise have
been reachable in a more global scope.  With ``set``, if there already
is an ``x`` variable you can use in a different scope higher than this
scope, it will be used, and only if it doesn't exist will a new ``x``
be made (and that new ``x`` will be global, not local).

.. _return:

RETURN
------

``return`` *expression(optional)* *dot(mandatory)*

Examples::

    return 3*x.

    return.

If your function needs to exit early, and/or if it needs to pass a
return value back to the user, you can use the RETURN statement to
do so.  RETURN accepts an optional argument - the value to pass back
to the caller.  Note that functions in kerboscript are very weakly
typed with late binding.  You cannot declare the expected return
type for the function, and it's up to you to ensure that all possible
returned values are useful and meaningful.

Example::

    // Note, in this example, the keyword 'declare' is
    // spelled out explicitly.  You can choose to do so
    // if you wish.  It's up to you what you aesthetically
    // prefer.

    // Calculate what component of a vessel's surface
    // velocity is Northward:
    declare function north_velocity {
      declare parameter which_vessel.

      return VDOT(which_vessel:velocity:surface, which_vessel:north:vector).
    }.

Passing by value
----------------

Parameters to user functions in kerboscript are all pass-by-value, with
an important caveat.  "Pass by value" means that the function is
working on a copy of the variable you passed in, rather than the
original variable.  This matters when the function tries to change the
value of the parameter, as in this example::

    function embiggen {
      parameter x.

      set x to x + 10.

      print "x has been embiggened to " + x.
    }.

    set global_val to 30.
    print global_val.
    embiggen(global_val).
    print global_val.


.. highlight:: none

The above example will print::

    30
    x has been embiggened to 40
    30

.. highlight:: kerboscript

Although the function added 10 to its OWN copy of the parameter, the
caller's copy of the parameter remained unchanged.

Important exception to passing by value - structures
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

If the value being sent to the function as its parameter is a
complex structure consisting of sub-parts (i.e. if it has
suffixes) rather than being a simple single scalar value like a
number, then the copy in the function is *really* a copy of
the reference pointing to the object, so changes you make
in the object really WILL change it, as shown here::

    function half_vector {
      parameter vec. //vector passed in.

      print "full vector is " + vec.

      set vec:x to vec:x/2.
      set vec:y to vec:y/2.
      set vec:z to vec:z/2.

      print "half vector is " + vec.
    }.

    set global_vec to V(10,20,30).
    half_vector(global_vec).
    print "afterward, global_vec is now " + global_vec.

.. highlight:: none

This will give the following result::

    full vector is v(10,20,30)
    half vector is v(5,10,15)
    afterward, global_vec is now v(5,10,15)

.. highlight:: kerboscript

Because a vector is a suffixed structure, it effectively acts as if
it was passed in by reference instead of by value, and so when it
was changed in the function, the caller's original copy is what was
being changed.

This may be hard to get used to for new programmers, however
experienced programmers who use some modern object-oriented languages
will find this behavior very familiar.  Only primitives are passed by
value.  Structures are passed by their reference rather than trying to
make a deep copy of the object for the function to use.

*This behavior is inherited from the fact that kerboscript is
implemented on top of C#, which is one of several OOP languages that
work like this.*

Nesting functions inside functions
----------------------------------

You are allowed to make a local function existing inside another function.

This means that the containing function is the only place the
nested function can be called from.

Example::

    function getMean {
      parameter aList.

      function getSum {
        parameter aList. // note, this is a local aList MASKING the other one.

        local sum is 0.
        for num in aList {
          set sum to sum + num.
        }.
        return sum.
      }.

      return getSum(aList) / aList:LENGTH.
    }.

    set L to LIST().
    L:ADD(10).
    L:ADD(9).
    print "mean average is " + getMean(L).

    // The following line will give an error because
    // getSum is local inside of getMean, and isn't allowed
    // to be called from here:
    //
    print "getSum is " + getSum(L).


Recursion
---------

Recursive algorithms ( http://en.wikipedia.org/wiki/Recursion#In_computer_science )
are possible with kerboscript functions, provided you remember to
always exclusively use local variables made with a declare statement
in the body of the function, and never use global variables for
something that you intended to be different per recursive call.

Anonymous functions
-------------------

You can make :ref:`Anonymous functions <anonymous_functions>` in kerboscript
by simply leaving off the function keyword and the name of the function,
and just using the curly braces (``{``, ``}``) around some statements.
When the compiler sees a standalone set of curly braces like this being used
in the context of an *expression* (rather than as a standalone statement),
then it will compile the contents of the braces as a function, meaning that
the keywords ``parameter`` and ``return`` will work as expected inside them.
Then it will leave a :struct:`KOSDelegate` of the function behind as the
value of the expression, which can then be assigned to a variable, or
passed as an argument, etc.  The full details of what this means, and how
to use it, is :ref:`explained elsewhere <anonymous_functions>`.

User Function Gotchas
---------------------

Calling program's functions from the interpreter
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

As :ref:`explained above <interpreter functions>`, kOS does
not support the calling of a function from the interpreter console
and if you attempt it you will get very strange and random errors
that you might waste a lot of time trying to track down.

Inconsistent returns
~~~~~~~~~~~~~~~~~~~~

Note that if you sometimes do and sometimes don't return a value, from
the same function, as in the example here::

    // A badly designed function, with inconsistency
    // in whether or not it returns a value:
    //
    DECLARE FUNCTION foo {
       DECLARE PARAMETER x.
       IF X < 0 {
         RETURN. // no return value.
       } ELSE {
         RETURN "hello". // a string return value
       }
    }

Then the kerboscript compiler is not clever enough to detect this
and warn you about it.  However, the internal stack will not get
corrupted by this error, as some experienced programmers might
expect upon hearing this (because secretly all kerboscript user
functions return a value of zero if they never gave an explicit 
return value, so there's universally always something to pop off
the stack even for the empty return statements.) 

In general, it's still a good idea to make sure that if you
*sometimes* return a value from a user function, that you
*always* do so in every path through your function.

Accidentally using globals
~~~~~~~~~~~~~~~~~~~~~~~~~~

It is possible to accidentally create global variables
when you didn't meant to, just because you made a typo.

For example::

    function mean {
      parameter the_list.
      local sum is 0.

      for item in the_list {
        set dum to sum + item. // typo - said 'dum' instead of 'sum'.
      }.

      return sum / the_list:length.
    }.

The above example contains a typo that causes a global variable to be
made where you didn't mean to.  You wanted to say "sum" but said "dum"
and instead of that being an error, kerboscript happily said "okay,
well since you're setting a variable name that doesn't exist yet,
I'll make it for you implicitly" (and it ends up being a global).

When you are writing libraries of code for yourself to call, this can
really be annoying.  And it's a very common problem with "sloppy"
declaration languages that allow you to use variable names without
declaring them first.  Most such languages have provided a way to
catch the problem, and allow you to instruct the compiler, "Please
don't let me do that.  Please force me to declare everything."

The way that is done in kerboscript is by using a ``@LAZYGLOBAL``
compiler directive, :ref:`as described here <lazyglobal>`.

Had the function above been compiled under a ``@LAZYGLOBAL off.``
compiler directive, the typo would be noticed::

    @lazyglobal off.

    local function mean {
      local parameter the_list.
      local sum is 0.

      for item in the_list {
        set dum to sum + item. // error - 'dum' is an unknown identifier.
      }.

      return sum / the_list:length.
    }.
