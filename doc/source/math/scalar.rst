.. _scalar:

Scalar
======

.. structure:: Scalar

    .. list-table:: **Members**
        
        * - (Scalar values have no suffixes other than the ones inherited from :struct:`Structure <structure>`.)

A Scalar value is the kind the system gives you whenever you are working
with a number of some sort which, unlike with a :struct:`Vector <vector>`,
does not have a pointing orientation in 3D space.

In other words, it's just a number.  A plain, no-frills, ordinary number.

Experienced programmers will be aware of the concept of there being
different kinds of number depending on what you want to do with it.
There's "integer" versus "floating point" versus "fixed point",
and theres single-precision, double-precision and so on.

kOS tries to be friendly to the new person just playing around with
simple programming without a lot of expertise, and to that end, the
difference between these types is abstracted away as much as possible.

.. _scalar_syntax:

Scalar Syntax
-------------

Scalar numbers are allowed to be represented in any number of ways, both
in the source code and in strings passed in to the :attr:`String:TOSCALAR`
method.

Underscores are allowed as visual spacers provided you don't
lead with an underscore. The purpose of the underscores is to 
group numbers apart, similar to how some cultures use
a comma in numbers like '1,234,567' (and others use a
dot).  There is no enforcement of rules for where you can
and cannot put the underscores for grouping.  kerboscript
just strips them out and ignores them anyway::

    // These are all the same number, shown different ways:
    1234567
    1_234_567
    1_2__3456_7

One decimal point is allowed to show fractional parts, but
you must lead with a digit, even it's just to say "0."::

    1234
    12.34
    .1234 
    0.1234
    0.123_4 // underscore ignored.
    
You may use scientific notation (with an 'e' and an optional
sign, and a string of digits) to shift the decimal place::

    123.4e4   // = 1234000
    1.234e+4  // = 12340
    1.234e-14  // = 0.00000000000001234
    123_456.78e-4  // = 12.345678  (note, again, the ignored underscore)

Operators
---------

The following basic arithmetic operators are defined when both ``a`` and
``b`` are scalars:

.. list-table::
    :widths: 1 2
    
    * - ``a ^ b``
      - exponent: ``a`` to the power ``b``
    * - ``-a``
      - negative of ``a``
    * - ``a * b`` ``a / b``
      - muiltiply or divide two numbers
    * - ``a + b`` ``a - b``
      - add or subtract two numbers

The order of operations is in the order of the table listing above.
For example, multiplication and division happen before subtraction
and addition.

Scientific Notation
-------------------

You can specify a number using scientific notation using the letter ``e``,
as shown::

    set x to 1.23e5.
    print x.  // prints 123000
    set x to 1.23e-5.
    print x.  // prints 0.0000123

Limitations of Scalars
----------------------

The implementation of Scalars can currently only store values that fit
the following criteria:

The value is a *real number*
~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Or "What the heck does 'attempted to push NaN onto the stack' mean?".

kOS does not have a numeric type designed to deal with 
*imaginary numbers* or *complex numbers*.  Therefore, for
example, if you attempted to perform ``sqrt(-4)``, you would get
a "NaN error", rather than the imaginary number ``2i``.  "NaN" stands
for "Not a Number" and it means the system is incapable of storing the
correct answer.  Another example of where you will get a "NaN error"
is if you attempt to perform ``arcsin(1.01)``, since there is no such
thing as the angle that gives a sine of 1.01.

The value must be a *rational number*
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

When you ask kOS to tell you ``constant:pi``, you are technically not
getting the *actual* correct value.  Instead you are getting a rational
number approximation that is accurate to about 15 decimal places.  In
kOS, scalar values cannot store irrational numbers.

The larger the magnitude, the less the precision
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
For example, while it *is* possible to store exactly the number ``99.001``,
it is not possible to store exactly the number ``999999999999999.001``, even
though both numbers are only precise up to the thousandths place.

If you attempt to ``set x to 999999999999999.001.`` and then ``print x.``,
you'll find that the value you get back has been rounded off a bit.

In a nutshell, what really matters is how many significant digits there are,
not how many places after the decimal point.  You can't have more than
roughly 15 significant decimal digits. (It's not exactly 15 because of
differences between binary and decimal counting, but that gives you
a rough estimate.)
