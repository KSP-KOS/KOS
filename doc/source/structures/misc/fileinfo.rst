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
        * - :attr:`MODIFIED`
          - string
          - The date the file was last modified
        * - :attr:`CREATED`
          - string
          - The date the file was first created


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

.. attribute:: FileInfo:MODIFIED

    :access: Get only
    :type: string

    The date the file was last modified, in :ref:`Real World Timestamp <real world timestamp>` format, described below.

.. attribute:: FileInfo:CREATED

    :access: Get only
    :type: string

    The date the file was first created, in :ref:`Real World Timestamp <real world timestamp>` format, described below.


.. _real world timestamp:

Real World Timestamp
--------------------

These timestamps are NOT in Kerbal Space Program's simulated clock, but are in real world time. This is for a good reason: the files exist outside of any one saved game and are global to all saved games you have. The format of the real-world timestamps is as follows::

    YYYY-MM-DDThh:mm:ss.sssssZ

Where:

YYYY
    The Four-digit year.
MM
    The Two-digit month, padded with zeroes (i.e. September is '09' rather than '9'.)
DD
    The Two-digit day of month, padded with zeroes (i.e. the 5th of the month is '05' rather than '5'.)
T
    Always a hardcoded capital letter "T".
hh
    The 24-hour clock time (5 AM is 05, 5 PM is 18).
mm
    The Two-digit minute-hand, padded with zeroes.
ss.ssss
    The seconds-hand, padded with zeroes to at least 2 digits before the decimal point, and a varying number of digits after that. It can store fractional parts of the second.
Z
    Always a hardcoded capital letter "Z", meaning its the local timezone-less timestamp.

This string format should be possible to sort on directly. This example checks if a file exists on the current volume::

    DECLARE PARAMETER searchFile.
    LIST FILES IN fileList.
    SET exists to FALSE.

    FOR file IN fileList {
        IF file:NAME = searchFile {
            set exists to TRUE.
        }
    }

    IF exists {
        PRINT searchFile + " exists".
    } ELSE {
        PRINT searchFile + " does not exist".
    }

