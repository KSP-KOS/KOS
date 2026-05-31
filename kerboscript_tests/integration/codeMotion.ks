@lazyGlobal OFF.

randomseed("random", 0).
local a is random("random").
local b is a + 1.
print(b).
from {local i is 0.}
until i = 4
step {set i to i + 1.}
do {
  set b to a + 2.
  print(i).
}
print(b).
for j in range(0, 4) {
  set b to a + 1.
  print (j).
}
print(b).