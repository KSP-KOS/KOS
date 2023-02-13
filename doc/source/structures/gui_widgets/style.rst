.. _gui_style:

Style
-----

.. structure:: Style

    This object represents the style of a widget. Styles can be either changed directly
    on a :struct:`Widget`, or changed on the GUI:SKIN so as to affect all subsequently
    created widgets of a particular type inside that GUI.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
    :attr:`HSTRETCH`                      :struct:`Boolean`               Should the widget stretch horizontally? (default depends on widget subclass)
    :attr:`VSTRETCH`                      :struct:`Boolean`               Should the widget stretch vertically?
    :attr:`WIDTH`                         :struct:`Scalar` (pixels)       Fixed width (or 0 if flexible).
    :attr:`HEIGHT`                        :struct:`Scalar` (pixels)       Fixed height (or 0 if flexible).
    :attr:`MARGIN`                        :struct:`StyleRectOffset`       Spacing between this and other widgets.
    :attr:`PADDING`                       :struct:`StyleRectOffset`       Spacing between the outside of the widget and its contents (text, etc.).
    :attr:`BORDER`                        :struct:`StyleRectOffset`       Size of the edges in the 9-slice image for BG images in NORMAL, HOVER, etc.
    :attr:`OVERFLOW`                      :struct:`StyleRectOffset`       Extra space added to the area of the background image. Allows the background to go beyond the widget's rectangle.
    :attr:`ALIGN`                         :struct:`String`                One of "CENTER", "LEFT", or "RIGHT". See note below.
    :attr:`FONT`                          :struct:`String`                The name of the font of the text on the content or "" if the default.
    :attr:`FONTSIZE`                      :struct:`Scalar`                The size of the text on the content.
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
    :attr:`BG`                            :struct:`String`                The same as NORMAL:BG. Name of a "9-slice" image file.
    :attr:`TEXTCOLOR`                     :ref:`Color <colors>`           The same as NORMAL:TEXTCOLOR. The color of the text on the label.
    :attr:`WORDWRAP`                      :struct:`Boolean`               Can labels be broken into multiple lines on word boundaries?
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

        :type: :struct:`Scalar`
        :access: Get/Set

        (pixels)       Fixed width (or 0 if flexible).

    .. attribute:: HEIGHT

        :type: :struct:`Scalar`
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

        :type: :struct:`String`
        :access: Get/Set

        One of "CENTER", "LEFT", or "RIGHT".

    .. note::

        The ALIGN attribute will not do anything useful unless either HSTRETCH is set to true or a fixed WIDTH is set,
        since otherwise it will be exactly the right size to fit the content of the widget with no alignment within that space being necessary.

        It is currently only relevant for the widgets that have scalar content (Label and subclasses).


    .. attribute:: FONT

        :type: :struct:`String`
        :access: Get/Set

        The name of the font of the text on the content or "" if the default.
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

    .. attribute:: FONTSIZE

        :type: :struct:`Scalar`
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

        :type: :struct:`String`
        :access: Get/Set

        The same as NORMAL:BG. Name of a "9-slice" image file.

    .. attribute:: TEXTCOLOR
    
        :type: :strucT:`color`
        
        The same as NORMAL:TEXTCOLOR. The color of the text on the label.

    .. attribute:: WORDWRAP

        :type: :struct:`Boolean`
        :access: Get/Set

        Can labels be broken into multiple lines on word boundaries?
