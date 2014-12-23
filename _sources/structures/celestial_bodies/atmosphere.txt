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
          - string
          - Name of the celestial body
        * - :attr:`EXISTS`
          - bool
          - True if this body has an atmosphere
        * - :attr:`OXYGEN`
          - bool
          - True if oxygen is present                           
        * - :attr:`SCALE`
          - scalar
          - Used to find atmospheric density
        * - :attr:`SEALEVELPRESSURE`
          - scalar (atm)
          - pressure at sea level
        * - :attr:`HEIGHT`
          - scalar (m)
          - advertised atmospheric height


.. attribute:: Atmosphere:BODY

    :type: string
    :access: Get only

    The Body that this atmosphere is around - as a STRING NAME, not a Body object.
    
.. attribute:: Atmosphere:EXISTS

    :type: bool
    :access: Get only

    True if this atmosphere is "real" and not just a dummy placeholder.
    
.. attribute:: Atmosphere:OXYGEN

    :type: bool
    :access: Get only

    True if the air has oxygen and could therefore be used by a jet engine's intake.
    
.. attribute:: Atmosphere:SCALE

    :type: scalar
    :access: Get only

    A math constant plugged into a formula to find atmosphere density (see below).
    
.. attribute:: Atmosphere:SEALEVELPRESSURE

    :type: scalar (atm)
    :access: Get only

    Number of Atm's at planet's sea level 1.0 Atm's = same as Kerbin.
    
.. attribute:: Atmosphere:HEIGHT

    :type: scalar (m)
    :access: Get only

    The altitude at which the atmosphere is "officially" advertised as ending. (actual ending value differs, see below).   

Atmospheric Math
----------------

The atmospheric effects of a planet's air need to be calculated using some formulas. First off, be aware that atmosphere can be measured three different ways:

Atm
    A multiple of the pressure at Kerbin sea level. An atmosphere of 0.5 is half as much air pressure as at Kerbin's sea level. This is the measure used by :SEALEVELPRESSURE
    
pressure
    A measure of the force the air pushes on a surface with. In SI units, it's Newtons per Square Meter. *This value is almost never used directly in any calculation. Instead you just calculate everything in terms of multiples of Atm's.*

density
    A measure of how much mass of air there is in a volume of space. In SI units, it's Kilograms per Cubic Meter.

.. note::

    **The following only applies to the STOCK KSP atmosphere.**

    If you have installed a mod such as `FAR`_, that changes the atmosphere, then much of what is said below will not apply.

.. _FAR: http://forum.kerbalspaceprogram.com/threads/20451-0-25-Ferram-Aerospace-Research-v0-14-3-2-10-21-14

The level of atmosphere can be calculated for any altitude as follows:

-  Number of **Atm's** = (Atm's at sea level) \* ( e ^ ( -
   sea\_level\_alt / scale ) )

The **TRUE** maximum height of the atmosphere is NOT the value returned by :HEIGHT, but rather it's the altitude at which the number of Atm's returned by the above formula is 0.000001. :HEIGHT is just the value as advertised by the game to the user. On some worlds it can be quite a ways off.

And once you have that number, then density can be calculated from it with this conversion factor:

-  air density = Number of Atm's \* 1.2230948554874.

Further information about the math formulas that Kerbal Space Program uses to calculate the atmosphere `can be found here <http://wiki.kerbalspaceprogram.com/wiki/Atmosphere>`__.

Examples::

    IF SHIP:ORBIT:BODY:ATM:EXISTS {
        SET thisAtmo TO SHIP:ORBIT:BODY:ATM.
        PRINT "The planet you are orbiting has an atmosphere.".
        PRINT "It's scale is " + thisAtmo:SCALE.
        PRINT "It's height is " + thisAtmo:HEIGHT.
        SET atmos TO thisAtmo:SEALEVELPRESSURE + ( CONSTANT():E ^ ( - SHIP:ALTITUDE / thisAtmo:SCALE ) ).
        PRINT "At this altitude the atmosphere is " + atmos + " Atm's.".
    } ELSE {
        PRINT "The planet you are orbiting has no atmosphere.".
    }.

