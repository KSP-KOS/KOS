.. _resource:

Resource
========

A single resource value a thing holds (i.e. fuel, electric charge, etc). This is the type returned by the :struct:`Part`:RESOURCES suffix

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
        * - :attr:`TOGGLEABLE`
          - boolean
          - Can this tank be removed from the fuel flow
        * - :attr:`ENABLED`
          - boolean
          - Is this tank currently in the fuel flow
        
		
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


.. attribute:: Resource:TOGGLEABLE

    :access: Get only
    :type: boolean

    Many, but not all, resources can be turned on and off, this removes them from the fuel flow. 

.. attribute:: Resource:ENABLED

    :access: Get/Set
    :type: boolean

    If the resource is TOGGLEABLE, setting this to false will prevent the resource from being taken out normally.
