// Testing the illegal use of modifiers.

print "This should complain that you can't use global with parameter:".
declare global parameter foo.
print "I was passed " + foo.
