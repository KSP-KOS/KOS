.. _dockingport:

DockingPort
===========

Some of the Parts returned by :ref:`LIST PARTS <list command>` will be of type :struct:`DockingPort`.
Such part can also be retrieved from :global:`TARGET`, if docking port is selected as target.

.. note::

    .. versionadded:: 0.18
        The spelling of suffixes `AQUIRERANGE`, `AQUIREFORCE`, and `AQUIRETORQUE` on the :struct:`DockingPort` structure has been corrected.  Please use `ACQUIRERANGE`, `ACQUIREFORCE`, and `ACQUIRETORQURE` instead.  Using the old incorrect spelling, a deprecation exception will be thrown, with instruction to use the new spelling.

.. structure:: DockingPort

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - All suffixes of :struct:`Decoupler`
          -
          - A :struct:`DockingPort` is a kind of :struct:`Decoupler` (which is :struct:`Part`)

        * - :attr:`ACQUIRERANGE`
          - scalar
          - active range of the port
        * - :attr:`ACQUIREFORCE`
          - scalar
          - force experienced when docking
        * - :attr:`ACQUIRETORQUE`
          - scalar
          - torque experienced when docking
        * - :attr:`REENGAGEDDISTANCE`
          - scalar
          - distance at which the port is reset
        * - :attr:`DOCKEDSHIPNAME`
          - :ref:`string <string>`
          - name of vessel the port is docked to
        * - :attr:`NODEPOSITION`
          - vector
          - coords of where the docking node attachment point is in SHIP-RAW xyz
        * - :attr:`NODETYPE`
          - :ref:`string <string>`
          - two nodes are only dockable together if their NODETYPE strings match
        * - :attr:`PORTFACING`
          - :struct:`Direction`
          - facing of the port
        * - :attr:`STATE`
          - :ref:`string <string>`
          - current state of the port
        * - :meth:`UNDOCK`
          -
          - callable to release the dock
        * - :attr:`PARTNER`
          - :struct:`DockingPort`
          - the docking port this docking port is attached to, or "None" if no such port
        * - :attr:`HASPARTNER`
          - boolean
          - whether or not this docking port is attached to another docking port
        * - :attr:`TARGETABLE`
          - boolean
          - check if this port can be targeted

.. note::

    :struct:`DockingPort` is a type of :struct:`Decoupler`, and therefore can use all the suffixes of :struct:`Decoupler`. Shown below are only the suffixes that are unique to :struct:`DockingPort`.


.. attribute:: DockingPort:ACQUIRERANGE

    :type: scalar
    :access: Get only

    gets the range at which the port will "notice" another port and pull on it.

.. attribute:: DockingPort:ACQUIREFORCE

    :type: scalar
    :access: Get only

    gets the force with which the port pulls on another port.

.. attribute:: DockingPort:ACQUIRETORQUE

    :type: scalar
    :access: Get only

    gets the rotational force with which the port pulls on another port.

.. attribute:: DockingPort:REENGAGEDDISTANCE

    :type: scalar
    :access: Get only

    how far the port has to get away after undocking in order to re-enable docking.

.. attribute:: DockingPort:DOCKEDSHIPNAME

    :type: :ref:`string <string>`
    :access: Get only

    name of vessel on the other side of the docking port.

.. attribute:: DockingPort:NODEPOSITION

    :type: vector
    :access: Get only

    The coordinates of the point on the docking port part where the
    port attachment spot is located.  This is different from the 
    part's position itself because that's the position of the center
    of the whole part.  This is the position of the face of the
    docking port.  Coordinates are in SHIP-RAW xyz coords.

.. attribute:: DockingPort:NODETYPE

    :type: :ref:`string <string>`
    :access: Get only

    Each docking port has a node type string that specifies its
    compatibility with other docking ports.  In order for two docking
    ports to be able to attach to each other, the values for their
    NODETYPEs must be the same.

    The base KSP stock docking port parts all use one of the following
    three values:

        - "size0" for all Junior-sized docking ports.
        - "size1" for all Normal-sized docking ports.
        - "size2" for all Senior-sized docking ports.

    Mods that provide their own new kinds of docking port might use
    any other value they feel like here, but only if they are trying
    to declare that the new part isn't supposed to be able to connect
    to stock docking ports.  Any docking port that is meant to connect
    to stock ports will have to adhere to the above scheme.

.. attribute:: DockingPort:PORTFACING

    :type: :struct:`Direction`
    :access: Get only

    Gets the facing of this docking port which may differ from the facing of the part itself if the docking port is aimed out the side of the part, as in the case of the inline shielded docking port.

.. attribute:: DockingPort:STATE

    :type: :ref:`string <string>`
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

.. attribute:: DockingPort:PARTNER

    :type: :struct:`DockingPort`, or the :struct:`String` "None" if no such port.
    :access: Get only

    The docking port this docking port is attached to.
    If this docking port is not actually attached to another port, attempting
    to call this will return a String instead of a DockingPort, and that String
    will have the value "None".  (Alternatively, you can test if this
    docking port has a partner port attached by calling
    :meth:`DockingPort:HASPARTER`.)

.. attribute:: DockingPort:HASPARTNER

    :type: :ref:`Boolean <boolean>`
    :access: Get only

    Whether or not this docking port is attached to another docking port.

.. attribute:: DockingPort:TARGETABLE

    :type: :ref:`Boolean <boolean>`
    :access: Get only

    True if this part can be picked with ``SET TARGET TO``.
