@lazyGlobal OFF.

print("beginning").
from {local i is 0.}
until i >= 10
step {
    if (mod(i, 2) = 0) {
        set i to i + 1.
    } else {
        set i to i + 2.
    }
}
do {
    print(i).
}
print("trunk").