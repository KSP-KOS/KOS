.. _kosdelegate:

KOSDelegate
===========

The structure `KOSDelegate` is what you get when you use the
:ref:`Delegate at-sign syntax <kosdelegate_atsign>`, as in
this example::

    function myfunc { print "hello, there". }

    local print_a_thing is myfunc@. // <--- Note the at-sign '@'.
    // print_a_thing is now a KOSDelegate of myfunc.

You also get a `KOSDelegate` when you use the
:ref:`Anonymous function <anonymous_functions>` syntax like so::

    set del1 to { print "hello, there". }.
    // del1 is now a KOSDelegate.

A KOSDelegate is a reference to the function that can be used to
call the function later elsewhere in the code.

Be aware, however, that it will not let you call the function
after the program is gone and you are back at the interactive
prompt again.  (See :attr:`ISDEAD`).

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
        * - :attr:`ISDEAD`
          - :struct:`Boolean`
          - True if the delegate refers to a program that's gone.

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

.. attribute:: KOSDelegate:ISDEAD

    :type: :struct:`Boolean`
    :access: Get only

    It is possible for a KOSDelegate to refer to some
    user code that no longer exists in memory because that
    program completed and exited.  If so, then ISDEAD will
    be true.
    
    This can happen because kOS lets global variables
    continue to live past the end of the program that
    made them.  So you can do something like this::

        function some_function {
            print "hello".
        }
        // NOTE: my_delegate is global so it keeps existing
        // after this program ends:
        set my_delegate to some_function@.

    If you run that program and get back to the interactive
    terminal prompt, then my_delegate is still a KOSDelegate,
    but now it refers to some code that is gone.  The body
    of some_function isn't there anymore.

    If you attempt to call my_delegate() at this point from
    the interpreter, it will complain with an error message
    because it knows the function it's trying to call isn't
    there.

.. _donothing:

DONOTHING (NODELEGATE)
----------------------

.. structure:: NoDelegate

    ======== ======== ===================
    Suffix   Type     Description
    ======== ======== ===================
    Every suffix of :struct:`KOSDelegate`
    -------------------------------------
    ======== ======== ===================

.. global:: DONOTHING

    There is a special keyword ``DONOTHING`` that refers to a special
    kind of :struct:`KosDelegate` called a :struct:`NoDelegate`.

    The type string returned by ``DONOTHING:TYPENAME`` is ``"NoDelegate"``.
    Otherwise an instance of :struct:`NoDelegate` has the same suffixes as one
    of :struct:`KOSDelegate`, although you're not usually
    expected to ever use them, except maybe ``TYPENAME`` to discover
    that it is a :struct:`NoDelegate`.

    ``DONOTHING`` is used when you're in a situation where you had
    previously assigned a :struct:`KosDelegate` to some callback hook
    the kOS system provides, but now you want the kOS system to stop
    calling it.  To do so, you assign that callback hook to the value
    ``DONOTHING``.

    ``DONOTHING`` is similar to making a :struct:`KosDelegate` that
    consists of just ``{return.}``.  If you attempt to call it from
    your own code, that's how it will behave.  But the one extra
    feature it has is that it allows kOS to understand your intent
    that you wish to disable a callback hook.  kOS can detect when
    the ``KosDelegate`` you assign to something happens to be the
    ``DONOTHING`` delegate.  When it is, kOS knows to not even
    bother calling the delegate at all anymore.
