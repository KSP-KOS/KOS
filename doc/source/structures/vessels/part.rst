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
        * - :attr:`CID`
          - :struct:`String`
          - Craft-Unique identifying number of this part
        * - :attr:`UID`
          - :struct:`String`
          - Universe-Unique identifying number of this part
        * - :attr:`ROTATION`
          - :struct:`Direction`
          - The rotation of this part's :math:`x`-axis
        * - :attr:`POSITION`
          - :struct:`Vector`
          - The location of this part in the universe
        * - :attr:`FACING`
          - :struct:`Direction`
          - the direction that this part is facing
        * - :attr:`BOUNDS`
          - :struct:`Bounds`
          - Bounding-box information about this part's shape
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
        * - :meth:`HASMODULE(name)`
          - :struct:`Boolean`
          - True if the part has the named module in it, false if not.
        * - :attr:`PARENT`
          - :struct:`Part`
          - Adjacent :struct:`Part` on this :struct:`Vessel`.
        * - :attr:`HASPARENT`
          - :struct:`Boolean`
          - Check if this part has a parent :struct:`Part`
        * - :attr:`DECOUPLER`
          - :struct:`Decoupler` or :struct:`String`
          - The decoupler/separator that will decouple this part when activated. `None` if no such exists.
        * - :attr:`SEPARATOR`
          - :struct:`Decoupler` or :struct:`String`
          - Alias name for :attr:`DECOUPLER <Part:DECOUPLER>`
        * - :attr:`DECOUPLEDIN`
          - :struct:`Scalar`
          - The stage number where this part will get decoupled. -1 if cannot be decoupled.
        * - :attr:`SEPARATEDIN`
          - :struct:`Scalar`
          - Alias name for :attr:`DECOUPLEDIN <Part:DECOUPLEDIN>`
        * - :attr:`HASPHYSICS`
          - :struct:`Boolean`
          - Does this part have mass or drag
        * - :attr:`CHILDREN`
          - :struct:`List`
          - List of attached :struct:`Parts <Part>`
        * - :attr:`SYMMETRYCOUNT`
          - :struct:`Scalar`
          - How many parts in this part's symmetry set
        * - :meth:`REMOVESYMMETRY`
          - none
          - Like the "Remove From Symmetry" button.
        * - :meth:`SYMMETRYPARTNER(index)`
          - :struct:`part`
          - Return one of the other parts symmetrical to this one.
        * - :meth:`PARTSNAMED(name)`
          - :struct:`List` (of :struct:`Part`)
          - Search the branch from here down based on name.
        * - :meth:`PARTSNAMEDPATTERN(pattern)`
          - :struct:`List` (of :struct:`Part`)
          - Regex search the branch from here down based on name.
        * - :meth:`PARTSTITLED(name)`
          - :struct:`List` (of :struct:`Part`)
          - Search the branch from here down for parts titled this.
        * - :meth:`PARTSTITLEDPATTERN(pattern)`
          - :struct:`List` (of :struct:`Part`)
          - Regex Search the branch from here down for parts titled this.
        * - :meth:`PARTSTAGGED(tag)`
          - :struct:`List` (of :struct:`Part`)
          - Search the branch from here down for parts tagged this.
        * - :meth:`PARTSTAGGEDPATTERN(pattern)`
          - :struct:`List` (of :struct:`Part`)
          - Regex Search the branch from here down for parts tagged this.
        * - :meth:`PARTSDUBBED(name)`
          - :struct:`List` (of :struct:`Part`)
          - Search the branch from here down for parts named, titled, or tagged this.
        * - :meth:`PARTSDUBBEDPATTERN(name)`
          - :struct:`List` (of :struct:`Part`)
          - Regex Search the branch from here down for parts named, titled, or tagged this.
        * - :meth:`MODULESNAMED(name)`
          - :struct:`List` (of :struct:`PartModule`)
          - Search the branch from here down for modules named, titled, or tagged this.
        * - :meth:`ALLTAGGEDPARTS`
          - :struct:`List` (of :struct:`Part`)
          - Search the branch from here down for all parts with a non-blank tag name.
        * - :meth:`ATTITUDECONTROLLERS`
          - :struct:`List` (of :struct:`AttitudeController`)
          - List of Attitude Controllers in this part.


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

.. attribute:: Part:CID

    :access: Get only
    :type: :struct:`String`

    Part Craft ID. This is similar to :attr:`Part:UID`, except that this
    ID is only unique per craft design.  In other words if you launch two
    copies of the same design without editing the design at all, then the
    same part in both copies of the design will have the same ``Part:CID``
    as each other.  (This value is kept in the *craft file* and repeated
    in each instance of the vessel that you launch).

.. attribute:: Part:UID

    :access: Get only
    :type: :struct:`String`

    Part Universal ID. All parts have a unique ID number. Part's uid never changes because it is the same value as stored in persistent.sfs. Although you can compare parts by comparing their uid it is recommended to compare parts directly if possible.

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

    The direction that this part is facing, which is also the rotation
    that would transform a vector from a coordinate space where the
    axes were oriented to match the part, to one where they're
    oriented to match the world's ship-raw coordinates.

.. attribute:: Part:BOUNDS

    :access: Get only
    :type: :struct:`Bounds`

    Constructs a "bounding box" structure that can be used to
    give your script some idea of the extents of the part's shape - how
    wide, long, and tall it is.

    It can be slightly expensive in terms of CPU time to keep calling
    this suffix over and over, as kOS has to perform some work to build
    this structure.  If you need to keep looking at a part's bounds again
    and again in a loop, and you know that part's shape isn't going to be
    changing (i.e. you're not going to extend a solar panel or something
    like that), then it's better for you to call this ``:BOUNDS`` suffix
    just once at the top, storing the result in a variable that you use in
    the loop.

    More detailed information is found on the documentation page for
    :struct:`Bounds`.

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

    True if this part can be selected by KSP as a target.

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

.. method:: Part:HASMODULE(name)

    :parameter name: (:struct:`String`) The name of the module to check for
    :returns: :struct:`Boolean`

    Checks to see if this part contains the :struct:`PartModule` with the name
    given.  If it does, this returns true, else it returns false.  (If 
    ``HASMODULE(name)`` returns false, then this means an attempt to use
    ``GETMODULE(name)`` would fail with an error.)

.. attribute:: Part:PARENT

    :access: Get only
    :type: :struct:`Part`

    When walking the :ref:`tree of parts <parts and partmodules>`, this is the part that this part is attached to on the way "up" toward the root part.

.. attribute:: Part:HASPARENT

    :access: Get only
    :type: :struct:`Boolean`

    When walking the :ref:`tree of parts <parts and partmodules>`, this is true as long as there is a parent part to this part, and is false if this part has no parent (which can only happen on the root part).

.. attribute:: Part:DECOUPLER

    :access: Get only
    :type: :struct:`Decoupler` or :struct:`String`

    The decoupler/separator that will decouple this part when activated. `None` if no such exists.

.. attribute:: Part:SEPARATOR

    :access: Get only
    :type: :struct:`Decoupler` or :struct:`String`
    
    Alias name for :attr:`DECOUPLER <Part:DECOUPLER>`

.. attribute:: Part:DECOUPLEDIN

    :access: Get only
    :type: :struct:`Scalar`
    
    The stage number where this part will get decoupled. -1 if cannot be decoupled.

.. attribute:: Part:SEPARATEDIN

    :access: Get only
    :type: :struct:`Scalar`
    
    Alias name for :attr:`DECOUPLEDIN <Part:DECOUPLEDIN>`

.. attribute:: Part:HASPHYSICS

    :access: Get only
    :type: bool

    This comes from a part's configuration and is an artifact of the KSP simulation.

    For a list of stock parts that have this attribute and a fuller explanation see `the KSP wiki page about massless parts <http://wiki.kerbalspaceprogram.com/wiki/Massless_part>`_.

.. attribute:: Part:CHILDREN

    :access: Get only
    :type: :struct:`List` of :struct:`Parts <Part>`

    When walking the :ref:`tree of parts <parts and partmodules>`, this is all the parts that are attached as children of this part. It returns a list of zero length when this part is a "leaf" of the parts tree.

.. attribute:: Part:SYMMETRYCOUNT

    :access: Get only
    :type: :struct:`Scalar`

    Returns how many parts are in the same symmetry set as this part.

    Note that all parts should at least return a minimum value of 1, since
    even a part placed without symmetry is technically in a group of 1 part,
    itself.

.. attribute:: Part:SYMMETRYTYPE

    :access: Get only
    :type: :struct:`Scalar`

    Tells you the type of symmetry this part has by returning a number
    as follows:

    0 = This part has radial symmetry

    1 = This part has mirror symmetry

    It's unclear if this means anything when the part's symmetry is 1x.

.. method:: Part:REMOVESYMMETRY

    :access: method
    :returns: nothing

    Call this method to remove this part from its symmetry group, reverting
    it back to a symmetry group of 1x (just itself).  This has the same
    effect as pressing the "Remove From Symmetry" button in the part's
    action window.

    Note that just like when you press the "Remove from Symmetry" button,
    once a part has been removed from symmetry you don't have a way to
    put it back into the symmetry group again.

.. method:: Part:SYMMETRYPARTNER(index)

    :access: method
    :parameter name: (:struct:`Scalar`) Index of which part in the symmetry group
    :returns: :struct:`Part`

    When a set of parts has been placed with symmetry in the Vehicle
    Assembly Building or Space Plane Hangar, this method can be used
    to find all the parts that are in the same symmetrical group.

    The index is numbered from zero to :attr:``SYMMETRYCOUNT`` minus one.

    The zero-th symmetry partner is this part itself.  Even parts placed
    without symmetry still are technically in a symmetry group of 1 part.

    The index also wraps around in a cycle, such that if there are 4 parts in
    symmetry, then ``SYMMETRYPARTNER(0)`` and ``SYMMETRYPARTNER(4)`` and
    ``SYMMETRYPARTNER(8)`` would all actually be the same part.

    Example::

        // Print the symmetry group a part is inside:
        function print_sym {
          parameter a_part.

          print a_part + " is in a " + a_part:SYMMETRYCOUNT + "x symmetry set.".

          if a_part:SYMMETRAYCOUNT = 1 {
            return. // no point in printing the list when its not in a group.
          }

          if a_part:SYMMETRYTYPE = 0 {
            print "  The symmetry is radial.".
          } else if a_part:SYMMETRYTYPE = 1 {
            print "  The symmetry is mirror.".
          } else {
            print "  The symmetry is some other weird kind that".
            print "  didn't exist back when this example was written.".
          }

          print "    The Symmetry Group is: ".
          for i in range (0, a_part:SYMMETRYCOUNT) {
            print "      [" + i + "] " + a_part:SYMMETRYPARTNER(i).
          }
        }

.. method:: Parts:PARTSNAMED(name)

    :parameter name: (:ref:`string <string>`) Name of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Same as :meth:`Vessel:PARTSNAMED(name)` except that this version
    doesn't search the entire vessel tree and instead it only searches the
    branch of the vessel's part tree from the current part down through
    its children and its children's children and so on.

.. method:: Part:PARTSNAMEDPATTERN(namePattern)

    :parameter namePattern: (:ref:`string <string>`) Pattern of the name of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Same as :meth:`Vessel:PARTSNAMEDPATTERN(namePattern)` except that this version
    doesn't search the entire vessel tree and instead it only searches the
    branch of the vessel's part tree from the current part down through
    its children and its children's children and so on.

.. method:: Part:PARTSTITLED(title)

    :parameter title: (:ref:`string <string>`) Title of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Same as :meth:`Vessel:PARTSTITLED(title)` except that this version
    doesn't search the entire vessel tree and instead it only searches the
    branch of the vessel's part tree from the current part down through
    its children and its children's children and so on.

.. method:: Part:PARTSTITLEDPATTERN(titlePattern)

    :parameter titlePattern: (:ref:`string <string>`) Patern of the title of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Same as :meth:`Vessel:PARTSTITLEDPATTERN(titlePattern)` except that this version
    doesn't search the entire vessel tree and instead it only searches the
    branch of the vessel's part tree from the current part down through
    its children and its children's children and so on.

.. method:: Part:PARTSTAGGED(tag)

    :parameter tag: (:ref:`string <string>`) Tag of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Same as :meth:`Vessel:PARTSTAGGED(tag)` except that this version
    doesn't search the entire vessel tree and instead it only searches the
    branch of the vessel's part tree from the current part down through
    its children and its children's children and so on.

.. method:: Part:PARTSTAGGEDPATTERN(tagPattern)

    :parameter tagPattern: (:ref:`string <string>`) Pattern of the tag of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Same as :meth:`Vessel:PARTSTAGGEDPATTERN(tagPattern)` except that this version
    doesn't search the entire vessel tree and instead it only searches the
    branch of the vessel's part tree from the current part down through
    its children and its children's children and so on.

.. method:: Part:PARTSDUBBED(name)

    :parameter name: (:ref:`string <string>`) name, title or tag of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Same as :meth:`Vessel:PARTSDUBBED(name)` except that this version
    doesn't search the entire vessel tree and instead it only searches the
    branch of the vessel's part tree from the current part down through
    its children and its children's children and so on.

.. method:: Part:PARTSDUBBEDPATTERN(namePattern)

    :parameter namePattern: (:ref:`string <string>`) Pattern of the name, title or tag of the parts
    :return: :struct:`List` of :struct:`Part` objects

    Same as :meth:`Vessel:PARTSDUBBEDPATERN(namePattern)` except that this version
    doesn't search the entire vessel tree and instead it only searches the
    branch of the vessel's part tree from the current part down through
    its children and its children's children and so on.

.. method:: Part:MODULESNAMED(name)

    :parameter name: (:ref:`string <string>`) Name of the part modules
    :return: :struct:`List` of :struct:`PartModule` objects

    Same as :meth:`Vessel:MODULESNAMED(name)` except that this version
    doesn't search the entire vessel tree and instead it only searches the
    branch of the vessel's part tree from the current part down through
    its children and its children's children and so on.

.. method:: Part:ALLTAGGEDPARTS()

    :return: :struct:`List` of :struct:`Part` objects

    Same as :meth:`Vessel:ALLTAGGEDPARTS()` except that this version
    doesn't search the entire vessel tree and instead it only searches the
    branch of the vessel's part tree from the current part down through
    its children and its children's children and so on.

.. method:: Vessel::ATTITUDECONTROLLERS()

    :return: :struct:`List` of :struct:`AttitudeController` objects

    Return all Attitude Controllers in this part.