.. _gui_widget:

Widget
------

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

        (No parameters, no return value).

        Call ``Widget:show()`` when you need to make the widget in question
        start appearing on the screen.  This is identical to setting
        :attr:`Widget:VISIBLE` to true.

        See :attr:`Widget:VISIBLE` below for further documentation.

        Note: Unless you use ``show()`` (or set the :struct:`Widget:VISIBLE`
        suffix to true) on the outermost :struct:`Box` of the GUI panel (the
        one you obtained from calling built-in function :func:`GUI`), nothing
        will ever be visible from your GUI.

    .. method:: HIDE

        (No parameters, no return value).

        Call ``Widget:hide()`` when you need to make the widget in question
        disappear from the screen.  This is identical to setting
        :attr:`Widget:VISIBLE` to false.
        
        See :attr:`Widget:VISIBLE` below for further documentation.

    .. attribute:: VISIBLE

        :type: :struct:`Scalar`
        :access: Get/Set

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

        (no parameters, no return value)

        Call ``Widget:DISPOSE()`` to permanenly make this widget go away.
        Not only will it make it invisible, but it will make it impossible
        to set it to visible again later.

    .. attribute:: ENABLED

        :type: :struct:`Boolean`
        :access: Get/Set

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
	:access: Get-only

	Widgets can be contained inside Boxes that are contained inside
	other Boxes, etc.  This suffix tells you which :struct:`Box` contains
	this one.  If you attempt to call this suffix on the outermost
	:struct:`GUI` Box that contains all the others in a panel,
	you may find that kOS throws a complaining error because there is
	no parent to the outermost widget.  To protect your code against this,
	use the :attr:`Widget:HASPARENT` suffix.

    .. attribute:: HASPARENT

	:type: :struct:`Boolean`
	:access: Get-only

	If trying to use :attr:`Widget:PARENT` would generate an error because
	this widget has no parent, then :attr:`HASPARENT` will be false.
	Otherwise it will be true.
