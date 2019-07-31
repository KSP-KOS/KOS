local empty_lex is lexicon().
local populated_lex is lexicon(
  "key0", 0, // valid suffix name
  "key1", 10, // valid suffix name
  "key2", 20, // valid suffix name
  "KEY3", 30, // valid suffix name
  // None of the following 3 should be valid suffix names because of the
  // spaces:
  " SpaceBefore", "----",
  "SpaceAfter ", "----",
  "Space Between", "----",
  V(1,0,0), "----", // not a valid suffix name because Vectors aren't strings.
  "zzzzzz", 9999 // valid suffix name
  ). 
print "Expect: True".
print "Actual: " + empty_lex:hassuffix("add"). // built-in suffix for all lex's.
print " ".
print "Expect: True".
print "Actual: " + populated_lex:hassuffix("add"). // built-in suffix for all lex's.
print " ".
print "Expect: False".
print "Actual: " + empty_lex:hassuffix("key2"). // only exists if key2 is in the lex.
print " ".
print "Expect: True".
print "Actual: " + populated_lex:hassuffix("key2"). // only exists if key2 is in the lex.
print " ".
print "Expect: True".
print "Actual: " + populated_lex:hassuffix("kEy3"). // case insensitive check.
print " ".
print "Expect: True".
print "Actual: " + populated_lex:haskey("Space Between"). // key exists
print " ".
print "Expect: False".
print "Actual: " + populated_lex:hassuffix("Space Between"). // but isn't a valid suffix name

// There should be 5 more suffixes in the populated list than in default lex's,
// because 5 of the keys in it form valid identifier strings:
// If this is more than 5, then keys that shouldn't be
// suffixes are getting into the list.
print "Expect: 5".
print "Actual: " + (populated_lex:suffixnames:length - empty_lex:suffixnames:length).

