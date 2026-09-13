@lazyGlobal OFF.

randomseed("random", 0).
local a is 1.
local b is random("random").
local d is random("random").

global c is a + b.

print c.
print a + b.
print a + b.
{
    print a + b.
    global e is b + d.
    print b + d.
    print b + d.
    local f is e.
    print f.
}
print "Outside".
print b + d.