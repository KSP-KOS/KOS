.. _list command:

.. index:: LIST (command)

``LIST`` Command
================

A :struct:`List` is a type of :ref:`Structure <features structures>` that stores a list of variables in it. The ``LIST`` command either prints or crates a :struct:`List` object containing items queried from the game. For more information, see the :ref:`page about the List structure <list>`.

``FOR`` Loop
------------

Lists need to be iterated over sometimes, to help with this we have the :ref:`FOR loop, explained on the flow control page <for>`. The ``LIST`` Command comes in 3 forms:

1. ``LIST.``
    When no parameters are given, the LIST command is exactly equivalent to the command::

        LIST FILES.

2. ``LIST ListKeyword.``
    This variant prints items to the termianl sceen. Depending on the *ListKeyword* used (see below), different values are printed.

3. ``LIST ListKeyword IN YourVariable.``
    This variant takes the items that would otherwise have been printed to the terminal screen, and instead makes a :struct:`List` of them in ``YourVariable``, that you can then iterate over with a :ref:`FOR loop <for>` if you like.

Available Listable Keywords
---------------------------

The *ListKeyword* in the above command variants can be any of the
following:

Universal Lists
^^^^^^^^^^^^^^^

These generate :struct:`lists <List>` that are not dependent on which :struct:`Vessel`:

``Bodies``
    :struct:`List` of :struct:`Celestial Bodies <Body>`

``Targets``
    :struct:`List` of possible target :struct:`Vessels <Vessel>`

.. _list_fonts:

``Fonts``
    :struct:`List` of available font names for use with either
    :attr:`Style:FONT` or :attr:`Skin:FONT`. This list includes
    everything that has been loaded into the game engine by
    either KSP itself or by one of the KSP mods you have installed.

Vessel Lists
^^^^^^^^^^^^

These generate :struct:`lists <List>` of items on the :struct:`Vessel`:

``Processors``
    :struct:`List` of :struct:`Processors <kOSProcessor>`
``Resources``
    :struct:`List` of :struct:`AggregateResources <Resource>`
``Parts``
    :struct:`List` of :struct:`Parts <Part>`
``Engines``
    :struct:`List` of :struct:`Engines <Engine>`
``RCS``
    :struct:`List` of :struct:`RCS <RCS>`
``Sensors``
    :struct:`List` of :struct:`Sensors <Sensor>`
``Elements``
    :struct:`List` of :ref:`Elements <element>` that comprise the current vessel.
``DockingPorts``
    list of `DockingPorts <DockingPort>`

File System Lists
^^^^^^^^^^^^^^^^^

These generate :struct:`lists <List>` about the files in the system:

``Files``
    :struct:`List` the items, both files and subdirectories, on the current Volume at the current
    directory (you have to use the ``cd("dir")`` command to change directories first if you want
    to get a list of files under some other location.) (note below) The list contains items of
    type :struct:`VolumeItem`
``Volumes``
    :struct:`List` all the :struct:`volumes <Volume>` that exist.

.. note::

    ``LIST FILES.`` is the default if you give the ``LIST`` command no parameters.

Examples::

    LIST.  // Prints the list of files (and subdirectories) on current volume.
    LIST FILES.  // Does the same exact thing, but more explicitly.
    LIST VOLUMES. // which volumes can be seen by this CPU?
    LIST FILES IN fileList. // fileList is now a LIST() containing :struct:`VolumeItem` structures.

.. _list files:

The file structures returned by ``LIST FILES IN fileList.`` are documented :ref:`on a separate page <VolumeItem>`.
The file list contains both actual files and subdirectories under the current directory level.  You can use the
:attr:`VolumeItem:IsFile` suffix on each element to find out if it's a file or a subdirectory.  If it is a file rather than a
subdirectory, then it will also have all the suffixes of :struct:`VolumeFile` on it.

Here are some more examples::

    // Prints the list of all
    // Celestial bodies in the system.
    LIST BODIES.

    // Puts the list of bodies into a variable.
    LIST BODIES IN bodList.
    // Iterate over everything in the list:
    SET totMass to 0.
    FOR bod in bodList {
        SET totMass to totMass + bod:MASS.
    }.
    PRINT "The mass of the whole solar system is " + totMass.

    // Adds variable foo that contains a list of
    // resources for my current vessel
    LIST RESOURCES IN foo.
    FOR res IN foo {
        PRINT res:NAME. // Will print the name of every
                        // resource in the vessel
    }.
