.. _gimbal:

Gimbal
======

Many engines in KSP have thrust vectoring gimbals and in ksp they are their own module


.. structure:: Gimbal

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`RANGE`
          - scalar
          - The Gimbal's Possible Range of movement

        * - :attr:`RESPONSESPEED`
          - scalar
          - The Gimbal's Possible Rate of travel

        * - :attr:`PITCHANGLE`
          - scalar
          - Current Gimbal Pitch 
		  
        * - :attr:`YAWANGLE`
          - scalar
          - Current Gimbal Yaw 
		  
        * - :attr:`ROLLANGLE`
          - scalar
          - Current Gimbal Roll 
		  
        * - :attr:`LOCK`
          - boolean
          - Is the gimbal free to travel? 
		  
.. attribute:: Gimbal:RANGE

    :type: scalar
    :access: Get only

    The maximum extent of travel possible for the gimbal along all 3 axis (Pitch, Yaw, Roll) 

.. attribute:: Gimbal:RESPONSESPEED

    :type: scalar
    :access: Get only

    A Measure of the rate of travel for the gimbal

.. attribute:: Gimbal:PITCHANGLE

    :type: scalar
    :access: Get only

    The gimbals current pitch, has a range of -1 to 1. Will always be 0 when LOCK is true

.. attribute:: Gimbal:YAWANGLE

    :type: scalar
    :access: Get only

    The gimbals current yaw, has a range of -1 to 1. Will always be 0 when LOCK is true

.. attribute:: Gimbal:ROLLANGLE

    :type: scalar
    :access: Get only

    The gimbals current roll, has a range of -1 to 1. Will always be 0 when LOCK is true

.. attribute:: Gimbal:LOCK

    :type: string
    :access: Get/Set
        
    Can this Gimbal produce torque right now, when you set it to false it will snap the engine back to 0s for pitch,yaw and roll

