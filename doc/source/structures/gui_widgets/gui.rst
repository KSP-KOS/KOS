.. _gui_structure:

GUI structure
-------------

.. structure:: GUI

    This object is created with the :func:`GUI(width,height)` function.

    A GUI object is a kind of :struct:`Box` that is the outermost
    window that holds all the other widgets.  In order to work at all, all
    widgets must be put inside of a ``GUI`` box, or in inside of another
    :struct:`Box` which in turn is inside a ``GUI`` box, etc.

    (To get rid of all GUIs that you created from this CPU, you can use
    the :func:`CLEARGUIS` built-in-function, which is a shorthand
    for calling :attr:`Widget:HIDE` and :attr:`Widget:DISPOSE` on each
    of them.)

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
       Every suffix of :struct:`BOX`.  Note, to add widgets to this window, see the BOX suffixes.
    -----------------------------------------------------------------------------------
    :attr:`X`                             :struct:`Scalar` (pixels)       X-position of the window. Negative values measure from the right.
    :attr:`Y`                             :struct:`Scalar` (pixels)       Y-position of the window. Negative values measure from the bottom.
    :attr:`DRAGGABLE`                     :struct:`Boolean`               True = user can move window.
    :attr:`EXTRADELAY`                    :struct:`Scalar` (seconds)      Add artificial delay to all communication with this GUI (good for testing before you get into deep space)
    :attr:`SKIN`                          :struct:`Skin`                  The skin defining the default style of widgets in this GUI.
    :attr:`TOOLTIP`                       :struct:`String`                Current value of hovertext
    :meth:`SHOW`                          none                            Call to make the gui appear
    :meth:`HIDE`                          none                            Call to make the gui disappear
    ===================================== =============================== =============

    .. attribute:: X

        :type: :struct:`Scalar`
        :access: Get/Set

        This is the X position of upper-left corner of window, in pixels.

        You can alter this value to move the window.

        If you use a negative value for the coordinate, then the coordiante will be
        measured in the reverse direction, from the right edge of the screen.  (i.e.
        setting it to -200 means 200 pixels away from the right edge of the screen.)

    .. attribute:: Y

        :type: :struct:`Scalar`
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

        :type: :struct:`Scalar`
        :access: Get/Set

        This is the number of extra seconds of delay to add to
        the GUI for testing purposes.

        If Remote Tech is installed, the GUI system :ref:`obeys the signal delay<gui_delay>`
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

        A :struct:`Skin` is a collection of :struct:`Style` objects to be
        used by different types of widgets within the GUI window.  With this
        suffix you can assign a different Skin to the window, which will then
        be used by default by all the widgets of the appropriate type
        inside the window.

    .. attribute:: TOOLTIP

        :type: :struct:`String`
        :access: Get/Set

        If the mouse pointer is hovering over a GUI label widget inside this
        window somewhere that has its :attr:`Label:TOOLTIP` property set, then
        this string will contain that TOOLTIP property copied into it (otherwise
        it will be an empty string, ``""``).  This is the value that will be
        displayed inside this window's :struct:`TipDisplay` widget if you add
        a TipDisplay widget to the window.  Using this value, you can come up
        with your own alternate ways to display tooltips to the user, if you
        like, instead of using the baked-in :struct:`TipDisplay` technique.

    .. method:: SHOW

        Synopsis::

            set g to gui(200).
            // .. call G:addbutton, G:addslider, etc etc here
            g:show().

        Call this suffix to make the GUI appear.  (Note this is really just
        :meth:`Widget:Show` but it's mentioned again here because it's
        vital when making a GUI to know that it won't show up if you don't
        call this.)

    .. method:: HIDE

        Synopsis::

            set g to gui(200).
            // .. call G:addbutton, G:addslider, etc etc here
            g:show().
            wait until done. // whatever you decide "done" is.
            g:hide().

        Call this suffix to make the GUI disappear.  (Note this is really just
        :meth:`Widget:Show` but it's mentioned again here.)
