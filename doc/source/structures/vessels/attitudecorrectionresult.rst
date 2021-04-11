.. _attitudecorrectionresult:

Attitude Correction Result
======

When you perform a control action on a ship (yaw, pitch, roll, fore, top, star, throttle) this always has two effects on the ship. Part of the impulse will be be imparted as rotation and part of it will be translation.

.. structure:: AttitudeCorrectionResult

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 2

        * - Suffix
          - Type (units)
          - Description

        * - :attr:`TORQUE`
          - :struct:`Vector <Vector>`
          - The torque vector (pitch, roll, yaw).
        * - :attr:`TRANSLATION`
          - :struct:`Vector <Vector>`
          - The translation force (fore, top, star)
