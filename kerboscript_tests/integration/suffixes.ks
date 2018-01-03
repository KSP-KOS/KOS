
set a to lexicon().

print(a:length).

set a["a"] to 2.

print(a:length).

print(a["a"]).

set a["b"] to list(1,2,3).

print(a["b"]:length).

set a:case to true.

print(a:length).

set a["a"] to 3.

print(a:haskey("A")).