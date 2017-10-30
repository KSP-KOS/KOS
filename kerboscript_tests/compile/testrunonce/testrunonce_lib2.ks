// Library to be called by testrunonce:

parameter dummy. // just to test passing args not getting stack misaligned.

// Lib2 runs Lib1 even though lib1 is also called by the main prog too:
run once testrunonce_lib1.

function lib2 {
  return "lib2 value".
}

print "MAINLINE CODE of lib2 is running.".
