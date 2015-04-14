// Testing the illegal use of modifiers.

print "This should complain that you can't use global with function:".
declare global function foo {
  print "hello".
}.

foo().
