.. _settingsWindows:

kOS Settings Windows
====================

.. _applauncher:

kOS Control Panel
-----------------

This panel behaves like the other display panels the launcher creates,
and operates mutually exclusively with them. (For example you can't
make the kOS App Control Panel appear at the same time as the stock
Resource display panel. Opening one toggles the other one off,
and visa versa.)

Here is an annotated image of the control panel and what it does:

.. figure:: /_images/general/controlPanelWindow.png
    :width: 80 %

Key Notes:

1. Contains every vessel and associated kOS processor that is currently fully loaded into the game.  kOS processors that are not currently in "physics range" because they are far enough away to be unloaded will not appear in the list, as they are not loaded and thus cannot do anything.
2. kOS version number.
3. kOS processor part name and name tag.
4. Power status display.  This is not an interactable button so as not to bypass attempts to lock out control of events/actions.
5. Toggle button to open or close the terminal window.
6. Toolbar button, click to toggle the control panel window on and off.
7. Toggle button to activate or deactivate telnet. See :attr:`Config:TELNET`.
8. Displays or sets the port that the telnet server will listen on. See :attr:`Config:TPORT`.
9. Toggle button to enable or disable forcing the telnet server to only listen on the local loopback address. See :attr:`Config:LOOPBACK`.
10. When you hover your cursor over a processor it will be highlighted purple.

.. _settingsWindow:

KSP Difficulty Settings Window
------------------------------

.. note::
    .. versionadded:: v1.0.2
        Previous versions of kOS kept all settings accessible from the App
        Launcher Window.  KSP version 1.2.0 introduced a new way to
        store settings within the save file itself, and most settings were
        migrated to this system/window.

This settings window is accessible when you first start a new game by clicking
on "Difficulty Options", or in an existing game by clicking on "Difficulty
Options" from the in game settings menu (accessed by pressing the :kbd:`Escape`
key, and then clicking "Settings" from the pop up window).

.. note::

    The only reason these settings are on the difficulty options screen is that
    it's the only place KSP allows mods like kOS to add a new section of custom
    settings to the user interface.  **Don't think of it as "cheating" to
    change them mid-game** because they're **not really difficulty options**,
    despite the name.

.. list-table:: Difficulty Buttons
    :header-rows: 1

    * - New game difficulty button
      - In game difficulty button
    * - .. image:: /_images/general/newGameDifficultyButton.png
      - .. image:: /_images/general/inGameDifficultyButton.png

By selecting the kOS tab of the Difficulty Settings Menu, you will be presented
with the following options.  All settings displayed in this window are local to
the current save game.

.. figure:: /_images/general/settingsWindow.png
    :width: 80%

Key Notes:

1. All CPU's run at a speed that executes up to this many kRISC opcodes per physics 'tick'. See :attr:`Config:IPU`
2. When storing local volumes' data in the saved game, it will be compressed then base64 encoded. See :attr:`Config:UCP`
3. After the outermost program is finished, you will see some profiling output describing how fast it ran. See :attr:`Config:STAT`
4. When launching a new ship, or reloading a scene, the default volume will start as 0 instead of 1. See :attr:`Config:ARCH`
5. When you press the "Hide UI" button (F2 in default bindings) kOS's terminals will hide themselves too. See :attr:`Config:OBEYHIDEUI`
6. kOS will throw an error if Infinity or Not-A-Number is the result of any expression.  This ensures no such values can ever get passed in to KSP's stock API, which doesn't protect itself against their effects. See :attr:`Config:SAFE`
7. When kOS throws an error, you hear a sound effect. See :attr:`Config:AUDIOERR`
8. When kOS has an error, some error messages have alternative longer paragraph-length descriptions that this enables. See :attr:`Config:VERBOSE`
9. If you have the "Blizzy Toolbar" mod installed, only put the kOS button on it instead of both it and the stock toolbar.
10. (For mod developers) Spams the Unity log file with a message for every time an opcode is executed in the virtual machine.  Very laggy. See :attr:`Config:DEBUGEACHOPCODE`
11. :ref:`Connectivity manager<connectivityManagers>` selector
12. List of all available :ref:`connectivity managers<connectivityManagers>`.
13. Brightness of a kOS terminal when it appears for the first time in a scene. (You must reload the scene to see the effect of any changes to this slider.)
