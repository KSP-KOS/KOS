.. _volumefile:

VolumeFile
================

File name and size information. You can obtain a list of values of type VolumeFile using the :ref:`LIST FILES <list command>` command.

.. structure:: VolumeFile

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 4

        * - Suffix
          - Type
          - Description


        * - :attr:`NAME`
          - :struct:`String`
          - Name of the file including extension
        * - :attr:`EXTENSION`
          - :struct:`String`
          - File extension
        * - :attr:`SIZE`
          - integer (bytes)
          - Size of the file
        * - :meth:`READALL`
          - :struct:`FileContent`
          - Reads file contents
        * - :meth:`WRITE(String|FileContent)`
          - boolean
          - Writes the given string to the file
        * - :meth:`WRITELN(string)`
          - :struct:`FileContent`
          - Writes the given string and a newline to the file
        * - :meth:`CLEAR`
          - None
          - Clears this file


.. attribute:: VolumeFile:NAME

    :access: Get only
    :type: :struct:`String`

    name of the file, including its file extension.

.. attribute:: VolumeFile:EXTENSION

    :access: Get only
    :type: :struct:`String`

    File extension (part of the filename after the last dot).

.. attribute:: VolumeFile:SIZE

    :access: Get only
    :type: scalar

    size of the file, in bytes.


.. method:: VolumeFile:READALL

    :return: :struct:`FileContent`

    Reads the content of the file.

.. method:: VolumeFile:WRITE(String|FileContent)

    :return: boolean

    Writes the given string or a :struct:`FileContent` to the file. Returns true if successful (lack of space on the :struct:`Volume` can cause a failure).

.. method:: VolumeFile:WRITELN(string)

    :return: boolean

    Writes the given string followed by a newline to the file. Returns true if successful.

.. method:: VolumeFile:CLEAR

    :return: None

    Clears this file
