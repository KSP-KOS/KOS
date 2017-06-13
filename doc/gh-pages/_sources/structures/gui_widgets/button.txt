.. _gui_button:

Button
------

.. structure:: Button

    A ``Button`` is a widget that can have script activity occur when the user
    presses it.

    ``Button`` widgets are created inside Box objects via one of these three methods:
    
    - :meth:`Box:ADDBUTTON` - for a button that pops back out again on its own after being clicked.
    - :meth:`Box:ADDCHECKBOX` - for a toggle button that stays on when 
      clicked and doesn't turn off until clicked again.
    - :meth:`Box:ADDRADIOBUTTON` - A kind of checkbox that forms part of
      a set of checkboxes that only allow one of themselves to be on at a time.

    The differences between how these types of button behave come from how
    they will have their default :attr:`TOGGLE` and :attr:`EXCLUSIVE`
    suffixes set when they are created.

    Buttons are a special case of :struct:`Label`, and can use all the features
    of :struct:`Label` to define how their text looks.

    ===================================== ========================================== =============
    Suffix                                Type                                       Description
    ===================================== ========================================== =============
                   Every suffix of :struct:`LABEL`
    ----------------------------------------------------------------------------------------------
    :attr:`PRESSED`                       :struct:`Boolean`                          Is the button currently down?
    :attr:`TAKEPRESS`                     :struct:`Boolean`                          Return the PRESSED value AND release the button if it's down.
    :attr:`TOGGLE`                        :struct:`Boolean`                          Is this button into a toggle-style button?
    :attr:`EXCLUSIVE`                     :struct:`Boolean`                          Does turning this button on cause other buttons to turn off?
    :attr:`ONCLICK`                       :struct:`KOSDelegate` (no args)            Your function called whenever the button gets clicked.
    :attr:`ONTOGGLE`                      :struct:`KOSDelegate` (:struct:`Boolean`)  Your function called whenever the button's PRESSED state changes.
    ===================================== ========================================== =============

    .. attribute:: PRESSED

        :type: :struct:`Boolean`
        :access: Get/Set

        You can read this value to see if the button is currently on (true)
        or off (false).  You can set this value to cause the button to become
        on or off.

    .. attribute:: TAKEPRESS

        :type: :struct:`Boolean`
        :access: Get-only

        You can read this value to see if the button is currently on (true)
        or off (false), however reading this value has a side-effect.
        When you read this value, if it said the button was on (pressed in),
        then reading it will cause the button to become off (popped out).

        This is useful only for normal buttons (buttons that have :attr:`TOGGLE`
        set to false).  It allows you to use the
        :ref:`polling technique <gui_polling_technique>` to repeatedly check to
        see if the button is on, and as soon as your script notices that it's on,
        it will pop it back out again so the user sees the proper visual feedback.

    .. attribute:: TOGGLE

        :type: :struct:`Boolean`
        :access: Get/Set

        This suffix determines whether this button has toggle behavior or button
        behaviour.  (Whether it stays pressed in until pressed a second time).
        By default, :meth`BOX:ADDBUTTON` will create a button with ``TOGGLE`` set
        to false, while :meth:`BOX:ADDCHECKBOX` and :meth:`BOX:ADDRADIOBUTTON` will
        create buttons which have their ``TOGGLE`` suffixes set to true.

        **Behaviour when TOGGLE is false (the default):**

        The conditions under which a button will automatically release itself when :attr:`TOGGLE` is
        set to `False` are:

        - When the script calls the :attr:`TAKEPRESS` suffix method.  When this is done, the
          button will become false even if it was was previously true.
        - If the script defines an :attr:`ONCLICK` user delegate.
          (Then when the :attr:`PRESSED` value becomes true, kOS will immediately set it
          back to false (too fast for the kerboscript to see it) and instead call the
          ``ONCLICK`` callback delegate you gave it.)

        **Behaviour when TOGGLE is true:**

        If TOGGLE is set to True, then the button will **not** automatically release after it is
        read by the script.  Instead it will need to be clicked by the user a second time to make
        it pop back out.  In this mode, the button's :attr:`PRESSED` value will never automatically
        reset to false on its own.

        If the Button is created by :meth:`Button:ADDCHECKBOX`, or by
        :meth:`Button:ADDRADIOBUTTON`, it will have a different visual
        style (the style called "toggle") and it will start already in TOGGLE mode.

    .. attribute:: EXCLUSIVE

        :type: :struct:`Boolean`
        :access: Get/Set

        If the Button is created by :meth:`Button:ADDRADIOBUTTON`, it will have
        its ``EXCLUSIVE`` suffix set to true by default.

        If ``EXCLUSIVE`` is set to True, when the button is clicked (or changed programmatically),
        other buttons with the same parent :struct:`Box` will be set to False (regardless of
        if they are EXCLUSIVE).

    .. attribute:: ONCLICK

        :type: :struct:`KOSDelegate`
        :access: Get/Set

        This is a :struct:`KOSDelegate` that takes no parameters and returns nothing.

        ``ONCLICK`` is what is known as a "callback hook".  This suffix allows
        you to use the :ref:`callback technique <gui_callback_technique>` of widget
        interaction.

        You can assign ``ONCLICK`` to a :struct:`KOSDelegate` of one of your
        functions (named or anonymous) and from then on kOS will call that
        function whenever the button becomes clicked by the user.

        The :attr:`ONCLICK` suffix is intended to be used for non-toggle buttons.

        Example::

            set mybutton:ONCLICK to { print "Do something here.". }.

        :attr:`ONCLICK` is called with no parameters.  To use it, your function must be
        written to expect no parameters.

    .. attribute:: ONTOGGLE

        :type: :struct:`KOSDelegate`
        :access: Get/Set

        This is a :struct:`KOSDelegate` taking one parameter (new boolean state) and returning nothing

        ``ONTOGGLE`` is what is known as a "callback hook".  This suffix allows
        you to use the :ref:`callback technique <gui_callback_technique>` of widget
        interaction.

        The ``ONTOGGLE`` delegate you assign will get called whenver kOS notices
        that this button has changed from false to true or from true to false.

        To use ``ONTOGGLE``, your function must be written to expect a single boolean parameter,
        which is the new state the button has just be changed to.

        Example::

            set mybutton:ONTOGGLE to { parameter val. print "Button value just became " + val. }.

        ``ONTOGGLE`` is really only useful with buttons where :attr:`TOGGLE` is true.

Example
-------

Here is a longer example of buttons using the button callback hooks::

    LOCAL doneYet is FALSE.
    LOCAL g IS GUI(200).

    // b1 is a normal button that auto-releases itself:
    // Note that the callback hook, myButtonDetector, is
    // a named function found elsewhere in this same program:
    LOCAL b1 IS g:ADDBUTTON("button 1").
    SET b1:ONCLICK TO myButtonDetector@.

    // b2 is also a normal button that auto-releases itself,
    // but this time we'll use an anonymous callback hook for it:
    LOCAL b2 IS g:ADDBUTTON("button 2").
    SET b2:ONCLICK TO { print "Button Two got pressed". }

    // b3 is a toggle button.
    // We'll use it to demonstrate how ONTOGGLE callback hooks look:
    LOCAL b3 IS g:ADDBUTTON("button 3 (toggles)").
    set b3:style to g:skin:button.
    SET b3:TOGGLE TO TRUE.
    SET b3:ONTOGGLE TO myToggleDetector@.

    // b4 is the exit button.  For this we'll use another
    // anonymous function that just sets a boolean variable
    // to signal the end of the program:
    LOCAL b4 IS g:ADDBUTTON("EXIT DEMO").
    SET b4:ONCLICK TO { set doneYet to true. }

    g:show(). // Start showing the window.

    wait until doneYet. // program will stay here until exit clicked.

    g:hide(). // Finish the demo and close the window.

    //END.

    function myButtonDetector {
      print "Button One got clicked.".
    }
    function myToggleDetector {
      parameter newState.
      print "Button Three has just become " + newState.
    }


