VesselSensors
=============

When you ask a Vessel to tell you its :attr:`Vessel:SENSORS` suffix, it returns an object of this type. It is a snapshot of sensor data taken at the moment the sensor reading was requested.

.. note::

    These values are only enabled if you have the proper type of sensor on board the vessel. If you don't have a thermometer, for example, then the :attr:`:TEMP <VesselSensors>` suffix will always read zero.

If you store this in a variable and wait, the numbers are frozen in time and won't change as the vessel's condition changes.

.. structure:: VesselSensors

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 3

        * - Suffix
          - Type
          - Description
        * - :attr:`ACC`
          - :struct:`Vector`
          - Acceleration experienced by the :struct:`Vessel`
        * - :attr:`PRES`
          - :struct:`Scalar`
          - Atmospheric Pressure outside this :struct:`Vessel`
        * - :attr:`TEMP`
          - :struct:`Scalar`
          - Temperature outside this :struct:`Vessel`
        * - :attr:`GRAV`
          - :struct:`Vector` (g's)
          - Gravitational acceleration
        * - :attr:`LIGHT`
          - :struct:`Scalar`
          - Sun exposure on the solar panels of this :struct:`Vessel`


.. attribute:: VesselSensors:ACC

    :access: Get only
    :type: :struct:`Vector`

    Accelleration the vessel is undergoing. A combination of both the gravitational pull and the engine thrust.

.. attribute:: VesselSensors:PRES

    :access: Get only
    :type: :struct:`Scalar`

    The current pressure of this ship.

.. attribute:: VesselSensors:TEMP

    :access: Get only
    :type: :struct:`Scalar`

    The current temperature.

.. attribute:: VesselSensors:GRAV

    :access: Get only
    :type: :struct:`Vector`

    Magnitude and direction of gravity acceleration where the vessel currently is. Magnitude is expressed in "G"'s (multiples of 9.802 m/s^2).

.. attribute:: VesselSensors:LIGHT

    :access: Get only
    :type: :struct:`Scalar`

    The total amount of sun exposure that exists here - only readable if there are solar panels on the vessel.
