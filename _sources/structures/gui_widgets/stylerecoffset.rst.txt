.. _gui_stylerectoffset:

StyleRectOffset
---------------

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


