.. _string:

String
======

A :struct:`String` is an immutable sequence of characters in kOS.

Creating strings
-------------------

Unlike other structures, strings are created with a special syntax::

    // Create a new string
    SET s TO "Hello, Strings!".


Strings are immutable. This means, once a string has been created, it
can not be directly modified. However, new strings can be created out
of existing strings. For example::

    // Create a new string with "Hello" replaced with "Goodbye"
    SET s TO "Hello, Strings!".
    SET t TO s:REPLACE("Hello", "Goodbye").

ACCESSING INDIVIDUAL CHARACTERS
-------------------------------

There's two main ways to access the individual characters
of a string - using an iterator or using index numbers:

Using an Iterator (FOR)
~~~~~~~~~~~~~~~~~~~~~~~

Strings can be treated a little bit like iterable lists
of characters. This allows them to be used in FOR loops
as in the example below::

  LOCAL str is "abcde".

  FOR c IN str {
    PRINT c.  // prints "a" the first time, then "b", etc.
  }

The reason you can use Strings with the FOR loop like this is
because you can obatain an :struct:`Iterator` of a string with the
:attr:`ITERATOR` suffix mentioned below.  (Any type that
implements the ITERATOR suffix can do this.)

Using an Index ( [i] )
~~~~~~~~~~~~~~~~~~~~~~

Strings can also be treated a little bit like lists in that
they allow you to use the square-brackets operator `[`..`]`
to choose one character by its index number (numbers start
counting at zero).  Here's an example that does the same thing
as the FOR loop above, but using index notation::

  LOCAL str is "abcde".
  local index is 0.
  until index = str:LENGTH {
    print str[index].
    set index to index + 1.
  }

Be aware that despite being able to read the characters this way,
you cannot set them this way.  The following will give
an error::

  LOCAL str is "abcde".

  // The following line gives an error because you can't
  // change the characters inside a string:
  set str[0] to "X".

Boolean Operators
-----------------

Equality
~~~~~~~~

Using the ``=`` and ``<>`` operators, two strings are equal
if and only if they are the same length and have letters that differ
only in capitalization (``a`` and ``A`` are considered the same letter
for this test).

Ordering
~~~~~~~~

Using the ``<``, ``>``, ``<=``, and ``>=`` operators, one
string is considered to be less than the other if it is alphabetically
sooner according to the ordering of its Unicode mapping, with the
exception that capitalization is ingored (``a`` and ``A`` are
considered the same letter).  Starting from the lefthand side of the
two strings, the characters are compered one at a time until the first
difference is found, and that first difference decides the ordering.
If one of the strings is shorter length than the other, and the characters
are all equal up until one of the two strings runs out of characters,
then the shorter string will be considered "less than" the longer one.

Mixtures of strings and non-strings
:::::::::::::::::::::::::::::::::::

If you attempt to compare two things only one of which is a string
and the other is not, then the non-string will be converted into a
string first, (Giving the same string as its :TOSTRING suffix would
give), and the two will be compared as strings.  Example::

    print (1234 < 99).    // prints "False"
    print ("1234" < 99).  // prints "True"

In the first example, both sides of the ``<`` operator are
:ref:`scalars <scalar>`, so the comparison is done numerically,
and 1234 is much bigger than 99.

In the second example, one side of the ``<`` operator is a
string, so the other side is converted from the :ref:`scalar <scalar>`
``99`` into the :ref:`string <string>` ``"99"`` to perform the
comparison, and then the string comparison looks one character at
a time and notices that "1" is less than "9" and calls "1234" the
lesser value.

CASE SENSITIVIY
~~~~~~~~~~~~~~~

NOTE: All string comparisons for equality and ordering, all substring
matches, all pattern matches, and all string searches, are currently
case **in** sensive, meaning that for example the letter "A" and the
letter "a" are indistinguishable.  There are future plans to add
mechanisms that will let you choose case-sensitivity when you prefer.

At the moment the only way to force a case-sensitive comparison is
to look at the characters one at a time and obtain their numerical
ordinal Unicode value with the :func:`unchar(a)` function.

Structure
---------

.. structure:: String

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :meth:`CONTAINS(string)`
          - :struct:`Boolean`
          - True if the given string is contained within this string
        * - :meth:`ENDSWITH(string)`
          - :struct:`Boolean`
          - True if this string ends with the given string
        * - :meth:`FIND(string)`
          - :struct:`Scalar`
          - Returns the index of the first occurrence of the given string in this string (starting from 0)
        * - :meth:`FINDAT(string, startAt)`
          - :struct:`Scalar`
          - Returns the index of the first occurrence of the given string in this string (starting from startAt)
        * - :meth:`FINDLAST(string)`
          - :struct:`Scalar`
          - Returns the index of the last occurrence of the given string in this string (starting from 0)
        * - :meth:`FINDLASTAT(string, startAt)`
          - :struct:`Scalar`
          - Returns the index of the last occurrence of the given string in this string (starting from startAt)
        * - :meth:`INDEXOF(string)`
          - :struct:`Scalar`
          - Alias for FIND(string)
        * - :meth:`INSERT(index, string)`
          - :struct:`String`
          - Returns a new string with the given string inserted at the given index into this string
        * - :attr:`ITERATOR`
          - :struct:`Iterator`
          - generates an iterator object the elements
        * - :meth:`LASTINDEXOF(string)`
          - :struct:`Scalar`
          - Alias for FINDLAST(string)
        * - :attr:`LENGTH`
          - :struct:`Scalar`
          - Number of characters in the string
        * - :meth:`MATCHESPATTERN(pattern)`
          - :struct:`Boolean`
          - Tests whether the string matches the given regex pattern.
        * - :meth:`PADLEFT(width)`
          - :struct:`String`
          - Returns a new right-aligned version of this string padded to the given width by spaces
        * - :meth:`PADRIGHT(width)`
          - :struct:`String`
          - Returns a new left-aligned version of this string padded to the given width by spaces
        * - :meth:`REMOVE(index,count)`
          - :struct:`String`
          - Returns a new string out of this string with the given count of characters removed starting at the given index
        * - :meth:`REPLACE(oldString, newString)`
          - :struct:`String`
          - Returns a new string out of this string with any occurrences of oldString replaced with newString
        * - :meth:`SPLIT(separator)`
          - :struct:`String`
          - Breaks this string up into a list of smaller strings on each occurrence of the given separator
        * - :meth:`STARTSWITH(string)`
          - :struct:`Boolean`
          - True if this string starts with the given string
        * - :meth:`SUBSTRING(start, count)`
          - :struct:`String`
          - Returns a new string with the given count of characters from this string starting from the given start position
        * - :attr:`TOLOWER`
          - :struct:`String`
          - Returns a new string with all characters in this string replaced with their lower case versions
        * - :attr:`TOUPPER`
          - :struct:`String`
          - Returns a new string with all characters in this string replaced with their upper case versions
        * - :attr:`TRIM`
          - :struct:`String`
          - returns a new string with no leading or trailing whitespace
        * - :attr:`TRIMEND`
          - :struct:`String`
          - returns a new string with no trailing whitespace
        * - :attr:`TRIMSTART`
          - :struct:`String`
          - returns a new string with no leading whitespace
        * - :meth:`TONUMBER(defaultIfError)`
          - :struct:`Scalar`
          - Parse the string into a number that can be used for mathematics.
        * - :meth:`TOSCALAR(defaultIfError)`
          - :struct:`Scalar`
          - Alias for :meth:`TONUMBER`


.. method:: String:CONTAINS(string)

    :parameter string: :struct:`String` to look for
    :type: :struct:`Boolean`

    True if the given string is contained within this string.

.. method:: String:ENDSWITH(string)

    :parameter string: :struct:`String` to look for
    :type: :struct:`Boolean`

    True if this string ends with the given string.

.. method:: String:FIND(string)

    :parameter string: :struct:`String` to look for
    :type: :struct:`String`

    Returns the index of the first occurrence of the given string in this string (starting from 0).

    If the ``string`` passed in is not found, this returns -1.

    If the ``string`` passed in is the empty string ``""``, this always claims to have
    successfully "found" that empty string at the start of the search.

.. method:: String:FINDAT(string, startAt)

    :parameter string: :struct:`String` to look for
    :parameter startAt: :struct:`Scalar` (integer) index to start searching at
    :type: :struct:`String`

    Returns the index of the first occurrence of the given string in this string (starting from startAt).

    If the ``string`` passed in is not found, this returns -1.

    If the ``string`` passed in is the empty string ``""``, this always claims to have
    successfully "found" that empty string at the start of the search.

.. method:: String:FINDLAST(string)

    :parameter string: :struct:`String` to look for
    :type: :struct:`String`

    Returns the index of the last occurrence of the given string in this string (starting from 0)

    If the ``string`` passed in is not found, this returns -1.

    If the ``string`` passed in is the empty string ``""``, this always claims to have
    successfully "found" that empty string at the beginning of the search.

.. method:: String:FINDLASTAT(string, startAt)

    :parameter string: :struct:`String` to look for
    :parameter startAt: :struct:`Scalar` (integer) index to start searching at
    :type: :struct:`String`

    Returns the index of the last occurrence of the given string in this string (starting from startAt)

    If the ``string`` passed in is not found, this returns -1.

    If the ``string`` passed in is the empty string ``""``, this always claims to have
    successfully "found" that empty string at the beginning of the search.

.. method:: String:INDEXOF(string)

    Alias for FIND(string)

.. method:: String:INSERT(index, string)

    :parameter index: :struct:`Scalar` (integer) index to add the string at
    :parameter string: :struct:`String` to insert
    :type: :struct:`String`

    Returns a new string with the given string inserted at the given index into this string

.. attribute:: String:ITERATOR

    :type: :struct:`Iterator`
    :access: Get only

    An alternate means of iterating over a string's characters
    (See: :struct:`Iterator`).

    For most programs you won't have to use this directly.  It's just
    what enables you to use a string with a FOR loop to get access
    to its characters one at a time.

.. method:: String:LASTINDEXOF(string)

    Alias for FINDLAST(string)

.. attribute:: String:LENGTH

    :type: :struct:`Scalar` (integer)
    :access: Get only

    Number of characters in the string

.. method:: String:MATCHESPATTERN(pattern)

    :parameter pattern: :struct:`String` pattern to be matched against the string
    :type: :struct:`Boolean`

    True if the string matches the given pattern (regular expression). The match is not anchored to neither the start nor the end of the string.
    That means that pattern ``"foo"`` will match ``"foobar"``, ``"barfoo"`` and ``"barfoobar"`` too. If you want to match from the start,
    you have to explicitly specify the start of the string in the pattern, i.e. for example to match strings starting with ``"foo"`` you need to
    use the pattern ``"^foo"`` (or equivalently ``"^foo.*"`` or even ``"^foo.*$"``).

    Regular expressions are beyond the scope of this documentation. For reference see `Regular Expression Language - Quick Reference <https://msdn.microsoft.com/en-us/library/az24scfc.aspx>`__\ .

.. method:: String:PADLEFT(width)

    :parameter width: :struct:`Scalar` (integer) number of characters the resulting string will contain
    :type: :struct:`String`

    Returns a new right-aligned version of this string padded to the given width by spaces.

.. method:: String:PADRIGHT(width)

    :parameter width: :struct:`Scalar` (integer) number of characters the resulting string will contain
    :type: :struct:`String`

    Returns a new left-aligned version of this string padded to the given width by spaces.

.. method:: String:REMOVE(index,count)

    :parameter index: :struct:`Scalar` (integer) position of the string from which characters will be removed from the resulting string
    :parameter count: :struct:`Scalar` (integer) number of characters that will be removing from the resulting string
    :type: :struct:`String`

    Returns a new string out of this string with the given count of characters removed starting at the given index.

.. method:: String:REPLACE(oldString,newString)

    :parameter oldString: :struct:`String` to search for
    :parameter newString: :struct:`String` that all occurances of oldString will be replaced with
    :type: :struct:`String`

    Returns a new string out of this string with any occurrences of oldString replaced with newString.

.. method:: String:SPLIT(separator)

    :parameter separator: :struct:`String` delimiter on which this string will be split
    :return: :struct:`List`

    Breaks this string up into a list of smaller strings on each occurrence of the given separator. This will return a
    list of strings, none of which will contain the separator character(s).

.. method:: String:STARTSWITH(string)

    :parameter string: :struct:`String` to look for
    :type: :struct:`Boolean`

    True if this string starts with the given string .

.. method:: String:SUBSTRING(start,count)

    :parameter start: :struct:`Scalar` (integer) starting index (from zero)
    :parameter count: :struct:`Scalar` (integer) resulting length of returned :struct:`String`
    :return: :struct:`String`

    Returns a new string with the given count of characters from this string starting from the given start position.

.. attribute:: String:TOLOWER

    :type: :struct:`String`
    :access: Get only

    Returns a new string with all characters in this string replaced with their lower case versions

.. attribute:: String:TOUPPER

    :type: :struct:`String`
    :access: Get only

    Returns a new string with all characters in this string replaced with their upper case versions

.. attribute:: String:TRIM

    :type: :struct:`String`
    :access: Get only

    returns a new string with no leading or trailing whitespace

.. attribute:: String:TRIMEND

    :type: :struct:`String`
    :access: Get only

    returns a new string with no trailing whitespace

.. attribute:: String:TRIMSTART

    :type: :struct:`String`
    :access: Get only

    returns a new string with no leading whitespace

.. method:: String:TONUMBER(defaultIfError)

    :parameter defaultIfError: (optional argument) :struct:`Scalar` to return as a default value if the string format is in error.
    :return: :struct:`Scalar`

    Returns the numeric version of the string, as a number that can be used
    for mathematics or anywhere a :struct:`Scalar` is expected.  If the
    string is not in a format that kOS is able to convert into a number, then
    the value ``defaultIfError`` is returned instead.  You can use this to
    either select a sane default, or to deliberately select a value you
    never expect to get in normal circumstances so you can use it as a
    test to see if the string was formatted well.

    The argument ``defaultIfError`` is optional.  If it is left off, then
    when there is a problem in the format of the string, you will get
    an error that stops the script instead of returning a value.

    The valid understood format allows an optional leading sign,
    a decimal point with fractional part, and scientific notation
    using "e" as in "1.23e3" for "1230" or "1.23e-3" for "0.00123".

    You may also include optional underscores in the string to
    help space groups of digits, and they will be ignored.
    (For example you may write "one thousand" as "1_000" instead
    of as "1000" if you like".)

    Example - using with math::

        set str to "16.8".
        print "half of " + str + " is " + str:tonumber() / 2.
        half of 16.8 is 8.4

    Example - checking for bad values by using defaultIfError::

        set str to "Garbage 123 that is not a proper number".
        set val to str:tonumber(-9999).
        if val = -9999 {
          print "that string isn't a number".
        } else {
          print "the string is a number: " + val.
        }

    Example - not setting a default value can throw an error::

       set str to "Garbage".
       set val to str:tonumber().  // the script dies with error here.
       print "value is " + val. // the script never gets this far.

.. method:: String:TOSCALAR(defaultIfError)

    Alias for :meth:`String:TONUMBER(defaultIfError)`

Access to Individual Characters
-------------------------------

All string indexes start counting at zero. (The characters are numbered from 0 to N-1 rather than from 1 to N.)

``string[expression]``

  - operator: access the character at position 'expression'. Any arbitrary complex expression may be used with this syntax, not just a number or variable name.

``FOR VAR IN STRING { ... }.``

  - :ref:`A type of loop <flow>` in which var iterates over all the characters of the string from 0 to LENGTH-1.

Examples::

                                                                    // CORRECT OUTPUTS
    SET s TO "Hello, Strings!".                                     // ---------------
    PRINT "Original String:               " + s.                    // Hello, Strings!
    PRINT "string[7]:                     " + s[7].                 // S
    PRINT "LENGTH:                        " + s:LENGTH.             // 15
    PRINT "SUBSTRING(7, 6):               " + s:SUBSTRING(7, 6).    // String
    PRINT "CONTAINS(''ring''):            " + s:CONTAINS("ring").   // True
    PRINT "CONTAINS(''bling''):           " + s:CONTAINS("bling").  // False
    PRINT "ENDSWITH(''ings!''):           " + s:ENDSWITH("ings!").  // True
    PRINT "ENDSWITH(''outs!''):           " + s:ENDSWITH("outs").   // False
    PRINT "FIND(''l''):                   " + s:FIND("l").          // 2
    PRINT "FINDLAST(''l''):               " + s:FINDLAST("l").      // 3
    PRINT "FINDAT(''l'', 0):              " + s:FINDAT("l", 0).     // 2
    PRINT "FINDAT(''l'', 3):              " + s:FINDAT("l", 3).     // 3
    PRINT "FINDLASTAT(''l'', 9):          " + s:FINDLASTAT("l", 9). // 3
    PRINT "FINDLASTAT(''l'', 2):          " + s:FINDLASTAT("l", 2). // 2
    PRINT "INSERT(7, ''Big ''):           " + s:INSERT(7, "Big ").  // Hello, Big Strings!

    PRINT " ".
    PRINT "                               |------ 18 ------|".
    PRINT "PADLEFT(18):                   " + s:PADLEFT(18).        //    Hello, Strings!
    PRINT "PADRIGHT(18):                  " + s:PADRIGHT(18).       // Hello, Strings!
    PRINT " ".

    PRINT "REMOVE(1, 3):                  " + s:REMOVE(1, 3).               // Ho, Strings!
    PRINT "REPLACE(''Hell'', ''Heaven''): " + s:REPLACE("Hell", "Heaven").  // Heaveno, Strings!
    PRINT "STARTSWITH(''Hell''):          " + s:STARTSWITH("Hell").         // True
    PRINT "STARTSWITH(''Heaven''):        " + s:STARTSWITH("Heaven").       // False
    PRINT "TOUPPER:                       " + s:TOUPPER().                  // HELLO, STRINGS!
    PRINT "TOLOWER:                       " + s:TOLOWER().                  // hello, strings!

    PRINT " ".
    PRINT "''  Hello!  '':TRIM():         " + "  Hello!  ":TRIM().          // Hello!
    PRINT "''  Hello!  '':TRIMSTART():    " + "  Hello!  ":TRIMSTART().     // Hello!
    PRINT "''  Hello!  '':TRIMEND():      " + "  Hello!  ":TRIMEND().       //   Hello!

    PRINT " ".
    PRINT "Chained: " + "Hello!":SUBSTRING(0, 4):TOUPPER():REPLACE("ELL", "ELEPHANT").  // HELEPHANT
