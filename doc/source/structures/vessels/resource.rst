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
          - :ref:`string <string>`
          - Resource name
        * - :attr:`AMOUNT`
          - :ref:`scalar <scalar>`
          - Amount of this resource left
        * - :attr:`DENSITY`
          - :ref:`scalar <scalar>`
          - Density of this resource
        * - :attr:`CAPACITY`
          - :ref:`scalar <scalar>`
          - Maximum amount of this resource
        * - :attr:`TOGGLEABLE`
          - :ref:`Boolean <boolean>`
          - Can this tank be removed from the fuel flow
        * - :attr:`ENABLED`
          - :ref:`Boolean <boolean>`
          - Is this tank currently in the fuel flow
        
		
.. attribute:: Resource:NAME

    :access: Get only
    :type: :ref:`string <string>`

    The name of the resource, i.e. "LIQUIDFUEL", "ELECTRICCHARGE", "MONOPROP".

.. attribute:: Resource:AMOUNT

    :access: Get only
    :type: :ref:`scalar <scalar>`

    The value of how much resource is left.

.. attribute:: Resource:DENSITY

    :access: Get only
    :type: :ref:`scalar <scalar>`

    The density value of this resource, expressed in Megagrams f mass
    per Unit of resource.  (i.e. a value of 0.005 would mean that each
    unit of this resource is 5 kilograms.  Megagrams [metric tonnes] is
    the usual unit that most mass in the game is represented in.)

.. attribute:: Resource:CAPACITY

    :access: Get only
    :type: :ref:`scalar <scalar>`

    What AMOUNT would be if the resource was filled to the top.


.. attribute:: Resource:TOGGLEABLE

    :access: Get only
    :type: :ref:`Boolean <boolean>`

    Many, but not all, resources can be turned on and off, this removes them from the fuel flow. 

.. attribute:: Resource:ENABLED

    :access: Get/Set
    :type: :ref:`Boolean <boolean>`

    If the resource is TOGGLEABLE, setting this to false will prevent the resource from being taken out normally.
