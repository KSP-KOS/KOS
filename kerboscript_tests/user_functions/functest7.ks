// One test of the nolazyglobal keyword.
// This should work correctly.

@lazyglobal off. // at the top - should be okay.

declare x to 1. // this should be fine.
set x to 2. // this should be fine because x exists now.
print "Should bomb out with error because there was no 'declare y' statement and lazyglobals disabled.".
set y to 1.

declare function foo {
  set z to 1.
  print "this should not get this far: z = " + z.
}.
foo().
