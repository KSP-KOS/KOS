.. _structure:

Structure:
==========

The root of all kOS data types.

.. contents::
    :local:
    :depth: 2

Overview
--------

All types of data that a kOS program can access, either via a variable, or
a suffix return value, or really just any expression's temporary result,
are now directly or indirectly derived from this one base type called just
``Structure``.

That means that there are a few generic suffixes that you should be able
to use on any value anywhere.  This page documents those suffixes.

This is true even of primitive value types such as ``1.0`` or ``false``
or ``42`` or ``"abc"``.  For example, you can do::

    print Mun:typename().
    Body   // <--- system prints this

    print ("hello"):typename().
    String // <--- system prints this

    print (12345.678):typename().
    Scalar // <--- system prints this

.. structure:: Structure

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`TOSTRING`
          - :struct:`String`
          - The string that gets shown on-screen when doing the PRINT command.

        * - :meth:`HASSUFFIX(name)`
          - :struct:`Boolean`
          - Test whether or not this value has a suffix with the given name.

        * - :attr:`SUFFIXNAMES`
          - :struct:`List <list>` of :struct:`strings <string>`
          - Gives a list of all the names of all the suffixes this thing has.

        * - :attr:`ISSERIALIZABLE`
          - :struct:`Boolean`
          - Is true if this type is one that works with :ref:`WRITEJSON <writejson>`

        * - :attr:`TYPENAME`
          - :struct:`String`
          - Gives a string for the name of the type of this object.

        * - :attr:`ISTYPE(name)`
          - :struct:`Boolean`
          - true if this value is of the given type name, or is derived from the given type name

        * - :attr:`INHERITANCE`
          - :struct:`String`
          - Gives a string describing the kOS type, and the kOS types it is inherited from.

.. attribute:: Structure:TOSTRING

    :type: :struct:`String`
    :access: Get only

    When issuing the command ``PRINT aaa.``, the variable ``aaa`` gets
    converted to a string and then the string is shown on the screen.
    This suffix universally lets you get that string version of any item,
    rather than showing it on the screen.

.. method:: Structure:HASSUFFIX(name)

    :parameter name: :struct:`String` name of the suffix being tested for
    :type: :struct:`Boolean`
    :access: Get only

    Given the name of a suffix, returns true if the object has a suffix
    by that name.  For example, if you have a variable that might be a
    :struct:`vessel <vessel>`, or might be a :struct:`Body <body>`,
    then this example::

        print thingy:hassuffix("maxthrust").

    would print ``True`` if ``thingy`` was a vessel of some sort, but
    ``False`` if ``thingy`` was a body, because there exists a maxthrust
    suffix for vessels but not for bodies.

    When searching for suffix names, the search is performed in a
    case-insensitive way.  Kerboscript cannot distinguish ":AAA"
    and ":aaa" as being two different suffixes.  In kerboscript,
    they'd be the same suffix.

    (Note that because a :struct:`Lexicon` can use a special
    :ref:`Lexicon suffix syntax <lexicon_suffix>`, it will also
    return true for suffix-usable keys when you call its
    HASSUFFIX method.)

.. attribute:: Structure:SUFFIXNAMES

    :type: :struct:`List <list>` of :struct:`strings <string>`
    :access: Get only

    Returns a list of all the string names of the suffixes that can
    be used by the thing you call it on.  As of this release, no
    information is shown about the parameters the suffix expects, or
    about the return value it gives.  All you see is the suffix names.

    If this object's type is inherited from other types (for example, a
    :struct:`Body <body>` is also a kind of :struct:`Orbitable <orbitable>`.)
    then what you see here contains the list of all the suffixes from the base
    type as well.  (Therefore the suffixes described here on this very page
    always appear in the list for any type.)
    
    Note, for some objects, like Vessels, this can be a rather long list.

    The list is returned sorted in alphabetical order.

    Example::

        set v1 to V(12,41,0.1). // v1 is a vector
        print v1:suffixnames.
        List of 14 items:
        [0] = DIRECTION
        [1] = HASSUFFIX
        [2] = ISSERIALIZABLE
        [3] = ISTYPE
        [4] = MAG
        [5] = NORMALIZED
        [6] = SQRMAGNITUDE
        [7] = SUFFIXNAMES
        [8] = TOSTRING
        [9] = TYPENAME
        [10] = VEC
        [11] = X
        [12] = Y
        [13] = Z

    (Note that because a :struct:`Lexicon` can use a special
    :ref:`Lexicon suffix syntax <lexicon_suffix>`, it will also
    include all of its suffix-usable keys when you call its
    SUFFIXNAMES method.)


.. attribute:: Structure:TYPENAME

    :type: :struct:`String`
    :access: Get only

    Gives the name of the type of the object, in kOS terminology.

    Type names correspond to the types mentioned throughout these
    documentation pages, at the tops of the tables that list
    suffixes.

    Examples::

        set x to 1.
        print x:typename
        Scalar

        set x to 1.1.
        print x:typename
        Scalar

        set x to ship:parts[2].
        print x:typename
        Part

        set x to Mun.
        print x:typename
        Body

    The kOS types described in these documentaion pages correspond
    one-to-one with underlying types in the C# code the implements
    them.  However they don't have the same name as the underlying 
    C# names.  This returns an abstraction of the C# name.  There
    are a few places in the C# code where an error message will 
    mention the C# type name instead of the kOS type name.  This is
    an issue that might be resolved in a later release.

.. attribute:: Structure:ISTYPE(name)

    :Parameter name: string name of the type being checked for
    :type: :struct:`Boolean`
    :access: Get only

    This is ``True`` if the value is of the type mentioned in the name, or
    if it is a type that is derived from the type mentioned in the name.
    Otherwise it is ``False``.

    Example::

        set x to SHIP.
        print x:istype("Vessel").
        True
        print x:istype("Orbitable").
        True
        print x:istype("Structure").
        True.
        print x:istype("Body").
        False
        print x:istype("Vector").
        False
        print x:istype("Some bogus type name that doesn't exist").
        False

    The type name is searched in a case-insensitive way.

.. attribute:: Structure:INHERITANCE

    :type: :struct:`String`
    :access: Get only

    Gives a string describing the typename of this value, and the
    typename of the type this value is inherited from, and the typename
    of the type that type is inherited from, etc all the way to 
    this root type of ``Structure`` that all values share.

    Example::

        set x to SHIP.
        print x:inheritance.
        Vessel derived from Orbitable derived from Structure

    (The kOS types described in that string are an abstraction of the
    underlying C# names in the mod's implementation, and a few of the
    C# types the mod uses to abstract a few things are skipped along
    the way, as they are types the script code can't see directly.)

.. attribute:: Structure:ISSERIALIZABLE

    :type: :struct:`Boolean`
    :access: Get only

    Not all types can be saved using the built-in serialization function
    :ref:`WRITEJSON <writejson>`.  For those that can, values of that
    type will return ``True`` for this suffix, otherwise it returns ``False``.

