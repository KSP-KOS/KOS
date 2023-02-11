.. _element:

Element
=======

An element is a *docked component* of a :struct:`Vessel`.  When you dock several
vessels together to create one larger vessel, you can obtain the "chunks" of the
larger vessel organized by which original vessel they came from.  These "chunks"
are called elements, and they are what the Element structure refers to.

A list of elements from the vessel can be created by using the command::

    list elements in eList.
    // now eList is a list of elements from the vessel.

Each item of that list is one of the elements.  The rest of this page describes the
elements and what they do.

.. note::
        Element boundries are not formed between two docking ports that were launched coupled. a craft with such an arrangement will appear as one element until you uncoupled the nodes and redocked

.. structure:: Element

    ===================================== ========================= =============
    Suffix                                Type                      Description
    ===================================== ========================= =============
    :attr:`NAME`                          :struct:`String`          The name of the docked craft
    :attr:`UID`                           :struct:`String`          Unique Identifier 
    :attr:`PARTS`                         :struct:`List`            all :struct:`Parts <Part>`
    :attr:`DOCKINGPORTS`                  :struct:`List`            all :struct:`DockingPorts <DockingPort>`
    :attr:`VESSEL`                        :struct:`Vessel`          the parent :struct:`Vessel`
    :attr:`RESOURCES`                     :struct:`List`            all :struct:`AggrgateResources <AggregateResource>`
    ===================================== ========================= =============

.. attribute:: Element:UID

    :type: :struct:`String`
    :access: Get only 

    A unique id

.. attribute:: Element:NAME

    :type: :struct:`String`
    :access: Get/Set

    The name of the Element element, is an artifact from the vessel the element belonged to before docking. Cannot be set to an empty :struct:`String`.

.. attribute:: Element:PARTS

    :type: :struct:`List` of :struct:`Part` objects
    :access: Get only

    A List of all the :struct:`parts <Part>` on the Element. ``SET FOO TO SHIP:PARTS.`` has exactly the same effect as ``LIST PARTS IN FOO.``. For more information, see :ref:`ship parts and modules <parts and partmodules>`.

.. attribute:: Element:DOCKINGPORTS

    :type: :struct:`List` of :struct:`DockingPort` objects
    :access: Get only

    A List of all the :struct:`docking ports <DockingPort>` on the Element. 

.. attribute:: Element:VESSEL

    :type: :struct:`Vessel`
    :access: Get only

    The parent vessel containing the element.

.. attribute:: Element:RESOURCES

    :type: :struct:`List` of :struct:`AggregateResource` objects
    :access: Get only

    A List of all the :struct:`AggregateResources <AggregateResource>` on the element.
