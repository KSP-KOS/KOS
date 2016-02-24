print " ".
print "Defaultable params calling functions".
print "from library program.".
print " ".

run paramtest4_lib.

print "Calling needsZeroOfOne('a')". // can't embed quotes yet.
needsZeroOfOne("a").
print "Calling needsZeroOfOne()".
needsZeroOfOne().

print "Calling needsTwoOfFour('a','b','c','d')".
needsTwoOfFour("a","b","c","d").
print "Calling needsTwoOfFour('a','b','c')".
needsTwoOfFour("a","b","c").
print "Calling needsTwoOfFour('a','b')".
needsTwoOfFour("a","b").

