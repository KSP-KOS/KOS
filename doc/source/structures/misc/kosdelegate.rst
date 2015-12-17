.. _kosdelegate:

KOSDelegate
===========

The structure `KOSDelegate` is what you get when you use the
:ref:`Delegate at-sign syntax <kosdelegate_atsign>`, as in
this example::

    function myfunc { print "hello, there". }

    local print_a_thing is myfunc@. // <--- Note the at-sign '@'.
    // print_a_thing is now a KOSDelegate of myfunc.

A KOSDelegate is a reference to the function that can be used to
call the function later elsewhere in the code.

The full explanation of the delegate feature
:ref:`is explained elsewhere <delegates>`.  This page just 
documents the members of the KOSDelegate structure for completeness.
The full explanation of how this structure works and what it's
for can be found on the :ref:`delegate page <delegates>`.

Structure
---------

.. structure:: KOSDelegate

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :meth:`CALL(varying arguments)`
          - same as the function this is a delegate for.
          - calls the function this delegate wraps.
        * - :meth:`BIND(varying arguments)`
          - another KOSDelegate
          - creates a new KOSDelegate with some arguments predefined.

.. method:: KOSDelegate:CALL(varying arguments)

    Calls the function this KOSDelegate is set up to call.

    The varying arguments you pass in to this are passed on to the
    function this KOSDelegate is calling.  The exact number of 
    arguments you pass should match the number the function expects,
    minus any that you have pre-set with the ``:BIND`` suffix.

    This is :ref:`further explained elsewhere <kosdelegate_call>`.

    Note that in some cases you can omit the use of the explicit suffix
    ``:CALL`` and just use parentheses abutted against the
    KOSDelegate variable itself to indicate that you wish to call the
    function.


.. method:: KOSDelegate:BIND(varying arguments)

    Creates a new KOSDelegate from the current one, which will call the 
    same function, but in which some or all of the parameters
    the function will be passed are pre-set.  The arguments you
    pass in will be bound to the leftmost parameters of the function.
    When using this new KOSDelegate to call the function, you pass in
    only the remaining arguments that were not designated in the 
    call to ``:BIND``.

    This is :ref:`further explained elsewhere <kosdelegate_bind>`.

