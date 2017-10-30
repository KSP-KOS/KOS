// Testing returns without arguments.

print "testing return statements without an argument".

declare function outer1 {
  print "function outer1: before for loop".
  set foo to list().
  foo:add(1).
  foo:add(2).
  foo:add(3).
  for thing in foo {
    print "thing is " + thing.
    if thing = 3 {
      return.
    }.
  }.
  print "function outer1: after for loop".
  return.
}.

print "before calling function outer1.".
outer1().

