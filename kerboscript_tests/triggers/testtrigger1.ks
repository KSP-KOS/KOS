print "Testing that an `on` trigger works with a complex expression.".

print "Trigger should fire only when x changes from even to odd or visa versa".

local x is 1.
on mod(x,2) {
  if mod(x,2)=0 {
    print "    TRIGGERED: x just became even.".
  } else {
    print "    TRIGGERED: x just became odd.".
  }

  preserve.
}

for num in LIST(1, 3, 5, 7, 11, 9, 10, 8, 2, 6, 19, 13, 15, 18, 20) {
  print "Changing x.".
  print "   x was " + x.
  set x to num.
  print "   x is now " + x.
  wait 0.2.
}


