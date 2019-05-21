print "Testing default function scoping rules".

run once "functest29_lib.ks".

print "Should say Func1: " + func1().
if (func1() <> "Func1") print "!!!!!!!!!! ERROR ERROR ERROR !!!!!!!!!!!" + char(7) + char(7).
local f2 is getfunc2().
print "Should say Func2: " + f2().
if (f2() <> "Func2") print "!!!!!!!!!! ERROR ERROR ERROR !!!!!!!!!!!" + char(7) + char(7).

local f3 is getfunc3().
print "Should say Func3: " + f3().
if (f3() <> "Func3") print "!!!!!!!!!! ERROR ERROR ERROR !!!!!!!!!!!" + char(7) + char(7).

