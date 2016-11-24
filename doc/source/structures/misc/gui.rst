.. _widgets

Creating GUIs
=============

You can create an object that represents a GUI drawn on the
user's screen (not in the terminal window). It can have buttons,
labels, and the usual GUI elements. In combination with a :ref:`boot file <boot>`,
entirely GUI-driven vessel controls can be developed.

.. figure:: /_images/general/gui-HelloWorld.png
    :width: 100%

The "Hello World" program::

        // "Hello World" program for kOS GUI.
        //
        // Create a GUI window
        LOCAL gui TO GUI(200,100).
        // Add widgets to the GUI
        LOCAL label TO gui:ADDLABEL("Hello world!").
        SET label:ALIGN TO "CENTER".
        SET label:VSTRETCH TO True. // Take up spare space.
        LOCAL ok TO gui:ADDBUTTON("OK").
        // Show the GUI.
        gui:SHOW().
        // Handle GUI widget interactions.
        // Can safely wait and GUI will still be responsive.
        UNTIL ok:PRESSED { PRINT("Waiting for GUI"). WAIT(0.1). }
        // Hide when done (will also hide if power lost).
        gui:HIDE().

Creating a Window
-----------------

.. function:: GUI(width, height)

This creates a new ``GUI`` object that you can then manipulate
to build up a GUI::

        SET gui TO GUI(200,100).
        SET button TO gui:ADDBUTTON("OK").
        gui:SHOW().
        UNTIL button:PRESSED WAIT(0.1).
        gui:HIDE().

See the "ADD" functions in the :struct:`BOX` structure for
the other widgets you can add.

Structure Reference
-------------------

The GUI elements, including the GUI type itself are in the
following hierarchy:

- :struct:`WIDGET`
    - :struct:`BOX`
        - :struct:`GUI`
    - :struct:`LABEL`
        - :struct:`BUTTON`
        - :struct:`TEXTFIELD`
    - :struct:`SLIDER`
    - :struct:`SPACING`


.. structure:: GUI

    This object is created with the GUI(width,height) function.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
                   Every suffix of :struct:`BOX`
    -----------------------------------------------------------------------------------
    :attr:`X`                             :struct:`scalar` (pixels)       X-position of the window. Negative values measure from the right side of the screen.
    :attr:`Y`                             :struct:`scalar` (pixels)       Y-position of the window. Negative values measure from the bottom of the screen.
    ===================================== =============================== =============

.. structure:: Widget

    This object is the base class of all GUI elements.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
    :attr:`MARGIN`                        :struct:`scalar` (pixels)       Spacing between this and other widgets.
    :attr:`PADDING`                       :struct:`scalar` (pixels)       Spacing between the outside of the widget and its contents.
    :attr:`WIDTH`                         :struct:`scalar` (pixels)       Fixed width (or 0 if flexible).
    :attr:`HEIGHT`                        :struct:`scalar` (pixels)       Fixed height (or 0 if flexible).
    :meth:`SHOW`                                                          Show the widget. All except GUI objects are shown by default.
    :meth:`HIDE`                                                          Hide the widget.
    :attr:`ENABLED`                       :struct:`Boolean`               Set to False to "grey out" the widget, preventing user interaction.
    :attr:`HSTRETCH`                      :struct:`Boolean`               Should the widget stretch horizontally?
    :attr:`VSTRETCH`                      :struct:`Boolean`               Should the widget stretch vertically?
    :attr:`BG`                            :struct:`string`                Name of a "9-slice" image file. See note below.
    :attr:`HBORDER`                       :struct:`scalar` (pixels)       Left and right column counts for BG image border.
    :attr:`VBORDER`                       :struct:`scalar` (pixels)       Top and bottom row counts for BG image border.
    ===================================== =============================== =============

.. note::

    The `BG` attribute comes in 8 flavours: `BG`, `BG_FOCUSED`, `BG_ACTIVE`, `BG_ON`, `BG_FOCUSED_ON`, `BG_ACTIVE_ON`, and `BG_HOVER_ON`.

    ============= =====================================================
    FOCUSED       The widget has keyboard focus.
    ACTIVE        The widget is hot (eg. button is being held down).
    HOVER         The widget is under the mouse.
    ON            The widget is on (eg. button is PRESSED).
    ============= =====================================================

    .. image:: /_images/general/9-slice.png
        :align: right

    The image file is a "9-slice" image, where the corners of the image are used as-is, but the pixels
    between them are stretched to make the full size of image required.
    The :attr:`VBORDER` attribute defines the top and bottom rows of pixels (default 8), and
    the :attr:`HBORDER` attribute defines the left and right rows of pixels (default 15).

    The image files are found relative to Ships/Scripts directory and specifying a ".png" extension is optional.

    If set to "", the background will default to the non-ON image and then default to the normal `BG` image,
    and then default to completely transparent.

.. structure:: Box

    `Box` objects are themselves created from other Box objects via ADDHBOX and other methods. The root `Box` is
    created with the GUI(width,height) function.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
                   Every suffix of :struct:`WIDGET`
    -----------------------------------------------------------------------------------
    :meth:`ADDLABEL(text)`                :struct:`Label`                 Creates a label in the Box.
    :meth:`ADDBUTTON(text)`               :struct:`Button`                Creates a clickable button in the Box.
    :meth:`ADDCHECKBOX(text)`             :struct:`Button`                Creates a toggleable button in the Box.
    :meth:`ADDTEXTFIELD(text)`            :struct:`TextField`             Creates an editable text field in the Box.
    :meth:`ADDHSLIDER(min,max)`           :struct:`Slider`                Creates a horizontal slider in the Box, slidable from min to max.
    :meth:`ADDVSLIDER(min,max)`           :struct:`Slider`                Creates a vertical slider in the Box, slidable from min to max.
    :meth:`ADDHBOX`                       :struct:`Box`                   Creates a nested horizontally-arranged Box in the Box.
    :meth:`ADDVBOX`                       :struct:`Box`                   Creates a nested vertically Box in the Box.
    :meth:`ADDSTACK`                      :struct:`Box`                   Creates a nested stacked Box in the Box. Only the first enabled subwidget is ever shown. See :meth:`SHOWONLY` below.
    :meth:`ADDSPACING(size)`              :struct:`Spacing`               Creates a blank space of the given size (flexible if -1).
    :attr:`WIDGETS`                       :struct:`List(Widget)`          Returns a LIST of the widgets that have been added to the Box.
    :meth:`SHOWONLY(widget)`                                              Hide all but the given widget.
    ===================================== =============================== =============

.. structure:: Label

    `Label` objects are created inside Box objects via ADDLABEL method.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
                   Every suffix of :struct:`WIDGET`
    -----------------------------------------------------------------------------------
    :attr:`TEXT`                          :struct:`string`                The text on the label. May include some markup. See RICHTEXT below.
    :attr:`IMAGE`                         :struct:`string`                The name of an image for the label. The images are in the Ships/Script directory and ".png" is optional.
    :attr:`TOOLTIP`                       :struct:`string`                A tooltip for the label.
    :attr:`ALIGN`                         :struct:`string`                One of "CENTER", "LEFT", or "RIGHT".
    :attr:`FONTSIZE`                      :struct:`scalar`                The size of the text on the label.
    :attr:`RICHTEXT`                      :struct:`Boolean`               Set to False to disable rich-text (<i>...</i>, etc.)
    :attr:`TEXTCOLOR`                     :ref:`Color <colors>`           The color of the text on the label.
    ===================================== =============================== =============

.. structure:: Button

    `Button` objects are created inside Box objects via ADDBUTTON and ADDCHECKBOX methods.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
                   Every suffix of :struct:`LABEL`
    -----------------------------------------------------------------------------------
    :attr:`PRESSED`                       :struct:`Boolean`               Has the button been pressed?
    :meth:`SETTOGGLE`                     :struct:`Boolean`               Set to True to make the button toggle between pressed and not pressed, like a :struct:`CheckBox`.
    ===================================== =============================== =============

.. note::

    Unless SETTOGGLE(True) is called, the value of :attr:`PRESSED` resets to False as
    soon as the value is accessed.

    If the Button is created by the Button:ADDCHECKBOX method, it will have a different visual
    style and it will start already in toggle mode.

.. structure:: TextField

    `TextField` objects are created inside Box objects via ADDTEXTFIELD method.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
                   Every suffix of :struct:`LABEL`
    -----------------------------------------------------------------------------------
    :attr:`CHANGED`                       :struct:`Boolean`               Has the text been edited?
    :attr:`CONFIRMED`                     :struct:`Boolean`               Has the user pressed Return in the field?
    ===================================== =============================== =============

.. note::

    The values of :attr:`CHANGED` and :attr:`CONFIRMED` reset to False as soon as their value is accessed.

.. structure:: Slider

    `Slider` objects are created inside Box objects via ADDHSLIDER and ADDVSLIDER methods.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
                   Every suffix of :struct:`WIDGET`
    -----------------------------------------------------------------------------------
    :attr:`VALUE`                         :struct:`scalar`                The current value. Initially set to :attr:`MIN`.
    :attr:`MIN`                           :struct:`scalar`                The minimum value (leftmost on horizontal slider).
    :attr:`MAX`                           :struct:`scalar`                The maximum value (bottom on vertical slider).
    ===================================== =============================== =============

.. structure:: Spacing

    `Spacing` objects are created inside Box objects via ADDSPACING method.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
                   Every suffix of :struct:`WIDGET`
    -----------------------------------------------------------------------------------
    :attr:`AMOUNT`                        :struct:`scalar`                The amount of space, or -1 for flexible spacing.
    ===================================== =============================== =============

.. note::

    If a GUI does not have enough content to fill the size it is set for, the bottom will be transparent but still
    clickable. It is recommended that you use an expanding widget or flexible spacing to ensure all space is used.

