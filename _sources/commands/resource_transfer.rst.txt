.. _resource transfer:

Transferring resources
======================

Resource transfer is an important part of many missions in Kerbal Space Program,
whether you are gassing up a lander or refuelling a station. Because of this, we need
a way to describe a resource transfer in script and monitor that transfer to be sure it 
was successful.

To accomplish all of this we have two new functions

::

    SET transferFoo TO TRANSFER(resourceName, fromParts, toParts, amount).
    SET transferBar TO TRANSFERALL(resourceName, fromParts, toParts).
    
TRANSFER will move the specified amount of a resource, where TRANSFERALL will move
the resource until the source is empty or the destination is full. Both functions
return a new :struct:`ResourceTransfer`. 

Resource
--------

in all transfers, you must specify a resource by name 
(eg "oxidizer", "LIQUIDFUEL", etc). this term is not case sensitive.
    
Source and Destination 
----------------------

In all transfers, you must specify a source and a destination, both of these need 
to be one of these three types:

* :struct:`Part`
* :struct:`List` of :struct:`Part`
* :struct:`Element`

Examples
--------

Building a list of :struct:`Element`, then transferring all of the oxidizer from 
one element to another: ::
    
    LIST ELEMENTS IN elist.
    SET foo TO TRANSFERALL("OXIDIZER", elist[0], elist[1]).
    SET foo:ACTIVE to TRUE.
    

Finding two parts by index, then transferring all of the oxidizer from 
part 1 to part 2: ::

    SET foo TO TRANSFERALL("OXIDIZER", SHIP:PARTS[0], SHIP:PARTS[1]).
    SET foo:ACTIVE to TRUE.
    
Finding two lists of parts by tag, then transferring 100 units of liquidfuel: ::

    SET sourceParts to SHIP:PARTSDUBBED("fooPart").
    SET destinationParts to SHIP:PARTSDUBBED("barPart").
    SET foo TO TRANSFER("liquidfuel", sourceParts, destinationParts, 100).
    SET foo:ACTIVE to TRUE.
