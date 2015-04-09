// Testing some deeply nested returns

declare function outer1 {
  print "function outer1: before for loop".
  set foo to list().
  foo:add(1).
  foo:add(2).
  foo:add(3).
  for thing in foo {
    print "thing is " + thing.
    if thing = 3 {
      return true.
    }.
  }.
  print "function outer1: after for loop".
  return false.
}.

declare function outer2 {
  print "function outer2: before inner function".

  print "function outer2 is now calling function outer1 again.".

  if outer1() {
    print "outer returned early.".
  } else {
    print "outer executed all the way to the bottom.".
  }

  print "function outer2 is done with function outer1.".

  declare function inner {
    set foo to list().
    foo:add(1).
    foo:add(2).
    foo:add(3).
    for thing in foo {
      print "inner: thing is " + thing.
      if thing = 3 {
	return true.
      }.
    }.
    return false.
  }.

  print "function outer2 is going to now call inner.".
  if inner() {
    print "inner returned early.".
  } else {
    print "inner executed all the way to the bottom.".
  }
  print "function outer2 is done calling inner.".

  print "function outer2: after inner function".
}.

print "before calling function outer1.".
if outer1() {
  print "outer returned early.".
} else {
  print "outer executed all the way to the bottom.".
}
print "after calling function outer1.".

print "before calling function outer2.".
outer2().
print "done calling function outer2.".

