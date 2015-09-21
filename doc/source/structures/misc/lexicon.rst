.. _lexicon:

Lexicon
====

A :struct:`Lexicon` is an associative array where the keys and values can be of any type in kOS. You can create your own :struct:`Lexicon` variables and kOS itself can return them to you. 

Constructing a lexicon
-------------------

If you wish to make your own lexicon from scratch you can do so with the
LEXICON() built-in function.  

    // Make an empty lexicon with zero items in it:
    set mylexicon to lexicon().

The keys and the values of a lexicon can be any type you feel like, and do not
need to be of a homogeneous type.


Structure
---------

.. structure:: Lexicon

    .. lexicon-table:: Members
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :meth:`ADD(key,value)`
          - None
          - append an item to the lexicon
        * - :attr:`CASESENSITIVE`
          - bool
          - changes the behaviour of string based keys. Which are by default case insensitive. Setting this will clear the lexicon.
        * - :attr:`CASE`
          - bool
          - A synonym for `CASESENSITIVE`
        * - :meth:`CLEAR`
          - None
          - remove all items in the lexicon
        * - :meth:`COPY`
          - :struct:`Lexicon`
          - returns a copy of the contents of the lexicon
        * - :meth:`DUMP`
          - string
          - verbose dump of all contained elements
        * - :meth:`HASKEY(keyvalue)`
          - bool
          - does the lexicon have a key of the given value?
        * - :meth:`HASVALUE(value)`
          - bool
          - does the lexicon have a value of the given value?
        * - :attr:`KEYS`
          - :struct:`List`
          - gives a flat :struct:`List` of the keys in the lexicon
        * - :attr:`LENGTH`
          - integer
          - number of pairs in the lexicon
        * - :meth:`REMOVE(keyvalue)`
          - None
          - removes the pair with the given key

.. method:: Lexicon:ADD(key, value)

    :parameter key: (any type) a unique key
    :parameter value: (any type) a value that is to be associated to the key
    
    Adds an additional pair to the lexicon. 

.. method:: Lexicon:REMOVE(key)

    :parameter key: the keyvalue of the pair to be removed
    
    Remove the pair with the given key from the lexicon.
    
.. method:: Lexicon:CLEAR

    Removes all of the pairs from the lexicon. Making it empty.
    
.. attribute:: Lexicon:LENGTH

    :type: integer
    :access: Get only

    Returns the number of pairs in the lexicon.

.. method:: Lexicon:COPY

    :type: :struct:`Lexicon`
    :access: Get only

    Returns a new lexicon that contains the same set of pairs as this lexicon.

.. method:: Lexicon:HASKEY(key)

    :parameter key: (any type) 
    :return: boolean

    Returns true if the lexicon contains the provided key
    
.. method:: Lexicon:HASVALUE(key)

    :parameter key: (any type) 
    :return: boolean

    Returns true if the lexicon contains the provided value
    
.. attribute:: Lexicon:DUMP

    :type: string
    :access: Get only

    Returns a string containing a verbose dump of the lexicon's contents.

.. attribute:: Lexicon:KEYS

    :type: List
    :access: Get only

    Returns a string containing a verbose dump of the lexicon's contents.

Access to Individual Elements
-----------------------------

``lexicon[expression]``
    operator: another syntax to access the element at position 'expression'. Works for get or set. Any arbitrary complex expression may be used with this syntax, not just a number or variable name. 
``FOR VAR IN LEXICON.KEYS { ... }.``
    :ref:`A type of loop <flow>` in which var iterates over all the items of lexicon from item 0 to item LENGTH-1.

Examples::

    SET BAR TO LEXICON().       // Creates a new empty lexicon in BAR variable
    BAR:ADD("FIRST",10).        // Adds a new element to the lexicon with the key of "FIRST"
    BAR:ADD("SECOND",20).       // Adds a new element to the lexicon with the key of "SECOND"
    BAR:ADD("LAST",30).         // Adds a new element to the lexicon with the key of "LAST"

    PRINT BAR["FIRST"].            // Prints 10
    PRINT BAR["SECOND"].            // Prints 20
    PRINT BAR["LAST"].            // Prints 30

    SET FOO TO LEXICON().           // Creates a new empty lexicon in FOO variable
    FOO:ADD("ALTITUDE", ALTITUDE).  // Adds current altitude number to the lexicon
    FOO:ADD("ETA", ETA:APOAPSIS).   // Adds current seconds to apoapsis to the lexicon at the index "ETA"

    // As a reminder, at this point your lexicon, if you did all the above
    // steps in order, would look like this now:
    //
    //  FOO["ALTITUDE"] = 99999. // or whatever your altitude was when you added it.
    //  FOO["ETA"] = 99. // or whatever your ETA:APOAPSIS was when you added it.

    PRINT FOO:LENGTH.        // Prints 2
    PRINT FOO:LENGTH().      // Also prints 2.  LENGTH is a method that, because it takes zero arguments, can omit the parentheses.
    SET x TO "ALTITUDE". PRINT FOO[x].  // Prints the same thing as FOO["ALTITUDE"].

    FOO:REMOVE("ALTITUDE").              // Removes the element at "ALTITUDE" from the lexicon.  
