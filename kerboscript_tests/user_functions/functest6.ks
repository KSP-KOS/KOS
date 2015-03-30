// One test of the nolazyglobal keyword.
// This should error out because nolazyglobal isn't at global scope.

set x to 1. // this should make a global x for us.


declare function foo {
  print "next line should bomb out because lazyglobal is nested inside braces".
  @lazyglobal off.
  set z to 1.
}.
foo().
