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

.. attribute:: KUniverse:CANREVERT

    :access: Get
    :type: boolean.

    Returns true if either revert to launch or revert to editor is available.  Note: either option may still be unavailable, use the specific methods below to check the exact option you are looking for.

.. attribute:: KUniverse:CANREVERTTOLAUNCH

    :access: Get
    :type: boolean.

    Returns true if either revert to launch is available.

.. attribute:: KUniverse:CANREVERTTOEDITOR

    :access: Get
    :type: boolean.

    Returns true if either revert to the editor is available.

.. attribute:: KUniverse:REVERTTOLAUNCH

    :access: Method
    :type: None.

    Initiate the KSP game's revert to launch function.  All progress so far will be lost, and the vessel will be returned to the launch pad or runway at the time it was initially launched.

.. attribute:: KUniverse:REVERTTOEDITOR

    :access: Method
    :type: None.

    Initiate the KSP game's revert to editor function.  The game will revert to the editor, as selected based on the vessel type.

.. method:: KUniverse:REVERTTO(editor)

    :parameter editor: The editor identifier
    :return: none

    Revert to the provided editor.  Valid inputs are `"VAB"` and `"SPH"`.

.. attribute:: KUniverse:ORIGINEDITOR

    :access: Get
    :type: string.

    Returns identifier of the orginating editor based on the vessel type.

.. attribute:: KUniverse:DEFAULTLOADDISTANCE

    :access: Get
    :type: :struct:`LoadDistance`.

    Get or set the default loading distance for vessels loaded in the future.  Note: this setting will not affect any vessel currently in the universe for the current flight session.  It will take effect the next time you enter a flight scene from the editor or tracking station.

.. attribute:: KUniverse:ACTIVEVESSEL

    :access: Get/Set
    :type: :struct:`Vessel`.

    Returns the active vessel object and allows you to set the active vessel.  Note: KSP will not allow you to change vessels by default when in the atmosphere or when the vessel is under acceleration.  Use :method:`FORCEACTIVE` under those circumstances.

.. method:: KUniverse:FORCEACTIVE(vessel)

    :parameter vessel: :struct:`Vessel` to switch to.
    :return: none

    Force KSP to change the active vessel to the one specified.  Note: Switching the active vessel under conditions that KSP normally disallows may cause unexpected results on the initial vessel.  It is possible that the vessel will be treated as if it is re-entering the atmosphere and deleted.
