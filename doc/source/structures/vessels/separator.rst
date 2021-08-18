.. _separator

Separator
=========

Some of the Parts returned by :ref:`LIST PARTS <list command>` will be of type :struct:`Separator`.

A separator part is one that detaches a vessel into at least two parts, in a permanent
way (rather than a docking port, which can be re-attached).  Both decouplers and
separators count.


.. structure:: Separator

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - All suffixes of :struct:`Decoupler`
          -
          - A :struct:`Separator` is a kind of :struct:`Decoupler` (which is :struct:`Part`)

        * - :attr:`EJECTIONFORCE`
          - :struct:`Scalar`
          - Force that applies when the decoupling event fires.
        * - :attr:`ISDECOUPLED`
          - :struct:`Boolean`
          - True if this part already has had its decoupling event triggered.
        * - :attr:`STAGED`
          - :struct:`Boolean`
          - True if this part is set up to decouple as part of the staging list.

.. attribute:: Separator:EJECTIONFORCE

    :type: :struct:`Scalar`
    :access: Get only

    Force of the push that happens when this decoupler is fired.

.. attribute:: Separator:ISDECOUPLED

    :type: :struct:`Boolean`
    :access: Get only

    True if this part has already had its decoupling event triggered.

.. attribute:: Separator:STAGED

    :type: :struct:`Boolean`
    :access: Get only

    True if this part's decoupling event is in the vessel's staging list.

