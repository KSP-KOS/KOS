// Test for empty return statement:

@lazyglobal off.
function reproduce_bug{
    return.
}
print "Testing whether or not naked return works.".
print "The next line should print zero.".
print reproduce_bug().
print "If it got this far, it worked.".
