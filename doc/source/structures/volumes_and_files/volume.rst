.. _volume:

Volume
======

Represents a :struct:`kOSProcessor` hard disk or the archive.


.. structure:: Volume

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`FREESPACE`
          - scalar
          - Free space left on the volume

        * - :attr:`CAPACITY`
          - scalar
          - Total space on the volume

        * - :attr:`NAME`
          - `String`
          - Volume name

        * - :attr:`RENAMEABLE`
          - scalar
          - True if the name can be changed

        * - :attr:`FILES`
          - :struct:`Lexicon`
          - Lexicon of all files on the volume

        * - :attr:`POWERREQUIREMENT`
          - scalar
          - Amount of power consumed when this volume is set as the current volume

        * - :meth:`EXISTS(filename)`
          - boolean
          - Returns true if the given file exists

        * - :meth:`CREATE(filename)`
          - :struct:`VolumeFile`
          - Creates a file

        * - :meth:`OPEN(filename)`
          - :struct:`VolumeFile`
          - Opens a file

        * - :meth:`DELETE(filename)`
          - boolean
          - Deletes a file

.. attribute:: Volume:FREESPACE

    :type: scalar
    :access: Get only

    Free space left on the volume

.. attribute:: Volume:CAPACITY

    :type: scalar
    :access: Get only

    Total space on the volume

.. attribute:: Volume:NAME

    :type: `String`
    :access: Get only

    Volume name. This name can be used instead of the volumeId with some :ref:`file and volume-related commands<files>`

.. attribute:: Volume:RENAMEABLE

    :type: boolean
    :access: Get only

    True if the name of this volume can be changed. Currently only the name of the archive can't be changed.


.. attribute:: Volume:FILES

    :type: :struct:`Lexicon` of :struct:`VolumeFile`
    :access: Get only

    List of files on this volume. Keys are the names of all files on this volume and values are the associated :struct:`VolumeFile` structures.


.. attribute:: Volume:POWERREQUIREMENT

    :type: scalar
    :access: Get only

    Amount of power consumed when this volume is set as the current volume


.. method:: Volume:EXISTS(filename)

    :return: boolean

    Returns true if the given file exists. This will also return true when the given file does not exist, but there is a file with the same name and `.ks` or `.ksm` extension added.
    Use ``Volume:FILES:HASKEY(filename)`` to perform a strict check.

.. method:: Volume:OPEN(filename)

    :return: :struct:`VolumeFile`

    Opens the file with the given name and returns :struct:`VolumeFile`. It will fail if the file doesn't exist.

.. method:: Volume:CREATE(filename)

    :return: :struct:`VolumeFile`

    Creates a file with the given name and returns :struct:`VolumeFile`. It will fail if the file already exists.

.. method:: Volume:DELETE(filename)

    :return: boolean

    Deletes the given file. It will return true if file was successfully deleted and false otherwise.

