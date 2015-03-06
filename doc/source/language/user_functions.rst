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
       - D. Return to where you remembered from (A) and continue from there.

    This feature goes by many different names, with slightly different
    precise meanings: *Subroutines*, *Procedures*, *Functions*, etc.
    For the purposes of kerboscript, we will refer to all uses of this
    feature with the term *Function*, whether it *technically* fits the
    mathematical definition of a "function" or not.

.. _declare function:

``DECLARE FUNCTION``
--------------------

In kerboscript, you can make your own user functions using the
DECLARE FUNCTION command, which has syntax as follows:

  ``declare function`` *identifier* ``{`` *statements* ``}`` *optional dot (.)*

example::

    // Print the string you pass in, in one of the 4 corners
    // of the terminal:
    //   mode = 1 for upper-left, 2 for upper-right, 3
    //          for lower-left, and 4 for lower-right:
    //
    declare function print_corner {
      declare parameter mode.
      declare parameter text.

      declare row to 0.
      declare col to 0.

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

``Declare function`` can appear anywhere in a kerboscript program,
and once its been "parsed" by the compiler, the function can be called
from anywhere in the program.  

The best design pattern is probably to create your library of function
calls as one or more separate .ks files that contain ONLY function
definitions and nothing else in them.  Then when you "run" the file
containing the functions, what you're really doing is just loading
the function definitions into memory so they can be called by other
programs.  At the top of your main script you can then "run" the
other scripts containing the library of functions to get them
compiled into memory.

``DECLARE PARAMETER``
---------------------

If your function expects to have parameters passed into it, you can
use the :ref:`DECLARE PARAMETER <declare parameter>` command to do
so.  This is the same command as is used to declare parameters for
running a whole script.  By putting a DECLARE PARAMETER statement
inside a function, you tell the kerboscript compiler that you want
the parameter to be for that function, not for the whole script.

An example of using ``declare parameter`` can be seen in the example
above, where it is used for the ``mode`` and ``text`` parameters.

``DECLARE .. TO``
-----------------

(aka: **local variables**)

Syntax: ``DECLARE`` *identifier* ``TO`` *expression* *dot*

Examples::

    declare x to 5.
    declare y to 2*x - 1.
    declare halfSpeed to SHIP:VELOCITY:ORBIT:MAG / 2.

If your function needs to make a local variable, it can do so using
the :ref:`DECLARE <declare>` command.  Whenever the DECLARE command is
seen inside a function, the compiler assumes the variable is meant to
be local to that function's block.  This also works with recursion.
If you recursively call a function again and again, there will be 
new copies stacked up of all the local variables made with DECLARE,
but not of the variables implicitly made global without DECLARE.

An example of using ``declare`` for a local variable can be seen in
the example above, where it is used for the ``row`` and ``col`` variables.

A more in-depth explanation of kerboscript's scoping rules and how they
work is found :ref:`on another page <scope>`

Initializers are now mandatory for the DECLARE statement
::::::::::::::::::::::::::::::::::::::::::::::::::::::::

This is now **illegal** syntax::

    declare x.  // no initial value for x given.

.. warning::
  .. versionadded:: 0.17
    **Breaking Change:** The kerboscript from prior versions
    of kOS did allow you do make ``declare`` statements 
    without any initializers in them (and in fact you couldn't
    provide an initializer for them in prior versions even if
    you wanted to.)

In order to avoid the issue of having uninitialized variables in
kerboscript, the declare command *requires* the use of the
initializer clause.

  *This is especially important as kerboscript is a late typing
  language in which it is impossible for the compiler to choose
  some implied default initial value for the variable from some
  language spec.  This is because until a value has been assigned
  into it, the compiler wouldn't even know what type of default to
  use - a string, an integer, a floating point number, etc.*

Difference between declare and set
::::::::::::::::::::::::::::::::::

You may think that::

    declare x to 5.

is identical to just not using a ``declare`` statement
at all, and just performing ``set x to 5.`` alone, but
it is not.  With ``declare``, a NEW variable called ``x`` will be made
at the current local scope, temporarily hiding any existing ``x``
variables that may otherwise have been reachable in a more global scope.
With ``set``, if there already is an ``x`` variable you can use in a
different scope higher than this scope, it will be used, and only
if it doesn't exist will a new ``x`` be made (and that new ``x`` will
be global, not local).

.. _return:

``RETURN``
----------

``return`` *expression(optional)* *dot(mandatory)*

examples::

    return 3*x.
    
    return.

If your function needs to exit early, and/or if it needs to pass a
return value back to the user, you can use the RETURN staement to
do so.  RETURN accepts an optional argument - the value to pass back
to the caller.  Note that functions in kerboscript are very weakly
typed with late binding.  You cannot declare the expected return
type for the function, and it's up to you to ensure that all possible
returned values are useful and meaningful.

example::

    // Cacluate what component of a vessel's surface
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

    declare function embiggen {
      declare parameter x.

      set x to x + 10.

      print "x has been embiggened to " + x.
    }.
    
    set global_val to 30.
    print global_val.
    embiggen(global_val).
    print global_val.

The above example will print::

    30
    x has been embiggened to 40
    30

Although the function added 10 to its OWN copy of the parameter, the 
caller's copy of the parameter remained unchanged.

Important exception to passing by value - structures
::::::::::::::::::::::::::::::::::::::::::::::::::::

If the value being sent to the function as its parameter is a
complex structure consisting of sub-parts (i.e. if it has
suffxes) rather than being a simple single scalar value like a
number, then the copy in the function is *really* a copy of
the reference pointing to the object, so changes you make
in the object really WILL change it, as shown here::

    define function half_vector {
      define parameter vec. //vector passed in.

      print "full vector is " + vec.

      set vec:x to vec:x/2.
      set vec:y to vec:y/2.
      set vec:z to vec:z/2.

      print "half vector is " + vec.
    }.

    set global_vec to V(10,20,30).
    half_vector(global_vec).
    print "afterward, global_vec is now " + global_vec.

This will give the following result::

    full vector is v(10,20,30)
    half vector is v(5,10,15)
    afterward, global_vec is now v(5,10,15)

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

Recursion
---------

Recursive algorithms (TODO: wikipedia link) are possible with kerboscript
functions, provided you remember to always exclusively use local variables
made with the ``declare .. to`` statement in the body of the function, and 
never use global variables for something that you intended to be
different per recursive call.


User Function Gotchas
---------------------

Inconsistent returns
::::::::::::::::::::

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
       }.
    }.

Then the kerboscript compiler is not clever enough to detect this
and warn you about it.  The internal stack will not get corrupted
by this error, as some experienced programmers might expect upon
hearing this (because secretly all kerboscript user functions
return a value even if it's never used, so there's universally
always something to pop off the stack even for the empty return
statements.) However, you will still have to deal with the fact
that the calling program might be getting nulls back some of the
time if you make this programming error.

In general, make sure that if you *sometimes* return a value from
a user function, that you *always* do so in every path through your
function.

Accidentally using globals
::::::::::::::::::::::::::

It is possible to accidentally create global variables
when you didn't meant to, just because you made a typo.

For example::

    define function mean {
      declare parameter the_list.
      declare sum to 0.

      for item in the_list {
        set dum to sum + item. // typo - said 'dum' instead of 'sum'.
      }.

      return sum / the_list:length.
    }.

The above example contains a typo that causes a global variable to be 
made where you didn't mean to.  You wanted to say "sum" but said "dum" 
and insted of that being an error, kerboscript happily said "okay,
well since you're setting a variable name that doesn't exist yet,
I'll make it for you implicitly" (and it ends up being a global).

When you are writing libraries of code for yourself to call, this can
really be annoying.  And it's a very common problem with "sloppy"
declaration languages that allow you to use variable names without
declaring them first.  Most such languages have provided a way to
catch the problem, and allow you to instruct the compiler "please
don't let me do that.  Please force me to declare everything".

The way that is done in kerboscript is by using a ``NOLAZYGLOBAL`` 
section, :ref:`as described here <nolazyglobal>`.

Had the function above been wrapped inside a NOLAZYGLOBAL section,
the typo would be noticed::

    nolazyglobal {

      define function mean {
        declare parameter the_list.
        declare sum to 0.

        for item in the_list {
          set dum to sum + item. // error - 'dum' is an unknown identifier.
        }.

        return sum / the_list:length.
      }.

    }.

