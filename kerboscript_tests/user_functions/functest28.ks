// Testing issue 1801 - functions nested in anon functions.

// global named function called from anon function:
function foo_works1 { print "Global named func called from anon works.". }
local fn_works1 is { foo_works1(). }.
fn_works1().

// anon function nested in anon function:
local fn_works2 is {
  local foo_works2 is {
    print "Anon func nested in anon func works.".
  }.
  foo_works2().
}.
fn_works2().

// nanmed function nested in named function:
function fn_works3 {
  function foo_works3 {
    print "named func nested in named func works.".
  }
  foo_works3().
}
fn_works3().

// named function inside anything that is not a function
if true {
  function func_inside_if {
    print "named func nested in generic braces works.".
  }
  func_inside_if().
}

// named function nested in anon function:
local fn_fails is {
  function foo_fails {
    print "named func nested in anon func works.".
  }
  foo_fails().
}.
fn_fails().

