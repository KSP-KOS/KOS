.. _sciencecontainermodule:

ScienceContainerModule
=======================

The type of structures returned by kOS when querying a module that stores science experiments.

.. structure:: ScienceContainerModule

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - All suffixes of :struct:`PartModule`
          -
          - :struct:`ScienceContainerModule` objects are a type of :struct:`PartModule`
        * - :meth:`DUMPDATA(DATA)`
          -
          - Discard the experiment
        * - :meth:`COLLECTALL()`
          -
          - Run the "collect all" action directly
        * - :attr:`HASDATA`
          - :ref:`Boolean <boolean>`
          - Does this part contain experiments
        * - :attr:`DATA`
          - :struct:`List` of :struct:`ScienceData`
          - List of scientific data obtained by this experiment

.. note::

    A :struct:`ScienceContainerModule` is a type of :struct:`PartModule`, and therefore can use all the suffixes of :struct:`PartModule`.

.. method:: ScienceContainerModule:DUMPDATA(DATA)

    Call this method to dump the particular experiment provided

.. method:: ScienceContainerModule:COLLECTALL()

    Call this method to run the unit's "collect all" action

.. attribute:: ScienceContainerModule:HASDATA

    :access: Get only
    :type: :ref:`Boolean <boolean>`

    True if this container has scientific data stored.

.. attribute:: ScienceContainerModule:DATA

    :access: Get only
    :type: :struct:`List` of :struct:`ScienceData`

    List of scientific data contained by this part
