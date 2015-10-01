.. kuniverse:

KUniverse 4th wall methods
==========================


.. structure:: KUniverse

    :struct:`KUniverse` is a special structure that allows your kerboscript programs to access some of the functions that break the "4th Wall".  It serves as a place to access object directly connected to the KSP game itself, rather than the interaction with the KSP world (vessels, planets, orbits, etc.).

    .. list-table:: Members and Methods
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

.. attribute:: Config:CANREVERT

    :access: Get
    :type: boolean.

    Returns true if either revert to launch or revert to editor is available.  Note: either option may still be unavailable, use the specific methods below to check the exact option you are looking for.

.. attribute:: Config:CANREVERTTOLAUNCH

    :access: Get
    :type: boolean.

    Returns true if either revert to launch is available.

.. attribute:: Config:CANREVERTTOEDITOR

    :access: Get
    :type: boolean.

    Returns true if either revert to the editor is available.

.. attribute:: Config:REVERTTOLAUNCH

    :access: Method
    :type: None.

    Initiate the KSP game's revert to launch function.  All progress so far will be lost, and the vessel will be returned to the launch pad or runway at the time it was initially launched.

.. attribute:: Config:REVERTTOEDITOR

    :access: Method
    :type: None.

    Initiate the KSP game's revert to editor function.  The game will revert to the editor, as selected based on the vessel type.

.. method:: Config:REVERTTO(editor)

    :parameter editor: The editor identifier
    :return: none

    Revert to the provided editor.  Valid inputs are `"VAB"` and `"SPH"`.

.. attribute:: Config:ORIGINEDITOR

    :access: Get
    :type: string.

    Returns identifier of the orginating editor based on the vessel type.
