print "Testing using Lex as a psuedo-class.".
print "------------------------------------".


print "Making 'fred', an instance of person'.".
local fred is construct_person("Fred", 23).
print "fred:greet() prints this:".
print fred:greet().

print "Making 'henri', an instance of frenchperson.".
local henri is construct_frenchperson("Henri", 19).
print henri:greet().

function construct_person {
  parameter name, age.

  local myself is LEX().

  set myself:name to name.
  set myself:age to age.
  set myself:greet to {
    return "Hello, my name is " + myself:name +" and I am " + myself:age + " years old.".
  }.

  return myself.
}.

function construct_frenchperson {
  parameter name, age.

  local myself is construct_person(name, age).

  // This is sort of like overriding a method:
  set myself:greet to {
    return "Bonjour, Je m'appelle " + myself:name + " et j'ai " + myself:age + " ans.".
  }.

  return myself.
}
