// Outermost level of testing the run-once system:

print "Testing run with a mix of 'once' and not 'once':".

print "running lib1 and lib2 first time.".
run once testrunonce_lib1. // should have no effect since its the first time.
run testrunonce_lib2.

print "Testing that the funcs exist:".
print "lib1 function returns: " + lib1().
print "lib2 function returns: " + lib2().

print "running lib1 and lib2 second time with 'once' - should NOT print.".
run once testrunonce_lib1.
run once testrunonce_lib2("dummy").

print "Testing that the funcs still exist:".
print "lib1 function returns: " + lib1().
print "lib2 function returns: " + lib2().

print "running lib1 and lib2 second time without 'once' - should print.".
run testrunonce_lib1.
run testrunonce_lib2("dummy").

print "Testing that the funcs still exist:".
print "lib1 function returns: " + lib1().
print "lib2 function returns: " + lib2().

print "running lib1 and lib2 second time with 'once' - should NOT print.".
run once testrunonce_lib1.
run once testrunonce_lib2("dummy").

print "Testing that the funcs still exist:".
print "lib1 function returns: " + lib1().
print "lib2 function returns: " + lib2().
