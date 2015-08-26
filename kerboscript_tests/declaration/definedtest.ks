// Tests the defined operator.
print "Because this test uses globals, it is vital that it only be run".
print "after a TOGGLE POWER has guaranteed the globals are cleared".

print "Testing the defined operator.".
print " 1: Should say False: " + (defined var1).
set var1 to 0.
print " 2: Should say True: " + (defined var1).
print " 3: Should say False: " + (defined var2).
local var2 is 0.
print " 4: Should say True: " + (defined var2).

// test with nest:
function nesttest {
  local nesttest_1 is 0.

  function nesttest_inner {
    local nesttest_inner_1 is 0.

    print " 9: Should say True: " + defined nesttest_1.
    print "10: Should say True: " + defined nesttest_inner_1.
  }

  print " 7: Should say True: " + defined nesttest_1.
  print " 8: Should say False: " + defined nesttest_inner_1.

  nesttest_inner().
}

// Can't see these from outside the function:
print " 5: Should say False: " + defined nesttest_1.
print " 6: Should say False: " + defined nesttest_inner_1.

nesttest().

// Test other kinds of braces besides functions:
if true {
  local truebrace_1 is 0.

  print "11: Should say True: " + defined truebrace_1.

  until truebrace_1 > 0 {
    local loopbrace_1 is 0.

    print "12: Should say True: " + defined loopbrace_1.
  
    set truebrace_1 to 1. // make it get out of the until loop.
  }

  print "13: Should say False: " + defined loopbrace_1.
}

// Can't see from outside the if braces:
print "14: Should say False: " + defined truebrace_1.
print "15: Should say False: " + defined loopbrace_1.
