.. _IR:

Infernal Robotics
=================

- Download: http://kerbal.curseforge.com/ksp-mods/220267
- Alternative download: https://kerbalstuff.com/mod/8/Magic_Smoke_Industries_Infernal_Robotics
- Forum thread, including full instructions: http://forum.kerbalspaceprogram.com/threads/116064

Infernal Robotics introduces robotics parts to the game, letting you create moving or spinning contraptions that just aren't possible under stock KSP.
.. figure:: http://i.imgur.com/O94LBvF.png

Starting version 0.20 of the Infernal Robotics, mod creators introduced API to for easier access to robotic features.

Access structure IRAddon via `ADDONS:IR`.

.. structure:: IRAddon

    ===================================== ========================= =============
     Suffix                                Type                      Description
    ===================================== ========================= =============
     :attr:`AVAILABLE`                     bool(readonly)            Returns True if mod Infernal Robotics is installed, available to KOS and applicable to current craft.
     :attr:`GROUPS`                        List (readonly)           Lists all Servo Groups for the Vessel on which CPU runs this command (see details below).
     :attr:`ALLSERVOS`                     List (readonly)           Lists all Servos for the Vessel on which CPU runs this command (see details below).
    ===================================== ========================= =============



.. attribute:: IRAddon:AVAILABLE

    :type: bool
    :access: Get only

    Returns True if mod Infernal Robotics is installed, available to KOS and applicable to current craft.
    Example of use::

        if ADDONS:IR:AVAILABLE
        {
            //some IR dependent code
        }

.. attribute:: IRAddon:GROUPS

    :type: List of :struct:`IRControlGroup` objects
    :access: Get only

    Lists all Servo Groups for the Vessel on which the script is being executed. On IR versions prior to 0.21.5 will always return servo groups for current focused vessel.
    Example of use::

        for g in ADDONS:IR:GROUPS
        {
            Print g:NAME + " contains " + g:SERVOS:LENGTH + " servos".
        }


.. attribute:: IRAddon:ALLSERVOS

    :type: List of :struct:`IRServo` objects
    :access: Get only

    Lists all Servos for the Vessel on which the script is being executed. On IR versions prior to 0.21.5 will always return servos for current focused vessel.
    Example of use::

        for s in ADDONS:IR:ALLSERVOS
        {
            print "Name: " + s:NAME + ", position: " + s:POSITION.
        }


.. structure:: IRControlGroup

    ===================================== ========================= =============
     Suffix                                Type                      Description
    ===================================== ========================= =============
     :attr:`NAME`                          string                    Name of the Control Group
     :attr:`SPEED`                         float                     Speed multiplier set in the IR UI
     :attr:`EXPANDED`                      bool                      True if Group is expanded in IR UI
     :attr:`FORWARDKEY`                    string                    Key assigned to forward movement
     :attr:`REVERSEKEY`                    string                    Key assigned to reverse movement
     :attr:`SERVOS`                        List (readonly)           List of servos in the group
     :attr:`VESSEL`                        :struct:`Vessel`          Vessel object, owning this servo group. Readonly, requires IR version 0.21.5 or later.

     :meth:`MOVERIGHT()`                   void                      Commands servos in the group to move in positive direction
     :meth:`MOVELEFT()`                    void                      Commands servos in the group to move in negative direction
     :meth:`MOVECENTER()`                  void                      Commands servos in the group to move to default position
     :meth:`MOVENEXTPRESET()`              void                      Commands servos in the group to move to next preset
     :meth:`MOVEPREVPRESET()`              void                      Commands servos in the group to move to previous preset
     :meth:`STOP()`                        void                      Commands servos in the group to stop
    ===================================== ========================= =============

.. attribute:: IRControlGroup:NAME

    :type: string
    :access: Get/Set

    Name of the Control Group (cannot be empty).

.. attribute:: IRControlGroup:SPEED

    :type: float
    :access: Get/Set

    Speed multiplier as set in the IR user interface. Avoid setting it to 0.

.. attribute:: IRControlGroup:EXPANDED

    :type: bool
    :access: Get/Set

    True if Group is expanded in IR UI

.. attribute:: IRControlGroup:FORWARDKEY

    :type: string
    :access: Get/Set

    Key assigned to forward movement. Can be empty.

.. attribute:: IRControlGroup:REVERSEKEY

    :type: string
    :access: Get/Set

    Key assigned to reverse movement. Can be empty.

.. attribute:: IRControlGroup:SERVOS

    :type: List of :struct:`IRServo` objects
    :access: Get only

    Lists Servos in the Group. Example of use::

        for g in ADDONS:IR:GROUPS
        {
            Print g:NAME + " contains " + g:SERVOS:LENGTH + " servos:".
            for s in g:servos
            {
                print "    " + s:NAME + ", position: " + s:POSITION.
            }
        }

.. attribute:: IRControlGroup:VESSEL

    :type: :struct:`Vessel`
    :access: Get only

    If IR 0.21.5 or later is installed will return a Vessel that owns this ServoGroup, otherwise will return current focused Vessel

.. method:: IRControlGroup:MOVERIGHT()

    :return: void

    Commands servos in the group to move in positive direction.

.. method:: IRControlGroup:MOVELEFT()

    :return: void

    Commands servos in the group to move in negative direction.

.. method:: IRControlGroup:MOVECENTER()

    :return: void

    Commands servos in the group to move to default position.

.. method:: IRControlGroup:MOVENEXTPRESET()

    :return: void

    Commands servos in the group to move to next preset

.. method:: IRControlGroup:MOVEPREVPRESET()

    :return: void

    Commands servos in the group to move to previous preset

.. method:: IRControlGroup:STOP()

    :return: void

    Commands servos in the group to stop


.. structure:: IRServo

    ===================================== ========================= =============
     Suffix                                Type                      Description
    ===================================== ========================= =============
     :attr:`NAME`                          string                    Name of the Servo
     :attr:`UID`                           int                       Unique ID of the servo part (part.flightID).
     :attr:`HIGHLIGHT`                     bool (set-only)           Set Hightlight status of the part.
     :attr:`POSITION`                      float (readonly)          Current position of the servo.
     :attr:`MINCFGPOSITION`                float (readonly)          Minimum position for servo as defined by part creator in part.cfg
     :attr:`MAXCFGPOSITION`                float (readonly)          Maximum position for servo as defined by part creator in part.cfg
     :attr:`MINPOSITION`                   float                     Minimum position for servo, from tweakable.
     :attr:`MAXPOSITION`                   float                     Maximum position for servo, from tweakable.
     :attr:`CONFIGSPEED`                   float (readonly)          Servo movement speed as defined by part creator in part.cfg
     :attr:`SPEED`                         float                     Servo speed multiplier, from tweakable.
     :attr:`CURRENTSPEED`                  float (readonly)          Current Servo speed.
     :attr:`ACCELERATION`                  float                     Servo acceleration multiplier, from tweakable.
     :attr:`ISMOVING`                      bool (readonly)           True if Servo is moving
     :attr:`ISFREEMOVING`                  bool (readonly)           True if Servo is uncontrollable (ex. docking washer)
     :attr:`LOCKED`                        bool                      Servo's locked status, set true to lock servo.
     :attr:`INVERTED`                      bool                      Servo's inverted status, set true to invert servo's axis.
     :attr:`PART`                          :struct:`Part`            A reference to a Part containing servo module.

     :meth:`MOVERIGHT()`                   void                      Commands servo to move in positive direction
     :meth:`MOVELEFT()`                    void                      Commands servo to move in negative direction
     :meth:`MOVECENTER()`                  void                      Commands servo to move to default position
     :meth:`MOVENEXTPRESET()`              void                      Commands servo to move to next preset
     :meth:`MOVEPREVPRESET()`              void                      Commands servo to move to previous preset
     :meth:`STOP()`                        void                      Commands servo to stop
     :meth:`MOVETO(position, speedMult)`   void                      Commands servo to move to `position` with `speedMult` multiplier
    ===================================== ========================= =============

.. attribute:: IRServo:NAME

    :type: string
    :access: Get/Set

    Name of the Control Group (cannot be empty).

.. attribute:: IRServo:UID

    :type: int
    :access: Get

    Unique ID of the servo part (part.flightID).

.. attribute:: IRServo:HIGHLIGHT

    :type: bool
    :access: Set

    Set Hightlight status of the part.

.. attribute:: IRServo:POSITION

    :type: float
    :access: Get

    Current position of the servo.

.. attribute:: IRServo:MINCFGPOSITION

    :type: float
    :access: Get

    Minimum position for servo as defined by part creator in part.cfg

.. attribute:: IRServo:MAXCFGPOSITION

    :type: float
    :access: Get

    Maximum position for servo as defined by part creator in part.cfg

.. attribute:: IRServo:MINPOSITION

    :type: float
    :access: Get/Set

    Minimum position for servo, from tweakable.

.. attribute:: IRServo:MAXPOSITION

    :type: float
    :access: Get/Set

    Maximum position for servo, from tweakable.

.. attribute:: IRServo:CONFIGSPEED

    :type: float
    :access: Get

    Servo movement speed as defined by part creator in part.cfg

.. attribute:: IRServo:SPEED

    :type: float
    :access: Get/Set

    Servo speed multiplier, from tweakable.

.. attribute:: IRServo:CURRENTSPEED

    :type: float
    :access: Get

    Current Servo speed.

.. attribute:: IRServo:ACCELERATION

    :type: float
    :access: Get/Set

    Servo acceleration multiplier, from tweakable.

.. attribute:: IRServo:ISMOVING

    :type: bool
    :access: Get

    True if Servo is moving

.. attribute:: IRServo:ISFREEMOVING

    :type: bool
    :access: Get

    True if Servo is uncontrollable (ex. docking washer)

.. attribute:: IRServo:LOCKED

    :type: bool
    :access: Get/Set

    Servo's locked status, set true to lock servo.

.. attribute:: IRServo:INVERTED

    :type: bool
    :access: Get/Set

    Servo's inverted status, set true to invert servo's axis.

.. attribute:: IRServo:PART

    :type: :struct:`Part`
    :access: Get

    Returns reference to the :struct:`Part` containing servo module. Please note that Part:UID does not equal IRServo:UID.


.. method:: IRServo:MOVERIGHT()

    :return: void

    Commands servo to move in positive direction

.. method:: IRServo:MOVELEFT()

    :return: void

    Commands servo to move in negative direction

.. method:: IRServo:MOVECENTER()

    :return: void

    Commands servo to move to default position

.. method:: IRServo:MOVENEXTPRESET()

    :return: void

    Commands servo to move to next preset

.. method:: IRServo:MOVEPREVPRESET()

    :return: void

    Commands servo to move to previous preset

.. method:: IRServo:STOP()

    :return: void

    Commands servo to stop

.. method:: IRServo:MOVETO(position, speedMult)

    :parameter position: (float) Position to move to
    :parameter speedMult: (float) Speed multiplier
    :return: void

    Commands servo to move to `position` with `speedMult` multiplier.


Example code::

    print "IR Iavailable: " + ADDONS:IR:AVAILABLE.

    Print "Groups:".

    for g in ADDONS:IR:GROUPS
    {
        Print g:NAME + " contains " + g:SERVOS:LENGTH + " servos:".
        for s in g:servos
        {
            print "    " + s:NAME + ", position: " + s:POSITION.
            if (g:NAME = "Hinges" and s:POSITION = 0)
            {
                s:MOVETO(30, 2).
            }
            else if (g:NAME = "Hinges" and s:POSITION > 0)
            {
                s:MOVETO(0, 1).
            }
        }
    }
