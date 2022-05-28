print "THESE are tests of the ternary CHOOSE operator.".

Print "Basic case - Next line should print 'A':".
print choose "A" if true else "B".

Print "Basic case - Next line should print 'B':".
print choose "A" if false else "B".


print "Testing nested CHOOSEs.  Next line should be 'ABCDE*****':".
set str to "".
for i in range(0,10) {
  set str to str + (choose "A" if i = 0 else choose "B" if i = 1 else choose "C" if i = 2 else choose "D" if i = 3 else choose "E" if i = 4 else "*").
  
}
print str.


print "Weird case - nesting choose in the boolean.".
set x to true.
set y to false.
set a to 1.
print "  Next 2 lines should be 'A':".
print "  " + (choose "A" if (choose x if a=1 else y) else "B").
print "  " + (choose "A" if choose x if a=1 else y else "B"). // same without parens should work.
print "  Next 2 lines should be 'B':".
print "  " + (choose "A" if (choose x if a=2 else y) else "B").
print "  " + (choose "A" if choose x if a=2 else y else "B"). // same without parens should work.

print "Complex case - selecting delegate with choose.".
set a to 1.
set del1 to choose { print "trueDel". } if a = 1 else { print "falseDel.". }.
set a to 2.
set del2 to choose { print "trueDel". } if a = 1 else { print "falseDel.". }.
print "Next line should say 'trueDel':".
del1:call().
print "Next line should say 'falseDel':".
del2:call().
