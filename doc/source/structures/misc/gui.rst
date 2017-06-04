.. _widgets:

Creating GUIs
=============

You can create an object that represents a GUI drawn on the
user's screen (not in the terminal window). It can have buttons,
labels, and the usual GUI elements. In combination with a :ref:`boot file <boot>`,
entirely GUI-driven vessel controls can be developed.

.. figure:: /_images/general/gui-HelloWorld.png
    :width: 100%

GUI Callbacks versus Polling
----------------------------

There are two general styles of interacting with GUI widgets,
called "callbacks" and "polling".

.. _gui_callback_technique:

The **callback technique** is when you create :struct:`KOSDelegate` objects
that are either anonymous functions or named user functions, and then
assign them to different widgets' "hook suffixes".  In this technique you
are telling the widget "Here is a function in my program that I want you to
call whenever you notice this particular thing has happened."  For example::

    set thisButton:ONCLICK to myclickFunction@.
    // Program continues on, executing further commands after this.

will interrupt whatever else you are doing and call a function you wrote
called ``myClickFunction`` whenever that button is clicked.

.. _gui_polling_technique:

The **polling technique** is when you actively keep checking the widget
again and again in your own script, to see if anything has happened.  In 
this technique, you are choosing when to pay attention to the GUI
widget.  For example::

    until thisButton:TAKEPRESS {
      // button still isn't pressed yet, let's keep waiting.
      wait 0.
    }

In general, *if you are trying to decide between using the callback or the polling
technique, you should **prefer using the callback technique** most of the time*.  It
takes less CPU time away from the rest of your program and is less of a burden on
the universe simulation.

Below are longer examples of the two techniques, and how the scripts that
use them would look.  The suffixes and built-in functions used in these
examples will be explained in detail later.

The "Hello World" program, version 1 with "callbacks"::

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

The same "Hello World" program, version 2 with "polling"::

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



Creating a Window
-----------------

.. function:: GUI(width [, height])

This is the first place any GUI control panel starts.

The GUI built-in function creates a new :struct:`GUI` object that you can then
manipulate to build up a GUI. If no height is specified, it will resize
automatically to fit the contents you put inside it.  The width can be set
to 0 to force automatic width resizing too::

        SET gui TO GUI(200).
        SET button TO gui:ADDBUTTON("OK").
        gui:SHOW().
        UNTIL button:TAKEPRESS WAIT(0.1).
        gui:HIDE().

See the "ADD" functions in the :struct:`BOX` structure for
the other widgets you can add.

Warning: Setting BOTH width and height to 0 to let it choose automatic
resizing in both dimensions will often lead to a look you won't like.
You may find that to have some control over the layout you will need to
specify one of the two dimensions and only let it resize the other.

Structure Reference
-------------------

The GUI elements, including the GUI type itself are in the
following hierarchy:

- :struct:`WIDGET` - base type of all other elements.
    - :struct:`BOX` - a rectangular widget that contains other widgets
        - :struct:`GUI` - the outermost ``Box`` that represents the GUI window panel.
        - :struct:`SCROLLBOX` - a ``Box`` that shows only a subset of itself at a time and can be scrolled.
    - :struct:`LABEL` - text (or image) for display
        - :struct:`BUTTON` - label that notices when it's clicked or toggled.
            - :struct:`POPUPMENU` - button that when clicked shows a list to pick from.
        - :struct:`TEXTFIELD` - label that is edit-able by the user.
    - :struct:`SLIDER` - vertical or horizontal movable handle that edits a :struct:`Scalar` value.
    - :struct:`SPACING` - empty whitespace area within the box for layout reasons.

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

If you want to test or experiment with what your GUI would be like under
a signal delay even though you don't *really* have a signal delay, you
can simulate the effect by setting the :attr:`GUI:EXTRADELAY` suffix
of the GUI window.

.. structure:: Widget

    This object is the base class of all GUI elements.  No matter which GUI element you
    are dealing with, it will have these properties at minimum.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
    :meth:`SHOW`                                                          Show the widget.
    :meth:`HIDE`                                                          Hide the widget.
    :attr:`VISIBLE`                                                       SET: Show or hide the widget. GET: see if it's showing.
    :meth:`DISPOSE`                                                       Remove the widget permanently.
    :attr:`ENABLED`                       :struct:`Boolean`               Set to False to "grey out" the widget, preventing user interaction.
    :attr:`STYLE`                         :struct:`Style`                 The style of the widget.
    :attr:`GUI`                           :struct:`GUI`                   The GUI ultimately containing this widget.
    :attr:`PARENT`                        :struct:`BOX`                   The Box containing this widget.
    :attr:`HASPARENT`                     :struct:`Boolean`               If this widget has no parent, returns false.
    ===================================== =============================== =============

    .. method:: SHOW

        (No parameters).

        Call ``Widget:show()`` when you need to make the widget in question
        start appearing on the screen.  This is identical to setting
        :attr:`Widget:VISIBLE` to true.

        See :attr:`Widget:VISIBLE` below for further documentation.

        Note: Unless you use ``show()`` (or set the :struct:`Widget:VISIBLE`
        suffix to true) on the outermost :struct:`Box` of the GUI panel (the
        one you obtained from calling built-in function :func:`GUI`), nothing
        will ever be visible from your GUI.

    .. method:: HIDE

        (No parameters).

        Call ``Widget:hide()`` when you need to make the widget in question
        disappear from the screen.  This is identical to setting
        :attr:`Widget:VISIBLE` to false.
        
        See :attr:`Widget:VISIBLE` below for further documentation.

    .. attribute:: VISIBLE

        :type: :struct:`Scalar`
        :accesss: Get/Set

        This is the setting which can also be changed by calling
        :meth:`Widget:show()` and :meth:`Widget:hide()`.

        Most new widgets are set to be visible by default, except for the outermost
        :struct:`Box` that represents a GUI panel window.  (The kind you can
        obtain by calling built-in function :func:`Gui`.)  Because of this, you
        generally only need to set the outermost GUI panel window to visible and
        then all the widgets inside of it should appear.

        The typical pattern is this::

           set panel to GUI(200).

           // <--- Add widgets to panel by calling things like panel:addbutton,
           //      panel:addhslider, panel:addvslider, panel:addlabel, etc...

           panel:show(). // or 'set panel:visible to true.' does the same thing.

        Note that the showing of a widget requires the showing of all widgets
        it's contained inside of.  Hiding a widget will hide all widgets inside
        it, regardless of their infividual visibility settings.  This is what
        is happening when you make a :struct:`GUI` Box with a call to :func:`GUI`,
        fill it with widgets, and then show it.  The widgets inside it were already
        set to "visible", but their visibility was suppressed by the fact that
        the :struct:`GUI` they were inside of was not visible.  Once you made the
        :struct:`GUI` panel visible, all the widgets inside it (that were already
        set to be visible) appeared with it.

    .. method:: DISPOSE

        (no parameters)

        Call ``Widget:DISPOSE()`` to permanenly make this widget go away.
        Not only will it make it invisible, but it will make it impossible
        to set it to visible again later.

    .. attribute:: ENABLED

        :type: :struct:`Boolean`
        :access: get/set

        (This is true by default for all newly created widgets.)

        When this is true, then the widget can be used by the user.

        When this is false, then the widget becomes read-only and its
        skin takes on a "greyed-out" theme.  The user cannot interact
        with it, even though it may still be visible on the screen.

    .. attribute:: STYLE

        :type: :struct:`Style`
        :access: Get/Set

        The style of the widget.

        A reasonable style will be chosen by default for most widgets.
        It will be one that is copied from the default style used in
        KSP's standard stock GUI skin.  But if you wish to change the
        appearance of the GUI widgets that kOS provides, you can create
        a modified style and set the widget's style to that style here,
        or you can "swap" styles by assigning this widget to the style
        usually used by a different widget.  (For example, making a
        button look like it's just a passive text label.)  Such
        changes should be carefully thought-out if you do them at all,
        because they can very easily confuse a user with conflicting
        visual cues.

        To see how to make a modified style, see the documentation
        for :struct:`Style`.

  .. attribute:: GUI

      :type: :struct:`GUI`
      :access: Get-only

      To be useful, all widgets (buttons, labels, textfields, etc) must
      either be contained inside a :struct:`GUI` widget directly, or be
      contained inside another :struct:`Widget` which in turn is also
      contained inside a :struct:`GUI` widget.  (Or contained inside
      a widget contained inside a widget contained inside a GUI, etc..)

      This suffix will find which :struct:`GUI` is the one which ultimately
      is the one holding this widget.

  .. attribute:: PARENT

      :type: :struct:`Box`
      :access: :Get-only

      Widgets can be contained inside Boxes that are contained inside
      other Boxes, etc.  This suffix tells you which :sturuct:`Box` contains
      this one.  If you attempt to call this suffix on the outermost
      :struct:`GUI` Box that contains all the others in a panel,
      you may find that kOS throws a complaining error because there is
      no parent to the outermost widget.  To protect your code against this,
      use the :attr:`Widget:HASPARENT` suffix.

  .. attribute:: HASPARENT

      :type: :struct:`Boolean`
      :access: :Get-only

      If trying to use :attr:`Widget:PARENT` would generate an error because
      this widget has no parent, then :attr:`HASPARENT` will be false.
      Otherwise it will be true.

.. structure:: Box

    ``Box`` objects are a type of :struct:`WIDGET`.
    
    A ``Box`` is a rectangular widget that holds other widgets inside it.
    
    A GUI window is a kind of ``Box``, and is created from the :func:`GUI`
    built-in function.  You always need at least one ``Box`` created this way
    in order to have any GUI at all show up, and then you add the widgets you
    want to that ``Box``.

    Since a ``Box`` is a :struct:`WIDGET`, and a ``Box`` is a rectangle that
    contains widgets, that means you can also put a ``Box` inside of another 
    ``Box``.  You do so by using the :meth:`ADDHBOX` or :meth:`ADDVBOX` suffixes
    of a box.  Usually the reason to do this is to define exactly how you want
    a set of widgets laid out.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
                   Every suffix of :struct:`WIDGET`
    -----------------------------------------------------------------------------------
    :meth:`ADDLABEL(text)`                :struct:`Label`                 Creates a label in the Box.
    :meth:`ADDBUTTON(text)`               :struct:`Button`                Creates a clickable button in the Box.
    :meth:`ADDCHECKBOX(text,on)`          :struct:`Button`                Creates a toggleable button in the Box.
    :meth:`ADDRADIOBUTTON(text,on)`       :struct:`Button`                Creates an exclusive toggleable button in the Box.
    :meth:`ADDTEXTFIELD(text)`            :struct:`TextField`             Creates an editable text field in the Box.
    :meth:`ADDPOPUPMENU`                  :struct:`PopupMenu`             Creates a popup menu.
    :meth:`ADDHSLIDER(init,min,max)`      :struct:`Slider`                Creates a horizontal slider in the Box.
    :meth:`ADDVSLIDER(init,min,max)`      :struct:`Slider`                Creates a vertical slider in the Box.
    :meth:`ADDHLAYOUT`                    :struct:`Box`                   Creates an undecorated invisible Box in the Box, with horizontal flow.
    :meth:`ADDVLAYOUT`                    :struct:`Box`                   Creates an undecorated invisible Box in the Box, with vertical flow.
    :meth:`ADDHBOX`                       :struct:`Box`                   Creates a visible Box in the Box, with horizontal flow.
    :meth:`ADDVBOX`                       :struct:`Box`                   Creates a visible Box in the Box, with vertical flow.
    :meth:`ADDSTACK`                      :struct:`Box`                   Creates a nested stacked Box in the Box.  Only one such box is shown at a time.
    :meth:`ADDSCROLLBOX`                  :struct:`ScrollBox`             Creates a nested scrollable Box of widgets.
    :meth:`ADDSPACING(size)`              :struct:`Spacing`               Creates a blank space of the given size (flexible if -1).
    :attr:`WIDGETS`                       :struct:`List(Widget)`          Returns a LIST of the widgets that have been added to the Box.
    :attr:`RADIOVALUE`                    :struct:`String`                The string name of the currently selected radio button.
    :attr:`ONRADIOCHANGE`                 :struct:`KOSDelegate` (button)  A callback you want kOS to call whenever the radio button selection changes.
    :meth:`SHOWONLY(widget)`                                              Hide all but the given widget.
    :meth:`CLEAR`                                                         Dispose all child widgets.
    ===================================== =============================== =============

    .. method:: ADDLABEL(text)

        :parameter:
        :type text: :struct:`String`
        :return:
        :rtype: :struct:`Label`

        Creates a :struct:`Label` widget in this ``Box``.  The label will
        display the text message given in the parameter.

    .. method:: ADDBUTTON(text)

        :parameter:
        :type text: :struct:`String`
        :return:
        :rtype: :struct:`Button`

        Creates a *clickable* :struct:`Button` widget in this ``Box``.

    .. method:: ADDCHECKBOX(text, on)

        :parameter: text to display
        :type text: :struct:`String`
        :parameter: state of the checkbox initially
        :type on: :struct:`Boolean`
        :return:
        :rtype: :struct:`Button`

        Creates a *toggle-able* :struct:`Button` widget in this ``Box``.
        The Button will display the text message given in the parameter.
        The Button will initially start off turned on or turned off
        depending on the state of the ``on`` parameter.

    .. method:: ADDRADIOBUTTON(text, on)

        :parameter: text to display
        :type text: :struct:`String`
        :parameter: state of the checkbox initially
        :type on: :struct:`Boolean`
        :return:
        :rtype: :struct:`Button`

        Creates an *exclusive toggle-able* :struct:`Button` widget in this ``Box``.
        The Button will display the text message given in the parameter.
        The Button will initially start off turned on or turned off depending
        on the state of the ``on`` parameter.

        This button will be set to be exclusive, which means all other
        buttons in this :struct:`Box` which are also exclusive will be
        turned off when this button is turned on.  All these "radio"
        buttons within this same box are considered to be in the same
        group for the sake of this check.  In order to make two
        different radio button groups, you would need to create a box
        for each with :meth:`BOX:ADDHBOX` or :meth:`BOX:ADDVBOX`, and
        then add radio buttons to each of those boxes.

        To read which radio button value is the one that is currently on,
        among the whole set of buttons, you can use :attr:`BOX:RADIOVALUE`.

    .. attribute:: ADDTEXTFIELD(text)

        :parameter: initial starting text in the field.
        :type text: :struct:`String`
        :return:
        :rtype: :struct:`TextField`

        Creates a :struct:`TextField` widget in this ``Box``.
        The textfield will allow the user to type a string into the field
        that you can read.
        The field will be a one-line string input.

    .. method:: ADDPOPUPMENU

        :return:
        :rtype: :struct:`PopupMenu`

        Creates a special kind of button known as a :struct:`PopupMenu`
        in the Box.  This is a button that, when clicked, brings up a list
        of values to choose from.  When the user picks a value, the popup
        list goes away and the button will be labeled with the selection
        from the list that was picked.

        The list of values that will pop up are in the
        suffix :attr:`PopupMenu:Options`, which you must populate after
        having called ``ADDPOPUPMENU``.
        
        Example::

            set mygui to GUI(100).
            // Make a popup menu that lets you choose one of 4 color names:
            set mypopup mygui:addpopupmenu().
            set mypopup:options to LIST("red", "green", "yellow", "white").

            mygui:show().
            wait 15. // let you play with it for 15 seconds.
            mygui:dispose(). // ditch the gui before leaving this example.

    .. method:: ADDHSLIDER(init, min, max)

        :parameter: starting value
        :type init: :struct:`Scalar`
        :parameter: left endpoint value
        :type min: :struct:`Scalar`
        :parameter: right endpoint value
        :type max: :struct:`Scalar`
        :return:
        :rtype: :struct:`Slider`

        Creates a horizontal :struct:`Slider` in the Box that adjusts a
        :struct:`Scalar` value.  The value can take on any fractional
        amount between the minimum and maximum values given.  Despite
        the names it is possible to make the ``min`` parameter larger than
        the ``max`` parameter, in which case the direction of the slider
        will be inverted, with the largest value at the left and the smallest
        at the right.

    .. method:: ADDVSLIDER(init, min, max)

        :parameter: starting value
        :type init: :struct:`Scalar`
        :parameter: top endpoint value
        :type min: :struct:`Scalar`
        :parameter: bottom endpoint value
        :type max: :struct:`Scalar`
        :return:
        :rtype: :struct:`Slider`

        Creates a vertical :struct:`Slider` in the Box that adjusts a
        :struct:`Scalar` value.  The value can take on any fractional
        amount between the minimum and maximum values given.  Despite
        the names it is possible to make the ``min`` parameter larger than
        the ``max`` parameter, in which case the direction of the slider
        will be inverted, with the largest value at the bottom and the smallest
        at the top.

        TODO: FIRE UP THE GAME AND TEST THE DIRECTION HERE.  I AM TYPING FROM MEMORY ABOUT
        THE DIRECTIONS (TOP BEING SMALLEST NORMALLY) - THAT COULD BE WRONG.

    .. method:: ADDHLAYOUT

        :return:
        :rtype: :struct:`Box`
        
        Creates a nested transparent horizontally-arranged :struct:`Box` in
        this :struct:`Box`.  You can't see any visual evidence of this
        box other than how it forces the widgets inside it to get arranged.
        (The box has no borders showing, no background color, etc).

        All the widgets added to such a box will arrange themselves
        horizontally (the more widgets you add, the wider the box gets).

        There are three reasons you might want to nest one Box inside another Box:

        - You wish to isolate some radio buttons into their own Box so they
          form one radio button group.
        - You wish to force the GUI automatic layout system to place widgets
          in a particular arrangement by making it treat a group of widgets
          as a single rectangular chunk that gets arranged together as a unit.

    .. method:: ADDVLAYOUT

        :return:
        :rtype: :struct:`Box`
        
        Creates a nested transparent vertically-arranged :struct:`Box` in
        this :struct:`Box`.  You can't see any visual evidence of this
        box other than how it forces the widgets inside it to get arranged.
        (The box has no borders showing, no background color, etc).

        All the widgets added to such a box will arrange themselves
        vertically (the more widgets you add, the taller the box gets).

        (The :struct:`Box` returned by calling the built-in function
        :funct:`Gui` is a "VLayout" box which arranges things vertically
        like this.)

        There are three reasons you might want to nest one Box inside another Box:

        - You wish to isolate some radio buttons into their own Box so they
          form one radio button group.
        - You wish to force the GUI automatic layout system to place widgets
          in a particular arrangement by making it treat a group of widgets
          as a single rectangular chunk that gets arranged together as a unit.

    .. method:: ADDHBOX

        :return:
        :rtype: :struct:`Box`
        
        This is identical to :meth:`BOX:ADDHLAYOUT`, other than the
        fact that it uses a different graphical style which lets you
        see the box.

    .. method:: ADDVBOX

        :return:
        :rtype: :struct:`Box`
        
        This is identical to :meth:`BOX:ADDVLAYOUT`, other than the
        fact that it uses a different graphical style which lets you
        see the box.

    .. method:: ADDSTACK

        :return:
        :rtype: :struct:`Box`
        
        Creates a nested stacked Box in this Box. (a Box which 
        can be swapped for other similarly created boxes that
        occupy the same space on the screen.)

        When you add several such boxes with multiple calls to
        :meth:`BOX:ADDSTACK`, then instead of these boxes
        being laid you horizontally or vertically next to each
        other as widgets would normally be, they all occupy the
        same space of the screen.  However, only one such box
        in the set of stacked boxes will be visible at a time.

        This is how you can implement a pane which has its contents
        replaced with several different variants depending on what
        variant you want to see at a time.  (i.e. a window with
        an area who's contents are toggled by hitting some "tab"
        buttons that change which version of the contents get shown.)

        When several such boxes have been added, you can individually
        choose which one is shown, by which one is enabled.  If two
        of them are enabled at the same time, then only the first
        enabled one it finds gets shown.
        
        See :meth:`SHOWONLY` below for more information on how to
        manipulate these kinds of sub-boxes.

    .. method:: ADDSCROLLBOX

        :return:
        :rtype: :struct:`ScrollBox`
        
        Creates a nested scrollable box of widgets. 

        Using this kind of box, you can create an area of the Gui
        which holds contents bigger than it can show at once.
        It will add scrollbars to let you pan the view to see
        the rest of the content that is outside the visible box size.

        To make this work, you will need to specify the size
        limits of the viewable area, otherwise the layout system
        will simply make the ScrollBox big enough to hold all
        the content, and thus it won't need the scrollbars.

        More details on how to do this can be found in the documentation
        for :struct:`ScrollBox`.
        
    .. method:: ADDSPACING(size)

        :parameter size: :??: the size of the area to take up with empty space.
        :return:
        :rtype: :struct:`Spacing`

        Creates blank space of the given size in pixels (flexible if -1).

        This is used for cases where you'd like to force a widget to get indented,
        or pushed further down.

        Whether this is horizontal or vertical space depends on whether it is
        inside a horizontal arrangement box or a vertical arrangement box.
        (``myBox:ADDSPACING(20).`` is 20 pixels of *width* if ``myBox``
        was a :meth:`BOX:ADDHLAYOUT`, but it's 20 pixels of *height* if it
        was a :meth:`BOX:ADDVLAYOUT`.)

        Example::

            set mygui to GUI(400).
            set mytitle to mygui:addlabel("This is my Panel").
            set box1 to mygui:ADDHLAYOUT().
            box1:addspacing(50). // 50 pixels indent inside horizontal box 1
            set button1 to box1:addbutton("indented").
            set box2 to mygui:ADDHLAYOUT().
            box2:addspacing(100). // 100 pixels indent inside horizontal box 2
            set button2 to box2:addbutton("indented more").
            myGui:show().
            print "Play with buttons for 15 seconds.".
            wait 15. 
            myGui:dispose(). // get rid of the GUI before quitting the program.

    .. attribute:: WIDGETS

        :type: :struct:`List(Widget)`
        :access: Get-only
        
        Returns a LIST of the widgets that have been added to the Box,
        so that you may examine them.  If you think of the GUI as a
        tree of widgets (which is what it is), then this is how you
        find the children of this box.  It's sort of the opposite of
        :attr:`Widget:PARENT`.

        Note that adding or deleting from this list will not actually
        add or remove widgets from the box itself.  (This list is
        an exported copy of the list of widgets, and not the actual
        list the box itself uses internally.)

    .. attribute:: RADIOVALUE

        :type: :struct:`String`
        :access: Get-only
        
        The text label of whichever radiobutton is turned on among all
        the radio buttons you've added with :meth:`BOX:ADDRADIOBUTTON(text, on)`
        to this box.

        Because only one of the radio buttons within this box can be on
        at a time, this can be a faster way to see which has been
        selected than reading each button one a time to see which one
        is on.

        If none of the buttons are turned on (for example, if the user
        hasn't selected anything yet since the box was displayed), then this
        will return a value of ``""`` (an empty string).

    .. attribute:: ONRADIOCHANGE

        :type: :struct:`KOSDelegate`
        :access: Get/Set

        The :struct:`KOSDelegate` accepts 1 parameter, a :struct:`Button`, and returns nothing.

        A callback hook you want kOS to call whenever the radio button
        selection within this box changes.

        Because a radio button set is defined at the level of a ``Box`` (see
        :meth:`BOX:ADDRADIOBUTTON(text, on)`), the callback hook you would
        like to be called whenever that radio button set changes which button
        is the selected one is also here on the ``Box`` widget.

        The KOSDelegate must be a function (or anonymous function) that
        behaves as follows::

            function myradiochangehook {
              parameter whichButton.

              // Do something here.  "whichButton" will be a variable set to
              // whichever radio button is the one that has just been switched
              // on.
            }
            set someBox:onradiochange to myradiochange@.

        Example, using an anonymous function::

            set someBox:onRadioChange to { parameter B.  print "You selected:  " + B:text. }.

    .. method:: SHOWONLY(widget)

        :parameter:
        :type widget: :struct:`Widget`

        When multiple widgets have been placed inside this ``Box``,
        this suffix is used to choose just one of them to be the
        one you want being shown at the moment.  All other widgets
        within this box will be immediately hidden.

        This is useful when you have several stacked boxes made with
        calls to :meth:`BOX:ADDSTACK`, and want to choose which one
        of them you are making visible at the moment.

    .. method:: CLEAR

        :return: none

        Calling :meth:`BOX:CLEAR()` will get rid of all widgets you have
        added to this box by use of any of the above "ADD....." suffixes.
        It will also call :meth:`Widget:DISPOSE()` on all of them.

.. structure:: GUI

    This object is created with the :func:`GUI(width,height)` function.

    A GUI object is a kind of :struct:`Box` that is the outermost
    window that holds all the other widgets.  In order to work at all, all
    widgets must be put inside of a ``GUI`` box, or in inside of another
    :struct:`Box` which in turn is inside a ``GUI`` box, etc.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
       Every suffix of :struct:`BOX`.  Note, to add widgets to this window, see the BOX suffixes.
    -----------------------------------------------------------------------------------
    :attr:`X`                             :struct:`scalar` (pixels)       X-position of the window. Negative values measure from the right.
    :attr:`Y`                             :struct:`scalar` (pixels)       Y-position of the window. Negative values measure from the bottom.
    :attr:`DRAGGABLE`                     :struct:`Boolean`               True = user can move window.
    :attr:`EXTRADELAY`                    :struct:`scalar` (seconds)      Add artificial delay to all communication with this GUI (good for testing before you get into deep space)
    :attr:`SKIN`                          :struct:`Skin`                  The skin defining the default style of widgets in this GUI.
    ===================================== =============================== =============

    .. attribute:: X

        :type: :struct:`scalar`
        :access: Get/Set

        This is the X position of upper-left corner of window, in pixels.

        You can alter this value to move the window.

        If you use a negative value for the coordinate, then the coordiante will be
        measured in the reverse direction, from the right edge of the screen.  (i.e.
        setting it to -200 means 200 pixels away from the right edge of the screen.)

    .. attribute:: Y

        :type: :struct:`scalar`
        :access: Get/Set

        This is the Y position of upper-left corner of window, in pixels.

        You can alter this value to move the window.

        If you use a negative value for the coordinate, then the coordiante will be
        measured in the reverse direction, from the bottom edge of the screen.  (i.e.
        setting it to -200 means 200 pixels away from the bottom edge of the screen.)

    .. attribute:: DRAGGABLE

        :type: :struct:`Boolean`
        :access: Get/Set

        Set to true to allow the window to be moved by the user dragging it.  If
        set to false, it can still be moved by the script setting :attr:`GUI:X`
        and :attr:`GUI:Y`, but can't be moved by the user.

    .. attribute:: EXTRADELAY

        :type: :struct:`scalar`
        :access: Get/Set

        This is the number of extra seconds of delay to add to
        the GUI for testing purposes.

        If Remote Tech is installed, the GUI system :ref:`obeys the signal delay<widgets_delay>`
        of the Remote Tech mod such that when you click a widget it can take
        time before the script notices you did so.  If you want to test how
        your GUI will work under a signal delay you can use this suffix to
        force a simulated additional signal delay even if you are not using
        RemoteTech.  (Or when you are using RemoteTech but are testing your GUI in
        situations where there isn't a noticable signal delay, like in Kerbin
        low orbit).

    .. attribute:: SKIN

        :type: :struct:`Skin`
        :access: Get/Set

        A :struct:`Skin` is a collection of :struct:`Style`s to be
        used by different types of widgets within the GUI window.  With this
        suffix you can assign a different Skin to the window, which will then
        be used by default by all the widgets of the appropriate type
        inside the window.

.. structure:: ScrollBox

    ``ScrollBox`` objects are created by using :meth:`BOX:ADDSCROLLBOX`.

    A scollbox is a box who's contents can be bigger than it is, accessable
    via scrollbars.

    To constrain the actual size of the box, you can use the ``:style``
    suffix of the box.  For example, this code::

        set sb to mygui:addscrollbox().
        set sb:style:width to 200.
        set sb:style:height to 200.

    would make a scrollbox whose visible part is limited to 200 pixels by 200 pixels.

    By default, the GUI layout manager would attempt to make the scrollbox as big
    as it can, within the constraints of the containing window.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
                   Every suffix of :struct:`BOX`.  :attr:
    -----------------------------------------------------------------------------------
    :attr:`HALWAYS`                       :struct:`Boolean`               Always show the horizontal scrollbar.
    :attr:`VALWAYS`                       :struct:`Boolean`               Always show the vertical scrollbar.
    :attr:`POSITION`                      :struct:`Vector`                The position of the scrolled content (Z is ignored).
    ===================================== =============================== =============

    .. attribute:: :HALWAYS

        :type: :struct:`Boolean`
        :access: Get/Set

        Set to true if you want the horizontal scrollbar to always appear for the
        box regardless of whether the contents are large enough to require it.

    .. attribute:: :VALWAYS

        :type: :struct:`Boolean`
        :access: Get/Set

        Set to true if you want the vertical scrollbar to always appear for the
        box regardless of whether the contents are large enough to require it.

    .. attribute:: POSITION

        :type: :struct:`Vector`
        :access: Get/Set

        This value tells you where within the window's content the currently
        visible portion is.  The Vector's X component tells you the X
        coordinate of the upper-left corner of the visible portion within
        the content.  The Vector's Y component tells you the Y coordinate
        of the upper-left corner of the visible portion within the content.
        The Vector's Z component is irrelevant and ignored.  (This is really
        an X/Y pair stored inside a 3D vector).

        You can set this value to force the window to scroll to a new position.

    .. structure:: Label

        ``Label`` widgets are created inside Box objects via :meth:`BOX:ADDLABEL`.

        A ``Label`` is a widget that just shows a bit of text or an image.  The base
        type of Label is just used for passive content that can't be edited or
        interacted with.

        (However, other widgets which *are* interactive are derived from ``Label``,
        such as :struct:`Button` and :struct:`TextField`.)

        ===================================== =============================== =============
        Suffix                                Type                            Description
        ===================================== =============================== =============
                       Every suffix of :struct:`WIDGET`
        -----------------------------------------------------------------------------------
        :attr:`TEXT`                          :struct:`string`                The text on the label.
        :attr:`IMAGE`                         :struct:`string`                Filename of an image for the label.
        :attr:`TOOLTIP`                       :struct:`string`                A tooltip for the label.
        ===================================== =============================== =============

    .. attribute:: TEXT

        :type: :struct:`String`
        :access: Get/Set

        The text which is shown the label.

        This text can contain some limited richtext markup,
        :ref:`described below <richtext>`, unless you have
        suppressed it using :attr:`Style:RICHTEXT` as follows::

            set thislabel:RICHTEXT to false. // prevent richtext markup in the label

    .. attribute:: IMAGE

        :type: :struct:`string`
        :access: Get/Set

        This is the filename of an image file to use in the label's background.

        If you prefer an image to a string label, you can set this suffix.  The
        filenames you use must be contained in the Archive (i.e. "/Ships/Script")
        volume, but are allowed to disobey the normal rules about reaching the
        archive with comms.  This is because these images conceptually represent
        the look and feel of control panels in the ship and not necessarily
        something that takes up "space" on the disk.

        PNG format images usually work best, although any format Unity
        is capable of reading can work here.

        You can leave off the ``".png"`` ending on the filename if you like
        and this suffix will presume you meant to read a .png file.  If you 
        wish to read a file in some other format than PNG, you will need
        to give its filename extension explicitly.

    .. attribute:: :TOOLTIP

        :type: :struct:`String`
        :access: Get/Set

        String which you wish to appear in a tooltip when the user hovers
        the mouse pointer over this widget.

.. _richtext:

Rich Text
---------

Labels (and several other widgets that take text strings) can use a limited
markup system called Rich Text.  (This comes from Unity itself).

It looks slightly like HTML, but with only a very small number of tags
supported.  The list of supported tags is shown below:

- **<b>string</b>** - Shows the string in bold face.
- **<i>string</i>** - Shows the string in italic face.
- **<size=nnn>string</size>** - Changes the font size to a number (Unity
  is unclear whether this is in pixels or points).
- **<color=name>string</color>** - Selects a color, which can be expressed
  by name, and is assumed to be opaque.
- **<color=#nnnnnnnn>string</color>** - Selects a color, expressed using
  8 hexidecimal digits in pairs representing red, green, blue, and alpha.
  (For example, all red, fully opaque would be ``#ff0000ff``, while all-red
  half-transparent would be ``#ff000080``.)


This feature can be suppressed in a widget if you don't like it.
You suppress it by setting that widget's :attr:`Style:RICHTEXT` suffix
to false, for example::

    set mylabel:style:richtext to false.

(Doing so can be useful if you're trying to display text which 
contains the punctuation marks ``"<"``, or ``">"``, and want
to prevent them from being interpreted as markup tags.)

Examples of usage::

    set mylabel1:text to "This is <b>important</b>.". // boldface
    set mylabel2:text to "This is <i>important</i>.". // italic
    set mylabel3:text to "This is <size=30>important</size>.". // enlarged font
    set mylabel4:text to "This is <color=orange>important</color>.". // orange by name
    set mylabel5:text to "This is <color=#ffaa00FF>important</color>.". // orange by hex code, opaque
    set mylabel6:text to "This is <color=#ffaa0080>important</color>.". // orange by hex code, halfway transparent
    

.. structure:: Button

    A ``Button`` is a widget that can have script activity occur when the user
    presses it.

    ``Button`` widgets are created inside Box objects via one of these three methods:
    
    - :meth:`Box:ADDBUTTON` - for a button that pops back out again on its own after being clicked.
    - :meth:`Box:ADDCHECKBOX` - for a toggle button that stays on when 
      clicked and doesn't turn off until clicked again.
    - :meth:`Box:ADDRADIOBUTTON` - A kind of checkbox that forms part of
      a set of checkboxes that only allow one of themselves to be on at a time.

    The differences between how these types of button behave come from how
    they will have their default :attr:`TOGGLE` and :attr:`EXCLUSIVE`
    suffixes set when they are created.

    Buttons are a special case of :struct:`Label`, and can use all the features
    of :struct:`Label` to define how their text looks.

    ===================================== ========================================== =============
    Suffix                                Type                                       Description
    ===================================== ========================================== =============
                   Every suffix of :struct:`LABEL`
    ----------------------------------------------------------------------------------------------
    :attr:`PRESSED`                       :struct:`Boolean`                          Is the button currently down?
    :attr:`TAKEPRESS`                     :struct:`Boolean`                          Return the PRESSED value AND release the button if it's down.
    :attr:`TOGGLE`                        :struct:`Boolean`                          Is this button into a toggle-style button?
    :attr:`EXCLUSIVE`                     :struct:`Boolean`                          Does turning this button on cause other buttons to turn off?
    :attr:`ONCLICK`                       :struct:`KOSDelegate` (no args)            Your function called whenever the button gets clicked.
    :attr:`ONTOGGLE`                      :struct:`KOSDelegate` (:struct:`Boolean`)  Your function called whenever the button's PRESSED state changes.
    ===================================== ========================================== =============

    .. attribute:: PRESSED

        :type: :struct:`Boolean`
        :access: Get/Set

        You can read this value to see if the button is currently on (true)
        or off (false).  You can set this value to cause the button to become
        on or off.

    .. attribute:: TAKEPRESS

        :type: :struct:`Boolean`
        :access: Get-only

        You can read this value to see if the button is currently on (true)
        or off (false), however reading this value has a side-effect.
        When you read this value, if it said the button was on (pressed in),
        then reading it will cause the button to become off (popped out).

        This is useful only for normal buttons (buttons that have :attr:`TOGGLE`
        set to false).  It allows you to use the
        :ref:`polling technique <gui_polling_technique>` to repeatedly check to
        see if the button is on, and as soon as your script notices that it's on,
        it will pop it back out again so the user sees the proper visual feedback.

    .. attribute:: TOGGLE

        :type: :struct:`Boolean`
        :access: Get/Set

        This suffix determines whether this button has toggle behavior or button
        behaviour.  (Whether it stays pressed in until pressed a second time).
        By default, :meth`BOX:ADDBUTTON` will create a button with ``TOGGLE`` set
        to false, while :meth:`BOX:ADDCHECKBOX` and :meth:`BOX:ADDRADIOBUTTON` will
        create buttons which have their ``TOGGLE`` suffixes set to true.

        **Behaviour when TOGGLE is false (the default):**

        The conditions under which a button will automatically release itself when :attr:`TOGGLE` is
        set to `False` are:

        - When the script calls the :attr:`TAKEPRESS` suffix method.  When this is done, the
          button will become false even if it was was previously true.
        - If the script defines an :attr:`ONCLICK` user delegate.
          (Then when the :attr:`PRESSED` value becomes true, kOS will immediately set it
          back to false (too fast for the kerboscript to see it) and instead call the
          ``ONCLICK`` callback delegate you gave it.)

        **Behaviour when TOGGLE is true:**

        If TOGGLE is set to True, then the button will **not** automatically release after it is
        read by the script.  Instead it will need to be clicked by the user a second time to make
        it pop back out.  In this mode, the button's :attr:`PRESSED` value will never automatically
        reset to false on its own.

        If the Button is created by :meth:`Button:ADDCHECKBOX`, or by
        :meth:`Button:ADDRADIOBUTTON`, it will have a different visual
        style (the style called "toggle") and it will start already in TOGGLE mode.

    .. attribute:: EXCLUSIVE

        :type: :struct:`Boolean`
        :access: Get/Set

        If the Button is created by :meth:`Button:ADDRADIOBUTTON`, it will have
        its ``EXCLUSIVE`` suffix set to true by default.

        If ``EXCLUSIVE`` is set to True, when the button is clicked (or changed programmatically),
        other buttons with the same parent :struct:`Box` will be set to False (regardless of
        if they are EXCLUSIVE).

    .. attribute:: ONCLICK

        :type: :struct:`KOSDelegate`
        :access: Get/Set

        This is a :struct:`KOSDelegate` that takes no parameters and returns nothing.

        ``ONCLICK is what is known as a "callback hook".  This suffix allows
        you to use the :ref:`callback technique <gui_callback_technique>` of widget
        interaction.

        You can assign ``ONCLICK`` to a :struct:`KOSDelegate` of one of your
        functions (named or anonymous) and from then on kOS will call that
        function whenever the button becomes clicked by the user.

        The :attr:`ONCLICK` suffix is intended to be used for non-toggle buttons.

        Example::

            set mybutton:ONCLICK to { print "Do something here.". }.

        :attr:`ONCLICK` is called with no parameters.  To use it, your function must be
        written to expect no parameters.

    .. attribute:: ONTOGGLE

        :type: :struct:`KOSDelegate`
        :access: Get/Set

        This is a :struct:`KOSDelegate` taking one parameter (new boolean state) and returning nothing

        ``ONTOGGLE`` is what is known as a "callback hook".  This suffix allows
        you to use the :ref:`callback technique <gui_callback_technique>` of widget
        interaction.

        The ``ONTOGGLE`` delegate you assign will get called whenver kOS notices
        that this button has changed from false to true or from true to false.

        To use ``ONTOGGLE``, your function must be written to expect a single boolean parameter,
        which is the new state the button has just be changed to.

        Example::

            set mybutton:ONTOGGLE to { parameter val. print "Button value just became " + val. }.

        ``ONTOGGLE`` is really only useful with buttons where :attr:`TOGGLE` is true.

Example
-------

Here is a longer example of buttons using the button callback hooks::

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

.. structure:: PopupMenu

    ``PopupMenu`` objects are created by calling :meth:`BOX:ADDPOPUPMENU`.

    A ``PopupMenu`` is a special kind of button for choosing from a list of things.
    It looks like a button who's face displays the currently selected thing.  When a user
    clicks on the button, it pops up a list of displayed strings to choose
    from, and when one is selected the popup goes away and the new choice is
    displayed on the button.

    The menu displays the string values in the OPTIONS property. If OPTIONS contains items that are not strings,
    then by default their :attr:`TOSTRING <Structure:TOSTRING>` suffixes will be used to display them as strings.
    You can change this default behaviour by setting the popupmenu's :attr:`OPTIONSUFFIX`.

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
    :attr:`MAXVISIBLE`                    :struct:`Scalar` (integer)                How many choices to show at once in the list (if more exist, it makes it scrollable).
    ===================================== ========================================= =============

    .. attribute:: OPTIONS

        :type: :struct:`List` (of any Structure)
        :access: Get/Set

        This is the list of options the user has to choose from.  They don't need
        to be Strings, but they must be capable of having a string extracted from
        them for display on the list, by use of the :attr"`OPTIONSSUFFIX` suffix.

    .. attribute:: OPTIONSSUFFIX

        :type: :struct:`String`
        :access: Get/Set

        This decides how you get strings from the list of items in :attr:`OPTIONS`.
        The usual way to use a ``PopupMenu`` would be to have it select from a list
        of strings.  But you can use any other kind of object you want in the
        :attr:`OPTIONS` list, provided all of them share a common suffix name
        that builds a string from them.  The default value for this is
        ``"TOSTRING:``, which is a suffix that all things in kOS have.  If you
        wish to use something other than :attr:`Structure:TOSTRING`, you
        can set this to that suffix's string name.

        This page begins with a good example of using this.  See above.

    .. method:: ADDOPTION(value)

        :parameter: - any kind of kOS type, provided it has the suffix mentioned in :attr:`OPTIONSSUFFIX` on it.
        :type value: :struct:`Structure`
        :access: Get/Set

        This appends another choice to the :attr:`OPTIONS` list.

    .. attribute:: VALUE

        :type: :struct:`Structure`
        :access: Get/Set

        Returns the value currently chosen from the list.
        If no selection has been made, it will return an empty :struct:`String` (``""``).

        If you set it, you are choosing which item is selected from the list.  If you
        set this to something that wasn't in the list, the attempt to set it will be
        rejected and instead the choice will become de-selected.

    .. attribute:: INDEX

        :type: :struct:`Scalar`
        :access: Get/Set

        Returns the number index into the :attr:`OPTIONS` list that goes with the
        current choice.  If this is set to -1, that means nothing has been
        selected.

        Setting this value causes the selected choice to change.  Setting it
        to -1 will de-select the choice.

    .. attribute:: CHANGED

        :type: :struct:`Boolean`
        :access: Get/Set

        Has the choice been changed since the last time this was checked?

        Note that reading this has a side effect.  When you read this value,
        you cause it to become false if it had been true.  (The system
        assumes that "last time this was checked" means "now" after you've
        read the value of this suffix.)

        This is intended to be used with the
        :ref:`polling technique <gui_polling_technique>` of reading the widget.
        You can query this field until it says it's true, at which point you
        know to go have a look at the current value to see what it is.

    .. attribute:: ONCHANGE

        :type: :struct:`KOSDelegate`
        :access: Get/Set

        This is a :struct:`KOSDelegate` that expects one parameter, the new value, and returns nothing.

        Sets a callback hook you want called when a new selection has
        been made.  This is for use with the
        :ref:`callback technique <gui_callback_technique>` of reading the widget.

        The function you specify must be designed to take one parameter,
        which is the new value (same as reading the :attr:`VALUE` suffix) of
        this widget, and return nothing.

        Example::

            set myPopupMenu:ONCHANGE to { parameter choice. print "You have selected: " + choice:TOSTRING. }.

    .. method:: CLEAR

        :return: (nothing)

        Calling this causes the ``PopupMenu`` to wipe out all the contents of its :attr:`OPTIONS`
        list.

    .. attribute:: MAXVISIBLE

        :type: :struct:`Scalar`
        :access: Get/Set

        (Default value is 15).

        This sets the largest number of choices (roughly) the layout
        system will be willing to grow the popup window to support
        before it resorts to using a scrollbar to show more choices,
        instead of letting the window get any bigger.  This value is
        only a rough hint.

        If this is set too large, it can become possible to make
        the popup menu so large it won't fit on the screen, if you
        give it a lot of items in the options list.

.. structure:: TextField

    ``TextField`` objects are created via :meth:`BOX:ADDTEXTFIELD`.

    A ``TextField`` is a special kind of :struct:`Label` that can be
    edited by the user.  Unlike a normal :struct:`Label`, a ``TextField``
    can only be textual (it can't be used for image files).

    A ``TextField`` has a default style that looks different from a
    passive :struct:`Label`.  In the default style, a ``TextField`` shows
    the area the user can click on and type into, using a recessed
    background.

    ===================================== ========================================= =============
    Suffix                                Type                                      Description
    ===================================== ========================================= =============
           Every suffix of :struct:`LABEL`.  Note you read :attr:`Label:TEXT` to see the TextField's current value.
    ---------------------------------------------------------------------------------------------
    :attr:`CHANGED`                       :struct:`Boolean`                         Has the text been edited?
    :attr:`ONCHANGED`                     :struct:`KOSDelegate` (:struct:`String`)  Your function called whenever the :attr:`CHANGED` state changes.
    :attr:`CONFIRMED`                     :struct:`Boolean`                         Has the user pressed Return in the field?
    :attr:`ONCONFIRMED`                   :struct:`KOSDelegate` (:struct:`String`)  Your function called whenever the :attr:`CONFIRMED` state changes.
    ===================================== ========================================= =============

    .. attribute:: CHANGED

        :type: :struct:`Boolean`
        :access: Get/Set

        Tells you whether :attr:`Label:TEXT` has been edited at all
        since the last time you checked.  Note that any edit counts.  If a
        user is trying to type "123" into the ``TextField`` and has so far
        written "1" and has just pressed the "2", then this will be true.
        If they then press "4" this will be true again.  If they then press
        "backspace" because this was type, this will be true again.  If
        they then press "3" this will be true again.  Literally *every*
        edit to the text counts, even if the user has not finished using
        the textfield.

        As soon as you read this suffix and it returns true, it will
        be reset to false again until the next time an edit happens.

        This suffix is intended to be used with the 
        :ref:`polling technique <gui_polling_technique>` of widget
        interaction.

    .. attribute:: ONCHANGED

        :type: :struct:`KOSDelegate`
        :access: Get/Set

        This :struct:`KOSDelegate` expects one parameter, a :struct:`String`, and returns nothing.

        This allows you to set a callback delegate to be called
        whenever the value of :attr:`Label:TEXT` changes in any
        way, whether that's inserting a character or deleting a
        character.

        The :struct:`KOSDelegate` you use must be made to expect
        one parameter, the new string value, and return nothing.

        Example::

            set myTextField:ONCHANGED to {parameter str. print "Value is now: " + str.}.

        This suffix is intended to be used with the 
        :ref:`callback technique <gui_callback_technique>` of widget
        interaction.

    .. attribute:: CONFIRMED

        :type: :struct:`Boolean`
        :access: Get/Set

        Tells you whether the user is finished editing :attr:`Label:TEXT`
        since the last time you checked.  This does not become true merely
        because the user typed one character into the field or deleted
        one character (unlike :attr:`CHANGED`, which does).  This only
        becomes true when the user does one of the following things:

        - Presses ``Enter`` or ``Return`` on the field.
        - Leaves the field (clicks on another field, tabs out, etc).

        As soon as you read this suffix and it returns true, it will
        be reset to false again until the next time the user commits
        a change to this field.

        This suffix is intended to be used with the 
        :ref:`polling technique <gui_polling_technique>` of widget
        interaction.

    .. attribute:: ONCONFIRMED

        :type: :struct:`KOSDelegate`
        :access: Get/Set

        This :struct:`KOSDelegate` expects one parameter, a :struct:`String`, and returns nothing.

        This allows you to set a callback delegate to be called
        whenever the user has finished editing :attr:`Label:TEXT`.
        Unlike :attr:`CHANGED`, this does not get called every
        time the user types a key into the field.  It only gets
        called when one of the following things happens reasons:

        - User presses ``Enter`` or ``Return`` on the field.
        - User leaves the field (clicks on another field, tabs out, etc).

        The :struct:`KOSDelegate` you use must be made to expect
        one parameter, the new string value, and return nothing.

        Example::

            set myTextField:ONCONFIRMED to {parameter str. print "Value is now: " + str.}.

        This suffix is intended to be used with the 
        :ref:`callback technique <gui_callback_technique>` of widget
        interaction.


    .. note::

        The values of :attr:`CHANGED` and :attr:`CONFIRMED` reset to False as soon as their value is accessed.

    .. structure:: Slider

        ``Slider`` widgets are created via :meth:`BOX:ADDHSLIDER`
        and :meth:`BOX:ADDVSLIDER`.

        A ``Slider`` is a widget that holds the value of a :struct:`Scalar`
        that the user can adjust by moving a sliding marker along a line.

        It is suited for real-number varying values, but not well suited
        for integer values.


    ===================================== ========================================= =============
    Suffix                                Type                                      Description
    ===================================== ========================================= =============
                   Every suffix of :struct:`WIDGET`
    ---------------------------------------------------------------------------------------------
    :attr:`VALUE`                         :struct:`scalar`                          The current value. Initially set to :attr:`MIN`.
    :attr:`ONCHANGED`                     :struct:`KOSDelegate` (:struct:`Scalar`)  Your function called whenever the :attr:`VALUE` changes.
    :attr:`MIN`                           :struct:`scalar`                          The minimum value (leftmost on horizontal slider).
    :attr:`MAX`                           :struct:`scalar`                          The maximum value (bottom on vertical slider).
    ===================================== ========================================= =============

    .. attribute:: VALUE

        :type: :struct:`Scalar`
        :access: Get/Set

        The current value of the slider.

    .. attribute:: ONCHANGED

        :type: :struct:`KOSDelegate`
        :access: Get/Set

        This :struct:`KOSDelegate` takes one parmaeter, the value, and returns nothing.

        This allows you to set a callback delegate to be called
        whenever the user has moved the slider to a new
        value.  Note that as the user moves the slider
        to a new position, this will get called several
        times along the way, giving sevearl intermediate
        values on the way to the final value the user leaves
        the slider at.

        Example::

            set mySlider:ONCHANGED to whenMySliderChanges@.

            function whenMySliderChanges {
              parameter newValue.

              print "Value is " + 
                     round(100*(newValue-mySlider:min)/(mySlider:max-mySlider:min)) +
                     "percent of the way between min and max.".
            }

        This suffix is intended to be used with the 
        :ref:`callback technique <gui_callback_technique>` of widget
        interaction.

    .. attribute:: MIN

        :type: :struct:`Scalar`
        :access: Get/Set

        The "left" (for horizontal sliders) or "top" (for vertical sliders)
        endpoint value of the slider.
        
        Note that despite the name, :attr:`MIN` doesn't have to be smaller
        than :attr:`MAX`.  If :attr:`MIN` is larger than :attr:`MAX`, then
        that causes the slider to swap their meaning, and reverse its direction.
        (i.e. where numbers normally get larger when you slide to the right,
        inverting MIN and MAX causes the numbers to get larger when you
        slide to the left.)

    .. attribute:: MAX

        :type: :struct:`Scalar`
        :access: Get/Set

        The "right" (for horizontal sliders) or "bottom" (for vertical sliders)
        endpoint value of the slider.
        
        Note that despite the name, :attr:`MIN` doesn't have to be smaller
        than :attr:`MAX`.  If :attr:`MIN` is larger than :attr:`MAX`, then
        that causes the slider to swapr their meaning, and reverse its direction.
        (i.e. where numbers normally get larger when you slide to the right,
        inverting MIN and MAX causes the numbers to get larger when you
        slide to the left.)


    .. structure:: Spacing

        ``Spacing`` widgets are created via :meth:`BOX:ADDSPACING`.

        A ``Spacing`` is just an invisible space for the purpose of
        pushing other widgets further to the right or further
        down, forcing the layout to come out the way you like.

        ===================================== =============================== =============
        Suffix                                Type                            Description
        ===================================== =============================== =============
                       Every suffix of :struct:`WIDGET`
        -----------------------------------------------------------------------------------
        :attr:`AMOUNT`                        :struct:`scalar`                The amount of space, or -1 for flexible spacing.
        ===================================== =============================== =============

    .. attribute:: AMOUNT

        :type: :struct:`Scalar`
        :access: Get/Set

        The number of pixels for this spacing to take up.  Whether this
        is horizontal or vertial space depends on whether this is being
        added to a horizontal-layout box or a vertical-layout box.

.. structure:: Skin

    A ``Skin`` is a set of :struct:`Style` settings defined for various
    widget types. It defines what default style will be used for each
    type of widget inside the GUI.  Changes to the styles on a GUI:SKIN
    will affect the subsequently created widgets inside that GUI window.
    Note that some of the styles are used by subparts of widgets, such as the
    HORIZONTALSLIDERTHUMB, which is used by a SLIDER when oriented horizontally.

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

    :meth:`ADD(name)`                      :struct:`Style`             Adds a new style.
    :meth:`HAS(name)`                      :struct:`Boolean`           Does the skin have the named style?
    :meth:`GET(name)`                      :struct:`Style`             Gets a style by name (including ADDed styles).
    ====================================== =========================== =============

    .. attribute:: BOX

        :type: :struct:`Style`
        :access: Get/Set

        Style for :struct:`Box` widgets.

    .. attribute:: BUTTON

        :type: :struct:`Style`
        :access: Get/Set

        Style for :struct:`Button` widgets.

    .. attribute:: HORIZONTALSCROLLBAR

        :type: :struct:`Style`
        :access: Get/Set

        Style for the horizontal scrollbar of :struct:`ScrollBox` widgets.

    .. attribute:: HORIZONTALSCROLLBARLEFTBUTTON

        :type: :struct:`Style`
        :access: Get/Set

        Style for the horizontal scrollbar left button of :struct:`ScrollBox` widgets.

    .. attribute:: HORIZONTALSCROLLBARRIGHTBUTTON

        :type: :struct:`Style`
        :access: Get/Set

        Style for the horizontal scrollbar right button of :struct:`ScrollBox` widgets.

    .. attribute:: HORIZONTALSCROLLBARTHUMB

        :type: :struct:`Style`
        :access: Get/Set

        Style for the horizontal scrollbar thumb of :struct:`ScrollBox` widgets.

    .. attribute:: HORIZONTALSLIDER

        :type: :struct:`Style`
        :access: Get/Set

        Style for horizontal :struct:`Slider` widgets.

    .. attribute:: HORIZONTALSLIDERTHUMB

        :type: :struct:`Style`
        :access: Get/Set

        Style for the thumb of horizontal :struct:`Slider` widgets.

    .. attribute:: VERTICALSCROLLBAR

        :type: :struct:`Style`
        :access: Get/Set

        Style for the vertical scrollbar of :struct:`ScrollBox` widgets.

    .. attribute:: VERTICALSCROLLBARLEFTBUTTON

        :type: :struct:`Style`
        :access: Get/Set

        Style for the vertical scrollbar left button of :struct:`ScrollBox` widgets.

    .. attribute:: VERTICALSCROLLBARRIGHTBUTTON

        :type: :struct:`Style`
        :access: Get/Set

        Style for the vertical scrollbar right button of :struct:`ScrollBox` widgets.

    .. attribute:: VERTICALSCROLLBARTHUMB

        :type: :struct:`Style`
        :access: Get/Set

        Style for the vertical scrollbar thumb of :struct:`ScrollBox` widgets.

    .. attribute:: VERTICALSLIDER

        :type: :struct:`Style`
        :access: Get/Set

        Style for vertical :struct:`Slider` widgets.

    .. attribute:: VERTICALSLIDERTHUMB

        :type: :struct:`Style`
        :access: Get/Set

        Style for the thumb of vertical :struct:`Slider` widgets.

    .. attribute:: LABEL

        :type: :struct:`Style`
        :access: Get/Set

        Style for :struct:`Label` widgets.

    .. attribute:: SCROLLVIEW

        :type: :struct:`Style`
        :access: Get/Set

        Style for :struct:`ScrollBox` widgets.

    .. attribute:: TEXTFIELD

        :type: :struct:`Style`
        :access: Get/Set

        Style for :struct:`TextField` widgets.

    .. attribute:: TOGGLE

        :type: :struct:`Style`
        :access: Get/Set

        Style for :struct:`Button` widgets in toggle mode (GUI:ADDCHECKBOX and GUI:ADDRADIOBUTTON).

    .. attribute:: FLATLAYOUT

        :type: :struct:`Style`
        :access: Get/Set

        Style for :struct:`Box` transparent widgets (GUI:ADDHLAYOUT and GUI:ADDVLAYOUT).

    .. attribute:: POPUPMENU

        :type: :struct:`Style`
        :access: Get/Set

        Style for :struct:`PopupMenu` widgets.

    .. attribute:: POPUPWINDOW

        :type: :struct:`Style`
        :access: Get/Set

        Style for the popup window of :struct:`PopupMenu` widgets.

    .. attribute:: POPUPMENUITEM

        :type: :struct:`Style`
        :access: Get/Set

        Style for the menu items of :struct:`PopupMenu` widgets.

    .. attribute:: LABELTIPOVERLAY

        :type: :struct:`Style`
        :access: Get/Set

        Style for tooltips overlayed on :struct:`Label` widgets.

    .. attribute:: WINDOW

        :type: :struct:`Style`
        :access: Get/Set

        Style for :struct:`GUI` windows.


    .. attribute:: FONT

        :type: :struct:`string`
        :access: Get/Set

        The name of the font used (if STYLE:FONT does not change it for an element).

    .. attribute:: SELECTIONCOLOR

        :type: :ref:`Color <colors>`
        :access: Get/Set
        
        The background color of selected text (eg. TEXTFIELD).

    .. method:: ADD(name)

        :parameter:
        :type name: :struct:`String`
        :return:
        :rtype: :struct:`Style`
        
        Adds a new style to the skin and names it.  The skin holds a list
        of styles by name which you can retrieve later.

    .. method:: HAS(name)

        :parameter:
        :type name: :struct:`String`
        :return:
        :rtype: :struct:`Style`
        
        Does the skin have the named style?

    .. method:: GET(name)

        :parameter:
        :type name: :struct:`String`
        :return:
        :rtype: :struct:`Style`
        
        Gets a style by name (including ADDed styles).

.. structure:: Style

    This object represents the style of a widget. Styles can be either changed directly
    on a :struct:`Widget`, or changed on the GUI:SKIN so as to affect all subsequently
    created widgets of a particular type inside that GUI.

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

    .. attribute:: HSTRETCH

        :type: :struct:`Boolean`
        :access: Get/Set

        Should the widget stretch horizontally? (default depends on widget subclass)

    .. attribute:: VSTRETCH

        :type: :struct:`Boolean`
        :access: Get/Set

        Should the widget stretch vertically?

    .. attribute:: WIDTH

        :type: :struct:`scalar`
        :access: Get/Set

        (pixels)       Fixed width (or 0 if flexible).

    .. attribute:: HEIGHT

        :type: :struct:`scalar`
        :access: Get/Set

        (pixels)       Fixed height (or 0 if flexible).

    .. attribute:: MARGIN

        :type: :struct:`StyleRectOffset`
        :access: Get/Set

        Spacing between this and other widgets.

    .. attribute:: PADDING

        :type: :struct:`StyleRectOffset`
        :access: Get/Set

        Spacing between the outside of the widget and its contents (text, etc.).

    .. attribute:: BORDER

        :type: :struct:`StyleRectOffset`
        :access: Get/Set

        Size of the edges in the 9-slice image for BG images in NORMAL, HOVER, etc.

    .. attribute:: OVERFLOW

        :type: :struct:`StyleRectOffset`
        :access: Get/Set

        Extra space added to the area of the background image. Allows the background to go beyond the widget's rectangle.

    .. attribute:: ALIGN

        :type: :struct:`string`
        :access: Get/Set

        One of "CENTER", "LEFT", or "RIGHT".

    .. note::

        The ALIGN attribute will not do anything useful unless either HSTRETCH is set to true or a fixed WIDTH is set,
        since otherwise it will be exactly the right size to fit the content of the widget with no alignment within that space being necessary.

        It is currently only relevant for the widgets that have scalar content (Label and subclasses).


    .. attribute:: FONT

        :type: :struct:`string`
        :access: Get/Set

        The name of the font of the text on the content or "" if the default.

    .. attribute:: FONTSIZE

        :type: :struct:`scalar`
        :access: Get/Set

        The size of the text on the content.

    .. attribute:: RICHTEXT

        :type: :struct:`Boolean`
        :access: Get/Set

        Set to False to disable rich-text (<i>...</i>, etc.)

    .. attribute:: NORMAL

        :type: :struct:`StyleState`
        :access: Get/Set

        Properties for the widget normally.

    .. attribute:: ON

        :type: :struct:`StyleState`
        :access: Get/Set

        Properties for when the widget is under the mouse and "on".

    .. attribute:: NORMAL_ON

        :type: :struct:`StyleState`
        :access: Get/Set

        Alias for ON.

    .. attribute:: HOVER

        :type: :struct:`StyleState`
        :access: Get/Set

        Properties for when the widget is under the mouse.

    .. attribute:: HOVER_ON

        :type: :struct:`StyleState`
        :access: Get/Set

        Properties for when the widget is under the mouse and "on".

    .. attribute:: ACTIVE

        :type: :struct:`StyleState`
        :access: Get/Set

        Properties for when the widget is active (eg. button being held down).

    .. attribute:: ACTIVE_ON

        :type: :struct:`StyleState`
        :access: Get/Set

        Properties for when the widget is active and "on".

    .. attribute:: FOCUSED

        :type: :struct:`StyleState`
        :access: Get/Set

        Properties for when the widget has keyboard focus.

    .. attribute:: FOCUSED_ON

        :type: :struct:`StyleState`
        :access: Get/Set

        Properties for when the widget has keyboard focus and is "on".

    .. attribute:: BG

        :type: :struct:`string`
        :access: Get/Set

        The same as NORMAL:BG. Name of a "9-slice" image file.

    .. attribute:: TEXTCOLOR
    
        :type: :strucT:`color`
        
        The same as NORMAL:TEXTCOLOR. The color of the text on the label.

.. structure:: StyleState

    A sub-structure of :struct:`Style`, used to define some properties
    of a style that only are applied under some dynamically changing
    conditions.   (For example, to set the color a widget will have
    when focused to be different from the color it will have when not
    focused.)

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
    :attr:`BG`                            :struct:`string`                Name of a "9-slice" image file. See note below.
    :attr:`TEXTCOLOR`                     :ref:`Color <colors>`           The color of the text on the label.
    ===================================== =============================== =============

    .. attribute:: BG

        :type: :struct:`String`
        :access: Get/Set

        This string is an image filename that must be stored in the archive
        folder (it cannot be on a local drive).  The image files are always
        found relative to volume 0 (the Ships/Scripts directory) and
        specifying a ".png" extension is optional.  Note, that this ignores the
        normal rules about finding the archive within comms range.  You are
        allowed to access these files even when not in range of the archive,
        because they represent the visual look of your ship's control panels,
        not actual files sent on the ship.

        This image is what is called a "9-slice image".  This is a kind of image
        designed to handle the difficulty of stretching an image properly
        to any size.  When you stretch an image for a background, you usually only
        want to stretch the middle part of the image in width and height, and not
        stretch the edges and corners of the image the same way.

        .. image:: /_images/general/9-slice.png
            :align: right

        The four corner pieces of the image are used as-is without stretching.

        The edge pieces of the image on the top and bottom are stretched
        horizontally but not vertically.

        The edge pieces of the image on the left and right are stretched
        vertically but not horizontally.

        Only the pixels in the center piece of the image are stretched
        both vertically and horizontally.

        The :attr:`Style:BORDER` attribute of the style for the widget
        defines where the left, right, top and bottom coordinates are
        to mark these 9 sections of the image.

        If set to "", these background images will default to the corresponding non-ON image
        and if that is also "", it will default to the normal `BG` image,
        and if that is also "", then it will default to completely transparent.

    .. attribute:: TEXTCOLOR

        :type: :struct:`Color`
        :access: Get/Set

        The color of foreground text within this widget when it is in this state.

.. structure:: StyleRectOffset

    A sub-structure of :struct:`Style`.

    This is used in places where you need to define a zone around the edges
    of a widget.  (Margins, padding, defining the segments of a 9-segment
    stretchable image, etc).

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

    .. attribute:: LEFT

        :type: :struct:`Scalar`
        :access: Get/Set

        Number of Pixels on the left

    .. attribute:: RIGHT

        :type: :struct:`Scalar`
        :access: Get/Set

        Number of Pixels on the right

    .. attribute:: TOP

        :type: :struct:`Scalar`
        :access: Get/Set

        Number of Pixels on the top

    .. attribute:: BOTTOM

        :type: :struct:`Scalar`
        :access: Get/Set

        Number of Pixels on the bottom

    .. attribute:: H

        :type: :struct:`Scalar`
        :access: Get/Set

        Sets the number of pixels on both the left and right to this
        same value. Getting the value returns just the value
        of LEFT (it does not test to see if RIGHT is the same value).

    .. attribute:: V

        :type: :struct:`Scalar`
        :access: Get/Set

        Sets the number of pixels on both the top and bottom to this
        same value. Getting the value returns just the value
        of TOP (it does not test to see if BOTTOM is the same value).

