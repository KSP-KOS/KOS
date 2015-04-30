.. _waypoint:

Waypoints
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


.. attribute:: CORE:PART

    :type: Part
    :access: Get only

    The Part object for the current processor.

.. attribute:: CORE:VESSEL

    :type: `VesselTarget`
    :access: Get only

    The vessel containing the current processor.


.. attribute:: CORE:ELEMENT

    :type: Element
    :access: Get only

    The element object containing the current procesor.

.. attribute:: CORE:VERSION

    :type: VersionInfo
    :access: Get only

    The kOS version currently running.
