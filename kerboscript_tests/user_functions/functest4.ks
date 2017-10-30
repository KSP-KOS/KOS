// functest4.
// An example of a complex function scoping situation with
// nested functions.

declare a to 1.

declare function foo {
  declare parameter parm.
  declare b to 2.

  declare function foo_bar {
    declare c to 3.

    return "a is " + a + ", b is " + b + ", c is " + c + " parm is " + parm.
  }.

  return foo_bar().
}.

print "calling foo() from global scope: " + foo(100).

// pointless nesting
{

  declare function more_pointless_nesting {
    declare a to 5.
    print "calling foo() from local scope depth 2: " + foo(100).
  }.
  declare a to 4.
  print "calling foo() from local scope depth 1: " + foo(100).

  more_pointless_nesting().
}.

// Should print this every time no matter where it's called from:
//    a is 1, b is 2, c is 3, parm is 100.
// not this:
//    a is 4, b is 2, c is 3, parm is 100.
// nor this:
//    a is 5, b is 2, c is 3, parm is 100.
