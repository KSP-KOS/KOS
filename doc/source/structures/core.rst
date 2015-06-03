.. _core:

Core
=========

.. contents::
    :local:
    :depth: 2

Core represents your ability to identify and interact directly with the running kOS processor.  You can use it to access the parent vessel, or to perform operations on the processor's part.

.. structure:: CORE

    .. list-table:: **Members**
        :widths: 1 1
        :header-rows: 1

        * - Suffix
          - Type

        * - :attr:`PART`
          - `Part`
        * - :attr:`VESSEL`
          - `Vessel`
        * - :attr:`ELEMENT`
          - `Element`
        * - :attr:`VERSION`
          - `Version`
        * - :attr:`BOOTFILENAME`
          - `String`
        * - :attr:`CURRENTVOLUME`
          - `Volume`


.. attribute:: CORE:PART

    :type: Part
    :access: Get only

    The Part object for the current processor.

.. attribute:: CORE:VESSEL

    :type: `VesselTarget`
    :access: Get only

    The vessel containing the current processor.


.. attribute:: CORE:ELEMENT

    :type: `Element`
    :access: Get only

    The element object containing the current processor.

.. attribute:: CORE:VERSION

    :type: `VersionInfo`
    :access: Get only

    The kOS version currently running.

.. attribute:: CORE:BOOTFILENAME

    :type: `String`
    :access: Get or Set

    The filename for the boot file on the current processor.  This may be set to an empty string `""` or to `"None"` to disable the use of a boot file.

.. attribute:: CORE:CURRENTVOLUME

    :type: `Volume`
    :access: Get only

    The currently selected volume for the current processor.  This may be useful to prevent deleting files on the Archive, or for interacting with multiple local hard disks.
