.. _volumeitem:

VolumeItem
==========

Contains suffixes common to :struct:`files <VolumeFile>` and :struct:`directories <VolumeDirectory>`.

.. structure:: VolumeItem

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 4

        * - Suffix
          - Type
          - Description


        * - :attr:`NAME`
          - :struct:`String`
          - Name of the item including extension
        * - :attr:`EXTENSION`
          - :struct:`String`
          - Item extension
        * - :attr:`SIZE`
          - :struct:`Scalar`
          - Size of the file
        * - :attr:`ISFILE`
          - :struct:`Boolean`
          - Size of the file

.. attribute:: VolumeItem:NAME

    :access: Get only
    :type: :struct:`String`

    Name of the item, including the extension.

.. attribute:: VolumeItem:EXTENSION

    :access: Get only
    :type: :struct:`String`

    Item extension (part of the name after the last dot).

.. attribute:: VolumeItem:SIZE

    :access: Get only
    :type: :struct:`Scalar`

    Size of the item, in bytes.

.. attribute:: VolumeItem:ISFILE

    :access: Get only
    :type: :struct:`Boolean`

    True if this item is a file

