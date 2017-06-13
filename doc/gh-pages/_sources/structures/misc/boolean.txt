.. _boolean:
.. _bool:

Boolean
=======

.. structure::  Boolean

    .. list-table:: Members
        :header-rows: 1
        :widths: 1

        * - (Boolean values have no suffixes, other than the :struct:`Structure <structure>` suffixes all values have.)
        * -

A Boolean value is the smallest unit of data possible in a computer program.
It can contain one of exactly two values, either ``True``, or ``False``.

When setting a Boolean value, you can use the special keywords ``true`` or
``false`` to give it a value::

    set myVariable to true.
    set myVariable to false.

You can also set it equal to the value of any true/false expression,
for example::

    set x to 781.
    set itHas3Digits to (x >= 100 and x <= 999).
    print itHas3Digits.
    True.

If printed to the terminal, a Boolean value will return the string
``"True"`` or ``"False"``.

Operators
---------

Boolean expressions can use any of the following operators:

These all assume both `a` and `b` are Boolean values:

- `not a` returns true if a is false, or false if a is true.
- `a and b` returns true if and only if both a and b are true, else returns false.
- `a or b` returns false if either a is true, b is true, or both are true.
  Only returns false with both a and b are false.

The order of operations is as shown above.  First it performs `not`, then
`and`, then `or`.  Parentheses can be used to force the order of operations
to be the way you want, as usual.

Example
-------

Boolean values stored in a variable can be used in place of any conditional
check syntax anywhere.  Example::

    set should_stage to false.

    // set should_stage to true if the ship has no active engines right now:
    //
    set should_stage to (ship:maxthrust = 0).

    // set should_stage to true if any of the active engines are flamed out,
    // which should cover most "asparagus staging" strategies:
    //
    list engines in englist.
    for eng in englist {

      // note, eng:flameout is a Boolean value here, being used as the
      // conditional check of this if-statement:
      //
      if eng:flameout {
        set should_stage to true.
      }
    }

    // Note 'should_stage' is a Boolean value here, being used as the
    // conditional check of this if-statement:
    //
    if should_stage {
      stage.
    }
