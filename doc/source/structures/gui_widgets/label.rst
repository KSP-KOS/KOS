.. _gui_label:

Label
-----

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

    .. attribute:: TOOLTIP

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
    


