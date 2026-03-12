parameter a is TRUE.
parameter c is 3.
parameter x is 2.
parameter z is 5.

print("test":typename()).
// v() is unavailable in unit testing.
//local v1 is v(1,2,3).
//local v2 is v(3,4,5).
// Multiplying scalars will work equally well.
local v1 is 2.
local v2 is 3.

print(vectorDotProduct(v1,v2)). // =6
print(vdot(v1,v2)).             // =6

if not a {
    print("Don't print this.").
}

if not (not a) {
    print("Print this.").
}

print(c*x+c*z).     // = 21
print(c*x+z*c).     // = 21
print(x*c+c*z).     // = 21
print(x*c+z*c).     // = 21

print(-x+z).        // = 3
print(x+-z).        // = -3

print(x--z).        // = 7

print(x^z/x).       // = 16
print(z^4/z).       // = 125

print(x^z*x).       // = 64
print(z^4*z).       // = 3125

print(x^z*x^c).     // = 256

print(x^z/x^c).     // = 4

print(z^3/z).       // = 25

print(z*z*z).       // = 125

print(z^3/z*z).     // = 125

print(x^2*x^z/x*x^3).   // = 512