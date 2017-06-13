.. _gui_box:

Box
---

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

        :parameter text: :struct:`String`
        :return: :struct:`Label`

        Creates a :struct:`Label` widget in this ``Box``.  The label will
        display the text message given in the parameter.

    .. method:: ADDBUTTON(text)

        :parameter text: :struct:`String`
        :return: :struct:`Button`

        Creates a *clickable* :struct:`Button` widget in this ``Box``.

    .. method:: ADDCHECKBOX(text, on)

        :parameter text: :struct:`String` text to display
        :parameter on: :struct:`Boolean` state of the checkbox initially 
        :return: :struct:`Button`

        Creates a *toggle-able* :struct:`Button` widget in this ``Box``.
        The Button will display the text message given in the parameter.
        The Button will initially start off turned on or turned off
        depending on the state of the ``on`` parameter.

    .. method:: ADDRADIOBUTTON(text, on)

        :parameter text: :struct:`String` text to display
	:parameter on: :struct:`Boolean` state of the checkbox initially
        :return: :struct:`Button`

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

    .. method:: ADDTEXTFIELD(text)

        :parameter text: struct:`String` initial starting text in the field.
        :return: :struct:`TextField`

        Creates a :struct:`TextField` widget in this ``Box``.
        The textfield will allow the user to type a string into the field
        that you can read.
        The field will be a one-line string input.

    .. method:: ADDPOPUPMENU

        :return: :struct:`PopupMenu`

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

        :parameter init: :struct:`Scalar` starting value
        :parameter min: :struct:`Scalar` left endpoint value
        :parameter max: :struct:`Scalar` right endpoint value
        :return: :struct:`Slider`

        Creates a horizontal :struct:`Slider` in the Box that adjusts a
        :struct:`Scalar` value.  The value can take on any fractional
        amount between the minimum and maximum values given.  Despite
        the names it is possible to make the ``min`` parameter larger than
        the ``max`` parameter, in which case the direction of the slider
        will be inverted, with the largest value at the left and the smallest
        at the right.

    .. method:: ADDVSLIDER(init, min, max)

        :parameter init: :struct:`Scalar` starting value
        :parameter min: :struct:`Scalar` top endpoint value
        :parameter max: :struct:`Scalar` bottom endpoint value
        :return: :struct:`Slider`

        Creates a vertical :struct:`Slider` in the Box that adjusts a
        :struct:`Scalar` value.  The value can take on any fractional
        amount between the minimum and maximum values given.  Despite
        the names it is possible to make the ``min`` parameter larger than
        the ``max`` parameter, in which case the direction of the slider
        will be inverted, with the largest value at the bottom and the smallest
        at the top.

    .. method:: ADDHLAYOUT

        :return: :struct:`Box`
        
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

        :return: :struct:`Box`
        
        Creates a nested transparent vertically-arranged :struct:`Box` in
        this :struct:`Box`.  You can't see any visual evidence of this
        box other than how it forces the widgets inside it to get arranged.
        (The box has no borders showing, no background color, etc).

        All the widgets added to such a box will arrange themselves
        vertically (the more widgets you add, the taller the box gets).

        (The :struct:`Box` returned by calling the built-in function
        :func:`Gui` is a "VLayout" box which arranges things vertically
        like this.)

        There are three reasons you might want to nest one Box inside another Box:

        - You wish to isolate some radio buttons into their own Box so they
          form one radio button group.
        - You wish to force the GUI automatic layout system to place widgets
          in a particular arrangement by making it treat a group of widgets
          as a single rectangular chunk that gets arranged together as a unit.

    .. method:: ADDHBOX

        :return: :struct:`Box`
        
        This is identical to :meth:`BOX:ADDHLAYOUT`, other than the
        fact that it uses a different graphical style which lets you
        see the box.

    .. method:: ADDVBOX

        :return: :struct:`Box`
        
        This is identical to :meth:`BOX:ADDVLAYOUT`, other than the
        fact that it uses a different graphical style which lets you
        see the box.

    .. method:: ADDSTACK

        :return: :struct:`Box`
        
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

        :return: :struct:`ScrollBox`
        
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

        :parameter size: :struct:`Scalar` the size of the area to take up with empty space.
        :return: :struct:`Spacing`

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

        :parameter widget:
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


