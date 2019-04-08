.. _IR:

Infernal Robotics
=================

.. warning::
    The Infernal Robotics mod has been migrating to Infernal Robotics Next. kOS
    includes support for IR Next only, and no longer supports re-compiled
    versions of the original IR.  The links to the original mod forum thread and
    downloads remain in this file for historical context and reference.

- Infernal Robotics Next download: https://github.com/meirumeiru/InfernalRobotics/releases
- Infernal Robotics Next forum thread: https://forum.kerbalspaceprogram.com/index.php?/topic/170898--/
- Historical download: http://kerbal.curseforge.com/ksp-mods/220267
- Historical alternative download: https://github.com/MagicSmokeIndustries/InfernalRobotics/releases
- Historical Forum thread, including full instructions: http://forum.kerbalspaceprogram.com/index.php?/topic/104535--/

Infernal Robotics might not be installed on your copy of the game.
Your script can test whether or not it's installed by using
the boolean expression ``addons:available("ir")``.

.. warning::
    At the time of this writing, IR Next officially supports KSP 1.3.1 and 1.4.2
    with the beta 3 patch 7 "pre-release".  The addon has been tested using
    this 1.4.2 zip file running on KSP 1.6.1 with no errors reported.  While
    using this addon, please keep in mind that IR Next may have underlying
    issues due to KSP version mismatch, and that newer releases to IR Next may
    cause this addon to break in unexpected ways.

Infernal Robotics introduces robotics parts to the game, letting you create moving or spinning contraptions that just aren't possible under stock KSP.

.. figure:: http://i.imgur.com/O94LBvF.png

Access structure IRAddon via ``ADDONS:IR``.

.. structure:: IRAddon

    ===================================== ==================================== =============
     Suffix                                Type                                 Description
    ===================================== ==================================== =============
     :attr:`AVAILABLE`                     :ref:`boolean <boolean>` (readonly)  Returns True if mod Infernal Robotics is installed, available to KOS and applicable to current craft.  It is better to use ``addons:available("rt")``.
     :attr:`GROUPS`                        List of :struct:`IRControlGroup`     Lists all  Servo Groups for the Vessel on which CPU runs this command (see details below).
     :attr:`ALLSERVOS`                     List of :struct:`IRServo`            Lists all  Servos for the Vessel on which CPU runs this command (see details below).
     :meth:`PARTSERVOS(Part)`              List of :struct:`IRServo`            Lists all Servos for the provided part
    ===================================== ==================================== =============



.. attribute:: IRAddon:AVAILABLE

    :type: :struct:`Boolean`
    :access: Get only

    It is better to first call ``ADDONS:AVAILABLE("IR")`` to find out if the
    plugin exists.

    Returns True if mod Infernal Robotics is installed, available to KOS and applicable to current craft.
    Example of use::

        if ADDONS:IR:AVAILABLE
        {
            //some IR dependent code
        }

.. attribute:: IRAddon:GROUPS

    :type: :struct:`List` of :struct:`IRControlGroup` objects
    :access: Get only

    Lists all Servo Groups for the Vessel on which the script is being executed.
    Example of use::

        for g in ADDONS:IR:GROUPS
        {
            Print g:NAME + " contains " + g:SERVOS:LENGTH + " servos".
        }


.. attribute:: IRAddon:ALLSERVOS

    :type: :struct:`List` of :struct:`IRServo` objects
    :access: Get only

    Lists all Servos for the Vessel on which the script is being executed.
    Example of use::

        for s in ADDONS:IR:ALLSERVOS
        {
            print "Name: " + s:NAME + ", position: " + s:POSITION.
        }

.. method:: IRAddon:PARTSERVOS(part)

    :parameter part: :struct:`Part` for which to return servos
    :type: :struct:`List` of :struct:`IRServo` objects

    Lists all Servos found on the given :struct:`Part`.


.. structure:: IRControlGroup

    ===================================== ========================= =============
     Suffix                                Type                      Description
    ===================================== ========================= =============
     :attr:`NAME`                          :ref:`string <string>`    Name of the Control Group
     :attr:`SPEED`                         :ref:`scalar <scalar>`    Speed multiplier set in the IR UI
     :attr:`EXPANDED`                      :ref:`Boolean <boolean>`  True if Group is expanded in IR UI
     :attr:`FORWARDKEY`                    :ref:`string <string>`    Key assigned to forward movement
     :attr:`REVERSEKEY`                    :ref:`string <string>`    Key assigned to reverse movement
     :attr:`SERVOS`                        List (readonly)           List of servos in the group
     :attr:`VESSEL`                        :struct:`Vessel`          Vessel object, owning this servo group. Readonly.

     :meth:`MOVERIGHT()`                   void                      Commands servos in the group to move in positive direction
     :meth:`MOVELEFT()`                    void                      Commands servos in the group to move in negative direction
     :meth:`MOVECENTER()`                  void                      Commands servos in the group to move to default position
     :meth:`MOVENEXTPRESET()`              void                      Commands servos in the group to move to next preset
     :meth:`MOVEPREVPRESET()`              void                      Commands servos in the group to move to previous preset
     :meth:`STOP()`                        void                      Commands servos in the group to stop
    ===================================== ========================= =============

.. attribute:: IRControlGroup:NAME

    :type: :ref:`string <string>`
    :access: Get/Set

    Name of the Control Group (cannot be empty).

.. attribute:: IRControlGroup:SPEED

    :type: :ref:`scalar <scalar>`
    :access: Get/Set

    Speed multiplier as set in the IR user interface. Avoid setting it to 0.

.. attribute:: IRControlGroup:EXPANDED

    :type: :ref:`Boolean <boolean>`
    :access: Get/Set

    True if Group is expanded in IR UI

.. attribute:: IRControlGroup:FORWARDKEY

    :type: :ref:`string <string>`
    :access: Get/Set

    Key assigned to forward movement. Can be empty.

.. attribute:: IRControlGroup:REVERSEKEY

    :type: :ref:`string <string>`
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

    Returns a Vessel that owns this ServoGroup

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

    ===================================== ==================================== =============
     Suffix                                Type                                 Description
    ===================================== ==================================== =============
     :attr:`NAME`                          :ref:`string <string>`               Name of the Servo
     :attr:`UID`                           :ref:`scalar <scalar>` (int)         Unique ID of the servo part (part.flightID).
     :attr:`HIGHLIGHT`                     :ref:`Boolean <boolean>` (set-only)  Set Hightlight status of the part.
     :attr:`POSITION`                      :ref:`scalar <scalar>` (readonly)    Current position of the servo.
     :attr:`MINCFGPOSITION`                :ref:`scalar <scalar>` (readonly)    Minimum position for servo as defined by part creator in part.cfg
     :attr:`MAXCFGPOSITION`                :ref:`scalar <scalar>` (readonly)    Maximum position for servo as defined by part creator in part.cfg
     :attr:`MINPOSITION`                   :ref:`scalar <scalar>`               Minimum position for servo, from tweakable.
     :attr:`MAXPOSITION`                   :ref:`scalar <scalar>`               Maximum position for servo, from tweakable.
     :attr:`CONFIGSPEED`                   :ref:`scalar <scalar>` (readonly)    Servo movement speed as defined by part creator in part.cfg
     :attr:`SPEED`                         :ref:`scalar <scalar>`               Servo speed multiplier, from tweakable.
     :attr:`CURRENTSPEED`                  :ref:`scalar <scalar>` (readonly)    Current Servo speed.
     :attr:`ACCELERATION`                  :ref:`scalar <scalar>`               Servo acceleration multiplier, from tweakable.
     :attr:`ISMOVING`                      :ref:`Boolean <boolean>` (readonly)  True if Servo is moving
     :attr:`ISFREEMOVING`                  :ref:`Boolean <boolean>` (readonly)  True if Servo is uncontrollable (ex. docking washer)
     :attr:`LOCKED`                        :ref:`Boolean <boolean>`             Servo's locked status, set true to lock servo.
     :attr:`INVERTED`                      :ref:`Boolean <boolean>`             Servo's inverted status, set true to invert servo's axis.
     :attr:`PART`                          :struct:`Part`                       A reference to a Part containing servo module.

     :meth:`MOVERIGHT()`                   void                                 Commands servo to move in positive direction
     :meth:`MOVELEFT()`                    void                                 Commands servo to move in negative direction
     :meth:`MOVECENTER()`                  void                                 Commands servo to move to default position
     :meth:`MOVENEXTPRESET()`              void                                 Commands servo to move to next preset
     :meth:`MOVEPREVPRESET()`              void                                 Commands servo to move to previous preset
     :meth:`STOP()`                        void                                 Commands servo to stop
     :meth:`MOVETO(position, speedMult)`   void                                 Commands servo to move to `position` with `speedMult` multiplier
    ===================================== ==================================== =============

.. attribute:: IRServo:NAME

    :type: :ref:`string <string>`
    :access: Get/Set

    Name of the Control Group (cannot be empty).

.. attribute:: IRServo:UID

    :type: :ref:`scalar <scalar>`
    :access: Get

    Unique ID of the servo part (part.flightID).

.. attribute:: IRServo:HIGHLIGHT

    :type: :ref:`Boolean <boolean>`
    :access: Set

    Set Hightlight status of the part.

.. attribute:: IRServo:POSITION

    :type: :ref:`scalar <scalar>`
    :access: Get

    Current position of the servo.

.. attribute:: IRServo:MINCFGPOSITION

    :type: :ref:`scalar <scalar>`
    :access: Get

    Minimum position for servo as defined by part creator in part.cfg

.. attribute:: IRServo:MAXCFGPOSITION

    :type: :ref:`scalar <scalar>`
    :access: Get

    Maximum position for servo as defined by part creator in part.cfg

.. attribute:: IRServo:MINPOSITION

    :type: :ref:`scalar <scalar>`
    :access: Get/Set

    Minimum position for servo, from tweakable.

.. attribute:: IRServo:MAXPOSITION

    :type: :ref:`scalar <scalar>`
    :access: Get/Set

    Maximum position for servo, from tweakable.

.. attribute:: IRServo:CONFIGSPEED

    :type: :ref:`scalar <scalar>`
    :access: Get

    Servo movement speed as defined by part creator in part.cfg

.. attribute:: IRServo:SPEED

    :type: :ref:`scalar <scalar>`
    :access: Get/Set

    Servo speed multiplier, from tweakable.

.. attribute:: IRServo:CURRENTSPEED

    :type: :ref:`scalar <scalar>`
    :access: Get

    Current Servo speed.

.. attribute:: IRServo:ACCELERATION

    :type: :ref:`scalar <scalar>`
    :access: Get/Set

    Servo acceleration multiplier, from tweakable.

.. attribute:: IRServo:ISMOVING

    :type: :ref:`Boolean <boolean>`
    :access: Get

    True if Servo is moving

.. attribute:: IRServo:ISFREEMOVING

    :type: :ref:`Boolean <boolean>`
    :access: Get

    True if Servo is uncontrollable (ex. docking washer)

.. attribute:: IRServo:LOCKED

    :type: :ref:`Boolean <boolean>`
    :access: Get/Set

    Servo's locked status, set true to lock servo.

.. attribute:: IRServo:INVERTED

    :type: :ref:`Boolean <boolean>`
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
