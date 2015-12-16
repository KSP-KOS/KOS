.. _delegates:

Delegates (function references)
===============================

.. versionadded:: 0.19.0
   The delegate feature described on this page did not exist
   prior to kOS 0.19.0.

.. contents:: Contents
    :local:
    :depth: 2
    
Overview
--------

There are times it would be useful to be able to store, not the
result of calling a function, but rather a reference to the function
itself without calling it yet.  Then you can use this value to call
the function later on.  Or it would be useful to choose one of several
functions you might want to call, store that choice in a variable, so
that you can call it multiple times later.

(If you are an experienced programmer, you have probably heard of this
feature or a similar feature under one of a number of names, depending
on which language you learned it in:  "Function pointers",
"Function references", "Callbacks", "Delegates", "Deferred execution",
etc.)

Kerboscript provides this feature with a built-in type called a 
:struct:`KOSDelegate`, which remembers the details needed
to be able to call a function later on.

.. note::
    It's important to know before going into this explanation, that the
    feature described here does not work on structure suffixes as of
    this release of kOS.  See the bottom of this page for more details.

.. _kosdelegate_atsign:

Syntax: @ symbol
~~~~~~~~~~~~~~~~

To obtain a *delegate* of a function in kOS, you place a single
at-sign (``@``) to the right of the function name, where the
parentheses and arguments would normally have gone, as shown
below::

    // example function:
    function myfunc { parameter a,b. return a+b. }

    // example delegate of that function.
    // Note the at-sign ('@'):
    set aaa to myfunc@.

When you do this, you are creating a variable of type
:struct:`KOSDelegate`, which can be passed around and
copied to other variables, sent as an argument to other
functions, and so on.

.. _kosdelegate_call:

Then you may call the function later on by using the ``:call``
suffix, and giving it the parameters that ``myfunc`` would normally
have expected, which might look something like this::

    print aaa:call(1, 2).

Here's the full example::

    function myfunc {
      parameter a,b.

      return a + b.
    }

    print myfunc(1, 2). // Prints the number 3, by calling myfunc now.
    set aaa to myfunc@. // You don't see any effect just yet from this.
    print aaa:call(1, 2).  // Now you see the number 3 printed,
                           // just like calling myfunc directly.

Omitting :CALL
~~~~~~~~~~~~~~

There are cases where you can call a KOSDelegate without the use of
the ``:call`` suffix, instead just using parentheses directly abutted
against the variable name.  This doesn't work in all cases due to
some syntax difficulties in the language, so its best to just be in
the habit of always explicitly using the ``:call`` suffix when working
with KOSDelegates.

Why the '@' sign?
~~~~~~~~~~~~~~~~~

In kerboscript, often when you mention a function's name and don't provide
any empty parentheses, if it's a function that takes zero arguments, it
ends up being called anyway.  Thus ``set x to myfunc.`` ends up doing
the same thing as ``set x to myfunc().``.  It ends up calling the 
function right now.  This is why you must append the ``@`` (at-sign)
symbol to the end of the function name to obtain a delegate of it.
It tells the compiler to suppress the normal automatic calling of the
function that would have occurred if you had left it bare.

Why?
----

There are several reasons this feature can be useful.  Some experienced
programmers will already know them, but here is an example of a useful
case as an illustration for people new to programming.  Let's say you
wanted to start from a list of numbers, and you wanted to create a
subset list of just those numbers which are negative.  You might write
code to do so like this::

    // Just a hodgepodge list of numbers to use as an example:
    local numlist is LIST(5, 6, 1, 49.1, 10, -2, 0, -12, 50, 0.3, 1.2, -1, 0).

    local result is list().
    for num in numlist {
      if num < 0 {
        result:add(num).
      }
    }
    // Now result is the subset list.

Okay, but then later let's say you want to do the same thing, but now you
want to get the subset which are integers (no fractional component after
the decimal point).  Then you might do this::
    
    local result is list().
    for num in numlist {
      if num = round(num,0) {
        result:add(num).
      }
    }
    // Now result is the subset list.

Okay, but then later let's say you want to do the same thing, but now you
want to get the subset which are even numbers::

    local result is list().
    for num in numlist {
      if mod(num,2) = 0 {
        result:add(num).
      }
    }
    // Now result is the subset list.

So you look at these three cases and think "well, gee, they're all pretty much
the same thing except for what I put in the 'if' check.  I should probably
combine them into one function."  You want to make one function that does
essentially this::

    function make_sublist {
      parameter
        input_list, // Full list to take a subset of.
        check.      // Condition to look for.

      local result is list().
      for num in input_list {
        if check...TO-DO, how do I do this?? {
          result:add(num).
        }
      }
      return result.
    }

But how do you call it telling it what condition to look for?  You're
essentially not trying to pass it a value, but you're trying to pass it
some code for it to run.

And that's what you would use a delegate for.  Here's the full example
that passes in a delegate where you tell it what kind of check you want
it to do by giving it a function you want it to call for the boolean check::

    function make_sublist {
      parameter
        input_list, // Full list to take a subset of.
        check_func. // pass in a delegate that expects 1 number parameter and returns 1 number.

      local result is list().
      for num in input_list {
        if check_func:call(num) {
          result:add(num).
        }
      }
      return result.
    }

    // Just a hodgepodge list of numbers to use as an example:
    local numlist is LIST(5, 6, 1, 49.1, 10, -2, 0, -12, 50, 0.3, 1.2, -1, 0).

    function is_neg { parameter n. return (n < 0). }
    function is_round { parameter n. return (num = round(num,0)). }
    function is_even { parameter n. return (mod(num,2) = 0). }

    print "A list of all the negatives:".
    print make_sublist(numlist, is_neg@). // note the '@' for a delegate of the function.
    
    print "A list of all the round numbers:".
    print make_sublist(numlist, is_round@). // note the '@' for a delegate of the function.

    print "A list of all the even numbers:".
    print make_sublist(numlist, is_even@). // note the '@' for a delegate of the function.

This technique can be chained together to form very powerful operations on
collections and enumerations of data.  You can start nesting several of
these types of function calls inside each other to perform a result, such
as "get the average mass of the subset of the subset of the parts on my
vessel that are fuel tanks that have oxidizer in them".  There is a style
of programming called
:ref:`Functional programming <https://en.wikipedia.org/wiki/Functional_programming>`_
in which you are meant to try to think this way about all possible problems
you are trying to solve.  While Kerboscript is mostly an
:ref:`imperative programming language <https://en.wikipedia.org/wiki/Imperative_programming>`_,
some limited concepts of functional programming style are possible through the use
of these delegates.

lib_enum in KSLib
-----------------

There is a library in the kslib that can be used to perform many data
set enumeration operations like the one described in the above section.
It was written to be released coinciding with the addition of this feature
to kerboscript.  In addition to being useful as a library, it also can
serve as a good list of example cases for how you can use this
"delegate" feature in your own code.  Please have a look at
:ref:`the lib_enum library in KSLib <https://github.com/KSP-KOS/KSLib/blob/master/doc/lib_enum.md>`_
to see what it has to offer.  It allows you to do things such as sorting
a LIST() based on whatever comparison criteria you like, finding the
minimum or maximum from a list, transforming all items in the list according
to a mapping rule, finding the index of the first hit in a list that 
matches given critiera, and so on.

Advanced topics
===============

.. _kosdelegate_bind:

Pre-binding arguments with :bind
--------------------------------

A :struct:`KOSDelegate` allows you to create another KOSDelegate that
has some of its parameters bound to some pre-set values, so you then
only need to supply the remaining, unbound values when you call it.
This allows you to implement certain types of functional programming
styles.  This is done using the ``:bind`` suffix of KOSDelegate.

Let's say you have a function you made that draws a vector arrow
from one ship to another, in a color of your choice, that looks like so::

    function draw_ship_to_ship {
      parameter
        ship1,
        ship2,
        drawColor.

      local vdraw is vecdraw().
      set vdraw:start to ship1:position.
      set vdraw:vec to ship2:position - ship1:position.
      set vdraw:color to drawColor.
      set vdraw:show to true.
      return vdraw.
    }

You realize that you'll be using this a lot with the same two ships
over and over.  You decide to create a variation of this function
that already has the two ships hardcoded to begin with, only
asking you for the final color parameter.

You can do that with KOSDelegates, using the ``:bind`` suffix of 
KOSDelegate, as follows::

    local draw_delegate is draw_ship_to_ship@.
    local draw_a_to_b is draw_delegate:bind(shipA, shipB).

    // Then later on you can call it with the first two arguments omitted
    // because you pre-loaded them with BIND:

    set greenvec to draw_a_to_b(green). // note, only passing 1 arg, the color.
    set tanvec to draw_a_to_b( rgb(0.7,0.6,0) ). // note, only passing 1 arg, the color.
    set whitevec to draw_a_to_b(white). // note, only passing 1 arg, the color.

Note that you can combine the two lines above that looked like this::

    local draw_delegate is draw_ship_to_ship@.
    local draw_a_to_b is draw_delegate:bind(shipA, shipB).

into just this::

    local draw_a_to_b is draw_a_to_b@:bind(shipA, shipB).

When you use the at-sign(``@``), you are returning an object of type
:struct:`KOSDelegate` that can be used in-line right in the expression,
as demonstrated above.

Currying
~~~~~~~~

It is possible to shave off exactly one parameter at a time in a chain
of these ``:bind`` calls.  You could do this, for example::

    // V() is the built-in function that makes a vector of x, y, and z
    // components.  You could bind the values one at a time as follows:
    local vecx is V@:bind(10). // vecx is now a KOSDelegate hardcoding x to 10 and taking just y and z args
    local vecxy is vecx:bind(5).  // vecxy is a KOSDelegate hardcoding x to 10 and y to 5, taking just the z arg
    local vecxyz is vecxy:bind(1).  // vecxyz is a KOSDelegate hardcoding x to 10, y to 5, and z to 1, taking no args.
    local vec is vecxyz:call(). // makes a V(10, 5, 1).

    // The above chain of bindings could have been chained together on one line like so:
    local vec is V@:bind(10):bind(5):bind(1):call().

The technique of transforming a function that takes many arguments into
a nested succession of functions that each only take one argument has a
name.  It's called :ref:`Currying <https://en.wikipedia.org/wiki/Currying>`_.
(It's named after mathemetician
:ref:`Haskell Curry <https://en.wikipedia.org/wiki/Haskell_Curry>`_
and has nothing to do with delicious spicy food).

(If anyone reading this is an experienced functional programmer and is thinking,
"But ``:bind`` as described here isn't currying",  yes, we are aware that this is 
correct.  The KOSDelegate suffix ``:bind`` is technically not a proper "curry" because
it is actually a
:ref:`partial function application <https://en.wikipedia.org/wiki/Partial_application>`_.
and thus doesn't *require* that you limit it to only one parameter at a time.)

Anonymous functions
-------------------

(If you are a beginner programmer, you can skip this paragraph.)

If you are an experienced programmer who knows of a concept
called "anonymous functions" in which you can create instant
delegates as just in-line expressions, you should know that this
feature is not supported in Kerboscript.  All KOSDelegates must
start as named functions you declare in the usual way.  The
anonymous function feature may be added in a future release,
or it might not, depending on how complex it becomes to add it
to the language syntax.  

Closures
--------

Kerboscript :struct:`KOSDelegates` of user functions do hold their
"closure" information inside themselves.  What on earth does that
mean?  If you haven't heard this term before, it essentially means
that the KOSDelegate "remembers" what the local variables were 
at the location where it was created.  It is possible for the
KOSDelegate you make of a function to access the local variables
that only that function is allowed to see, even if you call that
delegate from a "foreign" location where those variables wouldn't
normally be in :ref:`scope <scope>`.

Kinds of Delegate (no suffixes)
===============================

Under the hood, kOS handles several different kinds of 'functions' and
methods that aren't actually implemented the same way.  A ``KOSDelegate``
attempts to hide the details of these differences from the user, but
one difference in particular still stands out.  In kOS version 0.19.0,
you cannot reliably make a delegate of a suffix just yet.  (*This is
intended as a future feature though.  It's been put off because it
involves decisions that impact the future of the language and once made,
can't be changed easily.*)

- You **can** make a delegate of a :ref:`user function <user_functions>`
  implemented in kerboscript code.::
    
    function mysquarefunc { parameter a. return a*a. }
    set x to mysquarefunc@.
    set y to x:call(5). // y is now 25.

- You **can** make a delegate of a built-in function provided by kOS
  itself, provided it isn't a structure suffix.::

    set r to round@.
    set s to sqrt@.
    print "square root of 7, to the nearest 2 places is: " + r:call(s:call(7), 2).

- You **cannot** make a delegate of a suffix of a structure (*yet?*)
  in kerboscript.::

    //
    // WON'T WORK, WILL GIVE ERROR:
    //
    set altpos to latlng(10,20):altitudeposition@. // altitudeposition is a suffix of geoposition.
    print "altpos at altitude 1000 is " + altpos:call(1000).

  However, if you like you can make your own user function that is a
  wrapper around a structure suffix call, and make a delegate of THAT.

