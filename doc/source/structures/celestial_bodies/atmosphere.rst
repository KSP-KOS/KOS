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
        * - :attr:`SCALE`
          - :ref:`scalar <scalar>`
          - Used to find atmospheric density
        * - :attr:`SEALEVELPRESSURE`
          - :ref:`scalar <scalar>` (atm)
          - pressure at sea level
        * - :attr:`HEIGHT`
          - :ref:`scalar <scalar>` (m)
          - advertised atmospheric height


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
    
.. attribute:: Atmosphere:SCALE

    :type: :ref:`scalar <scalar>`
    :access: Get only

    A math constant plugged into a formula to find atmosphere density.
    
.. attribute:: Atmosphere:SEALEVELPRESSURE

    :type: :ref:`scalar <scalar>` (atm)
    :access: Get only

    Number of Atm's at planet's sea level 1.0 Atm's = same as Kerbin.
    
.. attribute:: Atmosphere:HEIGHT

    :type: :ref:`scalar <scalar>` (m)
    :access: Get only

    The altitude at which the atmosphere is "officially" advertised as ending. (actual ending value differs, see below).   

Atmospheric Math
----------------

.. note::

   **[Section deleted]**

   This documentation used to contain a description of how the math for
   Kerbal Space Program's default stock atmospheric model works, but
   everything that was mentioned here became utterly false when KSP 1.0
   was released with a brand new atmospheric model that invalided pretty
   much everything that was said here.  Rather than teach people incorrect
   information, it was deemed that no documentation is better than misleading
   documentation, so this section below this point has been removed.

