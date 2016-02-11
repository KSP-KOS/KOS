.. _range:

Range
=====

:struct:`Range` is a type that represents a sequence of numbers. The sequence can start and finish at any number, can be either descending or ascending and can skip numbers.

There are 3 ways of constructing a :struct:`Range`:

- :code:`RANGE(FROM, TO, STEP)`
  Will create a sequence of numbers that starts with `FROM` and contains all numbers `N + STEP` such that `N` belongs to the sequence and that are smaller than `TO`. For example
  :code:`RANGE(3,8,2)` will contain numbers 3, 5 and 7. If `FROM` > `TO` then the sequence will be descending. For example :code:`RANGE(2,-9,3)` will contain numbers 2, -1, -4 and -7.
  `STEP` should always be > 0.
- :code:`RANGE(FROM, TO)`
  Same as above but `STEP` is assumed to be 1.
- :code:`RANGE(TO)`
  Same as above but `FROM` is assumed to be 0.

Code examples
-------------

::

  FOR I IN RANGE(5) {
    PRINT I.
  }
  // will print numbers from 0 to 4

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
        * - :attr:`FROM`
          - scalar
          - initial element of the range
        * - :attr:`TO`
          - scalar
          - range limit
        * - :attr:`STEP`
          - scalar
          - step size

.. note::

    This type is serializable.


.. attribute:: Range:FROM

    :type: scalar
    :access: Get only

    Returns the initial element of the range.

.. attribute:: Range:TO

    :type: scalar
    :access: Get only

    Returns the range limit

.. attribute:: Range:STEP

    :type: scalar
    :access: Get only

    Returns the step size

