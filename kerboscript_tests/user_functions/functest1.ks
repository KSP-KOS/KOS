// Basic simple function define function test:

set glob_one to 10.
set x to 0.

lock bar to 5.
declare function foo {
  declare x to 1.
  print "Inside function foo: x is " + x.

  return "FooValue".
}.

print "Before calling foo(), outside of foo, x is " + x.

print "return value from foo() is: " + foo().

print "After calling foo(), outside of foo, x is " + x.

print "using a lock, bar = " + bar.

print "Deliberate error to force a program dump:".
set x to 1/0.
