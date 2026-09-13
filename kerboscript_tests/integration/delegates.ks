@lazyGlobal OFF.

local s is sin@.
local c is cos@.
if (random() > 1) {
    set c to arcCos@.
}

local t is {parameter x. print x. }.

local function test {
    parameter x.
    parameter y.
    print x + y.
}

local z is test@.

print(s(0)).
print(c(0)).
t(1).
z(1, 1).

print(s:call(90)).
print(c:call(90)).
t:call(2).
z:call(2, 2).

local s_ is s:bind(45).
local c_ is c:bind(45).
local t_ is t:bind(3).
local z_ is z:bind(3).

print(s_()).
print(c_()).
t_().
z_(3).

print(s_:call()).
print(c_:call()).
t_:call().
z_:call(4).

print(s:bind(135):call()).
print(c:bind(-45)()).
// The base compiler cannot handle this statement:
//print((choose s if random() > 1 else c)(0)).
print((choose s if random() > 1 else c):call(0)).