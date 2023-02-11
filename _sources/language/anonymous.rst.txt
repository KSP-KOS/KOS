.. _anonymous_functions:

Anonymous Functions (A kind of Delegate)
========================================

.. note::
    .. versionadded:: 1.0.0
        The anonymous function feature described on
        this page did not exist prior to kOS 1.0.0

.. contents:: Contents
    :local:
    :depth: 2

Overview
--------

There is a certain kind of :ref:`Delegate <delegates>` in which you
don't need to actually name the function in question, and can just
assign it right into a variable *as* a delegate to begin with, or
pass it *as* a delegate to another function call.

This is referred to as an
`Anonymous Function <https://en.wikipedia.org/wiki/Anonymous_function>`__.

Syntax
------

In kerboscript, you can use this ability and it looks like this:

Given any :ref:`user function<user_functions>` like so::

    function my_function_name {
      // ---.
      //    |
      //    |---  The body of the function goes here.
      //    |
      // ---'
    }

You can make that function into a delegate that can be assigned into a
variable, or passed as an argument to another function by simply leaving
off the name and the 'function' keyword and just using the section in
the curly braces by itself, like in this example::

    set some_variable to {
      // ---.
      //    |
      //    |---  The body of the function goes here.
      //    |
      // ---'
    }.   // <-- Note the period ('.') statement terminator is mandatory
         //     here because this is actually an ordinary SET statement.

    // some_variable is now a KOSDelegate of this function.
    // It can be called just like delegates can, like so:
    some_variable().
    // or like so:
    some_variable:call().

Passing in to other functions
-----------------------------

Where this is often useful is in passing a small function into another
function.  Here's an example.  Let's say you have a function that
returns a list of all the :ref:`bodies <body>` in your game that fit
some criteria that are unspecified until it gets used, like so::

    function select_bodies {

      // The parameter is expected to be a
      // delegate you can call on a body, that
      // returns true if the body should be
      // included, or false if it shouldn't:
      parameter should_include.

      local all_bodies is LIST().
      local some_bodies is LIST().

      list BODIES in all_bodies.
      for bod in all_bodies {
        if should_include(bod) {
          some_bodies:ADD(bod).
        }
      }
      return some_bodies.
    }

    // Example of how it could have been used with a traditional named function:
    // -------------------------------------------------------------------------
    //
    // function is_smaller_than_mun {
    //   parameter b. return (b:RADIUS < Mun:RADIUS).
    // }
    //
    // local small_bodies is select_bodies( is_smaller_than_mun@ ).

    // But we're going to do the same thing using an anonymous function instead:
    // -------------------------------------------------------------------------

    local small_bodies is select_bodies( { parameter b. return (b:RADIUS < Mun:RADIUS).} ).



    print "List of all bodies smaller than Mun is:".
    print small_bodies.

Anywhere a :ref:`Delegate <delegates>` was expected to be used, you can use
an anonymous function in its place instead.

Lexicon of functions
--------------------

One example of a useful way to use anonymous functions is to create for
yourself a collection of delegates::

    function make_vessel_utilities {
      parameter ves.

      // Create a lexicon of anonymous functions to use on vessel ves:
      //
      return LEXICON(
          "isSmall", {return ves:mass < 50.},
          "isBig", {return ves:mass > 150.},
          "circularEnough", {return ves:obt:eccentricity < 0.1.}
        ).
    }

    local that_ship_utils is make_vessel_utilities(Vessel("that ship")).

    if that_ship_utils["isSmall"]() {
      print "that ship is small".
    }

    if that_ship_utils["circularEnough"]() {
      print "that ship is circularized".
    }

Although kerboscript isn't *entirely* "object oriented", some kinds of
object-oriented ways of thinking can be simulated with techniques
like this.  Once you have the ability to treat a function as being
a piece of data, a lot of possibilities open up.
