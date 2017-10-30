// Test weird case with index nested inside suffix and visa versa.
// within SET statements.

set a to list(1,2,3).
set endIndex to a:length()-1.

print "This test case should NOT error out.".
print "------------------------------------".
print " ".
print "Testing nesting suffixes inside set list element:".
print "Of the list " + a + " ...".
print "the last element is currently " + a[endIndex].
print "or also gotten another way, " + a[a:length()-1].

print "And now we change to 5 it one way:".
set a[endIndex] to 5.
print "And the list is now: " + a.

print "And now we change to 10 a different way:".
set a[a:length()-1] to 10.
print "And the list is now: " + a.

print " ".
print "Testing list element lookups inside setting suffixes:".

set b to ship.
print "Setting ship's rootpart tagname to a new name: 'aaaa' ".
set b:parts[0]:tag to "aaaa".
print "New name is '" + b:parts[0]:tag + "'".

