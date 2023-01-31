.. _gui_spacing:

Spacing
-------

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
    :attr:`AMOUNT`                        :struct:`Scalar`                The amount of space, or -1 for flexible spacing.
    ===================================== =============================== =============

    .. attribute:: AMOUNT

        :type: :struct:`Scalar`
        :access: Get/Set

        The number of pixels for this spacing to take up.  Whether this
        is horizontal or vertial space depends on whether this is being
        added to a horizontal-layout box or a vertical-layout box.
