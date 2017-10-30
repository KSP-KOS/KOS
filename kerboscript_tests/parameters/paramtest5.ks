print " ".
print "Defaultable params calling nested functions".
print " ".

function needsTwoOfFour {
  parameter funcParam1, funcParam2, funcParam3 is "def3".

  // Tricky and ugly example - inner function is nested BEFORE the end of
  // the parameters  - to test the nested tracking of params:

  function needsZeroOfOne {
    parameter funcParam1 is "inner def1".

    print "      inside needsZeroOfOne".
    print "        the function is seeing param:".
    print "        " + funcParam1.
  }

  parameter funcParam4 is "def4".

  print "  inside needsTwoOfFour.".
  print "    the function is seeing these params:".
  print "    " + funcParam1 + ", " + funcParam2 + ", " + funcParam3 + ", " + funcParam4.
  needsZeroOfOne("a").
  needsZeroOfOne().

}

print "Calling needsTwoOfFour('a','b','c','d')".
needsTwoOfFour("a","b","c","d").
print "Calling needsTwoOfFour('a','b','c')".
needsTwoOfFour("a","b","c").
print "Calling needsTwoOfFour('a','b')".
needsTwoOfFour("a","b").

