// Testing the use of syntax modifiers:

set a to -1.
set b to -1.
set c to -1.
set d to -1.
set e to -1.
set f to -1.
set g to -1.
set h to -1.
set i to -1.
set j to -1.

{
  declare a to 1.
  declare b is 2.
  declare local c to 3.
  declare local d is 4.
  declare global e to 5.
  declare global f is 6.
  global g to 7.
  global h is 8.
  local i is 9.
  local j is 10.

  print "In the list below. you should see ".
  print "[a] through [j] getting values 1 to 10:".
  print "---------------------------------------".
  print "in local scope, a = " + a.
  print "in local scope, b = " + b.
  print "in local scope, c = " + c.
  print "in local scope, d = " + d.
  print "in local scope, e = " + e.
  print "in local scope, f = " + f.
  print "in local scope, g = " + g.
  print "in local scope, h = " + h.
  print "in local scope, i = " + i.
  print "in local scope, j = " + j.
}

print "In the list below. you should see ".
print "that everything reverted to -1 except".
print "for e,f,g, and h, which were being globally".
print "edited, not locally edited".
print "---------------------------------------".
print "in global scope, a = " + a.
print "in global scope, b = " + b.
print "in global scope, c = " + c.
print "in global scope, d = " + d.
print "in global scope, e = " + e.
print "in global scope, f = " + f.
print "in global scope, g = " + g.
print "in global scope, h = " + h.
print "in global scope, i = " + i.
print "in global scope, j = " + j.
