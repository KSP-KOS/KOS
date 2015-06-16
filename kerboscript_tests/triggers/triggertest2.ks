// Testing triggers - this test makes
// a deliberately super long bunch of triggers
// that take thousands of instructions each.

local outerCount is 0.

when outerCount > 300 then {
  local i is 0.
  until i > 100 {
    set i to i + 1.
    if mod(i,50) = 0 {
      print "  med busy wait hit number " + i.
    }
  }
  preserve. // med busy wait goes forever.
}

when outerCount > 500 then {
  print "Long length trigger initiated".
  local i is 0.
  until i > 500 {
    set i to i + 1.
    if mod(i,100) = 0 {
      print "  long busy wait hit number " + i.
    }
  }
  // long wait has no preserve - this is a fire once test.
}

print "Mainline code is also doing pointless busy waiting forever.".
until outerCount > 1000 {
  set outerCount to outerCount + 1.
  if mod(outerCount,100) = 0 {
    print "main busy wait thinking about number " + outerCount.
  }
}
