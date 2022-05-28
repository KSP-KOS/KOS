local my_lex is lexicon(
  "Key0" , "A",
  "Key1" , "B",
  "Key2" , "C",
  "Key3" , "D",
  "Key4" , "E",
  "Key5" , "F").

print "Expect: ABCDEF".
print "Actual: " + concat_lex(my_lex).

set my_lex:key1 to "_".
set my_lex:key3 to "_".
set my_lex:key5 to "_".

print "Expect: A_C_E_".
print "Actual: " + concat_lex(my_lex).

set my_lex:key1 to { return "%". }.
set my_lex:key3 to { local a is 1. local b is 4. return a+b. }.
set my_lex:key5 to { return 3/2. }.

print "Expect: A%C5E1.5".
print "Actual: " + concat_lex(my_lex).

function concat_lex {
  parameter the_lex.

  local str is "".
  for key in the_lex:keys {
    if the_lex[key]:istype("Delegate") 
      set str to str + the_lex[key]().
    else
      set str to str + the_lex[key].
  }
  return str.
}
