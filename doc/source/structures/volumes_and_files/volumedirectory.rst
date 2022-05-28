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

        * - :meth:`LEXICON`
          - :struct:`LEXICON` of :struct:`VolumeFile` or :struct:`VolumeDirectory`
          - Lists all files and directories

        * - :meth:`LEX`
          - :struct:`LEXICON` of :struct:`VolumeFile` or :struct:`VolumeDirectory`
          - Alias for :meth:`LEXICON`

        * - :meth:`LIST`
          - :struct:`LEXICON` of :struct:`VolumeFile` or :struct:`VolumeDirectory`
          - Alias for :meth:`LEXICON`


.. method:: VolumeDirectory:LEXICON

    :return: :struct:`Lexicon` of :struct:`VolumeFile` or :struct:`VolumeDirectory`

    Returns a Lexicon of all files and directories in this directory,
    with each pair in the Lexcion having the string name of the file or directory
    as the key, and the :struct:`VolumeFile` or :struct:`VolumeDirectory` itself
    as the value.

.. method:: VolumeDirectory:LEX

    An alias for :meth:`LEXICON`.

.. method:: VolumeDirectory:LIST

    An alias for :meth:`LEXICON`. It's slightly wrong that a method called "List"
    returns a Lexicon instead of a List, but it has been that way long enough that
    now for backward compatibility, the name "List" had to remain as an alias
    for this method.
