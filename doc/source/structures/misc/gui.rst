.. _widgets:

Creating GUIs
=============

You can create an object that represents a GUI drawn on the
user's screen (not in the terminal window). It can have buttons,
labels, and the usual GUI elements. In combination with a :ref:`boot file <boot>`,
entirely GUI-driven vessel controls can be developed.

.. figure:: /_images/general/gui-HelloWorld.png
    :width: 100%

The "Hello World" program, version 1 with "polling"::

        // "Hello World" program for kOS GUI.
        //
        // Create a GUI window
        LOCAL gui IS GUI(200).
        // Add widgets to the GUI
        LOCAL label IS gui:ADDLABEL("Hello world!").
        SET label:STYLE:ALIGN TO "CENTER".
        SET label:STYLE:HSTRETCH TO True. // Fill horizontally
        LOCAL ok TO gui:ADDBUTTON("OK").
        // Show the GUI.
        gui:SHOW().
        // Handle GUI widget interactions.
        //
        // This is the technique known as "polling" - In a loop you
        // continually check to see if something has happened:
        LOCAL isDone IS FALSE.
        UNTIL isDone
        {
          if (ok:TAKEPRESS)
            SET isDone TO TRUE.
          WAIT 0.1. // No need to waste CPU time checking too often.
        }
        print "OK pressed.  Now closing demo.".
        // Hide when done (will also hide if power lost).
        gui:HIDE().

The same "Hello World" program, version 2 with "callbacks"::

        // "Hello World" program for kOS GUI.
        //
        // Create a GUI window
        LOCAL gui IS GUI(200).
        // Add widgets to the GUI
        LOCAL label IS gui:ADDLABEL("Hello world!").
        SET label:STYLE:ALIGN TO "CENTER".
        SET label:STYLE:HSTRETCH TO True. // Fill horizontally
        LOCAL ok TO gui:ADDBUTTON("OK").
        // Show the GUI.
        gui:SHOW().
        // Handle GUI widget interactions.
        //
        // This is the technique known as "callbacks" - instead
        // of actively looking again and again to see if a button was
        // pressed, the script just tells kOS that it should call a
        // delegate function when it notices the button has done
        // something, and then the program passively waits for that
        // to happen:
        LOCAL isDone IS FALSE.
        function myClickChecker {
          SET isDone TO TRUE.
        }
        SET ok:ONCLICK TO myClickChecker@. // This could also be an anonymous function instead.
        wait until isDone.

        print "OK pressed.  Now closing demo.".
        // Hide when done (will also hide if power lost).
        gui:HIDE().

Both techniques (the "polling" and the "callbacks" style) of interacting with the GUI are
supported by the widgets in the system.  The "callbacks" style is supported by the
use of various "ON" suffixes with names like ``ONCLICK``, ``ONTOGGLE``, and so on.


Creating a Window
-----------------

.. function:: GUI(width [, height])

This creates a new ``GUI`` object that you can then manipulate
to build up a GUI. If no height is specified, it will resize automatically.
The width can be set to 0 to force automatic width resizing too::

        SET gui TO GUI(200).
        SET button TO gui:ADDBUTTON("OK").
        gui:SHOW().
        UNTIL button:TAKEPRESS WAIT(0.1).
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

    This object is created with the :func:`GUI(width,height)` function.

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
    :meth:`SHOW`                                                          Show the widget. Equivalent to setting VISIBLE to True.
    :meth:`HIDE`                                                          Hide the widget. Equivalent to setting VISIBLE to False.
    :attr:`VISIBLE`                                                       Show or hide the widget. All except top-level GUI objects are shown by default.
    :meth:`DISPOSE`                                                       Remove the widget permanently.
    :attr:`ENABLED`                       :struct:`Boolean`               Set to False to "grey out" the widget, preventing user interaction.
    :attr:`STYLE`                         :struct:`Style`                 The style of the widget.
    :attr:`GUI`                           :struct:`GUI`                   The GUI ultimately containing this widget.
    ===================================== =============================== =============

.. structure:: Box

    ``Box`` objects are themselves created from other Box objects via ADDHBOX and other methods. The root ``Box`` is
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
    :attr:`RADIOVALUE`                    :struct:`String`                Returns the string name of the currently selected radio button within this box of radio buttons (empty string if no such value).
    :attr:`ONRADIOCHANGE`                 :struct:`KOSDelegate` (button)  A callback hook you want called whenever the radio button selection within this box changes (it gets called with a parameter: the button that has been switched to).
    :meth:`SHOWONLY(widget)`                                              Hide all but the given widget.
    :meth:`CLEAR`                                                         Dispose all child widgets.
    ===================================== =============================== =============


.. structure:: Label

    ``Label`` objects are created inside Box objects via ADDLABEL method.

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

    ``Button`` objects are created inside Box objects via ADDBUTTON and ADDCHECKBOX methods.

    ===================================== ========================================== =============
    Suffix                                Type                                       Description
    ===================================== ========================================== =============
                   Every suffix of :struct:`LABEL`
    ----------------------------------------------------------------------------------------------
    :attr:`PRESSED`                       :struct:`Boolean`                          Is the button currently down?
    :attr:`TAKEPRESS`                     :struct:`Boolean`                          Return the PRESSED value AND release the button if it's down.
    :attr:`TOGGLE`                        :struct:`Boolean`                          Set to true to make this button into a toggle-style button (stays down when clicked until clicked again).
    :attr:`EXCLUSIVE`                     :struct:`Boolean`                          If true, sibling Buttons will unpress automatically. See Box:ADDRADIOBUTTON.
    :attr:`ONCLICK`                       :struct:`KOSDelegate` (no args)            Your function called whenever the button gets clicked.
    :attr:`ONTOGGLE`                      :struct:`KOSDelegate` (:struct:`Boolean`)  Your function called whenever the button's PRESSED state changes.
    ===================================== ========================================== =============

.. note::

    Reading the value of the :attr:`PRESSED` suffix will tell you if the button is pressed in (true)
    or released (false).  But be aware that when :attr:`TOGGLE` is false, then the button will
    remain pressed-in until such a time as your script detects that it has been pressed (so that
    way the button won't press in-and-out too quickly for your script to notice).

    **Behaviour when TOGGLE is false (the default):**

    By default, :attr:`TOGGLE` is set to false.  This means the button does not require
    a second click by the user to pop back out again after being pushed in.
    The button's :attr:`PRESSED` suffix will only stay true long enough for the kerboscript
    to tell kOS "Yes I have seen the fact that it was pressed".  (See next paragraph).
    After that happens, :attr:`PRESSED` will become false again (and the button will pop back
    out).

    The conditions under which a button will automatically release itself when :attr:`TOGGLE` is
    set to `False` are:

    - When the script calls the :attr:`TAKEPRESS` suffix method.  When this is done, the
      button will become false even if it was was previously true.
    - If the script defines an :attr:`ONCLICK` user delegate.
      (Then when the :attr:`PRESSED` value becomes true, kOS will immediately set it
      back to false (too fast for the kerboscript to see it) and instead Call the
      ``ONCLICK`` delegate.)

    The :attr:`TAKEPRESS` suffix method is intended to be used for non-toggle buttons
    in scripts that use the "polling" method of looking for a button change.  You can
    put a check for ``if mybutton:TAKEPRESS { print "do something here". }`` in an
    ``until`` loop or a ``when`` trigger and TAKEPRESS will only be true long enough
    for the script to see it once, at which point it will become false again right away.

    The :attr:`ONCLICK` suffix is intended to be used for non-toggle buttons in
    scripts that use the "callbacks" method of looking for a button change.  This
    method is more efficient and simpler to use.  To use ONCLICK you set the ONCLICK
    suffix to a user delegate you create that kOS will call when the button gets clicked.
    example::

        set mybutton:ONCLICK to { print "Do something here.". }.

    **Behaviour when TOGGLE is true:**

    If TOGGLE is set to True, then the button will **not** automatically release after it is
    read by the script.  Instead it will need to be clicked by the user a second time to make
    it pop back out.  In this mode, the button's :attr:`PRESSED` value will never automatically
    reset to false on its own.

    If the Button is created by the Button:ADDCHECKBOX method, it will have a different visual
    style (the style called "toggle") and it will start already in TOGGLE mode.

    If EXCLUSIVE is set to True, when the button is clicked (or changed programmatically),
    other buttons with the same parent :struct:`Box` will be set to False (regardless of
    if they are EXCLUSIVE).

    If the Button is created by the Button:ADDRADIOBUTTON method, it will have the checkbox
    style (the style called "toggle"), and it will start already in TOGGLE and EXCLUSIVE modes.

    **Callback hooks ONCLICK, ONTOGGLE:**

    The two suffixes :attr:`ONTOGLE`, and :attr:`ONCLICK` are similar
    to each other.  They are what is known as a "callback hook".  You can assign them to
    a :struct:`KOSDelegate` of one of your functions (named or anonymous) and from then on
    kOS will call that function whenever the button gets changed as described.

    :attr:`ONCLICK` is called with no parameters.  To use it, your function must be
    written to expect no parameters.

    :attr:`ONTOGGLE` is called with one parameter, the boolean value the button got changed to.
    To use :attr:`ONTOGGLE`, your function must be written to expect a single boolean parameter.
    :attr:`ONTOGGLE` is really only useful with buttons where :attr:`TOGGLE` is true.

    Here is an example of using the button callback hooks::

        LOCAL doneYet is FALSE.
        LOCAL g IS GUI(200).

        // b1 is a normal button that auto-releases itself:
        // Note that the callback hook, myButtonDetector, is
        // a named function found elsewhere in this same program:
        LOCAL b1 IS g:ADDBUTTON("button 1").
        SET b1:ONCLICK TO myButtonDetector@.

        // b2 is also a normal button that auto-releases itself,
        // but this time we'll use an anonymous callback hook for it:
        LOCAL b2 IS g:ADDBUTTON("button 2").
        SET b2:ONCLICK TO { print "Button Two got pressed". }

        // b3 is a toggle button.
        // We'll use it to demonstrate how ONTOGGLE callback hooks look:
        LOCAL b3 IS g:ADDBUTTON("button 3 (toggles)").
        set b3:style to g:skin:button.
        SET b3:TOGGLE TO TRUE.
        SET b3:ONTOGGLE TO myToggleDetector@.

        // b4 is the exit button.  For this we'll use another
        // anonymous function that just sets a boolean variable
        // to signal the end of the program:
        LOCAL b4 IS g:ADDBUTTON("EXIT DEMO").
        SET b4:ONCLICK TO { set doneYet to true. }

        g:show(). // Start showing the window.

        wait until doneYet. // program will stay here until exit clicked.

        g:hide(). // Finish the demo and close the window.

        //END.

        function myButtonDetector {
          print "Button One got clicked.".
        }
        function myToggleDetector {
          parameter newState.
          print "Button Three has just become " + newState.
        }

    ** TODO - PUT AN EXAMPLE WITH A RADIO BUTTON HERE OR IN BOX: **

    TODO....

.. structure:: PopupMenu

    ``PopupMenu`` objects are created inside Box objects via ADDPOPUPMENU method.

    A ``PopupMenu`` is a special kind of button for choosing from a list of things.
    It looks like a button who's face displays the currently selected thing.  When a user
    clicks on the button, it pops up a list of displayed strings to choose
    from, and when one is selected the popup goes away and the new choice is
    displayed on the button.

    The menu displays the string values in the OPTIONS property. If OPTIONS contains items that are not strings,
    then by default their :attr:`TOSTRING <Structure:TOSTRING>` suffixes will be used to display them as strings.

    You can change this default behaviour by setting the popupmenu's :OPTIONSUFFIX to a different suffix
    name other than "TOSTRING". In the example below which builds a list of bodies for the pulldown list,
    the body:NAME suffix will be used instead of the body:TOSTRING suffix for all the items in the list.

    Example::

	local popup is gui:addpopupmenu().

        // Make the popup display the Body:NAME's instead of the Body:TOSTRING's:
	set popup:OPTIONSUFFIX to "NAME".

	list bodies in bodies.
	for planet in bodies {
		if planet:hasbody and planet:body = Sun {
			popup:addoption(planet).
		}
	}
	set popup:value to body.


    ===================================== ========================================= =============
    Suffix                                Type                                      Description
    ===================================== ========================================= =============
                   Every suffix of :struct:`BUTTON`
    ---------------------------------------------------------------------------------------------
    :attr:`OPTIONS`                       :struct:`List`                            List of options to display.
    :attr:`OPTIONSUFFIX`                  :struct:`string`                          Name of the suffix used for display names. Default = TOSTRING.
    :meth:`ADDOPTION(value)`                                                        Add a value to the end of the list of options.
    :attr:`VALUE`                         Any                                       Returns the current selected value.
    :attr:`INDEX`                         :struct:`Scalar`                          Returns the index of the current selected value.
    :attr:`CHANGED`                       :struct:`Boolean`                         Has the user chosen something?
    :attr:`ONCHANGED`                     :struct:`KOSDelegate` (:struct:`String`)  Your function called whenever the :attr:`CHANGED` state changes.
    :meth:`CLEAR`                                                                   Removes all options.
    ===================================== ========================================= =============

.. structure:: TextField

    ``TextField`` objects are created inside Box objects via ADDTEXTFIELD method.

    ===================================== ========================================= =============
    Suffix                                Type                                      Description
    ===================================== ========================================= =============
                   Every suffix of :struct:`LABEL`
    ---------------------------------------------------------------------------------------------
    :attr:`CHANGED`                       :struct:`Boolean`                         Has the text been edited?
    :attr:`ONCHANGED`                     :struct:`KOSDelegate` (:struct:`String`)  Your function called whenever the :attr:`CHANGED` state changes.
    :attr:`CONFIRMED`                     :struct:`Boolean`                         Has the user pressed Return in the field?
    :attr:`ONCONFIRMED`                   :struct:`KOSDelegate` (:struct:`String`)  Your function called whenever the :attr:`CONFIRMED` state changes.
    ===================================== ========================================= =============

.. note::

    The values of :attr:`CHANGED` and :attr:`CONFIRMED` reset to False as soon as their value is accessed.

.. structure:: Slider

    ``Slider`` objects are created inside Box objects via ADDHSLIDER and ADDVSLIDER methods.

    ===================================== ========================================= =============
    Suffix                                Type                                      Description
    ===================================== ========================================= =============
                   Every suffix of :struct:`WIDGET`
    ---------------------------------------------------------------------------------------------
    :attr:`VALUE`                         :struct:`scalar`                          The current value. Initially set to :attr:`MIN`.
    :attr:`ONCHANGED`                     :struct:`KOSDelegate` (:struct:`String`)  Your function called whenever the :attr:`VALUE` changes.
    :attr:`MIN`                           :struct:`scalar`                          The minimum value (leftmost on horizontal slider).
    :attr:`MAX`                           :struct:`scalar`                          The maximum value (bottom on vertical slider).
    ===================================== ========================================= =============

.. structure:: ScrollBox

    ``ScrollBox`` objects are created inside Box objects via ADDSCROLLBOX method.

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

    ``Spacing`` objects are created inside Box objects via ADDSPACING method.

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

    If you wish to make a complete new Skin, the cleanest method would be to put all
    the graphics in a directory, along with a kOS script that given a GUI:SKIN, changes
    everything in that skin as needed, allowing users to run your script with their GUI:SKIN
    to make it use your custom skin.

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
    :attr:`TEXTFIELD`                      :struct:`Style`             Style for :struct:`TextField` widgets.
    :attr:`TOGGLE`                         :struct:`Style`             Style for :struct:`Button` widgets in toggle mode (GUI:ADDCHECKBOX and GUI:ADDRADIOBUTTON).
    :attr:`FLATLAYOUT`                     :struct:`Style`             Style for :struct:`Box` transparent widgets (GUI:ADDHLAYOUT and GUI:ADDVLAYOUT).
    :attr:`POPUPMENU`                      :struct:`Style`             Style for :struct:`PopupMenu` widgets.
    :attr:`POPUPWINDOW`                    :struct:`Style`             Style for the popup window of :struct:`PopupMenu` widgets.
    :attr:`POPUPMENUITEM`                  :struct:`Style`             Style for the menu items of :struct:`PopupMenu` widgets.
    :attr:`LABELTIPOVERLAY`                :struct:`Style`             Style for tooltips overlayed on :struct:`Label` widgets.
    :attr:`WINDOW`                         :struct:`Style`             Style for :struct:`GUI` windows.

    :attr:`FONT`                           :struct:`string`            The name of the font used (if STYLE:FONT does not change it for an element).
    :attr:`SELECTIONCOLOR`                 :ref:`Color <colors>`       The background color of selected text (eg. TEXTFIELD).

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
    :attr:`OVERFLOW`                      :struct:`StyleRectOffset`       Extra space added to the area of the background image. Allows the background to go beyond the widget's rectangle.
    :attr:`ALIGN`                         :struct:`string`                One of "CENTER", "LEFT", or "RIGHT". See note below.
    :attr:`FONT`                          :struct:`string`                The name of the font of the text on the content or "" if the default.
    :attr:`FONTSIZE`                      :struct:`scalar`                The size of the text on the content.
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

    The ``BG`` attribute is a "9-slice" image.

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


.. _widgets_delay:

Communication Delay
-------------------

If communication delay is enabled (eg. using RemoteTech), you will still be
able to interact with a GUI, but changes to values and messages will incur
the same sort of signal delay that interactive control over the vessel would
incur.  (If your vessel can be controlled immediately because there's a
kerbal on board, then your GUI for the vessel can be controlled immediately,
but if your attempts to control the vessel are being subject to a signal
delay, then your attempts to click on the GUI elements will get the same
delay). Similarly, changes to values in the GUI will be delayed coming
back by the same rules. Some things such as GUI creation, adding widgets,
etc. are immediate for simplicity.
