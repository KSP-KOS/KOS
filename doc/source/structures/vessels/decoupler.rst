.. _decoupler:

Decoupler
=========

Some of the Parts returned by :ref:`LIST PARTS <list command>` will be of type :struct:`Decoupler`.
It is a type of :struct:`Part`, and therefore can use all the suffixes of :struct:`Part`.
It serves as base for all decouplers, separators, launch clamps and docking ports.
Alias :struct:`Separator` can also be used for :struct:`Decoupler`.

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

.. structure:: Separator

    :struct:`Separator` is an alias for the structure :struct:`Decoupler`
