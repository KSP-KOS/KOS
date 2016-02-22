Scalar
======

.. structure:: Scalar

    .. list-table:: **Members**
        :widths: 1
        :header-rows: 1
        
        * - (Scalar values have no suffixes other than the ones inherited from :struct:`Structure <structure>`.)

	* -
          
A Scalar value is the kind the system gives you whenever you are working
with a number of some sort which, unlike with a :struct:`Vector <vector>`,
does not have a pointing orientation in 3D space.

In other words, it's just a number.  A plain no-frills ordinary number.

Experienced programmers will be aware of the concept of there being
different kinds of number depending on what you want to do with it.
There's "integer" versus "floating point" versus "fixed point",
and theres single-precision, double-precision and so on.

kOS tries to be friendly to the new person just playing around with
simple programming without a lot of expertise, and to that end, the
difference between these types is abstracted away as much as possible.

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
a "NaN error", rather than the irrational number ``2i``.  NaN means
"Not a Number" and it means the system is incapable of storing the
correct answer.  Another example of where you will get a "NaN error"
would be if you attempted to perform ``arcsin(1.01)``, since there is
no such thing as the angle that gives a sine of 1.01.

The value must be a *rational number*
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

When you ask kOS to tell you `constant:pi`, you are technically not
getting the *actual* correct value.  Instead you are getting a rational
number approximation that is accurate to about 15 decimal places.  In
kOS, Scalar values cannot store irrational numbers.

The larger the magnitude, the less the precision
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
For example, while it *is* possible to store exactly the number ``99.001``,
it is not possible to store exactly the number ``999999999999999.001``, even
though both numbers are only precise up to the thousandths place.

If you attempt to ``set x to 999999999999999.001.`` and then ``print x.``,
you'll find that the value you get back has been rounded off a bit.

In a nutshell, what really matters is how many significant digits there are,
not how many places after the decimal point.  You can't have more than
roughly 15 significant decimal digits (it's not exactly 15 because of
differences between binary and decimal counting, but that gives you
a rough estimate).

