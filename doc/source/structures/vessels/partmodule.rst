.. partmodule:

PartModule
==========

Almost everything done with at right-click menus and action group action can be accessed via the :struct:`PartModule` objects that are attached to :struct:`Parts <Part>` of a :struct:`Vessel`.

The exact arrangement of :struct:`PartModule` to :struct:`Parts <Part>` to :struct:`Vessels <Vessel>`, and how to make use of a :struct:`PartModule` is a complex enough topic to warrant its own separate subject, described on the :ref:`Ship parts and Modules <parts and partmodules>` page. If you have not read that page, it is recommended that you do so before using :struct:`PartModules <PartModule>`. The page you are reading now is meant as just a reference summary, not a tutorial.

Once you have a :struct:`PartModule`, you can use it to invoke the behaviors that are connected to its right-click menu and to its action groups.


.. structure:: PartModule

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`NAME`
          - string
          - Name of this part module
        * - :attr:`PART`
          - :struct:`Part`
          - :struct:`Part` attached to
        * - :attr:`ALLFIELDS`
          - :struct:`List` of strings
          - Accessible fields
        * - :attr:`ALLFIELDNAMES`
          - :struct:`List` of strings
          - Accessible fields (name only)
        * - :attr:`ALLEVENTS`
          - :struct:`List` of strings
          - Triggerable events
        * - :attr:`ALLEVENTNAMES`
          - :struct:`List` of strings
          - Triggerable event names
        * - :attr:`ALLACTIONS`
          - :struct:`List` of strings
          - Triggerable actions
        * - :attr:`ALLACTIONNAMES`
          - :struct:`List` of strings
          - Triggerable event names
        * - :meth:`GETFIELD(name)`
          -
          - Get value of a field by name
        * - :meth:`SETFIELD(name,value)`
          -
          - Set value of a field by name
        * - :meth:`DOEVENT(name)`
          -
          - Trigger an event button
        * - :meth:`DOACTION(name,bool)`
          -
          - Activate action by name with True or False
        * - :meth:`HASFIELD(name)`
          - :ref:`Boolean <boolean>`
          - Check if field exists
        * - :meth:`HASEVENT(name)`
          - :ref:`Boolean <boolean>`
          - Check if event exists
        * - :meth:`HASACTION(name)`
          - :ref:`Boolean <boolean>`
          - Check if action exists




.. attribute:: PartModule:NAME

    :access: Get only
    :test: string

    Get the name of the module. Note that it's the same as the name given in the MODULE section of the Part.cfg file for the part.

.. attribute:: PartModule:PART

    :access: Get only
    :test: :struct:`Part`

    Get the :struct:`Part` that this PartModule is attached to.

.. attribute:: PartModule:ALLFIELDS

    :access: Get only
    :test: :struct:`List` of strings

    Get a list of all the names of KSPFields on this PartModule that the kos script is CURRENTLY allowed to get or set with :GETFIELD or :SETFIELD. Note the Security access comments below. This list can become obsolete as the game continues running depending on what the PartModule chooses to do.

.. attribute:: PartModule:ALLFIELDNAMES

     :access: Get only
     :test: :struct:`List` of strings
     
     Similar to :ALLFIELDS except that it returns the string without the formatting to make it easier to use in a script. This list can become obsolete as the game continues running depending on what the PartModule chooses to do.
     
.. attribute:: PartModule:ALLEVENTS

    :access: Get only
    :test: :struct:`List` of strings

    Get a list of all the names of KSPEvents on this PartModule that the kos script is CURRENTLY allowed to trigger with :DOEVENT. Note the Security access comments below. This list can become obsolete as the game continues running depending on what the PartModule chooses to do.

.. attribute:: PartModule:ALLEVENTNAMES

     :access: Get only
     :test: :struct:`List` of strings
     
     Similar to :ALLEVENTS except that it returns the string without the formatting to make it easier to use in a script. This list can become obsolete as the game continues running depending on what the PartModule chooses to do.
     
.. attribute:: PartModule:ALLACTIONS

    :access: Get only
    :test: :struct:`List` of strings

    Get a list of all the names of KSPActions on this PartModule that the kos script is CURRENTLY allowed to trigger with :DOACTION. Note the Security access comments below.

.. attribute:: PartModule:ALLACTIONNAMES

     :access: Get only
     :test: :struct:`List` of strings
     
     Similar to :ALLACTIONS except that it returns the string without the formatting to make it easier to use in a script. This list can become obsolete as the game continues running depending on what the PartModule chooses to do.
     
.. method:: PartModule:GETFIELD(name)

    :parameter name: (string) Name of the field
    :return: varies

    Get the value of one of the fields that this PartModule has placed onto the rightclick menu for the part. Note the Security comments below.

.. method:: PartModule:SETFIELD(name,value)

    :parameter name: (string) Name of the field

    Set the value of one of the fields that this PartModule has placed onto the rightclick menu for the part. Note the Security comments below.

    WARNING: This suffix is only settable for parts attached to the :ref:`CPU Vessel <cpu vessel>`

    SYMMETRY NOTE: There is one important difference between using
    SETFIELD to set a field versus what happens when you use the mouse
    to do it in the game's GUI.  In the GUI, often if the part is
    in a 2x, 3x, 4x, 6x, or 8x symmetry group, setting a field on
    one part will cause the other parts' fields to also change along
    with it.  Generally that does NOT happen when you use kOS to set
    the field.  If you want to set the same value to all the parts in
    a symmetry group, you need to iterate over all the parts yourself
    using the part's :attr:`Part::SYMMETRYCOUNT` suffix to see how
    many symmetrical parts there are, and iterate over them with
    :attr:`Part:SYMMETRYPARTNER(index)`, calling ``SETFIELD`` on
    them one at a time.

.. method:: PartModule:DOEVENT(name)

    :parameter name: (string) Name of the event

    Trigger an "event button" that is on the rightclick part menu at the moment. Note the Security comments below.

    WARNING: This suffix is only callable for parts attached to the :ref:`CPU Vessel <cpu vessel>`

.. method:: PartModule:DOACTION(name,bool)

    :parameter name: (string) Name of the action
    :parameter bool: (:ref:`Boolean <boolean>`) Value to set: True or False

    Activate one of this PartModule's action-group-able actions, bypassing the action group system entirely by just activating it for this one part directly. The :ref:`Boolean <boolean>` value decides whether you are toggling the action ON or toggling it OFF. Note the Security comments below.

    WARNING: This suffix is only callable for parts attached to the :ref:`CPU Vessel <cpu vessel>`

.. method:: PartModule:HASFIELD(name)

    :parameter name: (string) Name of the field
    :return: :ref:`Boolean <boolean>`

    Return true if the given field name is currently available for use with :GETFIELD or :SETFIELD on this PartModule, false otherwise.

.. method:: PartModule:HASEVENT(name)

    :parameter name: (string) Name of the event
    :return: :ref:`Boolean <boolean>`

    Return true if the given event name is currently available for use with :DOEVENT on this PartModule, false otherwise.

.. method:: PartModule:HASACTION(name)

    :parameter name: (string) Name of the action
    :return: :ref:`Boolean <boolean>`

    Return true if the given action name is currently available for use with :DOACTION on this PartModule, false otherwise.



Notes
-----

In all the above cases where there is a name being passed in to :GETFIELD, :SETFIELD, :DOEVENT, or :DOACTION, the name is meant to be the name that is seen by you, the user, in the GUI screen, and NOT necessarily the actual name of the variable that the programmer of that PartModule chose to call the value behind the scenes. This is so that you can view the GUI rightclick menu to see what to call things in your script.

.. note::

    **Security and Respecting other Mod Authors**

    There are often a lot more fields and events and actions that a partmodule can do than are usable via kOS. In designing kOS, the kOS developers have deliberately chosen NOT to expose any "hidden" fields of a partmodule that are not normally shown to the user, without the express permission of a mod's author to do so.

The access rules that kOS uses are as follows:

KSPFields
~~~~~~~~~

Is this a value that the user can normally see on the right-click context menu for a part? If so, then let kOS scripts GET the value.  Is this a value that the user can normally manipulate via "tweakable" adjustments on the right-click context menu for a part, AND, is that tweakable a CURRENTLY enabled one? If so, then let KOS scripts SET the value, BUT they must set it to one of the values that the GUI would normally allow, according to the following rules.

- If the KSPField is boolean:
    - The value must be true, false, or 0 or 1.

- If the KSPField is an integer:
    - The value must be a whole number.

- If the KSPField is a floating point sliding number:
    - The GUI for this field will be defined as a slider with a min value, a max value, with a fixed increment interval where the detents are. When setting such a value, you will be constrained to the limits of this slider. For example: If a slider is defined to have a minimum value of 2.0, a maximum value of 5.0, and a minimum allowed delta increment of 0.1:

    - If you try to set it to 0, it will instead become 2, the minimum allowed value. If you try to set it to 9, it will instead become 5, the maximum allowed value. If you try to set it to 3.14159, it will instead become 3.1, because that's rounding to the nearest increment step the slider supports.

- If the KSPField is a string:
    - There is currently no way to set these because kOS uses the existence of a gui tweakable as "proof" that it's okay to modify the field, and in the stock game there are no gui tweakables for string fields. This may change in the future if mods that extend the tweakables system are taken into account.

KSPEvents
~~~~~~~~~

Is this an event that has a GUI button associated with it that is currently visible on the right-click menu? If the answer is yes, then it will also be triggerable by kOSScripts, otherwise it won't.

KSPActions
~~~~~~~~~~

Is this an action that the KSP user would have been allowed to set as part of an action group during building in the VAB or SPH? If so, then allow a kOS script to use it, EVEN IF it has never actually been added to an action group for this vessel.

.. note::

    **If a KSPField, KSPEvent, or KSPAction has been disallowed, often in kOS it won't even appear to be a field of the PartModule at all.**

    This is necessary because for some modules, the number of fields you can use are far outnumberd by the number of fields that exist but are normally hidden from view. It would become unworkable if all of the unusable ones were exposed to kOS scripts to see as fields.

.. note::

    **Which KSPFields, KSPEvents, and KSPActions exist on a PartModule can change during runtime!**

    A PartModule is allowed to change the look and feel of its rightclick menu fields on the fly as the game runs. Therefore a field that didn't exist the last time you looked might now exist, and might not exist again next time. The list of what fields exist is context dependant. For example, a docking port may have an event button on it called "Undock Node", that only exists when that port is connected to another port. If it's not connected, the button may be gone. Similarly, a PartModule might toggle something by using a pair of two events that swap in and out depending on the current state. For example, many of the stock lights in the game have a "Turn on" button that after it's been clicked, gets replaced with a "Turn off" button until it's clicked again. A boolean toggle with a KSPFIeld would be simpler, but until "tweakables" existed in the main game, that wasn't an option so a lot of older Partmodules still do things the old way with two KSPEvents that swap in and out.
