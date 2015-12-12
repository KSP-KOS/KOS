print "ship vel = " + ship:velocity:orbit:mag().

function foo {
  print "   -- proof I am in foo.".
}

function bar {
  parameter a, b, c.

  print "   -- proof I am in bar("+a+", "+b+", "+c+")".
  return a+b+c.
}

print "== calling foo normally ==".
foo().
print "== getting a delegate of foo ==".
set foo_ref to foo@.
print "== calling the delegate of foo ==".
foo_ref:call().

print "== calling bar normally ==".
set sum to bar(1,2,3).
print "bar returned " + sum.
print "== calling bar through a delegate ==".
set bar_ref to bar@.
set refsum to bar_ref:call(1,2,3).
print "bar via a ref returned " + refsum + " which should be 6".
print "== getting a curry of bar delegate ==".
set bar_curry to bar_ref:curry(1,2).
print "== calling bar through curry ==".
set refsum to bar_curry:call(3).
print "bar via a curry returned " + refsum + " which should be 6".
