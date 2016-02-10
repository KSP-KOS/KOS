// Serializes a queue with nested structures in it

set q to queue().

q:push("stringvalue").
q:push(1).
q:push(true).
q:push(1.5).
q:push(body:atm). // put in a non-serializable type, this should use object's ToString() method

set nested to lexicon().
nested:add("nestedkey1", "value").
q:push(nested).

set nested_list to list().
nested_list:add("element1").
nested_list:add(1.2).
q:push(nested_list).

set nested_stack to stack().
nested_stack:push("test_stack").
nested_stack:push(2).
q:push(nested_stack).

set nested_queue to queue().
nested_queue:push(1).
nested_queue:push("test_queue").
q:push(nested_queue).

writejson(q, "queue.json").

// Reading & verification

set read to readjson("queue.json").

print "These should all be 'True':".

print read:length = 9.

print read:pop = "stringvalue".
print read:pop = 1.
print read:pop.
print read:pop = 1.5.
print read:pop:contains("BODYATMOSPHERE").

set l to read:pop.
print l["nestedkey1"] = "value".

set l to read:pop.
print l[0] = "element1".
print l[1] = 1.2.

set s to read:pop.
print s:pop = 2.
print s:pop = "test_stack".

set q to read:pop.
print q:pop = 1.
print q:pop = "test_queue".

