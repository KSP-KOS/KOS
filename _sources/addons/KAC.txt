.. _KAC:

Kerbal Alarm Clock
==================

- Download: https://github.com/TriggerAu/KerbalAlarmClock/releases
- Alternative download https://kerbalstuff.com/mod/231/Kerbal%20Alarm%20Clock
- Forum thread, including full instructions: http://forum.kerbalspaceprogram.com/threads/24786

You can find out if Kerbal Alarm Clock addon is available in the
current game installation by usng the boolean expression
``addons:available("KAC")``.

Note that due to changes in Kerbal Alarm Clock, kOS can no longer support
versions of KAC that are older than 3.0.0.2.  The API that Kerbal
Alarm Clock publishes for other mods to use changed such that
kOS can ether support newer Kerbal Alarm Clock, or older Kerbal Alarm
Clock, but not both.

The Kerbal Alarm Clock is a plugin that allows you to create reminder alarms at future periods to help you manage your flights and not warp past important times.

.. figure:: http://triggerau.github.io/KerbalAlarmClock/images/KACForumPic.png

Creator of the KAC provides API for integration with other mods. In KOS we provide limited access to KAC alarms via following structure and functions.

Access structure KACAddon via ``ADDONS:KAC``.

.. structure:: KACAddon

    ===================================== ========================= =============
     Suffix                                Type                      Description
    ===================================== ========================= =============
     :attr:`AVAILABLE`                     bool(readonly)            True if KAC is installed and KAC integration enabled.  It is better to use ``addons:available("KAC")`` for this purpose.
     :meth:`ALARMS()`                      List                      List all alarms
    ===================================== ========================= =============



.. attribute:: KACAddon:AVAILABLE

    :type: bool
    :access: Get only

    It is better to use ``ADDONS:AVAILABLE("KAC")`` first to discover if
    KAC is installed.

    True if KAC is installed and KAC integration enabled.
    Example of use::

        if ADDONS:KAC:AVAILABLE
        {
            //some KAC dependent code
        }

.. method:: KACAddon:ALARMS()

    :return: List of :struct:`KACAlarm` objects

    List **all** the alarms set up in Kerbal Alarm Clock. Example of use::

        for i in ADDONS:KAC:ALARMS
        {
        	print i:NAME + " - " + i:REMAINING + " - " + i:TYPE+ " - " + i:ACTION.
        }



.. structure:: KACAlarm

    ===================================== ============================ =============
     Suffix                                Type                         Description
    ===================================== ============================ =============
     :attr:`ID`                            :struct:`string` (readonly)   Unique identifier
     :attr:`NAME`                          :struct:`string`              Name of the alarm
     :attr:`ACTION`                        :struct:`string`              What should the Alarm Clock do when the alarm fires
     :attr:`TYPE`                          :struct:`string` (readonly)   What type of Alarm is this - affects icon displayed and some calc options
     :attr:`NOTES`                         :struct:`string`              Long description of the alarm (optional)
     :attr:`REMAINING`                     :struct:`scalar` (s)          Time remaining until alarm is triggered
     :attr:`REPEAT`                        :struct:`boolean`             Should the alarm be repeated once it fires
     :attr:`REPEATPERIOD`                  :struct:`scalar` (s)          How long after the alarm fires should the next alarm be set up
     :attr:`ORIGINBODY`                    :struct:`string`              Name of the body the vessel is departing from
     :attr:`TARGETBODY`                    :struct:`string`              Name of the body the vessel is arriving at
    ===================================== ============================ =============

.. attribute:: KACAlarm:ID

    :type: :ref:`string <string>`
    :access: Get only

    Unique identifier of the alarm.

.. attribute:: KACAlarm:NAME

    :type: :ref:`string <string>`
    :access: Get/Set

    Name of the alarm. Displayed in main KAC window.

.. attribute:: KACAlarm:ACTION

    :type: :ref:`string <string>`
    :access: Get/Set

    Should be one of the following

      * `MessageOnly` - Message Only-No Affect on warp
      * `KillWarpOnly` - Kill Warp Only-No Message
      * `KillWarp` - Kill Warp and Message
      * `PauseGame` - Pause Game and Message

    If set incorrectly will log a warning in Debug log and revert to previous or default value.

.. attribute:: KACAlarm:TYPE

    :type: :ref:`string <string>`
    :access: Get only

    Can only be set at Alarm creation.
    Could be one of the following as per API

    	* Raw (default)
        * Maneuver
        * ManeuverAuto
        * Apoapsis
        * Periapsis
        * AscendingNode
        * DescendingNode
        * LaunchRendevous
        * Closest
        * SOIChange
        * SOIChangeAuto
        * Transfer
        * TransferModelled
        * Distance
        * Crew
        * EarthTime

    **Warning**: Unless you are 100% certain you know what you're doing, create only "Raw" AlarmTypes to avoid unnecessary complications.

.. attribute:: KACAlarm:NOTES

    :type: :ref:`string <string>`
    :access: Get/Set

    Long description of the alarm. Can be seen when alarm pops or by double-clicking alarm in UI.

    **Warning**: This field may be reserved in the future version of KAC-KOS integration for automated script execution upon triggering of the alarm.

.. attribute:: KACAlarm:REMAINING

    :type: :ref:`scalar <scalar>`
    :access: Get only

    Time remaining until alarm is triggered.

.. attribute:: KACAlarm:REPEAT

    :type: :ref:`boolean <boolean>`
    :access: Get/Set

    Should the alarm be repeated once it fires.

.. attribute:: KACAlarm:REPEATPERIOD

    :type: :ref:`scalar <scalar>`
    :access: Get/Set

    How long after the alarm fires should the next alarm be set up.

.. attribute:: KACAlarm:ORIGINBODY

    :type: :ref:`string <string>`
    :access: Get/Set

    Name of the body the vessel is departing from.

.. attribute:: KACAlarm:TARGETBODY

    :type: :ref:`string <string>`
    :access: Get/Set

    Name of the body the vessel is arriving to.



Available Functions
-------------------

============================================= ===================================================
 Function                                      Description
============================================= ===================================================
 :func:`ADDALARM(AlarmType, UT, Name, Notes)`  Create new alarm of AlarmType at UT
 :func:`LISTALARMS(alarmType)`                 List alarms with type `alarmType`.
 :func:`DELETEALARM(alarmID)`                  Delete alarm with ID = alarmID
============================================= ===================================================

.. function:: ADDALARM(AlarmType, UT, Name, Notes)

    Creates alarm of type `KACAlarm:ALARMTYPE` at `UT` with `Name` and `Notes` attributes set. Attaches alarm to current :ref:`CPU Vessel <cpu vessel>`.  Returns :struct:`KACAlarm` object if creation was successful and empty string otherwise::

        set na to addAlarm("Raw",time:seconds+300, "Test", "Notes").
        print na:NAME. //prints 'Test'
        set na:NOTES to "New Description".
        print na:NOTES. //prints 'New Description'

.. function:: LISTALARMS(alarmType)

    If `alarmType` equals "All", returns :struct:`List` of *all* :struct:`KACAlarm` objects attached to current vessel or have no vessel attached.
    Otherwise returns :struct:`List` of all :struct:`KACAlarm` objects with `KACAlarm:TYPE` equeal to `alarmType` and attached to current vessel or have no vessel attached.::

        set al to listAlarms("All").
        for i in al
        {
            print i:ID + " - " + i:name.
        }

.. function:: DELETEALARM(alarmID)

    Deletes alarm with ID equal to alarmID. Returns True if successful, false otherwise::

        set na to addAlarm("Raw",time:seconds+300, "Test", "Notes").
        if (DELETEALARM(na:ID))
        {
            print "Alarm Deleted".
        }
