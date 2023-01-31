.. _aggregateresource:

AggregateResource
=================

A ship can have many parts that contain resources (i.e. fuel, electric charge, etc). kOS has several tools for getting the summation of each resource.

This is the type returned as the elements of the list from ``LIST RESOURCES.``

IN MyList <list command> ::

    PRINT "THESE ARE ALL THE RESOURCES ON THE SHIP:".
    LIST RESOURCES IN RESLIST.
    FOR RES IN RESLIST {
        PRINT "Resource " + RES:NAME.
        PRINT "    value = " + RES:AMOUNT.
        PRINT "    which is "
              + ROUND(100*RES:AMOUNT/RES:CAPACITY)
              + "% full.".
    }.

This is also the type returned by STAGE:RESOURCES ::

    PRINT "THESE ARE ALL THE RESOURCES active in this stage:".
    SET RESLIST TO STAGE:RESOURCES.
    FOR RES IN RESLIST {
        PRINT "Resource " + RES:NAME.
        PRINT "    value = " + RES:AMOUNT.
        PRINT "    which is "
              + ROUND(100*RES:AMOUNT/RES:CAPACITY)
              + "% full.".
    }.

.. structure:: AggregateResource

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`NAME`
          - :struct:`String`
          - Resource name
        * - :attr:`AMOUNT`
          - :struct:`Scalar`
          - Total amount remaining
        * - :attr:`CAPACITY`
          - :struct:`Scalar`
          - Total amount when full
        * - :attr:`DENSITY`
          - :struct:`Scalar`
          - Density of this resource
        * - :attr:`PARTS`
          - :struct:`List`
          - Parts containing this resource


.. attribute:: AggregateResource:NAME

    :access: Get only
    :type: :struct:`String`

    The name of the resource, i.e. "LIQUIDFUEL", "ELECTRICCHARGE", "MONOPROP".

.. attribute:: AggregateResource:AMOUNT

    :access: Get only
    :type: :struct:`Scalar`

    The value of how much resource is left.

.. attribute:: AggregateResource:CAPACITY

    :access: Get only
    :type: :struct:`Scalar`

    What AMOUNT would be if the resource was filled to the top.

.. attribute:: AggregateResource:DENSITY

    :access: Get only
    :type: :struct:`Scalar`

    The density value of this resource, expressed in Megagrams f mass
    per Unit of resource.  (i.e. a value of 0.005 would mean that each
    unit of this resource is 5 kilograms.  Megagrams [metric tonnes] is
    the usual unit that most mass in the game is represented in.)

.. attribute:: AggregateResource:PARTS

    :access: Get only
    :type: :struct:`List`

    Because this is a summation of the resources from many parts. kOS gives you the list of all parts that do or could contain the resource.
