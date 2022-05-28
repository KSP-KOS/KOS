.. _consumedresource:

ConsumedResource
=================

A single resource value an engine consumes (i.e. fuel, oxidizer, etc). This is the type returned by the :struct:`Engine`:CONSUMEDRESOURCES suffix

.. structure:: ConsumedResource

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
          - Total amount remaining (only valid while engine is running)
        * - :attr:`CAPACITY`
          - :ref:`scalar <scalar>`
          - Total amount when full (only valid while engine is running)
        * - :attr:`DENSITY`
          - :ref:`scalar <scalar>`
          - Density of this resource
        * - :attr:`FUELFLOW`
          - :ref:`scalar <scalar>`
          - Current volumetric flow rate of fuel
        * - :attr:`MAXFUELFLOW`
          - :ref:`scalar <scalar>`
          - Untweaked maximum volumetric flow rate of fuel
        * - :attr:`REQUIREDFLOW`
          - :ref:`scalar <scalar>`
          - Required volumetric flow rate of fuel for current throttle
        * - :attr:`MASSFLOW`
          - :ref:`scalar <scalar>`
          - Current mass flow rate of fuel
        * - :attr:`MAXMASSFLOW`
          - :ref:`scalar <scalar>`
          - Untweaked maximum mass flow rate of fuel
        * - :attr:`RATIO`
          - :ref:`scalar <scalar>`
          - Volumetric flow ratio of this resource


.. attribute:: ConsumedResource:NAME

    :access: Get only
    :type: :ref:`string <string>`

    The name of the resource, i.e. "LIQUIDFUEL", "ELECTRICCHARGE", "MONOPROP".

.. attribute:: ConsumedResource:AMOUNT

    :access: Get only
    :type: :ref:`scalar <scalar>`

    The value of how much resource is left and accessible to this engine. Only valid while the engine is running.

.. attribute:: ConsumedResource:CAPACITY

    :access: Get only
    :type: :ref:`scalar <scalar>`

    What AMOUNT would be if the resource was filled to the top. Only valid while the engine is running.

.. attribute:: ConsumedResource:DENSITY

    :access: Get only
    :type: :ref:`scalar <scalar>`

    The density value of this resource, expressed in Megagrams f mass
    per Unit of resource.  (i.e. a value of 0.005 would mean that each
    unit of this resource is 5 kilograms.  Megagrams [metric tonnes] is
    the usual unit that most mass in the game is represented in.)

.. attribute:: ConsumedResource:FUELFLOW

    :access: Get only
    :type: :ref:`scalar <scalar>`

    How much volume of this fuel is this engine consuming at this very moment.

.. attribute:: ConsumedResource:MAXFUELFLOW

    :access: Get only
    :type: :ref:`scalar <scalar>`

    How much volume of this fuel would this engine consume at standard pressure and velocity if the throttle was max at 1.0, and the thrust limiter was max at 100%.
    
.. attribute:: ConsumedResource:REQUIREDFLOW

    :access: Get only
    :type: :ref:`scalar <scalar>`

    How much volume of this fuel does this require at this very moment for the current throttle setting.
    This will usually equal FUELFLOW but may be higher for INTAKEAIR for instance.

.. attribute:: ConsumedResource:MASSFLOW

    :access: Get only
    :type: :ref:`scalar <scalar>`

    How much mass of this fuel is this engine consuming at this very moment.

.. attribute:: ConsumedResource:MAXMASSFLOW

    :access: Get only
    :type: :ref:`scalar <scalar>`

    How much mass of this fuel would this engine consume at standard pressure and velocity if the throttle was max at 1.0, and the thrust limiter was max at 100%.
    
.. attribute:: ConsumedResource:RATIO

    :access: Get only
    :type: :ref:`scalar <scalar>`

    What is the volumetric ratio of this fuel as a proportion of the overall fuel mixture, e.g. if this is 0.5 then this fuel is half the required fuel for the engine.

