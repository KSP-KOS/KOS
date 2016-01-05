.. _fileinfo:

File Information
================

File name and size information. You can obtain a list of values of type FileInfo using the :ref:`LIST FILES <list command>` command.

.. structure:: FileInfo

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 4

        * - Suffix
          - Type
          - Description


        * - :attr:`NAME`
          - string
          - Name of the file including extension
        * - :attr:`FILETYPE`
          - string
          - Type of the file
        * - :attr:`SIZE`
          - integer (bytes)
          - Size of the file


.. attribute:: FileInfo:NAME

    :access: Get only
    :type: string

    name of the file, including its file extension.

.. attribute:: FileInfo:FILETYPE

    :access: Get only
    :type: string

    Type of the file as a string. Can be one of the following:\

    ASCII
        A file containing ASCII text, like the result of a LOG command.

    KERBOSCRIPT
        (unimplemented) A type of ASCII file containing Kerboscript ascii code. At the moment this type does not ever get returned. You will always get files of type ASCII instead.

    KSM
        A type of file containing KerboMachineLanguage compiled code, that was created from the :ref:`COMPILE command <compiling>`.

    UNKNOWN
        Any other type of file.

.. attribute:: FileInfo:SIZE

    :access: Get only
    :type: scalar

    size of the file, in bytes.

