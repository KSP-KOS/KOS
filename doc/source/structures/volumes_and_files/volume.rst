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
          - :struct:`Scalar`
          - Free space left on the volume

        * - :attr:`CAPACITY`
          - :struct:`Scalar`
          - Total space on the volume

        * - :attr:`NAME`
          - :struct:`String`
          - Get or set volume name

        * - :attr:`RENAMEABLE`
          - :struct:`Scalar`
          - True if the name can be changed

        * - :attr:`ROOT`
          - :struct:`VolumeDirectory`
          - Volume's root directory

        * - :attr:`FILES`
          - :struct:`Lexicon`
          - Lexicon of all files and directories on the volume

        * - :attr:`POWERREQUIREMENT`
          - :struct:`Scalar`
          - Amount of power consumed when this volume is set as the current volume

        * - :meth:`EXISTS(path)`
          - :struct:`Boolean`
          - Returns true if the given file or directory exists

        * - :meth:`CREATE(path)`
          - :struct:`VolumeFile`
          - Creates a file

        * - :meth:`CREATEDIR(path)`
          - :struct:`VolumeDirectory`
          - Creates a directory

        * - :meth:`OPEN(path)`
          - :struct:`VolumeItem` or :struct:`Boolean`
          - Opens a file or directory

        * - :meth:`DELETE(path)`
          - :struct:`Boolean`
          - Deletes a file or directory

.. attribute:: Volume:FREESPACE

    :type: :struct:`Scalar`
    :access: Get only

    Free space left on the volume

.. attribute:: Volume:CAPACITY

    :type: :struct:`Scalar`
    :access: Get only

    Total space on the volume

.. attribute:: Volume:NAME

    :type: :struct:`String`
    :access: Get and Set

    Gets or sets volume name. This name can be used instead of the volumeId with some :ref:`file and volume-related commands<files>`

.. attribute:: Volume:RENAMEABLE

    :type: :struct:`Boolean`
    :access: Get only

    True if the name of this volume can be changed. Currently only the name of the archive can't be changed.

.. attribute:: Volume:FILES

    :type: :struct:`Lexicon` of :struct:`VolumeItem`
    :access: Get only

    List of files and directories on this volume. Keys are the names of all items on this volume and values are the associated :struct:`VolumeItem` structures.

.. attribute:: Volume:ROOT

    :type: :struct:`VolumeDirectory`
    :access: Get only

    Returns volume's root directory

.. attribute:: Volume:POWERREQUIREMENT

    :type: :struct:`Scalar`
    :access: Get only

    Amount of power consumed when this volume is set as the current volume

.. method:: Volume:EXISTS(path)

    :return: :struct:`Boolean`

    Returns true if the given file or directory exists. This will also return true when the given file does not exist, but there is a file with the same name and `.ks` or `.ksm` extension added.
    Use ``Volume:FILES:HASKEY(name)`` to perform a strict check.

    Paths passed as the argument to this command should not contain a volume id or name and should not be relative.

.. method:: Volume:OPEN(path)

    :return: :struct:`VolumeItem` or :struct:`Boolean` false

    Opens the file or directory pointed to by the given path and returns :struct:`VolumeItem`. It will return a boolean false if the given file or directory does not exist.

    Paths passed as the argument to this command should not contain a volume id or name and should not be relative.

.. method:: Volume:CREATE(path)

    :return: :struct:`VolumeFile`

    Creates a file under the given path and returns :struct:`VolumeFile`. It will fail if the file already exists.

    Paths passed as the argument to this command should not contain a volume id or name and should not be relative.

.. method:: Volume:CREATEDIR(path)

    :return: :struct:`VolumeDirectory`

    Creates a directory under the given path and returns :struct:`VolumeDirectory`. It will fail if the directory already exists.

    Paths passed as the argument to this command should not contain a volume id or name and should not be relative.

.. method:: Volume:DELETE(path)

    :return: boolean

    Deletes the given file or directory (recursively). It will return true if the given item was successfully deleted and false otherwise.

    Paths passed as the argument to this command should not contain a volume id or name and should not be relative.

