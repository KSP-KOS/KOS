.. _kosprocessor:

kOSProcessor
==================

The type of structures returned by kOS when querying a module that contains a kOS processor.


.. structure:: kOSProcessor

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - All suffixes of :struct:`PartModule`
          -
          - :struct:`kOSProcessor` objects are a type of :struct:`PartModule`
        * - :attr:`MODE`
          - :struct:`String`
          - `OFF`, `READY` or `STARVED`
        * - :meth:`ACTIVATE`
          - None
          - Activates this processor
        * - :meth:`DEACTIVATE`
          - None
          - Deactivates this processor
        * - :attr:`TAG`
          - :struct:`String`
          - This processor's name tag
        * - :attr:`VOLUME`
          - :struct:`Volume`
          - This processor's hard disk
        * - :attr:`BOOTFILENAME`
          - :struct:`String`
          - The filename for the boot file on this processor
        * - :attr:`CONNECTION`
          - :struct:`Connection`
          - Returns your connection to this processor

.. note::

    A :struct:`kOSProcessor` is a type of :struct:`PartModule`, and therefore can use all the suffixes of :struct:`PartModule`.

.. attribute:: kOSProcessor:MODE

    :access: Get only
    :type: :struct:`String`

    Indicates the current state of this processor. `OFF` - deactivated, `READY` - active, or `STARVED` - no power.

.. method:: kOSProcessor:ACTIVATE

    :returns: None

    Activate this processor

.. method:: kOSProcessor:DEACTIVATE

    :returns: None

    Deactivate this processor

.. attribute:: kOSProcessor:TAG

    :access: Get only
    :type: :struct:`String`

    This processor's name tag

.. attribute:: kOSProcessor:VOLUME

    :access: Get only
    :type: :struct:`Volume`

    This processor's hard disk.

.. attribute:: kOSProcessor:BOOTFILENAME

    :access: Get or Set
    :type: :struct:`String`

    The filename for the boot file on this processor. This may be set to an empty :ref:`string <string>` “” or to “None” to disable the use of a boot file.

.. attribute:: kOSProcessor:CONNECTION()

    :return: :struct:`Connection`

    Returns your connection to this processor.
