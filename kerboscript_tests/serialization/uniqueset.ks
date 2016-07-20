// Serializes a set with nested structures in it

set l to uniqueset().

l:add("stringvalue").
l:add(1).
l:add(true).
l:add(1.5).
l:add("3.5").
l:add(body:atm). // put in a non-serializable type, this should use object's ToString() method

set nested to lexicon().
nested:add("nestedkey1", "value").
l:add(nested).

set nested_set to uniqueset().
nested_set:add("element1").
nested_set:add(1.2).
l:add(nested_set).

set nested_stack to stack().
nested_stack:push("test_stack").
nested_stack:push(2).
l:add(nested_stack).

set nested_queue to queue().
nested_queue:push(1).
nested_queue:push("test_queue").
l:add(nested_queue).

writejson(l, "set.json").

// Reading & verification

set read to readjson("set.json").

print "These should all be 'True':".

print read:length = 10.

print read:contains("stringvalue").
print read:contains(1).
print not read:contains("1").
print read:contains(true).
print not read:contains("true").
print read:contains(1.5).
print not read:contains("1.5").
print read:contains(body:atm:tostring).
