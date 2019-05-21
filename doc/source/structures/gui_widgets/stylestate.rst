.. _gui_stylestate:

StyleState
----------

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
        specifying a ".png" extension is optional.  Note, that this ignores
        the normal rules about finding the archive within comms range.
        You are allowed to access these files even when not
        in range of the archive, because they represent the visual look
        of your ship's control panels, not actual files sent on the ship.

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

        If set to ``""``, these background images will default to the
        corresponding normal image and if that is also ``""``, it will
        default to the normal ``BG`` image, and if that is also ``""``,
        then it will default to completely transparent.

    .. attribute:: TEXTCOLOR

        :type: :struct:`Color`
        :access: Get/Set

        The color of foreground text within this widget when it is in this state.


