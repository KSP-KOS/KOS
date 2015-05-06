Querying a vessel's parts
=========================

This is a quick list to get the idea across fast. The actual
details of the meaning of these things is complex enough to
warrant `its own
topic <../general/parts_and_partmodules.html>`__.

To get the parts of a vessel (such as your current vessel,
called SHIP), you can do the following things:

These are equivalent. They get the full list of all the parts:

::

    LIST PARTS IN MyPartList.
    SET MyPartlist TO SHIP:PARTS.

This gets all the parts that have the name given, as either a
nametag (Part:TAG), a title (Part:TITLE), or a name, (Part:NAME):

::

    SET MyPartList to SHIP:PARTSDUBBED("something").

These are other ways to get parts that are more specific about what
exact nomenclature system is being used:

::

    SET MyPartList to SHIP:PARTSTAGGED("something"). // only gets parts with Part:TAG = "something".
    SET MyPartList to SHIP:PARTSTITLED("something"). // only gets parts with Part:TITLE = "something".
    SET MyPartList to SHIP:PARTSNAMED("something"). // only gets parts with Part:NAME = "something".

This gets all the PartModules on a ship that have the same module name:

::

    SET MyModList to SHIP:MODULESNAMED("something").

This gets all the parts that have been defined to have some sort
of activity occur from a particular action group:

::

    SET MyPartList to SHIP:PARTSINGROUP( AG1 ). // all the parts in action group 1.

This gets all the modules that have been defined to have some sort
of activity occur from a particular action group:

::

    SET MyModList to SHIP:MODULESINGROUP( AG1 ). // all the parts in action group 1.

This gets the primary root part of a vessel (the command core that you
placed FIRST when building the ship in the VAB or SPH):

::

    SET firstPart to SHIP:ROOTPART.

This lets you query all the parts that are immediate children of the
current part in the tree:

::

    SET firstPart to SHIP:ROOTPART.
    FOR P IN firstPart:CHILDREN {
      print "The root part as an immediately attached part called " + P:NAME.
    }.

You could keep walking down the tree this way, or go upward with PARENT
and HASPARENT:

TODO - **NEED TO MAKE A GOOD EXAMPLE OF WALKING THE PARTS TREE HERE WITH RECURSION ONCE THE SYNTAX IS NAILED DOWN FOR THAT.**

::

    IF thisPart:HASPARENT {
      print "This part's parent part is "+ thisPart:PARENT:NAME.
    }.

