.. _gui_scrollbox:

Scrollbox
---------

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
                   Every suffix of :struct:`BOX`.
    -----------------------------------------------------------------------------------
    :attr:`HALWAYS`                       :struct:`Boolean`               Always show the horizontal scrollbar.
    :attr:`VALWAYS`                       :struct:`Boolean`               Always show the vertical scrollbar.
    :attr:`POSITION`                      :struct:`Vector`                The position of the scrolled content (Z is ignored).
    ===================================== =============================== =============

    .. attribute:: HALWAYS

        :type: :struct:`Boolean`
        :access: Get/Set

        Set to true if you want the horizontal scrollbar to always appear for the
        box regardless of whether the contents are large enough to require it.

    .. attribute:: VALWAYS

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
