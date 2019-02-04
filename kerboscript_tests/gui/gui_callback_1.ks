PARAMETER taking_press is true.

PRINT "Testing Simple dumb buttons".
PRINT "Call with a parameter of False to see".
PRINT "How Button 1 would behave without takepress.".

{
  LOCAL G is GUI(200).
  LOCAL done is false.

  local B1 is g:addbutton("button 1 (polling, not a toggle)").

  local B2 is g:addbutton("button 2 (polling, a toggle)").
  set B2:toggle to true.

  local B3 is g:addbutton("button 3 (callback, not a toggle)").
  set B3:ontoggle to {parameter newval. print "Button 3 onchange(" + newval + ")".}.
  set B3:onclick to {print "Button 3 onclick.".}.

  local B4 is g:addbutton("button 4 (callback, a toggle)").
  set B4:toggle to true.
  set B4:ontoggle to {parameter newval. print "Button 4 onchange(" + newval + ")".}.
  set B4:onclick to {print "Button 4 onclick".}.

  local q_button is g:addbutton("Quit").
  set q_button:onclick to {print "q_button clicked.". set done to true.}.

  G:SHOW().

  // Check the polling technique buttons in this loop:
  local b1_prev is false.
  local b2_prev is false.
  until done {
    if b1_prev <> b1:pressed {
      print "Button 1 changed state: is now " + b1:pressed.
      set b1_prev to b1:pressed.
    }
    if taking_press {
      if b1:takepress {
        print "Button 1 takepress happened.".
      }
    }
    if b2_prev <> b2:pressed {
      print "Button 2 changed state: is now " + b2:pressed.
      set b2_prev to b2:pressed.
    }
    wait 0.
  }
  
  print "Done with polling loop.".

  G:HIDE().
}
