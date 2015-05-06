// Test the list constructor.

local l1 is list().
local l2 is list(5,10,15,20).
// 2-d test:
local m1 is list( list(10,20,30,40), list(15,25,35,45), list(11,22,33,44) ).

print "----------------------".
print "list l1 is: (should be empty)".
for item in l1 { print item + " " . }.

print "----------------------".
print "list l2 is: (should be 4 things)".
for item in l2 { print item + " " . }.

print "----------------------".
print "2-D list m1 is:".
for row in m1 {
  local str is "(".
  for item in row {
    set str to str + item + " ".
  }.
  set str to str + ") ".
  print str.
}.
