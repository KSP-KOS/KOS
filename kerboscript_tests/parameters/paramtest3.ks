print " ".
print "Defaultable params calling functions".
print "from same program they're declared in.".
print " ".

function needsZeroOfOne {
  parameter funcParam1 is "def1".

  print "inside function, the function is seeing param:".
  print "   " + funcParam1.
}

function needsTwoOfFour {
  parameter funcParam1, funcParam2, funcParam3 is "def3", funcParam4 is "def4".

  print "inside function, the function is seeing params:".
  print "   " + funcParam1 + ", " + funcParam2 + ", " + funcParam3 + ", " + funcParam4.
}

print "Calling needsZeroOfOne('a')". // can't embed quotes yet.
needsZeroOfOne("a").
print "Calling needsZeroOfOne()".
needsZeroOfOne().

print "Calling needsTwoOfFour('a','b','c','d')".
needsTwoOfFour("a","b","c","d").
print "Calling needsTwoOfFour('a','b','c')".
needsTwoOfFour("a","b","c").
print "Calling needsTwoOfFour('a','b')".
needsTwoOfFour("a","b").

