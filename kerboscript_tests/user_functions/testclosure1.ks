// Simple test of lock closures
//  Test 1: one scope level, not in a function:

set x to 1.
lock testlock to x.

print "testlock before scope = " + testlock.
if true { // pointless 'if' to have an excuse for a set of braces
  declare local_x to 4.

  lock testlock to x + local_x. // a mix of both local and global things in the expression.

  print "testlock inside scope = " + testlock.
}.
print "testlock after scope = " + testlock.
