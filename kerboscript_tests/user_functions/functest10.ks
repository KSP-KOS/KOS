// Testing a script calling a script with arguments.

declare parameter arg1,arg2,arg3.

print "Outer script (functest10) called with arguments:".
print "   arg1=" + arg1.
print "   arg2=" + arg2.
print "   arg3=" + arg3.

print "Now functest10 is going to call functest10_inner,".
print "Giving it the same args in the same order:".
run functest10_inner(arg1,arg2,arg3).
