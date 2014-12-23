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

**Arithmetic Operators**::

    +  -  *  /  ^  e  (  )

**Logic Operators**::

    not  and  or  true  false  <>  >=  <=  =  >  <

**Instructions and keywords**::

    set  to  if  else  until  lock  unlock  print  at  on  toggle
    wait  when  then  off  stage  clearscreen  add  remove  log
    break  preserve  declare  parameter  switch  copy  from  rename
    volume  file  delete  edit  run  compile  list  reboot  shutdown
    for  unset  batch  deploy  in  all

**Other symbols**::

    {  }  [  ]  ,  :  //

*Comments* consist of everything from a "//" symbol to the end of the line::

    set x to 1. // this is a comment.

**Identifiers**: Identifiers consist of: a string of (letter, digit, or
underscore). The first character must be a letter. The rest may be letters, digits or underscores. **Identifiers are case-insensitive**. The following are identical identifiers::

    My_Variable  my_varible  MY_VARAIBLE

**case-insensitivity**
    The same case-insensitivity applies throughout the entire language, with all keywords except when comparing literal strings. The values inside the strings are still case-sensitive, for example, the following will print "unequal"::

        if "hello" = "HELLO" {
            print "equal".
        } else {
            print "unequal".
        }

**Suffixes**
    Some variable types are structures that contain sub-portions. The separator between the main variable and the item inside it is a colon character (``:``). When this symbol is used, the part on the right-hand side of the colon is called the "suffix"::

        list parts in mylist.
        print mylist:length. // length is a suffix of mylist

Suffixes can be chained together, as in this example::

    print ship:velocity:orbit:x.

In the above example you'd say "``velocity`` is a suffix of ``ship``", and "``orbit`` is a suffix of ``ship:velocity``", and "``x`` is a suffix of ``ship:velocity:orbit``".

Functions
---------

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

Built-In Special Variable Names
-------------------------------

Some variable names have special meaning and will not work as identifiers. Understanding this list is crucial to using kOS effectively, as these special variables are the usual way to query flight state information. :ref:`The full list of reserved variable names is on its own page <bindings>`.

What does not exist (yet?)
--------------------------

Concepts that many other languages have, that are missing from **KerboScript**, are listed below. Many of these are things that could be supported some day, but at the moment with the limited amount of developer time available they haven't become essential enough to spend the time on supporting them.

**user-made functions**
    There are built-in functions you can call, but you can't make your own in the script. The closest you can come to this is to make a separate script file and you can ``RUN`` the script file from another script file.

**local variables**
    All variables are in the same global namespace. You can't make local variables. If homemade functions are ever supported, that is when local variables would become useful.

**user-made structures or classes**
    Several of the built-in variables of **kOS** are essentially "classes" with methods and fields, however there's currently no way for user code to create its own classes or structures. Supporting this would open up a *large* can of worms, as it would then make the **kOS** system more complex.
