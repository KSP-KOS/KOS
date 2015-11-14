// Library to be called by testrunonce:

// Lib1 runs Lib2 even though lib2 is also called by the main prog too:
run once testrunonce_lib2("dummy").

function lib1 {
  return "lib1 value".
}

print "MAINLINE CODE of lib1 is running.".
