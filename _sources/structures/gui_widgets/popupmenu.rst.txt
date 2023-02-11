.. _gui_popupmenu:

PopupMenu
---------

.. structure:: PopupMenu

    ``PopupMenu`` objects are created by calling :meth:`BOX:ADDPOPUPMENU`.

    A ``PopupMenu`` is a special kind of button for choosing from a list of things.
    It looks like a button who's face displays the currently selected thing.  When a user
    clicks on the button, it pops up a list of displayed strings to choose
    from, and when one is selected the popup goes away and the new choice is
    displayed on the button.

    The menu displays the string values in the OPTIONS property. If OPTIONS contains items that are not strings,
    then by default their :attr:`TOSTRING <Structure:TOSTRING>` suffixes will be used to display them as strings.
    You can change this default behaviour by setting the popupmenu's :attr:`OPTIONSUFFIX`.

    Example::

	local popup is gui:addpopupmenu().

        // Make the popup display the Body:NAME's instead of the Body:TOSTRING's:
	set popup:OPTIONSUFFIX to "NAME".

	list bodies in bodies.
	for planet in bodies {
		if planet:hasbody and planet:body = Sun {
			popup:addoption(planet).
		}
	}
	set popup:value to body.


    ===================================== ========================================= =============
    Suffix                                Type                                      Description
    ===================================== ========================================= =============
                   Every suffix of :struct:`BUTTON`
    ---------------------------------------------------------------------------------------------
    :attr:`OPTIONS`                       :struct:`List`                            List of options to display.
    :attr:`OPTIONSUFFIX`                  :struct:`String`                          Name of the suffix used for display names. Default = TOSTRING.
    :meth:`ADDOPTION(value)`                                                        Add a value to the end of the list of options.
    :attr:`VALUE`                         Any                                       Returns the current selected value.
    :attr:`INDEX`                         :struct:`Scalar`                          Returns the index of the current selected value.
    :attr:`CHANGED`                       :struct:`Boolean`                         Has the user chosen something?
    :attr:`ONCHANGE`                      :struct:`KOSDelegate` (:struct:`String`)  Your function called whenever the :attr:`CHANGED` state changes.
    :meth:`CLEAR`                                                                   Removes all options.
    :attr:`MAXVISIBLE`                    :struct:`Scalar` (integer)                How many choices to show at once in the list (if more exist, it makes it scrollable).
    ===================================== ========================================= =============

    .. attribute:: OPTIONS

        :type: :struct:`List` (of any Structure)
        :access: Get/Set

        This is the list of options the user has to choose from.  They don't need
        to be Strings, but they must be capable of having a string extracted from
        them for display on the list, by use of the :attr"`OPTIONSSUFFIX` suffix.

    .. attribute:: OPTIONSUFFIX

        :type: :struct:`String`
        :access: Get/Set

        This decides how you get strings from the list of items in :attr:`OPTIONS`.
        The usual way to use a ``PopupMenu`` would be to have it select from a list
        of strings.  But you can use any other kind of object you want in the
        :attr:`OPTIONS` list, provided all of them share a common suffix name
        that builds a string from them.  The default value for this is
        ``"TOSTRING:``, which is a suffix that all things in kOS have.  If you
        wish to use something other than :attr:`Structure:TOSTRING`, you
        can set this to that suffix's string name.

        This page begins with a good example of using this.  See above.

    .. method:: ADDOPTION(value)

        :parameter value: - any kind of kOS type, provided it has the suffix mentioned in :attr:`OPTIONSSUFFIX` on it.
        :type value: :struct:`Structure`
        :access: Get/Set

        This appends another choice to the :attr:`OPTIONS` list.

    .. attribute:: VALUE

        :type: :struct:`Structure`
        :access: Get/Set

        Returns the value currently chosen from the list.
        If no selection has been made, it will return an empty :struct:`String` (``""``).

        If you set it, you are choosing which item is selected from the list.  If you
        set this to something that wasn't in the list, the attempt to set it will be
        rejected and instead the choice will become de-selected.

    .. attribute:: INDEX

        :type: :struct:`Scalar`
        :access: Get/Set

        Returns the number index into the :attr:`OPTIONS` list that goes with the
        current choice.  If this is set to -1, that means nothing has been
        selected.

        Setting this value causes the selected choice to change.  Setting it
        to -1 will de-select the choice.

    .. attribute:: CHANGED

        :type: :struct:`Boolean`
        :access: Get/Set

        Has the choice been changed since the last time this was checked?

        Note that reading this has a side effect.  When you read this value,
        you cause it to become false if it had been true.  (The system
        assumes that "last time this was checked" means "now" after you've
        read the value of this suffix.)

        This is intended to be used with the
        :ref:`polling technique <gui_polling_technique>` of reading the widget.
        You can query this field until it says it's true, at which point you
        know to go have a look at the current value to see what it is.

    .. attribute:: ONCHANGE

        :type: :struct:`KOSDelegate`
        :access: Get/Set

        This is a :struct:`KOSDelegate` that expects one parameter, the new value, and returns nothing.

        Sets a callback hook you want called when a new selection has
        been made.  This is for use with the
        :ref:`callback technique <gui_callback_technique>` of reading the widget.

        The function you specify must be designed to take one parameter,
        which is the new value (same as reading the :attr:`VALUE` suffix) of
        this widget, and return nothing.

        Example::

            set myPopupMenu:ONCHANGE to { parameter choice. print "You have selected: " + choice:TOSTRING. }.

    .. method:: CLEAR

        :return: (nothing)

        Calling this causes the ``PopupMenu`` to wipe out all the contents of its :attr:`OPTIONS`
        list.

    .. attribute:: MAXVISIBLE

        :type: :struct:`Scalar`
        :access: Get/Set

        (Default value is 15).

        This sets the largest number of choices (roughly) the layout
        system will be willing to grow the popup window to support
        before it resorts to using a scrollbar to show more choices,
        instead of letting the window get any bigger.  This value is
        only a rough hint.

        If this is set too large, it can become possible to make
        the popup menu so large it won't fit on the screen, if you
        give it a lot of items in the options list.
