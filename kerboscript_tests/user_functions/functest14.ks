// An example testing nesting a function inside a function
// when both of them end up having the same name:
// Proper nesting rules should mask the outer function
// with the inner function.

declare function samename {
  declare x to 1.
  print "outer function samename() has x = " + x.

  declare function samename {
    declare x to 2.
    print "inner function samename() has x = " + x.
  }.

  samename().

}.

samename().
