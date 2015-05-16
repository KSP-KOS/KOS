// Test of from loop syntax:

print "Simple from loop to print from 1 to 10.".

from {local i is 1.} until i > 10 step {set i to i+1.} do {
  print i.
}
print "done".
