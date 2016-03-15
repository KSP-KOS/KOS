print "Testing an array of delegates.".

function f1 { parameter a,b.  return a+b. }

local f1_curry1 is f1@:bind(1).
local f1_curry2 is f1_curry1:bind(2).
local mod_del is mod@:bind(8,5). // testing a delegate of a built-in too.

local func_arr is LIST(f1@, f1_curry1, f1_curry2, mod_del).

print "Array of functions is:".

print "--- func_arr[0] -----".
print func_arr[0].
print "--- func_arr[1] -----".
print func_arr[1].
print "--- func_arr[2] -----".
print func_arr[2].
print "--- func_arr[3] -----".
print func_arr[3].

print "Calling them.  These should all print 3:".
print "calling func_arr[0](1,2) returns " + func_arr[0](1,2).
print "calling func_arr[1](2)   returns " + func_arr[1](2).
print "calling func_arr[2]()    returns " + func_arr[2]().
print "calling func_arr[3]()    returns " + func_arr[3]().
