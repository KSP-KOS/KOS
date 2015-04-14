// Testing the illegal use of modifiers.

@lazyglobal off.

print "This should complain that you can't leave local or global implicit with @lazyglobal off".
declare foo is 1. // This should require local or global and be an error.
print "foo is " + foo.
