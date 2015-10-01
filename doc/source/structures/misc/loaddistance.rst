.. loaddistance:

Vessel Load Distance
====================


.. structure:: LoadDistance

    :struct:`LoadDistance` is a special structure that allows your kerboscript programs to access some of the functions that break the "4th Wall".  It serves as a place to access object directly connected to the KSP game itself, rather than the interaction with the KSP world (vessels, planets, orbits, etc.).

    .. list-table:: Members and Methods
        :header-rows: 1
        :widths: 2 2 1 4

        * - Suffix
          - Type
          - Get/Set
          - Description

        * - :attr:`ESCAPING`
          - :struct:`SituationLoadDistance`
          - Get
          - Load Distance while escaping the current body
        * - :attr:`FLYING`
          - :struct:`SituationLoadDistance`
          - Get
          - Load Distance while flying in atmosphere
        * - :attr:`LANDED`
          - :struct:`SituationLoadDistance`
          - Get
          - Load Distance while landed on the surface
        * - :attr:`ORBIT`
          - :struct:`SituationLoadDistance`
          - Get
          - Load Distance while in orbit
        * - :attr:`PRELAUNCH`
          - :struct:`SituationLoadDistance`
          - Get
          - Load Distance while on the launch pad or runway
        * - :attr:`SPLASHED`
          - :struct:`SituationLoadDistance`
          - Get
          - Load Distance while splashed in water
        * - :attr:`SUBORBITAL`
          - :struct:`SituationLoadDistance`
          - Get
          - Load Distance while on a suborbital trajectory

Situation Load Distance
======================


.. structure:: SituationLoadDistance

  :struct:`SituationLoadDistance` is a special structure that allows your kerboscript programs to access some of the functions that break the "4th Wall".  It serves as a place to access object directly connected to the KSP game itself, rather than the interaction with the KSP world (vessels, planets, orbits, etc.).

  .. list-table:: Members and Methods
      :header-rows: 1
      :widths: 2 1 1 4

        * - Suffix
          - Type
          - Get/Set
          - Description

        * - :attr:`LOAD`
          - scalar
          - Get/Set
          - The load distance
        * - :attr:`UNLOAD`
          - scalar
          - Get/Set
          - The unload distance
        * - :attr:`UNPACK`
          - scalar
          - Get/Set
          - The unpack distance
        * - :attr:`PACK`
          - scalar
          - Get/Set
          - The pack distance

.. attribute:: SituationLoadDistance:LOAD

    :access: Get/sET
    :type: scalar

    Get or set the load distance.  This is the distance at which a :struct:`Vessel` will load into physics (as opposed to "on rails").  This value must be greater than :attr:`UNLOAD`, and will automatically be adjusted accordingly.

.. attribute:: SituationLoadDistance:UNLOAD

    :access: Get/sET
    :type: scalar

    Get or set the unload distance.  This is the distance at which a :struct:`Vessel` will unload from physics (or go "on rails").  This value must be less than :attr:`LOAD`, and will automatically be adjusted accordingly.

.. attribute:: SituationLoadDistance:UNPACK

    :access: Get/sET
    :type: scalar

    Get or set the unpack distance.  This is the distance at which a :struct:`Vessel` will be unpacked, which among other things allows a docking port to be targeted.  This value must be less than :attr:`PACK`, and will automatically be adjusted accordingly.

.. attribute:: SituationLoadDistance:PACK

    :access: Get/sET
    :type: scalar

    Get or set the pack distance.  This is the distance at which a :struct:`Vessel` will be packed, which among other things prevents docking ports from being targeted.  This value must be greater than :attr:`UNPACK`, and will automatically be adjusted accordingly.
