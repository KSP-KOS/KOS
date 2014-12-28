.. _dockingport:

DockingPort
===========

Some of the Parts returned by :ref:`LIST PARTS <list command>` will be of type :struct:`DockingPort`.


.. structure:: DockingPort

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - All suffixes of :struct:`Part`
          -
          - A :struct:`DockingPort` is a kind of :struct:`Part`

        * - :attr:`AQUIRERANGE`
          - scalar
          - active range of the port
        * - :attr:`AQUIREFORCE`
          - scalar
          - force experienced when docking
        * - :attr:`AQUIRETORQUE`
          - scalar
          - torque experienced when docking
        * - :attr:`REENGAGEDDISTANCE`
          - scalar
          - distance at which the port is reset
        * - :attr:`DOCKEDSHIPNAME`
          - string
          - name of vessel the port is docked to
        * - :attr:`PORTFACING`
          - :struct:`Direction`
          - facing of the port
        * - :attr:`STATE`
          - string
          - current state of the port
        * - :meth:`UNDOCK`
          - 
          - callable to release the dock
        * - :attr:`TARGETABLE`
          - boolean
          - check if this port can be targeted

.. note::

    :struct:`DockingPort` is a type of :struct:`Part`, and therefore can use all the suffixes of :struct:`Part`. Shown below are only the suffixes that are unique to :struct:`DockingPort`.


.. attribute:: DockingPort:AQUIRERANGE

    :type: scalar
    :access: Get only

    gets the range at which the port will "notice" another port and pull on it.

.. attribute:: DockingPort:AQUIREFORCE

    :type: scalar
    :access: Get only

    gets the force with which the port pulls on another port.

.. attribute:: DockingPort:AQUIRETORQUE

    :type: scalar
    :access: Get only

    gets the rotational force with which the port pulls on another port.

.. attribute:: DockingPort:REENGAGEDDISTANCE

    :type: scalar
    :access: Get only

    how far the port has to get away after undocking in order to re-enable docking.

.. attribute:: DockingPort:DOCKEDSHIPNAME

    :type: string
    :access: Get only

    name of vessel on the other side of the docking port.

.. attribute:: DockingPort:PORTFACING

    :type: :struct:`Direction`
    :access: Get only

    Gets the facing of this docking port which may differ from the facing of the part itself if the docking port is aimed out the side of the part, as in the case of the inline shielded docking port.

.. attribute:: DockingPort:STATE

    :type: string
    :access: Get only

    One of the following string values:

    ``Ready``
        Docking port is not yet attached and will attach if it touches another.
    ``Docked (docker)``
        One port in the joined pair is called the docker, and has this state
    ``Docked (dockee)``
        One port in the joined pair is called the dockee, and has this state
    ``Docked (same vessel)``
        Sometimes KSP says this instead. It's unclear what it means.
    ``Disabled``
        Docking port will refuse to dock if it bumps another docking port.
    ``PreAttached``
        Temporary state during the "wobbling" while two ports are magnetically touching but not yet docked solidly. During this state the two vessels are still tracked as separate vessels and haven't become one yet.


.. method:: DockingPort:UNDOCK

    Call this to cause the docking port to detach.

.. attribute:: DockingPort:TARGETABLE

    :type: boolean
    :access: Get only

    True if this part can be picked with ``SET TARGET TO``.



