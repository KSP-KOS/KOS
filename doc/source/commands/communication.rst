.. _communication:

Communication
=============================

A vessel can potentially have more than one :struct:`processor <kOSProcessor>` on board. It is possible for them to query information about each other and interact.

Accessing processors
--------------------

The easiest way of accessing the vessel's :struct:`processors <kOSProcessor>` is to use the following function:

.. function:: PROCESSOR(volumeOrNameTag)

    :parameter volumeOrNameTag: (:struct:`Volume` | `String`) can be either an instance of :struct:`Volume` or a string

    Depending on the type of the parameter value will either return the processor associated with the given :struct:`Volume` or the processor with the given name tag.

A list of all processors can be obtained using the :ref:`List <list>` command::

  LIST PROCESSORS IN ALL_PROCESSORS.
  PRINT ALL_PROCESSORS[0]:NAME.

Finally, processors can be accessed directly, like other :ref:`parts and modules <part>`::

  PRINT SHIP:MODULESNAMED("kOSProcessor")[0]:VOLUME:NAME.

Inter-processor communication
-----------------------------

Inter-vessel communication
--------------------------
