.. _gui_slider:

Slider
------

.. structure:: Slider

        ``Slider`` widgets are created via :meth:`BOX:ADDHSLIDER`
        and :meth:`BOX:ADDVSLIDER`.

        A ``Slider`` is a widget that holds the value of a :struct:`Scalar`
        that the user can adjust by moving a sliding marker along a line.

        It is suited for real-number varying values, but not well suited
        for integer values.


    ===================================== ========================================= =============
    Suffix                                Type                                      Description
    ===================================== ========================================= =============
                   Every suffix of :struct:`WIDGET`
    ---------------------------------------------------------------------------------------------
    :attr:`VALUE`                         :struct:`Scalar`                          The current value. Initially set to :attr:`MIN`.
    :attr:`ONCHANGE`                      :struct:`KOSDelegate` (:struct:`Scalar`)  Your function called whenever the :attr:`VALUE` changes.
    :attr:`MIN`                           :struct:`Scalar`                          The minimum value (leftmost on horizontal slider).
    :attr:`MAX`                           :struct:`Scalar`                          The maximum value (bottom on vertical slider).
    ===================================== ========================================= =============

    .. attribute:: VALUE

        :type: :struct:`Scalar`
        :access: Get/Set

        The current value of the slider.

    .. attribute:: ONCHANGE

        :type: :struct:`KOSDelegate`
        :access: Get/Set

        This :struct:`KOSDelegate` takes one parmaeter, the value, and returns nothing.

        This allows you to set a callback delegate to be called
        whenever the user has moved the slider to a new
        value.  Note that as the user moves the slider
        to a new position, this will get called several
        times along the way, giving sevearl intermediate
        values on the way to the final value the user leaves
        the slider at.

        Example::

            set mySlider:ONCHANGE to whenMySliderChanges@.

            function whenMySliderChanges {
              parameter newValue.

              print "Value is " + 
                     round(100*(newValue-mySlider:min)/(mySlider:max-mySlider:min)) +
                     "percent of the way between min and max.".
            }

        This suffix is intended to be used with the 
        :ref:`callback technique <gui_callback_technique>` of widget
        interaction.

    .. attribute:: MIN

        :type: :struct:`Scalar`
        :access: Get/Set

        The "left" (for horizontal sliders) or "top" (for vertical sliders)
        endpoint value of the slider.
        
        Note that despite the name, :attr:`MIN` doesn't have to be smaller
        than :attr:`MAX`.  If :attr:`MIN` is larger than :attr:`MAX`, then
        that causes the slider to swap their meaning, and reverse its direction.
        (i.e. where numbers normally get larger when you slide to the right,
        inverting MIN and MAX causes the numbers to get larger when you
        slide to the left.)

    .. attribute:: MAX

        :type: :struct:`Scalar`
        :access: Get/Set

        The "right" (for horizontal sliders) or "bottom" (for vertical sliders)
        endpoint value of the slider.
        
        Note that despite the name, :attr:`MIN` doesn't have to be smaller
        than :attr:`MAX`.  If :attr:`MIN` is larger than :attr:`MAX`, then
        that causes the slider to swapr their meaning, and reverse its direction.
        (i.e. where numbers normally get larger when you slide to the right,
        inverting MIN and MAX causes the numbers to get larger when you
        slide to the left.)
