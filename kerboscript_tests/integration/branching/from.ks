@lazyGlobal OFF.

print("beginning").
from {local i is 0.}
until i = 10
step {set i to i + 1.}
do {
    print("body").
}
print("trunk").