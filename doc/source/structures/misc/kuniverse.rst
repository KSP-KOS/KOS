.. kuniverse:

KUniverse 4th wall methods
==========================


.. structure:: KUniverse

    :struct:`KUniverse` is a special structure that allows your kerboscript programs to access some of the functions that break the "4th Wall".  It serves as a place to access object directly connected to the KSP game itself, rather than the interaction with the KSP world (vessels, planets, orbits, etc.).

    .. list-table:: Members (all Gettable and Settable)
        :header-rows: 1
        :widths: 3 1 1 4

        * - Suffix
          - Type
          - Get/Set
          - Description

        * - :attr:`CANREVERT`
          - boolean
          - Get
          - Is any revert possible?
        * - :attr:`CANREVERTTOLAUNCH`
          - boolean
          - Get
          - Is revert to launch possible?
        * - :attr:`CANREVERTTOEDITOR`
          - boolean
          - Get
          - Is revert to editor possible?
        * - :attr:`REVERTTOLAUNCH`
          - none
          - Method
          - Invoke revert to launch
        * - :attr:`REVERTTOEDITOR`
          - none
          - Method
          - Invoke revert to editor
        * - :attr:`REVERTTO(name)`
          - string
          - Method
          - Invoke revert to the named editor
        * - :attr:`ORIGINEDITOR`
          - string
          - Get
          - Returns the name of this vessel's editor.
