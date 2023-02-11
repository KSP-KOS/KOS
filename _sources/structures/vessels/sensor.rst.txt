.. _sensor:

Sensor
======

The type of structures returned by :ref:`LIST SENSORS IN SOMEVARIABLE <list command>`. This is not fully understood because the type of :struct:`PartModule` in the KSP API called ``ModuleEnviroSensor``, which all Sensors are a kind of, is not well documented. Here is an example of using :struct:`Sensor`::

    PRINT "Full Sensor Dump:".
    LIST SENSORS IN SENSELIST.

    // TURN EVERY SINGLE SENSOR ON
    FOR S IN SENSELIST {
      PRINT "SENSOR: " + S:TYPE.
      PRINT "VALUE:  " + S:DISPLAY.
      IF S:ACTIVE {
        PRINT "     SENSOR IS ALREADY ON.".
      } ELSE {
        PRINT "     SENSOR WAS OFF.  TURNING IT ON.".
        S:TOGGLE().
      }
    }

.. structure:: Sensor

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - All suffixes of :struct:`Part`
          -
          - :struct:`Sensor` objects are a type of :struct:`Part`
        * - :attr:`ACTIVE`
          - :struct:`Boolean`
          - Check if this sensor is active
        * - :attr:`TYPE`
          -
          -
        * - :attr:`DISPLAY`
          - :struct:`String`
          - Value of the readout
        * - :attr:`POWERCONSUMPTION`
          - :struct:`Scalar`
          - Rate of required electric charge
        * - :meth:`TOGGLE()`
          -
          - Call to activate/deactivate

.. note::

    A :struct:`Sensor` is a type of :struct:`Part`, and therefore can use all the suffixes of :struct:`Part`.

.. attribute:: Sensor:ACTIVE

    :access: Get only
    :type: :struct:`Boolean`

    True of the sensor is enabled. Can SET to cause the sensor to activate or de-activate.

.. attribute:: Sensor:TYPE

    :access: Get only

.. attribute:: Sensor:DISPLAY

    :access: Get only
    :type: :struct:`String`

    The value of the sensor's readout, usualy including the units.

.. attribute:: Sensor:POWERCONSUMPTION

    :access: Get only
    :type: :struct:`Scalar`

    The rate at which this sensor drains ElectricCharge.

.. method:: Sensor:TOGGLE()

    Call this method to cause the sensor to switch between active and deactivated or visa versa.
