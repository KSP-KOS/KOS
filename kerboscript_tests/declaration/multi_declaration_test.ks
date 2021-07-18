// Tests comma-separated declarations of variables

print "Testing with 'SET'".
set a to 1, b to 2, c to "a".

print " 1. The first variable has the correct value: " + (a = 1).
print " 2. The second variable has the correct value: " + (b = 2).
print " 3. The third variable has the correct value: " + (c = "a").

print "Testing with 'GLOBAL'".
global d is 5, e to 2.

print " 1. The first variable has the correct value: " + (d = 5).
print " 2. The second variable has the correct value: " + (e = 2).

print "Testing with 'DECLARE'".

declare f is 12, g to 1.

print " 1. The first variable has the correct value: " + (f = 12).
print " 2. The second variable has the correct value: " + (g = 1).


print "Testing with 'DECLARE GLOBAL'".

declare h is 7, i to 15, j to 3.

print " 1. The first variable has the correct value: " + (h = 7).
print " 2. The second variable has the correct value: " + (i = 15).
print " 3. The thrid variable has the correct value: " + (j = 3).
