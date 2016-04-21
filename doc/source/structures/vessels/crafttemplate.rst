.. _crafttemplate:

CraftTemplate
=============

.. structure:: CraftTemplate

    You can access :struct:`CraftTemplate` objects from the :struct:`KUniverse`
    bound variable.  Templates can be used to launch new vessels, or read initial
    data about a craft, such as the description.

    .. list-table::
        :header-rows: 1
        :widths: 1 1 2

        * - Suffix
          - Type
          - Description

        * - :attr:`NAME`
          - :struct:`String`
          - Name of this craft template
        * - :attr:`FILEPATH`
          - :struct:`String`
          - The path to the craft file
        * - :attr:`DESCRIPTION`
          - :struct:`String`
          - The description as saved in the editor
        * - :attr:`EDITOR`
          - :struct:`String`
          - The editor where this craft was saved
        * - :attr:`LAUNCHSITE`
          - :struct:`String`
          - The default launch site for this craft
        * - :attr:`MASS`
          - :struct:`Scalar`
          - The default mass of the craft
        * - :attr:`COST`
          - :struct:`Scalar`
          - The default cost of the craft
        * - :attr:`PARTCOUNT`
          - :struct:`Scalar`
          - The total number of parts in this craft.


.. attribute:: CraftTemplate:NAME

    :access: Get only
    :type: :struct:`String`

    Returns the name of the craft.  It may differ from the file name.

.. attribute:: CraftTemplate:FILEPATH

    :access: Get only
    :type: :struct:`String`

    Returns the absolute file path to the craft file.

.. attribute:: CraftTemplate:DESCRIPTION

    :access: Get only
    :type: :struct:`String`

    Returns the description field of the craft, which may be edited from the
    drop down window below the craft name in the editor.

.. attribute:: CraftTemplate:EDITOR

    :access: Get only
    :type: :struct:`String`

    Name of the editor from which the craft file was saved.  Valid values are
    ``"VAB"`` and ``"SPH"``.

.. attribute:: CraftTemplate:LAUNCHSITE

    :access: Get only
    :type: :struct:`String`

    Returns the name of the default launch site of the craft.  Valid values are
    ``"LAUNCHPAD"`` and ``"RUNWAY"``.

.. attribute:: CraftTemplate:MASS

    :access: Get only
    :type: :struct:`Scalar`

    Returns the total default mass of the craft.  This includes the dry mass and the
    mass of any resources loaded onto the craft by default.

.. attribute:: CraftTemplate:COST

    :access: Get only
    :type: :struct:`Scalar`

    Returns the total default cost of the craft.  This includes the cost of the
    vessel itself as well as any resources loaded onto the craft by default.

.. attribute:: CraftTemplate:PARTCOUNT

    :access: Get only
    :type: :struct:`Scalar`

    Returns the total number of parts on the craft.
