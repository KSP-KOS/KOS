// Tests comma-separated declarations of variables

print("Testing with 'SET'").
set a to 1, b to 2, c to "a".

print " 1. The first variable has the correct value: " + (a = 1).
print " 2. The second variable has the correct value: " + (b = 2).
print " 3. The third variable has the correct value" + (c = "a").
