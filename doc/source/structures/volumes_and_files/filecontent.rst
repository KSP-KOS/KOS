.. _filecontent:

FileContent
================

Represents the contents of a file. You can obtain an instance of this class using :meth:`VolumeFile:READALL`.

Internally this class stores raw data (a byte array). It can be passed around as is, for example this will copy a file::

  SET CONTENTS TO OPEN("filename"):READALL.
  SET NEWFILE TO CREATE("newfile").
  NEWFILE:WRITE(CONTENTS).

You can parse the contents to read them as a string::

  SET CONTENTS_AS_STRING TO OPEN("filename"):READALL:STRING.
  // do something with a string:
  PRINT CONTENTS_AS_STRING:CONTAINS("test").

Instances of this class can be iterated over. In each iteration step a single line of the file will be read.

.. structure:: FileContent

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 4

        * - Suffix
          - Type
          - Description


        * - :attr:`LENGTH`
          - :ref:`scalar <scalar>`
          - File length (in bytes)
        * - :attr:`EMPTY`
          - :ref:`boolean <boolean>`
          - True if the file is empty
        * - :attr:`TYPE`
          - :struct:`String`
          - Type of the content
        * - :attr:`STRING`
          - :struct:`String`
          - Contents of the file decoded using UTF-8 encoding
        * - :attr:`BINARY`
          - :struct:`List`
          - Contents of the file as a list of bytes
        * - :attr:`ITERATOR`
          - :struct:`Iterator`
          - Iterates over the lines of a file
.. note::

    This type is serializable.


.. attribute:: FileContent:LENGTH

    :type: :ref:`scalar <scalar>`
    :access: Get only

    Length of the file.

.. attribute:: FileContent:EMPTY

    :type: :ref:`boolean <boolean>`
    :access: Get only

    True if the file is empty

.. attribute:: FileContent:TYPE

    :access: Get only
    :type: :struct:`String`

    Type of the content as a string. Can be one of the following:\

    TOOSHORT
        Content too short to establish a type

    ASCII
        A file containing ASCII text, like the result of a LOG command.

    KSM
        A type of file containing KerboMachineLanguage compiled code, that was created from the :ref:`COMPILE command <compiling>`.

    BINARY
        Any other type of file.

.. attribute:: FileContent:STRING

    :access: Get only
    :type: :struct:`String`

    Contents of the file decoded using UTF-8 encoding

.. attribute:: FileContent:BINARY

    :access: Get only
    :type: :struct:`List`

    Contents of the file as a list of bytes. Each item in the list is a number between 0 and 255 representing a single byte from the file.

.. attribute:: FileContent:ITERATOR

    :access: Get only
    :type: :struct:`Iterator`

    Iterates over the lines of a file
