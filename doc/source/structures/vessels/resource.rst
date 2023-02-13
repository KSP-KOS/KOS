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
          - :struct:`String`
          - Resource name
        * - :attr:`AMOUNT`
          - :struct:`Scalar`
          - Amount of this resource left
        * - :attr:`DENSITY`
          - :struct:`Scalar`
          - Density of this resource
        * - :attr:`CAPACITY`
          - :struct:`Scalar`
          - Maximum amount of this resource
        * - :attr:`TOGGLEABLE`
          - :struct:`Boolean`
          - Can this tank be removed from the fuel flow
        * - :attr:`ENABLED`
          - :struct:`Boolean`
          - Is this tank currently in the fuel flow
        
		
.. attribute:: Resource:NAME

    :access: Get only
    :type: :struct:`String`

    The name of the resource, i.e. "LIQUIDFUEL", "ELECTRICCHARGE", "MONOPROP".

.. attribute:: Resource:AMOUNT

    :access: Get only
    :type: :struct:`Scalar`

    The value of how much resource is left.

.. attribute:: Resource:DENSITY

    :access: Get only
    :type: :struct:`Scalar`

    The density value of this resource, expressed in Megagrams f mass
    per Unit of resource.  (i.e. a value of 0.005 would mean that each
    unit of this resource is 5 kilograms.  Megagrams [metric tonnes] is
    the usual unit that most mass in the game is represented in.)

.. attribute:: Resource:CAPACITY

    :access: Get only
    :type: :struct:`Scalar`

    What AMOUNT would be if the resource was filled to the top.


.. attribute:: Resource:TOGGLEABLE

    :access: Get only
    :type: :struct:`Boolean`

    Many, but not all, resources can be turned on and off, this removes them from the fuel flow. 

.. attribute:: Resource:ENABLED

    :access: Get/Set
    :type: :struct:`Boolean`

    If the resource is TOGGLEABLE, setting this to false will prevent the resource from being taken out normally.
