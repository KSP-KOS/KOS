print " ".
print "Running Library paramtest4_lib".
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

