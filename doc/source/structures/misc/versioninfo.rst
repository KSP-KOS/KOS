.. _versioninfo:

A ``VersionInfo`` is a structure that breaks the version string for
kOS down into its component fields as numbers (rather than as
a string) so you can look at them one at a time.

You obtain a :struct:`VersionInfo` by calling :attr:`CORE:VERSION`.

Structure
---------

.. structure:: VersionInfo

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 1 4

        * - Suffix
          - Type
          - Get/Set
          - Description

        * - :attr:`MAJOR`
          - :struct:`Scalar`
          - get only
          - The "NN" in the version string ``vNN.xx.xx.xx``.

        * - :attr:`MINOR`
          - :struct:`Scalar`
          - get only
          - The "NN" in the version string ``vxx.NN.xx.xx``.

        * - :attr:`PATCH`
          - :struct:`Scalar`
          - get only
          - The "NN" in the version string ``vxx.xx.NN.xx``.

        * - :attr:`BUILD`
          - :struct:`Scalar`
          - get only
          - The "NN" in the version string ``vxx.xx.xx.NN``.

.. attribute:: VersionInfo:MAJOR

    :access: Get only
    :type: :struct:`Scalar`.

    The first number in the version string.  i.e. the
    "NN" in the version string ``vNN.xx.xx.xx``.

.. attribute:: VersionInfo:MINOR

    :access: Get only
    :type: :struct:`Scalar`.

    The second number in the version string.  i.e. the
    "NN" in the version string ``vxx.NN.xx.xx``.

.. attribute:: VersionInfo:PATCH

    :access: Get only
    :type: :struct:`Scalar`.

    The third number in the version string.  i.e. the
    "NN" in the version string ``vxx.xx.NN.xx``.

.. attribute:: VersionInfo:BUILD

    :access: Get only
    :type: :struct:`Scalar`.

    The fourth number in the version string.  i.e. the
    "NN" in the version string ``vxx.xx.xx.NN``.
