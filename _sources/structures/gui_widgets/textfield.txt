.. _gui_textfield:

TextField
---------

.. structure:: TextField

    ``TextField`` objects are created via :meth:`BOX:ADDTEXTFIELD`.

    A ``TextField`` is a special kind of :struct:`Label` that can be
    edited by the user.  Unlike a normal :struct:`Label`, a ``TextField``
    can only be textual (it can't be used for image files).

    A ``TextField`` has a default style that looks different from a
    passive :struct:`Label`.  In the default style, a ``TextField`` shows
    the area the user can click on and type into, using a recessed
    background.

    ===================================== ========================================= =============
    Suffix                                Type                                      Description
    ===================================== ========================================= =============
           Every suffix of :struct:`LABEL`.  Note you read :attr:`Label:TEXT` to see the TextField's current value.
    ---------------------------------------------------------------------------------------------
    :attr:`CHANGED`                       :struct:`Boolean`                         Has the text been edited?
    :attr:`ONCHANGE`                      :struct:`KOSDelegate` (:struct:`String`)  Your function called whenever the :attr:`CHANGED` state changes.
    :attr:`CONFIRMED`                     :struct:`Boolean`                         Has the user pressed Return in the field?
    :attr:`ONCONFIRM`                     :struct:`KOSDelegate` (:struct:`String`)  Your function called whenever the :attr:`CONFIRMED` state changes.
    ===================================== ========================================= =============

    .. attribute:: CHANGED

        :type: :struct:`Boolean`
        :access: Get/Set

        Tells you whether :attr:`Label:TEXT` has been edited at all
        since the last time you checked.  Note that any edit counts.  If a
        user is trying to type "123" into the ``TextField`` and has so far
        written "1" and has just pressed the "2", then this will be true.
        If they then press "4" this will be true again.  If they then press
        "backspace" because this was type, this will be true again.  If
        they then press "3" this will be true again.  Literally *every*
        edit to the text counts, even if the user has not finished using
        the textfield.

        As soon as you read this suffix and it returns true, it will
        be reset to false again until the next time an edit happens.

        This suffix is intended to be used with the 
        :ref:`polling technique <gui_polling_technique>` of widget
        interaction.

    .. attribute:: ONCHANGE

        :type: :struct:`KOSDelegate`
        :access: Get/Set

        This :struct:`KOSDelegate` expects one parameter, a :struct:`String`, and returns nothing.

        This allows you to set a callback delegate to be called
        whenever the value of :attr:`Label:TEXT` changes in any
        way, whether that's inserting a character or deleting a
        character.

        The :struct:`KOSDelegate` you use must be made to expect
        one parameter, the new string value, and return nothing.

        Example::

            set myTextField:ONCHANGE to {parameter str. print "Value is now: " + str.}.

        This suffix is intended to be used with the 
        :ref:`callback technique <gui_callback_technique>` of widget
        interaction.

    .. attribute:: CONFIRMED

        :type: :struct:`Boolean`
        :access: Get/Set

        Tells you whether the user is finished editing :attr:`Label:TEXT`
        since the last time you checked.  This does not become true merely
        because the user typed one character into the field or deleted
        one character (unlike :attr:`CHANGED`, which does).  This only
        becomes true when the user does one of the following things:

        - Presses ``Enter`` or ``Return`` on the field.
        - Leaves the field (clicks on another field, tabs out, etc).

        As soon as you read this suffix and it returns true, it will
        be reset to false again until the next time the user commits
        a change to this field.

        This suffix is intended to be used with the 
        :ref:`polling technique <gui_polling_technique>` of widget
        interaction.

    .. attribute:: ONCONFIRM

        :type: :struct:`KOSDelegate`
        :access: Get/Set

        This :struct:`KOSDelegate` expects one parameter, a :struct:`String`, and returns nothing.

        This allows you to set a callback delegate to be called
        whenever the user has finished editing :attr:`Label:TEXT`.
        Unlike :attr:`CHANGED`, this does not get called every
        time the user types a key into the field.  It only gets
        called when one of the following things happens reasons:

        - User presses ``Enter`` or ``Return`` on the field.
        - User leaves the field (clicks on another field, tabs out, etc).

        The :struct:`KOSDelegate` you use must be made to expect
        one parameter, the new string value, and return nothing.

        Example::

            set myTextField:ONCONFIRM to {parameter str. print "Value is now: " + str.}.

        This suffix is intended to be used with the 
        :ref:`callback technique <gui_callback_technique>` of widget
        interaction.


    .. note::

        The values of :attr:`CHANGED` and :attr:`CONFIRMED` reset to False as soon as their value is accessed.


