.. _syntax:

**KerboScript** Syntax Specification
====================================

This describes what is and is not a syntax error in the **KerboScript** programming language. It does not describe what function calls exist or which commands and built-in variables are present. Those are contained in other documents.

.. contents:: Contents
    :local:
    :depth: 2
    
General Rules
-------------

*Whitespace* consisting of consecutive spaces, tabs, and line breaks are all considered identical to each other. Because of this, indentation is up to you. You may indent however you like.

.. note::

    Statements are ended with a **period** character (".").

The following are **reserved command keywords** and special
operator symbols:

.. highlight:: none

**Arithmetic Operators**::

    +  -  *  /  ^  e  (  )

**Logic Operators**::

    not  and  or  true  false  <>  >=  <=  =  >  <

**Instructions and keywords**::

    add all at batch break clearscreen compile copy declare delete
    deploy do do edit else file for from from function global if
    in list local lock log off on once parameter preserve print reboot
    remove rename run set shutdown stage step switch then to toggle
    unlock unset until volume wait when

**Other symbols**::

    {  }  [  ]  ,  :  //

.. highlight:: kerboscript

*Comments* consist of everything from a "//" symbol to the end of the line::

    set x to 1. // this is a comment.

.. highlight:: none

**Identifiers**: Identifiers consist of: a string of (letter, digit, or
underscore). The first character must be a letter or an underscore.
The rest may be letters, digits or underscores.

**Identifiers are case-insensitive**. The following are identical identifiers::

    my_variable
    My_Variable 
    MY_VARIABLE 

.. note::
  .. versionadded:: 1.1.0
    Kerboscript accepts Unicode source code, encoded using the UTF-8
    encoding method.  Because of this, the definition of a "letter"
    character for an identifier includes letters from many languages'
    alphabets, including accented Latin alphabet characters, Cyrllic
    characters, etc.  Not all languages have been tested but in
    principle they should work as long as they have a Unicode standard
    accepted definition of what counts as a "letter".  We defer to
    the .NET libraries' definition of what constitutes the "same" letter
    in uppercase and lowercase forms, and we hope this is right for
    most alphabets.

.. highlight:: kerboscript

**case-insensitivity**
    The same case-insensitivity applies throughout the entire language, with all keywords and when comparing literal strings. The values inside the strings are also case-insensitive, for example, the following will print "equal"::

        if "hello" = "HELLO" {
            print "equal".
        } else {
            print "unequal".
        }

.. note::
  .. versionadded:: 1.1.0
    Again, depending on the alphabet being used, the concept of
    "uppercase" and "lowercase" might not make sense in some
    languages.  kOS defers to .NET's interpretation of what
    letters in Unicode are paired together as the "upper" and
    "lower" versions of the same letter.  For obvious reasons,
    the kOS developers cannot test every language and verify if
    this is correct or not.

**Suffixes**
    Some variable types are structures that contain sub-portions. The separator between the main variable and the item inside it is a colon character (``:``). When this symbol is used, the part on the right-hand side of the colon is called the "suffix"::

        list parts in mylist.
        print mylist:length. // length is a suffix of mylist

Suffixes can be chained together, as in this example::

    print ship:velocity:orbit:x.

In the above example you'd say "``velocity`` is a suffix of ``ship``", and "``orbit`` is a suffix of ``ship:velocity``", and "``x`` is a suffix of ``ship:velocity:orbit``".

Numbers (scalars)
-----------------

Numbers in kerboscript are referred to as "scalars", to distinguish
them from the many cases where a values will be represnted
as a vectors.  You are allowed to use integers, decimal fractional numbers
(numbers with a decimal point and a fractional part), and scientific
notation numbers.

The following are valid scalar syntax::

   12345678
   12_345_678 (The underscores are ignored as just visual spacers)
   12345.6789
   12_345.6789
   -12345678
   1.123e12
   1.234e-12

Kerobscript does not support imaginary numbers or irrational numbers
or rational numbers that cannot be represented as a finite decimal
(i.e.  sqrt(-1) returns a Not-a-Number error.  Pi will have to be
an approximation.  "One third", ends up being something like 0.333333333).)

Under the hood, these numbers are stored as either 32-bit integers or as
64-bit double floats, depending on the need, but kerboscript attempts
to hide this detail from the programmer as much as possible.

Braces (statement blocks)
-------------------------

Anywhere you feel like, you may insert braces around a list of statements
to get the language to treat them all as a single statement block.

For example: the IF statement expects one statement as its body, like so::

    if x = 1
      print "it's 1".

But you can put multiple statements there as its body by surrounding them
with braces, like so::

    if x = 1 { print "it's 1".  print "yippieee.".  }

(Although this is usually preferred to be indented as follows)::

    if x = 1 {
      print "it's 1".
      print "yippieee.".
    }

or::

    if x = 1
    {
      print "it's 1".
      print "yippieee.".
    }

Kerboscript does not require proper indentation of the brace sections,
but it is a good idea to make things clear.

You are allowed to just insert braces anywhere you feel like even when the
language does not require it, as shown below::

    declare x to 3.
    print "x here is " + x.
    {
      declare x to 5.
      print "x here is " + x.
      {
        declare x to 7.
        print "x here is " + x.
      }
    }

The usual reason for doing this is to create a
:ref:`local scope section <scope>` for yourself.
In the above example, there are actually 3 *different*
variables called 'x' - each with a different scope.

Functions (built-in)
--------------------

There exist a number of built-in functions you can call using their names. When you do so, you can do it like so::

    functionName( *arguments with commas between them* ).

For example, the ``ROUND`` function takes 2 arguments::

    print ROUND(1230.12312, 2).

The ``SIN`` function takes 1 argument::

    print SIN(45).

When a function requires zero arguments, it is legal to call it using the parentheses or not using them. You can pick either way::

    // These both work:
    CLEARSCREEN.
    CLEARSCREEN().

Suffixes as Functions (Methods)
-------------------------------

Some suffixes are actually functions you can call. When that is the case, these suffixes are called "method suffixes". Here are some examples::

    set x to ship:partsnamed("rtg").
    print x:length().
    x:remove(0).
    x:clear().

.. _syntax functions:

User Functions
--------------

.. note::
    .. versionadded:: 0.17
        This feature did not exist in prior versions of kerboscript.

Help for the new user - What is a Function?
    In programming terminology, there is a commonly used feature of
    many programming languages that works as follows:

    - 1. Create a chunk of program instructions that you don't intend to execute YET.
    - 2. Later, when executing other parts of the program, do the following:

        - A. Remember the current location in the program.
        - B. Jump to the previously created chunk of code from (1) above.
        - C. Run the instructions there.
        - D. Return to where you remembered from (2.A) and continue from there.

    This feature goes by many different names, with slightly different
    precise meanings: *Subroutines*, *Procedures*, *Functions*, etc.
    For the purposes of kerboscript, we will refer to all uses of this
    feature with the term *Function*, whether it *technically* fits the
    mathematical definition of a "function" or not.

In kerboscript, you can make your own user functions using the
DECLARE FUNCTION command, which is structured as follows:

  ``declare function`` *identifier* ``{`` *statements* ``}`` *optional dot (.)*

Functions are a long enough topic as to require a
:ref:`separate documentation page, here. <user_functions>`

Built-In Special Variable Names
-------------------------------

Some variable names have special meaning and will not work as identifiers. Understanding this list is crucial to using kOS effectively, as these special variables are the usual way to query flight state information. :ref:`The full list of reserved variable names is on its own page <bindings>`.

What does not exist (yet?)
--------------------------

Concepts that many other languages have, that are missing from **KerboScript**, are listed below. Many of these are things that could be supported some day, but at the moment with the limited amount of developer time available they haven't become essential enough to spend the time on supporting them.

**user-made structures or classes**
    Several of the built-in variables of **kOS** are essentially "classes" with methods and fields, however there's currently no way for user code to create its own classes or structures. Supporting this would open up a *large* can of worms, as it would then make the **kOS** system more complex.
