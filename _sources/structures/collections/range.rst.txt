.. _range:

Range
=====

:struct:`Range` is a type that represents a sequence of scalar whole numbers. The sequence can start and finish at any whole number, can be either descending or ascending and can skip numbers.

.. note::

    This is one of the few places in kOS where there is a distinction between
    decimal (floating point, or fractional) scalar numbers and whole (integer,
    or round) scalar numbers.  Using a decimal scalar will not throw an error,
    however it may give unexpected results due to rounding.

There are 3 ways of constructing a :struct:`Range`:

- :code:`RANGE(START, STOP, STEP)`

  Will create a sequence of numbers that starts counting with `START`,
  and stops counting just before *but not including* `STOP`, counting
  by increments of size `STEP`.  In formal mathematics terms, the bounds are
  [`START`,`STOP`), rather than [`START`,`STOP`].

  :code:`RANGE(3,8,1)` will contain numbers 3, 4, 5, 6, and 7.

  :code:`RANGE(3,8,2)` will contain numbers 3, 5 and 7.

  *Will count backward automatically if need be*: If `START` > `STOP` then
  the sequence will be descending.  `STEP` should always be > 0 even when
  the sequence counts backward like this.
  
  :code:`RANGE(2,-9,3)` will contain numbers 2, -1, -4 and -7.

- :code:`RANGE(START, STOP)`

  Same as above but `STEP` is assumed to be 1.

- :code:`RANGE(STOP)`

  Same as above but `STEP` is assumed to be 1, and `START` is assumed to
  be 0.

Code examples
-------------

::

  FOR I IN RANGE(5) {
    PRINT I.
  }
  // will print numbers 0,1,2,3,4

  FOR I IN RANGE(2, 5) {
    PRINT I*I.
  }
  // will print 4, 9 and 16

Structure
---------

.. structure:: Range

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - All suffixes of :struct:`Enumerable`
          -
          - :struct:`Range` objects are a type of :struct:`Enumerable`
        * - :attr:`START`
          - :struct:`Scalar`
          - initial element of the range
        * - :attr:`STOP`
          - :struct:`Scalar`
          - range limit
        * - :attr:`STEP`
          - :struct:`Scalar`
          - step size

.. note::

    This type is serializable.


.. attribute:: Range:START

    :type: :struct:`Scalar`
    :access: Get only

    Returns the initial element of the range.  Must be a round number.

.. attribute:: Range:STOP

    :type: :struct:`Scalar`
    :access: Get only

    Returns the range limit.  Must be a round number.

.. attribute:: Range:STEP

    :type: :struct:`Scalar`
    :access: Get only

    Returns the step size.  Must be a round number.
