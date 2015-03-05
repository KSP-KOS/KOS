.. _features:

General Features of the **KerboScript** Language
================================================

.. contents::
    :local:
    :depth: 2

Case Insensitivity
------------------

Everything in **KerboScript** is case-insensitive, including your own variable names and filenames. The only exception is when you perform a string comparison, (``"Hello"="HELLO"`` will return false.)

Most of the examples here will show the syntax in all-uppercase to help make it stand out from the explanatory text.

Expressions
-----------

KerboScript uses an expression evaluation system that allows you to perform math operations on variables. Some variables are defined by you. Others are defined by the system. There are four basic types:

1. Numbers
~~~~~~~~~~

You can use mathematical operations on numbers, like this::

    SET X TO 4 + 2.5.
    PRINT X.             // Outputs 6.5

The system follows the order of operations, but currently the implementation is imperfect. For example, multiplication will always be performed before division, regardless of the order they come in. This will be fixed in a future release.

2. Strings
~~~~~~~~~~

Strings are pieces of text that are generally meant to be printed to the screen. For example::

    PRINT "Hello World!".

To concatenate strings, you can use the + operator. This works with mixtures of numbers and strings as well::

    PRINT "4 plus 3 is: " + (4+3).

.. _features structures:

3. Structures
~~~~~~~~~~~~~

Structures are variables that contain more than one piece of information. For example, a Vector has an X, a Y, and a Z component. Structures can be used with SET.. TO just like any other variable. To access the sub-elements of a structure, you use the colon operator (":"). Here are some examples::

    PRINT "The Mun's periapsis altitude is: " + MUN:PERIAPSIS.
    PRINT "The ship's surface velocity is: " + SHIP:VELOCITY:SURFACE.

Many structures also let you set a specific component of them, for example::

    SET VEC TO V(10,10,10).  // A vector with x,y,z components
                             // all set to 10.
    SET VEC:X to VEC:X * 4.  // multiply just the X part of VEC by 4.
    PRINT VEC.               // Results in V(40,10,10).

.. _features methods:

4. Structure Methods
~~~~~~~~~~~~~~~~~~~~

Structures also often contain methods. A method is a suffix of a structure that actually performs an activity when you mention it, and can sometimes take parameters. The following are examples of calling methods of a structure::

    SET PLIST TO SHIP:PARTSDUBBED("my engines"). // calling a suffix
                                                 // method with one
                                                 // argument that
                                                 // returns a list.
    PLIST:REMOVE(0). // calling a suffix method with one argument that
                     // doesn't return anything.
    PRINT PLIST:SUBLIST(0,4). // calling a suffix method with 2
                              // arguments that returns a list.

.. note::
    .. versionadded:: 0.15
        Methods now perform the activity when the interpreter comes up to it. Prior to this version, execution was sometimes delayed until some later time depending on the trigger setup or flow-control.

For more information, see the :ref:`Structures Section <language structures>`. A full list of structure types can be found on the :ref:`Structures <structures>` page. For a more detailed breakdown of the language, see the :ref:`Language Syntax Constructs <syntax>` page.

.. _feature functions:

User Functions
--------------

.. note::
    .. versionadded:: 0.17
        This feature did not exist in prior versions of kerboscript.

Kerboscript supports user functions which you can write yourself
and call from your own scripts.  *These are not* :ref:`structure
methods <methods>` *(which as of this writing are a feature which
only works for the built-in kOS types, and are not yet supported
by the kerboscript language for user functions you write yourself).*

Example::

    DECLARE FUNCTION DEGREES_TO_RADIANS {
      DECLARE PARAMETER DEG.

      RETURN CONSTANT():PI * DEG/180.
    }.

    SET ALPHA TO 45.
    PRINT ALPHA + " degrees is " + DEGREES_TO_RADIANS(ALPHA) + " radians.".

For a more detailed description of how to declare your own user functions,
see the :ref:`Language Syntax Constructs, User Functions <syntax functions>`
section.

.. _language structures:

Structures
----------

Structures, :ref:`introduced above <features structures>`, are variable *types* that contain more than one piece of information. All structures contain sub-values or :ref:`methods <features methods>` that can be accessed with a colon (``:``) operator. Multiple structures can be chained together with more than one colon (``:``) operator::

    SET myCraft TO SHIP.
    SET myMass TO myCraft:MASS.
    SET myVel TO myCraft:VELOCITY:ORBIT.

These terms are referred to as "suffixes". For example ``Velocity`` is a suffix of ``Vessel``. It is possible to **set** some suffixes as well. The second line in the following example sets the ``ETA`` of a ``NODE`` 500 seconds into the future::

    SET n TO Node( TIME:SECONDS + 60, 0, 10, 10).
    SET n:ETA to 500.

The full list of available suffixes for each type :ref:`can be found here <structures>`.

