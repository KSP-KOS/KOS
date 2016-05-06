.. _gimbal:

Gimbal
======

Many engines in KSP have thrust vectoring gimbals which are handled by their own module


.. structure:: Gimbal

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type (units)
          - Description

        * - All suffixes of :struct:`PartModule`
          -
          -

        * - :attr:`LOCK`
          - :struct:`Boolean`
          - Is the Gimbal locked in neutral position?

        * - :attr:`PITCH`
          - :struct:`Boolean`
          - Does the Gimbal respond to pitch controls?

        * - :attr:`YAW`
          - :struct:`Boolean`
          - Does the Gimbal respond to yaw controls?

        * - :attr:`ROLL`
          - :struct:`Boolean`
          - Does the Gimbal respond to roll controls?

        * - :attr:`LIMIT`
          - :struct:`Scalar` (%)
          - Percentage of the maximum range the Gimbal is allowed to travel

        * - :attr:`RANGE`
          - :struct:`Scalar` (deg)
          - The Gimbal's Possible Range of movement

        * - :attr:`RESPONSESPEED`
          - :struct:`Scalar`
          - The Gimbal's Possible Rate of travel

        * - :attr:`PITCHANGLE`
          - :struct:`Scalar`
          - Current Gimbal Pitch

        * - :attr:`YAWANGLE`
          - :struct:`Scalar`
          - Current Gimbal Yaw

        * - :attr:`ROLLANGLE`
          - :struct:`Scalar`
          - Current Gimbal Roll


.. note::

    :struct:`Gimbal` is a type of :struct:`PartModule`, and therefore can use all the suffixes of :struct:`PartModule`. Shown below are only the suffixes that are unique to :struct:`Gimbal`.
    :struct:`Gimbal` can be accessed as :attr:`Engine:GIMBAL` atribute of  :struct:`Engine`.

.. attribute:: Gimbal:LOCK

    :type: :struct:`Boolean`
    :access: Get/Set

    Is this gimbal locked to neutral position and not responding to steering controls right now? When you set it to true it will snap the engine back to 0s for pitch, yaw and roll

.. attribute:: Gimbal:PITCH

    :type: :struct:`Boolean`
    :access: Get/Set

    Is the gimbal responding to pitch controls? Relevant only if the gimbal is not locked.

.. attribute:: Gimbal:YAW

    :type: :struct:`Boolean`
    :access: Get/Set

    Is the gimbal responding to yaw controls? Relevant only if the gimbal is not locked.

.. attribute:: Gimbal:ROLL

    :type: :struct:`Boolean`
    :access: Get/Set

    Is the gimbal responding to roll controls? Relevant only if the gimbal is not locked.

.. attribute:: Gimbal:LIMIT

    :type: :struct:`Scalar` (%)
    :access: Get/Set

    Percentage of maximum range this gimbal is allowed to travel

.. attribute:: Gimbal:RANGE

    :type: :struct:`Scalar` (deg)
    :access: Get only

    The maximum extent of travel possible for the gimbal along all 3 axis (Pitch, Yaw, Roll)

.. attribute:: Gimbal:RESPONSESPEED

    :type: :struct:`Scalar`
    :access: Get only

    A Measure of the rate of travel for the gimbal

.. attribute:: Gimbal:PITCHANGLE

    :type: :struct:`Scalar`
    :access: Get only

    The gimbals current pitch, has a range of -1 to 1. Will always be 0 when LOCK is true

.. attribute:: Gimbal:YAWANGLE

    :type: :struct:`Scalar`
    :access: Get only

    The gimbals current yaw, has a range of -1 to 1. Will always be 0 when LOCK is true

.. attribute:: Gimbal:ROLLANGLE

    :type: :struct:`Scalar`
    :access: Get only

    The gimbals current roll, has a range of -1 to 1. Will always be 0 when LOCK is true
