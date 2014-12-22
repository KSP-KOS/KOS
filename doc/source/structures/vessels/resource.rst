.. _resource:

Resource
========

A single resource value a thing holds (i.e. fuel, electric charge, etc). This is the type returned as the elements of the list in :ref:`LIST RESOURCES
IN MyList <list command>`::

    PRINT "THESE ARE ALL THE RESOURCES ON THE SHIP:".
    LIST RESOURCES IN RESLIST.
    FOR RES IN RESLIST {
        PRINT "Resource " + RES:NAME.
        PRINT "    value = " + AMOUNT.
        PRINT "    which is "
              + ROUND(100*RES:AMOUNT/RES:CAPACITY)
              + "% full.".
    }.


.. structure:: Resource

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`NAME`
          - string
          - Resource name
        * - :attr:`AMOUNT`
          - scalar
          - Amount of this resource left
        * - :attr:`CAPACITY`
          - scalar
          - Maximum amount of this resource

.. attribute:: Resource:NAME

    :access: Get only
    :type: string

    The name of the resource, i.e. "LIQUIDFUEL", "ELECTRICCHARGE", "MONOPROP".

.. attribute:: Resource:AMOUNT

    :access: Get only
    :type: scalar

    The value of how much resource is left.

.. attribute:: Resource:CAPACITY

    :access: Get only
    :type: scalar

    What AMOUNT would be if the resource was filled to the top.

