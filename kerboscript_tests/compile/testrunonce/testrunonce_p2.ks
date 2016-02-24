// Outermost level of testing the run-once system:

print "Testing run with 'once' to test new functionality.".

print "running lib1 and lib2 first time.".
run once testrunonce_lib1.
run once testrunonce_lib2("dummy").

print "Testing that the funcs exist:".
print "lib1 function returns: " + lib1().
print "lib2 function returns: " + lib2().

print "running lib1 and lib2 second time.".
print "Next line should NOT print 'MAINLINE CODE IS RUNNING.".
run once testrunonce_lib1.
run once testrunonce_lib2("dummy").

print "Testing that the funcs still exist:".
print "lib1 function returns: " + lib1().
print "lib2 function returns: " + lib2().



