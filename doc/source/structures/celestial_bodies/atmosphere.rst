.. _atmosphere:

Atmosphere
==========

A Structure closely tied to :struct:`Body` A variable of type :struct:`Atmosphere` usually is obtained by the :attr:`:ATM <Body:ATM>` suffix of a :struct:`Body`. ALL The following values are read-only. You can't change the value of a body's atmosphere.

.. structure:: Atmosphere

    .. list-table::
        :header-rows: 1
        :widths: 1 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`BODY`
          - :ref:`string <string>`
          - Name of the celestial body
        * - :attr:`EXISTS`
          - :ref:`boolean <boolean>`
          - True if this body has an atmosphere
        * - :attr:`OXYGEN`
          - :ref:`boolean <boolean>`
          - True if oxygen is present
        * - :attr:`SCALE` (DEPRECATED)
          - :ref:`scalar <scalar>`
          - Used to find atmospheric density
        * - :attr:`SEALEVELPRESSURE`
          - :ref:`scalar <scalar>` (atm)
          - pressure at sea level
        * - :meth:`ALTITUDEPRESSURE(altitude)`
          - :ref:`scalar <scalar>` (atm)
          - pressure at the givel altitude
        * - :attr:`HEIGHT`
          - :ref:`scalar <scalar>` (m)
          - advertised atmospheric height
        * - :attr:`MOLARMASS`
          - :ref:`scalar <scalar>` (kg/mol)
          - The molecular mass of the atmosphere's gas
        * - :attr:`ADIABATICINDEX`
          - :ref:`scalar <scalar>`
          - The Adiabatic index of the atmosphere's gas
        * - :attr:`ADBIDX`
          - :ref:`scalar <scalar>`
          - Short alias for :attr:`ADIABATICINDEX`
        * - :meth:`ALTITUDETEMPERATURE(altitude)`
          - :ref:`scalar <scalar>`
          - Estimate of temperature at the given altitude.
        * - :meth:`ALTTEMP(altitude)`
          - :ref:`scalar <scalar>`
          - Short alias for :attr:`ALTITUDETEMPERATURE`


.. attribute:: Atmosphere:BODY

    :type: :ref:`string <string>`
    :access: Get only

    The Body that this atmosphere is around - as a STRING NAME, not a Body object.

.. attribute:: Atmosphere:EXISTS

    :type: :ref:`boolean <boolean>`
    :access: Get only

    True if this atmosphere is "real" and not just a dummy placeholder.

.. attribute:: Atmosphere:OXYGEN

    :type: :ref:`boolean <boolean>`
    :access: Get only

    True if the air has oxygen and could therefore be used by a jet engine's intake.

.. attribute:: Atmosphere:SEALEVELPRESSURE

    :type: :ref:`scalar <scalar>` (atm)
    :access: Get only

    Pressure at the body's sea level.

    Result is returned in Atmospheres.  1.0 Atmosphere = same as Kerbin or Earth.
    If you prefer to see the answer in KiloPascals, multiply the answer by
    :global:`Constant:AtmToKPa`.

    .. warning::
        .. versionchanged:: 1.1.0
            Previous versions returned this value in KiloPascals by mistake,
            which has now been changed to Atmospheres.

.. method:: Atmosphere:ALTITUDEPRESSURE(altitude)

    :parameter altitude: The altitude above sea level (in meters) you want to know the pressure for.
    :type: :ref:`scalar <scalar>` (atm)

    Number of Atm's of atmospheric pressure at the given altitude.
    If you pass in zero, you should get the sea level pressure.
    If you pass in 10000, you get the pressure at altitude=10,000m.
    This will return zero if the body has no atmosphere, or if the altitude you
    pass in is above the max atmosphere altitude for the body.

    Result is returned in Atmospheres.  1.0 Atmosphere = same as Kerbin or Earth.
    If you prefer to see the answer in KiloPascals, multiply the answer by
    :global:`Constant:AtmToKPa`.

.. attribute:: Atmosphere:HEIGHT

    :type: :ref:`scalar <scalar>` (m)
    :access: Get only

    The altitude at which the atmosphere is "officially" advertised as ending. (actual ending value differs, see below).

.. attribute:: Atmosphere:MOLARMASS

    :type: :ref:`scalar <scalar>`
    :acces: Get only

    The Molecular Mass of the gas the atmosphere is composed of.
    Units are in kg/mol.
    `Wikipedia Molar Mass Explanation <https://en.wikipedia.org/wiki/Molar_mass>`_.

.. attribute:: Atmosphere:ADIABATICINDEX

    :type: :ref:`scalar <scalar>`
    :access: Get only

    The Adiabatic index of the gas the atmosphere is composed of.
    `Wikipedia Adiabatic Index Explanation <https://en.wikipedia.org/wiki/Heat_capacity_ratio>`_.

.. attribute:: Atmosphere:ADBIDX

    :type: :ref:`scalar <scalar>`
    :access: Get only

    A shorthand alias for :attr:ADIABATICINDEX.

.. method:: Atmosphere:ALTITUDETEMPERATURE(altitude)

    :parameter: altitude (:ref:`scalar <scalar>`) the altitude to query temperature at.
    :access: Get only

    Returns an approximate atmosphere temperature on this world at the given altitude.
    Note that this is only approximate because the temperature will vary depending
    on the sun position in the sky (i.e. your latitude and what time of day it is).

.. method:: Atmosphere:ALTTEMP(altitude)

   A shorthand alias for :meth:ALTITUDETEMPERATURE(altitude).

Deprecated Suffix
-----------------

.. attribute:: Atmosphere:SCALE

    :type: :ref:`scalar <scalar>`
    :access: Get only

    A math constant plugged into a formula to find atmosphere density.

    .. note::

        .. deprecated:: 0.17.2

           Removed to account for significant changes to planetary atmosphere mechanics introduced in KSP 1.0
