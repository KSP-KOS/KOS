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

Strings are iterable. This scripts prints the string's characters one per line::

  SET str TO "abcde".

  FOR c IN str {
    PRINT c.
  }



CASE SENSITIVIY
~~~~~~~~~~~~~~~

NOTE: All string comparisons, substring matches, and searches, are
currently case **in** sensive, meaning that for example the letter
"A" and the letter "a" are indistinguishable.  There are future
plans to add mechanisms that will let you choose case-sensitivity
when you prefer.
	
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
          - boolean
          - True if the given string is contained within this string  
        * - :meth:`ENDSWITH(string)`
          - boolean
          - True if this string ends with the given string 
        * - :meth:`FIND(string)`
          - integer
          - Returns the index of the first occurrence of the given string in this string (starting from 0)
        * - :meth:`FINDAT(string, startAt)`
          - integer
          - Returns the index of the first occurrence of the given string in this string (starting from startAt)
        * - :meth:`FINDLAST(string)`
          - integer
          - Returns the index of the last occurrence of the given string in this string (starting from 0)
        * - :meth:`FINDLASTAT(string, startAt)`
          - integer
          - Returns the index of the last occurrence of the given string in this string (starting from startAt)
        * - :meth:`INDEXOF(string)`
          - integer
          - Alias for FIND(string)
        * - :meth:`INSERT(index, string)`
          - :struct:`String`
          - Returns a new string with the given string inserted at the given index into this string
        * - :meth:`LASTINDEXOF(string)`
          - integer
          - Alias for FINDLAST(string)
        * - :attr:`LENGTH`
          - integer
          - Number of characters in the string
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
          - boolean
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


.. method:: String:CONTAINS(string)

    :parameter string: :struct:`String` to look for
    :type: boolean
    
    True if the given string is contained within this string.

.. method:: String:ENDSWITH(string)

    :parameter string: :struct:`String` to look for
    :type: boolean

    True if this string ends with the given string.

.. method:: String:FIND(string)

    :parameter string: :struct:`String` to look for
    :type: :struct:`String`
    
    Returns the index of the first occurrence of the given string in this string (starting from 0).
    
.. method:: String:FINDAT(string, startAt)

    :parameter string: :struct:`String` to look for
    :parameter startAt: integer index to start searching at
    :type: :struct:`String`
    
    Returns the index of the first occurrence of the given string in this string (starting from startAt).

.. method:: String:FINDLAST(string)

    :parameter string: :struct:`String` to look for
    :type: :struct:`String`

    Returns the index of the last occurrence of the given string in this string (starting from 0)

.. method:: String:FINDLASTAT(string, startAt)

    :parameter string: :struct:`String` to look for
    :parameter startAt: integer index to start searching at
    :type: :struct:`String`

    Returns the index of the last occurrence of the given string in this string (starting from startAt)

.. method:: String:INDEXOF(string)

    Alias for FIND(string)

.. method:: String:INSERT(index, string)

    :parameter index: integer index to add the string at
    :parameter string: :struct:`String` to insert
    :type: :struct:`String`

    Returns a new string with the given string inserted at the given index into this string

.. method:: String:LASTINDEXOF(string)

    Alias for FINDLAST(string)

.. attribute:: String:LENGTH

    :type: integer
    :access: Get only

    Number of characters in the string

.. method:: String:PADLEFT(width)

    :parameter width: integer number of characters the resulting string will contain
    :type: :struct:`String`

    Returns a new right-aligned version of this string padded to the given width by spaces.

.. method:: String:PADRIGHT(width)

    :parameter width: integer number of characters the resulting string will contain
    :type: :struct:`String`

    Returns a new left-aligned version of this string padded to the given width by spaces.

.. method:: String:REMOVE(index,count)

    :parameter index: integer position of the string from which characters will be removed from the resulting string
    :parameter count: integer number of characters that will be removing from the resulting string
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
    :type: boolean

    True if this string starts with the given string .

.. method:: String:SUBSTRING(start,count)

    :parameter start: (integer) starting index (from zero)
    :parameter count: (integer) resulting length of returned :struct:`String`
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

    
Access to Individual Characters
-------------------------------

All string indexes start counting at zero. (The characters are numbered from 0 to N-1 rather than from 1 to N.)

``string[expression]``
    operator: access the character at position 'expression'. Any arbitrary complex expression may be used with this syntax, not just a number or variable name.
``FOR VAR IN STRING { ... }.``
    :ref:`A type of loop <flow>` in which var iterates over all the characters of the string from 0 to LENGTH-1.

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
