@lazyGlobal OFF.

global a is 5.
global _false is false.

local b is 2.
local c is 4.
local d is 7.
local h is false.

print("test").  // The assignments should happen after this line.

print(b+c).     // 6
print(h).       // False

global function func {
    print(b+c). // 6
    print(b+d). // 9
    print(b+a). // 7
    set h to true.
}

func().

{
    local i is 10.
    print(i).   // 10
    print(i+c). // 14
}

print(h).   // True

set h to false.

print(h).   // False

lock e to a + b.

print(e).   // 7
set b to 1.
print(e).   // 6

lock g to c + d.
print(g).   // 11
set d to 5.
print(g).   // 9

when _false then {
    set c to 10.
}

print(b+c). // 5

set b to 1.
set b to b + d.

print(b).   // 6

until b = 1 {
    set b to b - 1.
}

print(b).   // 1
