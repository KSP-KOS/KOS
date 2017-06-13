.. _lexicon:

Lexicon
=======

A :struct:`Lexicon` is
`an associative array <https://en.wikipedia.org/wiki/Associative_array>`_,
and is similar to the :ref:`LIST type <list>`.  If you are an experienced
programmer who already knows what "associative array" means, you can
probably skip this section and go to the next part of the page further
down, otherwise read on:

In a normal array, or in kerboscript's :ref:`LIST type <list>` you
specify which item in the list you want by giving its integer position
in the list.

But in a :struct:`Lexicon`, you store pairs of keys and values, where
the keys can be any type of thing you like, not just integers, and
then you specify which item you want by using that key's value.

Here's a small example::

    set arr to lexicon().
    arr:add( "ABC", 1234.1 ).
    arr:add( "Carmine", 4.1 ).
    print arr["ABC"]. // prints 1234.1
    print arr["Carmine"]. // prints 4.1

Notice how it looks a lot like a list, but the values in the
index brackets are strings instead of integers.  This is the
most common use of a lexicon, to use strings as the key index
values (and in fact why it's called "lexicon").  However you can 
really use any value you feel like for the keys - strings, RGB colors,
numbers, etc.


Lexicons are case-insensitive
-----------------------------

One important difference between Lexicons in kerboscript and associative
arrays in most other languages is that kerboscript Lexicons use
case-insensitive keys by default (when the keys are strings).  This
behaviour can be changed with the ``:CASESENSITIVE`` flag described below.

Constructing a lexicon
----------------------

If you wish to make your own lexicon from scratch you can do so with the
LEXICON() or LEX() built-in function::

    // Make an empty lexicon with zero items in it:
    set mylexicon to lexicon().

If ``LEXICON()`` is given arguments then they are interpreted as alternating
keys and values::

    set mylexicon to lexicon("key1", "value1", "key2", "value2").

Will have the same effect as::

    set mylexicon to lexicon().
    mylexicon["key1"] = "value1".
    mylexicon["key2"] = "value2".

Obviously when this syntax is used an even number of arguments is expected.

You can also pass any enumerable to ``LEXICON()``. Its elements will be
interpreted as alternating keys and values just like above. The following will have
the same effect as the previous code fragment::

    set mylist to list("key1", "value1", "key2", "value2").
    set mylexicon to lexicon(mylist).

The keys and the values of a lexicon can be any type you feel like, and do not
need to be of a homogeneous type.

Structure
---------

.. structure:: Lexicon

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :meth:`ADD(key,value)`
          - None
          - append an item to the lexicon
        * - :attr:`CASESENSITIVE`
          - :ref:`boolean <boolean>`
          - changes the behaviour of string based keys. Which are by default case insensitive. Setting this will clear the lexicon.
        * - :attr:`CASE`
          - :ref:`boolean <boolean>`
          - A synonym for `CASESENSITIVE`
        * - :meth:`CLEAR`
          - None
          - remove all items in the lexicon
        * - :meth:`COPY`
          - :struct:`Lexicon`
          - returns a (shallow) copy of the contents of the lexicon
        * - :meth:`DUMP`
          - :ref:`string <string>`
          - verbose dump of all contained elements
        * - :meth:`HASKEY(keyvalue)`
          - :ref:`boolean <boolean>`
          - does the lexicon have a key of the given value?
        * - :meth:`HASVALUE(value)`
          - :ref:`boolean <boolean>`
          - does the lexicon have a value of the given value?
        * - :attr:`KEYS`
          - :struct:`List`
          - gives a flat :struct:`List` of the keys in the lexicon
        * - :attr:`VALUES`
          - :struct:`List`
          - gives a flat :struct:`List` of the values in the lexicon
        * - :attr:`LENGTH`
          - :ref:`scalar <scalar>`
          - number of pairs in the lexicon
        * - :meth:`REMOVE(keyvalue)`
          - None
          - removes the pair with the given key
.. note::

    This type is serializable.

.. method:: Lexicon:ADD(key, value)

    :parameter key: (any type) a unique key
    :parameter value: (any type) a value that is to be associated to the key
    
    Adds an additional pair to the lexicon. 

.. attribute:: Lexicon:CASESENSITIVE

    :type: :ref:`Boolean <boolean>`
    :access: Get or Set
    
    The case sensitivity behaviour of the lexicon when the keys are strings.
    By default, all kerboscript lexicons use case-insensitive keys, at
    least for those keys that are string types, meaning that
    mylexicon["AAA"] means the same exact thing as mylexicon["aaa"].  If
    you do not want this behaviour, and instead want the key "AAA" to be
    different from the key "aaa", you can set this value to true.

    Be aware, however, that if you change this, it has the side effect
    of *clearing out* the entire contents of the lexicon.  This is done so
    as to avoid any potential clashes when the rules about what constitutes
    a duplicate key changed after the lexicon was already populated.
    Therefore you should probably only set this on a brand new lexicon,
    right after you've created it, and never change it after that.

.. attribute:: Lexicon:CASE

    :type: :ref:`Boolean <boolean>`
    :access: Get or Set
     
    Synonym for CASESENSITIVE (see above).

.. method:: Lexicon:REMOVE(key)

    :parameter key: the keyvalue of the pair to be removed
    
    Remove the pair with the given key from the lexicon.
    
.. method:: Lexicon:CLEAR

    Removes all of the pairs from the lexicon. Making it empty.
    
.. attribute:: Lexicon:LENGTH

    :type: :ref:`scalar <scalar>`
    :access: Get only

    Returns the number of pairs in the lexicon.

.. method:: Lexicon:COPY

    :type: :struct:`Lexicon`
    :access: Get only

    Returns a new lexicon that contains the same set of pairs as this lexicon.
    Note that this is a "shallow" copy, meaning that if there is a value in
    the list that refers to, for example, another Lexicon, or a Vessel, or
    a Part, the new copy will still be referring to the same object as the
    original copy in that value.

.. method:: Lexicon:HASKEY(key)

    :parameter key: (any type) 
    :return: :ref:`boolean <boolean>`

    Returns true if the lexicon contains the provided key
    
.. method:: Lexicon:HASVALUE(key)

    :parameter key: (any type) 
    :return: :ref:`boolean <boolean>`

    Returns true if the lexicon contains the provided value
    
.. attribute:: Lexicon:DUMP

    :type: :ref:`string <string>`
    :access: Get only

    Returns a string containing a verbose dump of the lexicon's contents.
    
    The difference between a DUMP and just the normal printing of a 
    Lexicon is in whether or not it recursively shows you the contents
    of every complex object inside the Lexicon.

    i.e::

        // Just gives a shallow list:
        print mylexicon.
        
        // Walks the entire tree of contents, descending down into
        // any Lists or Lexicons that are stored inside this Lexicon:
        print mylexicon:dump.

.. attribute:: Lexicon:KEYS

    :type: List
    :access: Get only

    Returns a List of the keys stored in this lexicon.

.. attribute:: Lexicon:VALUES

    :type: List
    :access: Get only

    Returns a List of the values stored in this lexicon.

Access to Individual Elements
-----------------------------

``lexicon[expression]``
    operator: another syntax to access the element at position 'expression'. Works for get or set. Any arbitrary complex expression may be used with this syntax, not just a number or variable name. 
``FOR VAR IN LEXICON:KEYS { ... }.``
    :ref:`A type of loop <flow>` in which var iterates over all the items of lexicon from item 0 to item LENGTH-1.

Implicit ADD when using index brackets with new key values
----------------------------------------------------------

**(a.k.a. The difference between GETTING and SETTING with nonexistant keys)**

If you attempt to use a key that does not exist in the lexicon, to
GET a value, as follows::

    SET ARR TO LEXICON().
    SET X TO ARR["somekey"].  // this will produce an error.

Then you will get a KOSKeyNotFoundException error, as you might expect,
because the key ``"somekey"`` isn't there in the empty lexicon you
just made.

*However* if you use a key that does not exist yet to SET a value rather
than to GET a value, you don't get an error.  Instead it actually
implicitly ADDS the new value to the lexicon with that key.  The example
below will not give you an error::

    SET ARR TO LEXICON().
    SET ARR["somekey"] TO 100. // adds new value to the lexicon.

The above ends up doing the same thing as if you had done this::

    SET ARR TO LEXICON().
    ARR:ADD("somekey",100).

Note that while using ``:ADD()`` to make a new value in the lexicon will
give you a duplicate key error if the value already does exist, using
SET to create the value implicitly won't because it simply replaces the
existing value in-place rather than trying to make a new one.

This gives a duplicate key error::

    SET ARR TO LEXICON().
    ARR:ADD("somekey",100).
    ARR:ADD("somekey",200).  // error, because "somekey" already exists.

While this does not::

    SET ARR TO LEXICON().
    SET ARR["somekey"] to 100.
    SET ARR["somekey"] to 200. // no error, because it replaces the value 100 with a 200.

In a nutshell, using [..] to set a value in a lexicon does this:  If the key already exists, replace the value with the new value.  If the key does not already exist, make it exist and give it this new value.

Examples
--------

::

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
