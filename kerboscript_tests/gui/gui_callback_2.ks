clearscreen.
PRINT "Testing radio buttons in Two Styles".
print " ".

{
  LOCAL G is GUI(500).
  LOCAL done is false.

  // Set 1 is a radio button box for radio station.
  // The buttons themselves are just circles, so
  // the labels have to be put separately as additional
  // widget objects.
  // ----------------------------------------------
  
  LOCAL SET1 is G:ADDHBOX.
  set SET1:STYLE:TEXTCOLOR to yellow.

  local b1_label is set1:addlabel("Choose"+char(10)+"radio"+char(10)+"station").

  global b1_jazz is set1:addradiobutton("Jazz",false).
  set b1_jazz:ontoggle to {parameter val. print "Jazz is " + val.}.
  set1:addlabel("Jazz").

  global b1_blues is set1:addradiobutton("Blues",false).
  set b1_blues:ontoggle to {parameter val. print "Blues is " + val.}.
  set1:addlabel("Blues").

  global b1_funk is set1:addradiobutton("Funk",false).
  set b1_funk:ontoggle to {parameter val. print "Funk is " + val.}.
  set1:addlabel("Funk").

  global b1_rock is set1:addradiobutton("Rock",false).
  set b1_rock:ontoggle to {parameter val. print "Rock is " + val.}.
  set1:addlabel("Rock").

  set SET1:ONRADIOCHANGE to my_set1_radio_monitor@.

  
  // Set 2 is another radio button box for species.
  // But this one paints the labels on the buttons,
  // by painting the button style as normal instead
  // of as toggle style:
  // ----------------------------------------------

  LOCAL SET2 is G:ADDHBOX.
  set SET2:STYLE:TEXTCOLOR to green.

  local b2_label is set2:addlabel("Choose Species").
  local b2_1 is set2:addradiobutton("Human", false).
  set b2_1:style:width to 80.
  set b2_1:style to G:Skin:Button. // toggle looks like buttons do.
  set b2_1:ontoggle to {parameter val. print "Human is " + val.}.

  local b2_2 is set2:addradiobutton("Kerbal", false).
  set b2_2:style:width to 80.
  set b2_2:style to G:Skin:Button. // toggle looks like buttons do.
  set b2_2:ontoggle to {parameter val. print "Kerbal is " + val.}.

  local b2_3 is set2:addradiobutton("Other", false).
  set b2_3:style:width to 80.
  set b2_3:style to G:Skin:Button. // toggle looks like buttons do.
  set b2_3:ontoggle to {parameter val. print "Other is " + val.}.

  local q_button is g:addbutton("Quit").
  set q_button:onclick to {
    print "q_button clicked.".
    G:HIDE().
    set done to true.
  }.

  set SET2:ONRADIOCHANGE to my_set2_radio_monitor@.

  G:SHOW().

  // Check the polling technique to be sure it's rignt:
  until done {
    print "(repeating polling every 5 seconds.)".
    print "Station is <" + set1:radiovalue + ">".
    print "Species is <" + set2:radiovalue + ">".
    print " ".
    wait 5.
  }
  
  print "Done with polling loop.".
}

function my_set1_radio_monitor {
  parameter the_value.

  print "Radio station selected is now: " + the_value.
}

function my_set2_radio_monitor {
  parameter the_value.

  print "Species selected is now: " + the_value.
}

