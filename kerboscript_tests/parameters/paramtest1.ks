print " ".
print "Defaultable prog params.".
print "Try calling with 2, 3, and 4 args.".

declare parameter p1, p2, p3 is sqrt(4) - 3. // an expression that evals to -1.
declare parameter p4 is -2. // a hardcoded -1, also test case where they aren't all on the same parameter statement.

print "Parameters as seen inside program are:".
print "  " + p1 + ", " + p2 + ", " + p3 + ", " + p4.
