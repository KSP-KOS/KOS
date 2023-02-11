.. _decoupler:

Decoupler
=========

Some of the Parts returned by :ref:`LIST PARTS <list command>` will be of type :struct:`Decoupler`.
It is a type of :struct:`Part`, and therefore can use all the suffixes of :struct:`Part`.
It serves as base for all decouplers, separators, launch clamps and docking ports.
Most :struct:`Decoupler` parts are actually :struct:`Separator` parts, which is where most
of the suffixes for them are found.

.. structure:: Decoupler

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - All suffixes of :struct:`Part`
          -
          - A :struct:`Decoupler` is a kind of :struct:`Part`
          
    Most :struct:`Decoupler` parts are also :struct:`Separator` parts, and most of
    the useful suffixes for them are in :struct:`Separator`.
