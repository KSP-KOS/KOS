.. _part:

Part
====

These are the generic properties every PART has. You can obtain a list of values of type Part using the :ref:`LIST PARTS command <list command>`.

.. structure:: Part

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 2

        * - Suffix
          - Type
          - Description

        * - :attr:`NAME`
          - :struct:`String`
          - Name of this part
        * - :attr:`TITLE`
          - :struct:`String`
          - Title as it appears in KSP
        * - :attr:`MASS`
          - :struct:`Scalar`
          - Current mass of part and its resources
        * - :attr:`DRYMASS`
          - :struct:`Scalar`
          - Mass of part if all resources were empty
        * - :attr:`WETMASS`
          - :struct:`Scalar`
          - Mass of part if all resources were full
        * - :attr:`TAG`
          - :struct:`String`
          - Name-tag if assigned by the player
        * - :meth:`CONTROLFROM`
          - None
          - Call to control-from to this part
        * - :attr:`STAGE`
          - :struct:`Scalar`
          - The stage this is associated with
        * - :attr:`UID`
          - :struct:`String`
          - Unique identifying number of this part
        * - :attr:`ROTATION`
          - :struct:`Direction`
          - The rotation of this part's :math:`x`-axis
        * - :attr:`POSITION`
          - :struct:`Vector`
          - The location of this part in the universe
        * - :attr:`FACING`
          - :struct:`Direction`
          - the direction that this part is facing
        * - :attr:`RESOURCES`
          - :struct:`List`
          - list of the :struct:`Resource` in this part
        * - :attr:`TARGETABLE`
          - :struct:`Boolean`
          - true if this part can be selected as a target
        * - :attr:`SHIP`
          - :struct:`Vessel`
          - the vessel that contains this part
        * - :meth:`GETMODULE(name)`
          - :struct:`PartModule`
          - Get one of the :struct:`PartModules <PartModule>` by name
        * - :meth:`GETMODULEBYINDEX(index)`
          - :struct:`PartModule`
          - Get one of the :struct:`PartModules <PartModule>` by index
        * - :attr:`MODULES`
          - :struct:`List`
          - Names (:struct:`String`) of all :struct:`PartModules <PartModule>`
        * - :attr:`ALLMODULES`
          - :struct:`List`
          - Same as :attr:`MODULES`
        * - :attr:`PARENT`
          - :struct:`Part`
          - Adjacent :struct:`Part` on this :struct:`Vessel`.
        * - :attr:`HASPARENT`
          - :struct:`Boolean`
          - Check if this part has a parent :struct:`Part`
        * - :attr:`HASPHYSICS`
          - :struct:`Boolean`
          - Does this part have mass or drag
        * - :attr:`CHILDREN`
          - :struct:`List`
          - List of attached :struct:`Parts <Part>`




.. attribute:: Part:NAME

    :access: Get only
    :type: :struct:`String`

    Name of part as it is used behind the scenes in the game's API code.

    A part's *name* is the name it is given behind the scenes in KSP. It never appears in the normal GUI for the user to see, but it is used in places like Part.cfg files, the saved game persistence file, the ModuleManager mod, and so on.

.. attribute:: Part:TITLE

    :access: Get only
    :type: :struct:`String`

    The title of the part as it appears on-screen in the gui.

    A part's *title* is the name it has inside the GUI interface on the screen that you see as the user.

.. attribute:: Part:TAG

    :access: Get / Set
    :type: :struct:`String`

    The name tag value that may exist on this part if you have given the part a name via the :ref:`name-tag system <nametag>`.

    A part's *tag* is whatever custom name you have given it using the :ref:`name-tag system described here <nametag>`. This is probably the best naming convention to use because it lets you make up whatever name you like for the part and use it to pick the parts you want to deal with in your script.

    WARNING: This suffix is only settable for parts attached to the :ref:`CPU Vessel <cpu vessel>`

    This example assumes you have a target vessel picked, and that the target vessel is loaded into full-physics range and not "on rails". vessels that are "on rails" do not have their full list of parts entirely populated at the moment::

        LIST PARTS FROM TARGET IN tParts.

        PRINT "The target vessel has a".
        PRINT "partcount of " + tParts:LENGTH.

        SET totTargetable to 0.
        FOR part in tParts {
            IF part:TARGETABLE {
                SET totTargetable TO totTargetable + 1.
            }
        }

        PRINT "...and " + totTargetable.
        PRINT " of them are targetable parts.".

.. method:: Part:CONTROLFROM

    :access: Callable function only
    :type: None

    Call this function to cause the game to do the same thing as when you right-click a part on a vessel and select "control from here" on the menu. It rotates the control orientation so that fore/aft/left/right/up/down now match the orientation of this part. NOTE that this will not work for every type of part. It only works for those parts that KSP itself allows this for (control cores and docking ports).  It accepts no arguments, and returns no value.
    All vessels must have at least one "control from"
    part on them somewhere, which is why there's no mechanism for un-setting
    the "control from" setting other than to pick another part and set it
    to that part instead.

    .. warning::
        This suffix is only callable for parts attached to the :ref:`CPU Vessel <cpu vessel>`

.. attribute:: Part:STAGE

    :access: Get only
    :type: :struct:`Scalar`

    the stage this part is part of.

.. attribute:: Part:UID

    :access: Get only
    :type: :struct:`String`

    All parts have a unique ID number. Part's uid never changes because it is the same value as stored in persistent.sfs. Although you can compare parts by comparing their uid it is recommended to compare parts directly if possible.

.. attribute:: Part:ROTATION

    :access: Get only
    :type: :struct:`Direction`

    The rotation of this part's X-axis, which points out of its side and is probably not what you want. You probably want the :attr:`Part:FACING` suffix instead.

.. attribute:: Part:POSITION

    :access: Get only
    :type: :struct:`Vector`

    The location of this part in the universe. It is expressed in the same frame of reference as all the other positions in kOS, and thus can be used to help do things like navigate toward the position of a docking port.

.. attribute:: Part:FACING

    :access: Get only
    :type: :struct:`Direction`

    the direction that this part is facing.

.. attribute:: Part:MASS

    :access: Get only
    :type: :struct:`Scalar`

    The current mass or the part and its resources. If the part has no physics this will always be 0.

.. attribute:: Part:WETMASS

    :access: Get only
    :type: :struct:`Scalar`

    The mass of the part if all of its resources were full. If the part has no physics this will always be 0.

.. attribute:: Part:DRYMASS

    :access: Get only
    :type: :struct:`Scalar`

    The mass of the part if all of its resources were empty. If the part has no physics this will always be 0.

.. attribute:: Part:RESOURCES

    :access: Get only
    :type: :struct:`List`

    list of the :struct:`Resource` in this part.

.. attribute:: Part:TARGETABLE

    :access: Get only
    :type: :struct:`Boolean`

    true if this part can be selected by KSP as a target.

.. attribute:: Part:SHIP

    :access: Get only
    :type: :struct:`Vessel`

    the vessel that contains this part.

.. method:: Part:GETMODULE(name)

    :parameter name: (:struct:`String`) Name of the part module
    :returns: :struct:`PartModule`

    Get one of the :struct:`PartModules <PartModule>` attached to this part, given the name of the module. (See :attr:`Part:MODULES` for a list of all the names available).

.. method:: Part:GETMODULEBYINDEX(index)

    :parameter index: (:struct:`Scalar`) Index number of the part module
    :returns: :struct:`PartModule`

    Get one of the :struct:`PartModules <PartModule>` attached to this part,
    given the index number of the module. You can use :attr:`Part:MODULES` for a
    list of names of all modules on the part. The indexes are not guaranteed to
    always be in the same order. It is recommended to iterate over the indexes
    with a loop and verify the module name::

        local moduleNames is part:modules.
        for idx in range(0, moduleNames:length) {
            if moduleNames[idx] = "test module" {
                local pm is part:getmodulebyindex(idx).
                DoSomething(pm).
            }
        }


.. attribute:: Part:MODULES

    :access: Get only
    :type: :struct:`List` of strings

    list of the names of :struct:`PartModules <PartModule>` enabled for this part.

.. attribute:: Part:ALLMODULES

    Same as :attr:`Part:MODULES`

.. attribute:: Part:PARENT

    :access: Get only
    :type: :struct:`Part`

    When walking the :ref:`tree of parts <parts and partmodules>`, this is the part that this part is attached to on the way "up" toward the root part.

.. attribute:: Part:HASPHYSICS

    :access: Get only
    :type: bool

    This comes from a part's configuration and is an artifact of the KSP simulation.

    For a list of stock parts that have this attribute and a fuller explanation see `the KSP wiki page about massless parts <http://wiki.kerbalspaceprogram.com/wiki/Massless_part>`_.

.. attribute:: Part:HASPARENT

    :access: Get only
    :type: :struct:`Boolean`

    When walking the :ref:`tree of parts <parts and partmodules>`, this is true as long as there is a parent part to this part, and is false if this part has no parent (which can only happen on the root part).

.. attribute:: Part:CHILDREN

    :access: Get only
    :type: :struct:`List` of :struct:`Parts <Part>`

    When walking the :ref:`tree of parts <parts and partmodules>`, this is all the parts that are attached as children of this part. It returns a list of zero length when this part is a "leaf" of the parts tree.
