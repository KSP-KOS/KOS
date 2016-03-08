// Outermost level of testing the run-once system:

print "Testing run without 'once' to ensure backward compatibility.".

print "running lib1 and lib2 first time.".
run testrunonce_lib1.
run testrunonce_lib2("dummy arg").

print "Testing that the funcs exist:".
print "lib1 function returns: " + lib1().
print "lib2 function returns: " + lib2().

print "running lib1 and lib2 second time.".
print "Next lines SHOULD print 'MAINLINE CODE' as it runs the libs.".
run testrunonce_lib1.
run testrunonce_lib2("dummy arg").

print "Testing that the funcs still exist:".
print "lib1 function returns: " + lib1().
print "lib2 function returns: " + lib2().
