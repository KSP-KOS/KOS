.. _path:

Path
====

Represents a path. Contains suffixes that can be helpful when using and manipulating paths. You can use
:ref:`path() <path_command>` to create new instances.

Instances of this structure can be passed as arguments instead of ordinary, string paths, for example::

  copypath("../file", path()).

.. structure:: Path

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`VOLUME`
          - :struct:`Volume`
          - Volume this path belongs to
        * - :attr:`SEGMENTS`
          - :struct:`List` of :struct:`String`
          - List of this path's segments
        * - :attr:`LENGTH`
          - :struct:`Scalar`
          - Number of segments in this path
        * - :attr:`NAME`
          - :struct:`String`
          - Name of file or directory this path points to
        * - :attr:`HASEXTENSION`
          - :struct:`Boolean`
          - True if path contains an extension
        * - :attr:`EXTENSION`
          - :struct:`String`
          - This path's extension
        * - :attr:`ROOT`
          - :struct:`Path`
          - Root path of this path's volume
        * - :attr:`PARENT`
          - :struct:`Path`
          - Parent path
        * - :meth:`CHANGENAME(name)`
          - :struct:`Path`
          - Returns a new path with its name (last segment) changed
        * - :meth:`CHANGEEXTENSION(extension)`
          - :struct:`Path`
          - Returns a new path with extension changed
        * - :meth:`ISPARENT(path)`
          - :struct:`Boolean`
          - True if `path` is the parent of this path
        * - :meth:`COMBINE(name1, [name2, ...])`
          - :struct:`Path`
          - Returns a new path created by adding further elements to this one

.. attribute:: Path:VOLUME

    :type: :struct:`Volume`
    :access: Get only

    Volume this path belongs to.

.. attribute:: Path:SEGMENTS

    :type: :struct:`List` of :struct:`String`
    :access: Get only

    List of segments this path contains. Segments are parts of the path separated by `/`. For example path `0:/directory/subdirectory/script.ks` contains the following segments:
    `directory`, `subdirectory` and `script.ks`.

.. attribute:: Path:LENGTH

    :type: :struct:`Scalar`
    :access: Get only

    Number of this path's segments.

.. attribute:: Path:NAME

    :type: :struct:`String`
    :access: Get only

    Name of file or directory this path points to (same as the last segment).


.. attribute:: Path:HASEXTENSION

    :type: :struct:`Boolean`
    :access: Get only

    True if the last segment of this path has an extension.

.. attribute:: Path:EXTENSION

    :type: :struct:`String`
    :access: Get only

    Extension of the last segment of this path.

.. attribute:: Path:ROOT

    :type: :struct:`Path`
    :access: Get only

    Returns a new path that points to the root directory of this path's volume.

.. attribute:: Path:PARENT

    :type: :struct:`Path`
    :access: Get only

    Returns a new path that points to this path's parent. This method will throw an exception if this path does not have a parent (its length is 0).

.. method:: Path:CHANGENAME(name)

    :parameter name: :struct:`String` new path name
    :return: :struct:`Path`

    Will return a new path with the value of the last segment of this path replaced (or added if there's none).

.. method:: Path:CHANGEEXTENSION(extension)

    :parameter extension: :struct:`String` new path extension
    :return: :struct:`Path`

    Will return a new path with the extension of the last segment of this path replaced (or added if there's none).

.. method:: Path:ISPARENT(path)

    :parameter path: :struct:`Path` path to check
    :return: :struct:`Boolean`

    Returns true if `path` is the parent of this path.

.. method:: Path:COMBINE(name1, [name2, ...])

    :parameter name: :struct:`String` segments to add
    :return: :struct:`Path`

    Returns a new path that represents the file or directory
    that would be reached by starting from this path and then
    appending the path elements given in the list.

    e.g::
    
        set p to path("0:/home").
        set p2 to p:combine("d1", "d2", "file.ks").
        print p2
        0:/home/d1/d2/file.ks

