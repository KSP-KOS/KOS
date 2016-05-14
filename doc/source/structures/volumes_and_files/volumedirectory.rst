.. _volumedirectory:

VolumeDirectory
===============

Represents a directory on a kOS file system.

Instances of this class are enumerable, every step of iteration will provide a :struct:`VolumeFile` or a :struct:`VolumeDirectory` contained in this directory.

.. structure:: VolumeDirectory

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 4

        * - Suffix
          - Type
          - Description

        * - All suffixes of :struct:`VolumeItem`
          -
          - :struct:`VolumeDirectory` objects are a type of :struct:`VolumeItem`

        * - :meth:`LIST`
          - :struct:`List` of :struct:`VolumeFile` or :struct:`VolumeDirectory`
          - Lists all files and directories


.. method:: VolumeDirectory:LIST

    :return: :struct:`List` of :struct:`VolumeFile` or :struct:`VolumeDirectory`

    Returns a list of all files and directories in this directory.

