.. _tip_display:

TipDisplay
----------

.. structure:: TipDisplay

    A ``TipDisplay`` widget is a special case kind of :struct:`Label` widget
    you can put inside Box objects via :meth:`BOX:ADDTIPDISPLAY`.

    In order to show tooltips in a kOS GUI, you need to place a TipDisplay
    Widget somewhere in your GUI window.  Tooltips will not just appear in
    a hovering little window next to the mouse pointer like you may be used
    to seeing in most GUI systems.  Instead to make them appear you need to
    define a location within the GUI window where you want them to show up.
    The ``TipDisplay`` is how you do that.

    It is generally good practice to only have one ``TipDisplay`` per GUI
    window.

    A good place to put a ``TipDisplay`` is either in a line by itself
    across the top of the window, or in a line by itself across the bottom
    of the window, although it's up to you where you want to put it.

    A ``TipDisplay`` is a :struct:`Label` in every way, using the same
    suffixes that :struct:`Label` has.  The only special thing it does
    is that it:

    - Has its own style, called "tipDisplay", which can get seperate style
      settings.
    - Has its :attr:`TEXT` value automatically populated by the Tooltips
      of the other widgets you hover over, within limits.  If no such hover
      text is avaiable, or the mouse isn't over a supported widget type,
      then the :attr:`TEXT` field will be an empty string (``""``).

    Please be aware that Unity3d's IMGUI system does not support Tooltips
    for all widget types.  Generally they exist for any widget you see
    here which is derived from :struct:`Label` like Labels and Buttons.
    But they don't exist for the other types of widget.  The best you
    can do is to have a tooltip on the label of the widget in those cases.

    Also, :struct:`TEXTFIELD` has a limitation to its tooltip type, which
    is explained on its page: :attr:`TEXTFIELD:TOOLTIP`.

    ===================================== =============================== =============
    Suffix                                Type                            Description
    ===================================== =============================== =============
		   Every suffix of :struct:`Label`
    -----------------------------------------------------------------------------------
    ===================================== =============================== =============

    Example - This small program draws a little GUI window in which
    the buttons have tooltips that appear at the bottom of the window
    when you hover the mouse over the buttons::

      local done is false.
      local g is gui(200).
      local tiptext is g:addtipdisplay().
   
      local buttonsBox is g:addhbox().

      local b1 is buttonsBox:addbutton("low").
      set b1:tooltip to "Makes low pitch note.".
      set b1:onclick to {getvoice(0):play(note(200,0.5)).}.
      
      local b2 is buttonsBox:addbutton("medium").
      set b2:tooltip to "Makes medium pitch note.".
      set b2:onclick to {getvoice(0):play(note(300,0.5)).}.
      
      local b3 is buttonsBox:addbutton("high").
      set b3:tooltip to "Makes high pitch note.".
      set b3:onclick to {getvoice(0):play(note(400,0.5)).}.

      local bClose is g:addbutton("Close").
      set bClose:tooltip to "Ends program.".
      set bClose:onclick to {set done to true.}.

      g:show().
      wait until done.
      g:dispose().
