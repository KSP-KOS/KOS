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
        LOCAL gui TO GUI(200).
        // Add widgets to the GUI
        LOCAL label TO gui:ADDLABEL("Hello world!").
        SET label:ALIGN TO "CENTER".
        SET label:HSTRETCH TO True. // Fill horizontally
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

.. function:: GUI(width [, height])

This creates a new ``GUI`` object that you can then manipulate
to build up a GUI. If no height is specified, it will resize automatically.
The width can be set to 0 to force automatic width resizing too::

        SET gui TO GUI(200).
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
        - :struct:`SCROLLBOX`
    - :struct:`LABEL`
        - :struct:`BUTTON`
            - :struct:`POPUPMENU`
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
    :attr:`DRAGGABLE`                     :struct:`Boolean`               Set to false to prevent the window being user-draggable.
    :attr:`EXTRADELAY`                    :struct:`scalar` (seconds)      Add artificial delay to all communication with this GUI (good for testing before you get into deep space)
    :attr:`SKIN`                          :struct:`Skin`                  The skin defining the default style of widgets in this GUI.
    ===================================== =============================== =============

.. structure:: Widget

    This object is the base class of all GUI elements.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
    :meth:`SHOW`                                                          Show the widget. All except GUI objects are shown by default.
    :meth:`HIDE`                                                          Hide the widget.
    :meth:`DISPOSE`                                                       Remove the widget permanently.
    :attr:`ENABLED`                       :struct:`Boolean`               Set to False to "grey out" the widget, preventing user interaction.
    :attr:`STYLE`                         :struct:`Style`                 The style of the widget.
    :attr:`GUI`                           :struct:`GUI`                   The GUI ultimately containing this widget.
    ===================================== =============================== =============

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
    :meth:`ADDCHECKBOX(text,on)`          :struct:`Button`                Creates a toggleable button in the Box, initially checked if on is true.
    :meth:`ADDRADIOBUTTON(text,on)`       :struct:`Button`                Creates an exclusive toggleable button in the Box, initially checked if on is true. Sibling buttons will turn off automatically.
    :meth:`ADDTEXTFIELD(text)`            :struct:`TextField`             Creates an editable text field in the Box.
    :meth:`ADDPOPUPMENU`                  :struct:`PopupMenu`             Creates a popup menu.
    :meth:`ADDHSLIDER(min,max)`           :struct:`Slider`                Creates a horizontal slider in the Box, slidable from min to max.
    :meth:`ADDVSLIDER(min,max)`           :struct:`Slider`                Creates a vertical slider in the Box, slidable from min to max.
    :meth:`ADDHLAYOUT`                    :struct:`Box`                   Creates a nested transparent horizontally-arranged Box in the Box.
    :meth:`ADDVLAYOUT`                    :struct:`Box`                   Creates a nested transparent vertically Box in the Box.
    :meth:`ADDHBOX`                       :struct:`Box`                   Creates a nested horizontally-arranged Box in the Box.
    :meth:`ADDVBOX`                       :struct:`Box`                   Creates a nested vertically Box in the Box.
    :meth:`ADDSTACK`                      :struct:`Box`                   Creates a nested stacked Box in the Box. Only the first enabled subwidget is ever shown. See :meth:`SHOWONLY` below.
    :meth:`ADDSCROLLBOX`                  :struct:`ScrollBox`             Creates a nested scrollable Box of widgets.
    :meth:`ADDSPACING(size)`              :struct:`Spacing`               Creates a blank space of the given size (flexible if -1).
    :attr:`WIDGETS`                       :struct:`List(Widget)`          Returns a LIST of the widgets that have been added to the Box.
    :meth:`SHOWONLY(widget)`                                              Hide all but the given widget.
    :meth:`CLEAR`                                                         Dispose all child widgets.
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
    :attr:`EXCLUSIVE`                     :struct:`Boolean`               If true, sibling Buttons will unpress automatically. See Box:ADDRADIOBUTTON.
    ===================================== =============================== =============

.. note::

    Unless SETTOGGLE(True) is called, the value of :attr:`PRESSED` resets to False as
    soon as the value is accessed.

    If the Button is created by the Button:ADDCHECKBOX method, it will have a different visual
    style and it will start already in toggle mode.

.. structure:: PopupMenu

    `PopupMenu` objects are created inside Box objects via ADDPOPUPMENU method.

    These objects have a list of values (not necessarily strings) which are presented to
    the user as a list from which they can choose. If the items in the list are not strings,
    you should generally set the OPTIONSUFFIX to something (eg. "NAME").

    Example::

	local popup to gui:addpopupmenu().
	set popup:OPTIONSUFFIX to "NAME".
	list bodies in bodies.
	for planet in bodies {
		if planet:hasbody and planet:body = Sun {
			popup:addoption(planet).
		}
	}
	set popup:value to body.


    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
                   Every suffix of :struct:`BUTTON`
    -----------------------------------------------------------------------------------
    :attr:`OPTIONS`                       :struct:`List`(Any)             List of options to display.
    :attr:`OPTIONSUFFIX`                  :struct:`string`                Name of the suffix that names the options.
    :meth:`ADDOPTION(value)`                                              Add a value to the end of the list of options.
    :attr:`VALUE`                         Any                             Returns the current selected value.
    :attr:`INDEX`                         :struct:`Scalar`                Returns the index of the current selected value.
    :attr:`CHANGED`                       :struct:`Boolean`               Has the user chosen something?
    :meth:`CLEAR`                                                         Removes all options.
    ===================================== =============================== =============

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

.. structure:: ScrollBox

    `ScrollBox` objects are created inside Box objects via ADDSCROLLBOX method.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
                   Every suffix of :struct:`BOX`
    -----------------------------------------------------------------------------------
    :attr:`HALWAYS`                       :struct:`Boolean`               Always show the horizontal scrollbar.
    :attr:`VALWAYS`                       :struct:`Boolean`               Always show the vertical scrollbar.
    :attr:`POSITION`                      :struct:`Vector`                The position of the scrolled content (Z is ignored).
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

.. structure:: Skin

    This object holds styles for all widget types. Changes to the styles on a GUI:SKIN
    will affect all subsequently created widgets. Note that some of the styles are used
    by subparts of widgets, such as the HORIZONTALSLIDERTHUMB, which is used by a SLIDER
    when oriented horizontally.

    If you create your own composite widgets, you can use ADD and GET to centralize setting
    up the style of your composite widgets.

    ====================================== =========================== =============
    Suffix                                 Type                        Description
    :attr:`BOX`                            :struct:`Style`             Style for :struct:`Box` widgets.
    :attr:`BUTTON`                         :struct:`Style`             Style for :struct:`Button` widgets.
    :attr:`HORIZONTALSCROLLBAR`            :struct:`Style`             Style for the horizontal scrollbar of :struct:`ScrollBox` widgets.
    :attr:`HORIZONTALSCROLLBARLEFTBUTTON`  :struct:`Style`             Style for the horizontal scrollbar left button of :struct:`ScrollBox` widgets.
    :attr:`HORIZONTALSCROLLBARRIGHTBUTTON` :struct:`Style`             Style for the horizontal scrollbar right button of :struct:`ScrollBox` widgets.
    :attr:`HORIZONTALSCROLLBARTHUMB`       :struct:`Style`             Style for the horizontal scrollbar thumb of :struct:`ScrollBox` widgets.
    :attr:`HORIZONTALSLIDER`               :struct:`Style`             Style for horizontal :struct:`Slider` widgets.
    :attr:`HORIZONTALSLIDERTHUMB`          :struct:`Style`             Style for the thumb of horizontal :struct:`Slider` widgets.
    :attr:`VERTICALSCROLLBAR`              :struct:`Style`             Style for the vertical scrollbar of :struct:`ScrollBox` widgets.
    :attr:`VERTICALSCROLLBARLEFTBUTTON`    :struct:`Style`             Style for the vertical scrollbar left button of :struct:`ScrollBox` widgets.
    :attr:`VERTICALSCROLLBARRIGHTBUTTON`   :struct:`Style`             Style for the vertical scrollbar right button of :struct:`ScrollBox` widgets.
    :attr:`VERTICALSCROLLBARTHUMB`         :struct:`Style`             Style for the vertical scrollbar thumb of :struct:`ScrollBox` widgets.
    :attr:`VERTICALSLIDER`                 :struct:`Style`             Style for vertical :struct:`Slider` widgets.
    :attr:`VERTICALSLIDERTHUMB`            :struct:`Style`             Style for the thumb of vertical :struct:`Slider` widgets.
    :attr:`LABEL`                          :struct:`Style`             Style for :struct:`Label` widgets.
    :attr:`SCROLLVIEW`                     :struct:`Style`             Style for :struct:`ScrollBox` widgets.
    :attr:`TEXTFIELD`                      :struct:`Style`             Style for :struct:`TextField widgets.
    :attr:`TOGGLE`                         :struct:`Style`             Style for :struct:`Button` widgets in toggle mode (GUI:ADDCHECKBOX and GUI:ADDRADIOBUTTON).
    :attr:`FLATLAYOUT`                     :struct:`Style`             Style for :struct:`Box` transparent widgets (GUI:ADDHLAYOUT and GUI:ADDVLAYOUT).
    :attr:`POPUPMENU`                      :struct:`Style`             Style for :struct:`PopupMenu` widgets.
    :attr:`POPUPWINDOW`                    :struct:`Style`             Style for the popup window of :struct:`PopupMenu` widgets.
    :attr:`POPUPMENUITEM`                  :struct:`Style`             Style for the menu items of :struct:`PopupMenu` widgets.
    :attr:`LABELTIPOVERLAY`                :struct:`Style`             Style for tooltips overlayed on :struct:`Label` widgets.
    :attr:`WINDOW`                         :struct:`Style`             Style for :struct:`GUI` windows.

    :attr:`ADD(name)`                      :struct:`Style`             Adds a new style.
    :attr:`HAS(name)`                      :struct:`Boolean`           Does the skin have the named style?
    :attr:`GET(name)`                      :struct:`Style`             Gets a style by name (including ADDed styles).
    ====================================== =========================== =============

.. structure:: Style

    This object represents the style of a widget. Styles can be either changed directly
    on a :struct:`Widget`, or changed on the GUI:SKIN so as to affect all subsequently
    created widgets.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
    :attr:`HSTRETCH`                      :struct:`Boolean`               Should the widget stretch horizontally? (default depends on widget subclass)
    :attr:`VSTRETCH`                      :struct:`Boolean`               Should the widget stretch vertically?
    :attr:`WIDTH`                         :struct:`scalar` (pixels)       Fixed width (or 0 if flexible).
    :attr:`HEIGHT`                        :struct:`scalar` (pixels)       Fixed height (or 0 if flexible).
    :attr:`MARGIN`                        :struct:`StyleRectOffset`       Spacing between this and other widgets.
    :attr:`PADDING`                       :struct:`StyleRectOffset`       Spacing between the outside of the widget and its contents (text, etc.).
    :attr:`BORDER`                        :struct:`StyleRectOffset`       Size of the edges in the 9-slice image for BG images in NORMAL, HOVER, etc.
    :attr:`ALIGN`                         :struct:`string`                One of "CENTER", "LEFT", or "RIGHT". See note below.
    :attr:`FONTSIZE`                      :struct:`scalar`                The size of the text on the label.
    :attr:`RICHTEXT`                      :struct:`Boolean`               Set to False to disable rich-text (<i>...</i>, etc.)
    :attr:`NORMAL`                        :struct:`StyleState`            Properties for the widget normally.
    :attr:`ON`                            :struct:`StyleState`            Properties for when the widget is under the mouse and "on".
    :attr:`NORMAL_ON`                     :struct:`StyleState`            Alias for ON.
    :attr:`HOVER`                         :struct:`StyleState`            Properties for when the widget is under the mouse.
    :attr:`HOVER_ON`                      :struct:`StyleState`            Properties for when the widget is under the mouse and "on".
    :attr:`ACTIVE`                        :struct:`StyleState`            Properties for when the widget is active (eg. button being held down).
    :attr:`ACTIVE_ON`                     :struct:`StyleState`            Properties for when the widget is active and "on".
    :attr:`FOCUSED`                       :struct:`StyleState`            Properties for when the widget has keyboard focus.
    :attr:`FOCUSED_ON`                    :struct:`StyleState`            Properties for when the widget has keyboard focus and is "on".
    :attr:`BG`                            :struct:`string`                The same as NORMAL:BG. Name of a "9-slice" image file.
    :attr:`TEXTCOLOR`                     :ref:`Color <colors>`           The same as NORMAL:TEXTCOLOR. The color of the text on the label.
    ===================================== =============================== =============

.. note::
    The ALIGN attribute will not do anything useful unless either HSTRETCH is set to true or a fixed WIDTH is set,
    since otherwise it will be exactly the right size to fit the content of the widget with no alignment within that space being necessary.

    It is currently only relevant for the widgets that have scalar content (Label and subclasses).

.. structure:: StyleState

    A sub-structure of :struct:`Style`.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
    :attr:`BG`                            :struct:`string`                Name of a "9-slice" image file. See note below.
    :attr:`TEXTCOLOR`                     :ref:`Color <colors>`           The color of the text on the label.
    ===================================== =============================== =============

.. note::

    The `BG` attribute is a "9-slice" image.

    .. image:: /_images/general/9-slice.png
        :align: right

    The corners of the image are used as-is, but the pixels
    between them are stretched to make the full size of image required.
    The :attr:`BORDER` attribute defines the left, right, top and bottom rows of unstretched pixels.

    The image files are always found relative to volume 0 (the Ships/Scripts directory) and
    specifying a ".png" extension is optional.

    If set to "", these background images will default to the corresponding non-ON image
    and if that is also "", it will default to the normal `BG` image,
    and if that is also "", then it will default to completely transparent.

.. structure:: StyleRectOffset

    A sub-structure of :struct:`Style`.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
    :attr:`LEFT`                          :struct:`Scalar`                Number of pixels on the left.
    :attr:`RIGHT`                         :struct:`Scalar`                Number of pixels on the right.
    :attr:`TOP`                           :struct:`Scalar`                Number of pixels on the top.
    :attr:`BOTTOM`                        :struct:`Scalar`                Number of pixels on the bottom.
    :attr:`H`                             :struct:`Scalar`                Sets the number of pixels on both the left and right. Reading returns LEFT.
    :attr:`V`                             :struct:`Scalar`                Sets the number of pixels on both the top and bottom. Reading returns TOP.
    ===================================== =============================== =============


Communication Delay
-------------------

If communication delay is enabled (eg. using RemoteTech), you will still be
able to interact with a GUI, but changes to values and messages will incur
a signal delay. Similarly, changes to values in the GUI will be delayed coming
back. Some things such as GUI creation, adding widgets, etc. are immediate for
simplicity.

