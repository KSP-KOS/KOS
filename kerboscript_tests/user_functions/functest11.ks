// Testing the case of a function that calls a RUN
// command from inside of itself:

declare function foo {
  print "Inside function: about to run functest10".
  run functest10(10,20,30).
  print "Inside function: done running functest10".
}.

print "Outside function: about to call function".
foo().
print "Outside function: done calling function".
