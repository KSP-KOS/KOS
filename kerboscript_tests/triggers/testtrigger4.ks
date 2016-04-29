print "Testing that a 'return' works in place of 'preserve'.".

on AG1 {
  print "AG1 triggered.  You can try hitting it again.".
  preserve.
}
on AG2 {
  print "AG2 triggered.  You can try hitting it again.".
  return true.
}
on AG3 {
  print "AG3 triggered.  It won't work again.".
}
on AG4 {
  print "AG4 triggered.  It won't work again.".
  return false. // same as saying nothing at all.
}

// This is the same as an "on" but just testing
// that the return syntax works with a when too:
set oldAG5 to AG5.
set countAG5 to 0.
when AG5 = not(oldAG5) then {
  set oldAG5 to AG5.
  set countAG5 to countAG5 + 1.
  print "You have hit AG5 " + countAG5 + " times.  After the 5th, it will stop working.".
  if countAG5 = 5 {
    print "I'm done listening to AG5 now.".
    return false. // don't preserve.
  } else {
    return true. // do preserve.
  }
}
set done to false.

on AG6 {
  set done to true.
}

print "To test: use the action groups to fire triggers:".
print "AG1 should keep preserving the trigger always.".
print "AG2 should keep preserving the trigger always.".
print "AG3 should only trigger once.".
print "AG4 should only trigger once.".
print "AG5 should trigger 5 times then quit triggering.".
print "AG6 should kill the program.".

wait until done.
