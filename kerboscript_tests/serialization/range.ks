set r to range(1, 13, 2).

writejson(r, "range.json").
set read to readjson("range.json").

print "These should all be 'True':".

print read:length = r:length.
print read:from = 1.
print read:to = 13.
print read:step = 2.
