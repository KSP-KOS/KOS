// One test of the nolazyglobal keyword.
// This should work correctly.

set x to 1. // this should make a global x for us.

print "next line should bomb out because lazyglobal isn't at the top.".
@lazyglobal off.

declare function foo {
  set z to 1.
}.
foo().
