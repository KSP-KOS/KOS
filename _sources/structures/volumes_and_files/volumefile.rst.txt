.. _volumefile:

VolumeFile
==========

File name and size information. You can obtain a list of values of type VolumeFile using the :ref:`LIST FILES <list command>` command.

.. structure:: VolumeFile

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 4

        * - Suffix
          - Type
          - Description

        * - All suffixes of :struct:`VolumeItem`
          -
          - :struct:`VolumeFile` objects are a type of :struct:`VolumeItem`

        * - :meth:`READALL`
          - :struct:`FileContent`
          - Reads file contents
        * - :meth:`WRITE(String|FileContent)`
          - :struct:`Boolean`
          - Writes the given string to the file
        * - :meth:`WRITELN(string)`
          - :struct:`Boolean`
          - Writes the given string and a newline to the file
        * - :meth:`CLEAR`
          - None
          - Clears this file


.. method:: VolumeFile:READALL

    :return: :struct:`FileContent`

    Reads the content of the file.

.. method:: VolumeFile:WRITE(String|FileContent)

    :return: :struct:`Boolean`

    Writes the given string or a :struct:`FileContent` to the file. Returns true if successful (lack of space on the :struct:`Volume` can cause a failure).

.. method:: VolumeFile:WRITELN(string)

    :return: :struct:`Boolean`

    Writes the given string followed by a newline to the file. Returns true if successful.

.. method:: VolumeFile:CLEAR

    :return: None

    Clears this file
