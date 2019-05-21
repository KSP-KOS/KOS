.. _gui_skin:

Skin
----

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

    :meth:`ADD(name, style)`               :struct:`Style`             Adds a new style.
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
        If you want to see the list of available font names, you can do
        so with :ref:`List Fonts. <list_fonts>`.  Please note that just
        because you see a font in that list on your computer,
        that doesn't always mean that same font will exist on
        someone else's computer.  KSP ships with a few fonts that it
        does universally put on all platform installs, but other
        fonts in that list might be installed locally on your computer
        only by other mods (like kOS itself, which loads all your
        monospaced fonts for optional use as the terminal font).
        Fonts that we know KSP itself tends to install are:
        Arial, CALIBRI, HEADINGFONT, calibri, calibrib, calibriz, calibril, and dotty

    .. attribute:: SELECTIONCOLOR

        :type: :ref:`Color <colors>`
        :access: Get/Set
        
        The background color of selected text (eg. TEXTFIELD).

    .. method:: ADD(name, style)

        :parameter name: :struct:`String`
        :parameter style: :struct:`Style` - a style to clone here.
        :return: :struct:`Style` - the copy of the style that was made.
        
        Adds a new style to the skin and names it.  The skin holds a list
        of styles by name which you can retrieve later.  Note, this makes
        a copy of the style you pass in, so changes you make to this new
        style afterward shouldn't affect the one you passed in, and visa versa.

    .. method:: HAS(name)

        :parameter name: :struct:`String`
        :return: :struct:`Style`
        
        Does the skin have the named style?

    .. method:: GET(name)

        :parameter name: :struct:`String`
        :return: :struct:`Style`
        
        Gets a style by name (including ADDed styles).


