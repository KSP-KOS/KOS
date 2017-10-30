// Serializes a lexicon with nested structures in it

set l to lexicon().

l:add("key1", "stringvalue^&*{}").
l:add("0", 1).
l:add(0, 1.5). // test a key that when stringified is the same as one of the previous keys
l:add("key5{}", body:atm). // put in a non-serializable type, this should use object's ToString() method
l:add(kerbin, true). // serializable type as key

set nested to lexicon().
nested:add("nestedkey1", "value").
l:add("nestedlexicon", nested).

set nested_list to list().
nested_list:add("element1").
nested_list:add(1.2).
l:add("nestedlist", nested_list).

set nested_stack to stack().
nested_stack:push("test_stack").
nested_stack:push(2).
l:add("nestedstack", nested_stack).

set nested_queue to queue().
nested_queue:push(1).
nested_queue:push("test_queue").
l:add("nestedqueue", nested_queue).

writejson(l, "lexicon.json").

print "--------".
print "This printout should look reasonable:".
print l:dump.

// Reading & verification

set read to readjson("lexicon.json").

print "--------".
print "These should all be 'True':".

print read:length = 9.

print read["key1"] = "stringvalue^&*{}".
print read["0"] = 1.
print read["0"] + 1 = 2.
print read[0] = 1.5.
print read[kerbin].
print read["key5{}"]:contains("BODYATMOSPHERE").

set l to read["nestedlexicon"].
print l["nestedkey1"] = "value".

set l to read["nestedlist"].
print l[0] = "element1".
print l[1] = 1.2.

set s to read["nestedstack"].
print s:pop = 2.
print s:pop = "test_stack".

set q to read["nestedqueue"].
print q:pop = 1.
print q:pop = "test_queue".
