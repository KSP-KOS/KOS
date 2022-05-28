.. _core:

Core
====

.. contents::
    :local:
    :depth: 2

Core represents your ability to identify and interact directly with the running kOS processor.  You can use it to access the parent vessel, or to perform operations on the processor's part.  You obtain a CORE structure by using the bound variable ``core``.

.. structure:: CORE

    .. list-table:: **Members**
        :widths: 1 1
        :header-rows: 1

        * - Suffix
          - Type

        * - All suffixes of :struct:`kOSProcessor`
          - :struct:`CORE` objects are a type of :struct:`kOSProcessor`

        * - :attr:`VESSEL`
          - `Vessel`
        * - :attr:`ELEMENT`
          - `Element`
        * - :attr:`TAG`
          - `The kOS nametag on the part this CPU runs on`
        * - :attr:`VERSION`
          - `Version`
        * - :attr:`CURRENTVOLUME`
          - :struct:`Volume`
        * - :attr:`MESSAGES`
          - :struct:`MessageQueue`


.. attribute:: CORE:VESSEL

    :type: `VesselTarget`
    :access: Get only

    The vessel containing the current processor.


.. attribute:: CORE:ELEMENT

    :type: `Element`
    :access: Get only

    The element object containing the current processor.

.. attribute:: CORE:VERSION

    :type: :struct:`VersionInfo`
    :access: Get only

    The kOS version currently running.

.. attribute:: CORE:TAG

    :type: :struct:`String`
    :access: Get/Set

    The kOS name tag currently assigned to the part this core is
    inside.  This is the same thing as Core:part:tag.

.. attribute:: CORE:CURRENTVOLUME

    :type: :struct:`Volume`
    :access: Get only

    The currently selected volume for the current processor.  This may be useful to prevent deleting files on the Archive, or for interacting with multiple local hard disks.

.. attribute:: CORE:MESSAGES

    :type: :struct:`MessageQueue`
    :access: Get only

    Returns this processsor's message queue.

