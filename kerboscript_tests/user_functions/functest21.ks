// Test a case where the same variable is used as both a lock
// and a set, but at two different scopes:

lock x to 1.
lock y to x/3.
print "Testing that locks don't get broken when running ".
print " a long sub-program that outlasts an IPU boundary.".
print "This program runs a long time counting in its head.".
print "Expect it to take a few seconds.".
print "before: x is " + x.
lock steering to up.
lock throttle to y. // indirect levels of locks to get to a value of 0.3333.

run functest21_inner.
print "If it got this far, then it worked.".
