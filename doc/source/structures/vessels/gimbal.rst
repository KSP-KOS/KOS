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
          - :ref:`Boolean <boolean>`
          - Is the Gimbal locked in neutral position? 
          
        * - :attr:`LIMIT`
          - :ref:`scalar <scalar>` (%)
          - Percentage of the maximum range the Gimbal is allowed to travel 

        * - :attr:`RANGE`
          - :ref:`scalar <scalar>` (deg)
          - The Gimbal's Possible Range of movement

        * - :attr:`RESPONSESPEED`
          - :ref:`scalar <scalar>`
          - The Gimbal's Possible Rate of travel

        * - :attr:`PITCHANGLE`
          - :ref:`scalar <scalar>`
          - Current Gimbal Pitch 
		  
        * - :attr:`YAWANGLE`
          - :ref:`scalar <scalar>`
          - Current Gimbal Yaw 
		  
        * - :attr:`ROLLANGLE`
          - :ref:`scalar <scalar>`
          - Current Gimbal Roll 


.. note::

    :struct:`Gimbal` is a type of :struct:`PartModule`, and therefore can use all the suffixes of :struct:`PartModule`. Shown below are only the suffixes that are unique to :struct:`Gimbal`.
    :struct:`Gimbal` can be accessed as :attr:`Engine:GIMBAL` atribute of  :struct:`Engine`.

.. attribute:: Gimbal:LOCK

    :type: :ref:`Boolean <boolean>`
    :access: Get/Set
        
    Is this gimbal locked to neutral position and not responding to steering controls right now? When you set it to true it will snap the engine back to 0s for pitch, yaw and roll

.. attribute:: Gimbal:LIMIT

    :type: :ref:`scalar <scalar>` (%)
    :access: Get/Set
        
    Percentage of maximum range this gimbal is allowed to travel

.. attribute:: Gimbal:RANGE

    :type: :ref:`scalar <scalar>` (deg)
    :access: Get only

    The maximum extent of travel possible for the gimbal along all 3 axis (Pitch, Yaw, Roll) 

.. attribute:: Gimbal:RESPONSESPEED

    :type: :ref:`scalar <scalar>`
    :access: Get only

    A Measure of the rate of travel for the gimbal

.. attribute:: Gimbal:PITCHANGLE

    :type: :ref:`scalar <scalar>`
    :access: Get only

    The gimbals current pitch, has a range of -1 to 1. Will always be 0 when LOCK is true

.. attribute:: Gimbal:YAWANGLE

    :type: :ref:`scalar <scalar>`
    :access: Get only

    The gimbals current yaw, has a range of -1 to 1. Will always be 0 when LOCK is true

.. attribute:: Gimbal:ROLLANGLE

    :type: :ref:`scalar <scalar>`
    :access: Get only

    The gimbals current roll, has a range of -1 to 1. Will always be 0 when LOCK is true

