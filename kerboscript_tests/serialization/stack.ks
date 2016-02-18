// Serializes a stack with nested structures in it

set s to stack().

s:push("stringvalue").
s:push(1).
s:push(true).
s:push(1.5).
s:push(body:atm). // put in a non-serializable type, this should use object's ToString() method

set nested to lexicon().
nested:add("nestedkey1", "value").
s:push(nested).

set nested_list to list().
nested_list:add("element1").
nested_list:add(1.2).
s:push(nested_list).

set nested_stack to stack().
nested_stack:push("test_stack").
nested_stack:push(2).
s:push(nested_stack).

set nested_queue to queue().
nested_queue:push(1).
nested_queue:push("test_queue").
s:push(nested_queue).

writejson(s, "stack.json").

// Reading & verification

set read to readjson("stack.json").

print "These should all be 'True':".

print read:length = 9.

set q to read:pop.
print q:pop = 1.
print q:pop = "test_queue".

set s to read:pop.
print s:pop = 2.
print s:pop = "test_stack".

set l to read:pop.
print l[0] = "element1".
print l[1] = 1.2.

set l to read:pop.
print l["nestedkey1"] = "value".


print read:pop:contains("BODYATMOSPHERE").
print read:pop = 1.5.
print read:pop.
print read:pop = 1.
print read:pop = "stringvalue".
