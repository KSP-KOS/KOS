// Serializes a list with nested structures in it

set l to list().

l:add("stringvalue").
l:add(1).
l:add(true).
l:add(1.5).
l:add("3.5").
l:add(body:atm). // put in a non-serializable type, this should use object's ToString() method

set nested to lexicon().
nested:add("nestedkey1", "value").
l:add(nested).

set nested_list to list().
nested_list:add("element1").
nested_list:add(1.2).
l:add(nested_list).

set nested_stack to stack().
nested_stack:push("test_stack").
nested_stack:push(2).
l:add(nested_stack).

set nested_queue to queue().
nested_queue:push(1).
nested_queue:push("test_queue").
l:add(nested_queue).

writejson(l, "list.json").

// Reading & verification

set read to readjson("list.json").

print "These should all be 'True':".

print read:length = 10.

print read[0]:typename = "String".
print read[0] = "stringvalue".
print read[1] = 1.
print read[1]:typename = "Scalar".
print read[1] + 1 = 2.
print read[2]:typename = "Boolean".
print read[2].
print read[3] = 1.5.
print read[3]:typename = "Scalar".
print read[3] + 1 = 2.5.
print read[4]:typename = "String".
print read[4] = "3.5".
print read[5]:contains("BODYATMOSPHERE").

set l to read[6].
print l["nestedkey1"] = "value".

set l to read[7].
print l[0] = "element1".
print l[1] = 1.2.

set s to read[8].
print s:pop = 2.
print s:pop = "test_stack".

set q to read[9].
print q:pop = 1.
print q:pop = "test_queue".
