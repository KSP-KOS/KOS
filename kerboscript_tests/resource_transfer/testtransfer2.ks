print "THIS SCRIPT TESTS RESOURCE TRANSFER".
print "WHEN DOING IT BY ELEMENTS.".
print " ".
print "To select which element to transfer from,".
print "just assign nametag 'from_this' to any of".
print "the parts inisde it.".
print " ".
print "To select which element to transfer to,".
print "just assign nametag 'to_this' to any of".
print "the parts inisde it.".

set fromparts to ship:partstagged("from_this").
set toparts to ship:partstagged("to_this").

if fromparts:length = 0 {
  print "You need to a part, and give it nametag 'from_this'.".
  print "deliberate error to die".  set x to 1/0.
}.
set frompart to fromparts[0].

if toparts:length = 0 {
  print "You need to a part, and give it nametag 'to_this'.".
  print "deliberate error to die".  set x to 1/0.
}
set topart to toparts[0].

// Now calculate which element contains those parts:
set fromElementNum to -1.
set toElementNum to -1.
LIST ELEMENTS IN eList.
set i to 0.
until i >= eLIst:length {
  set ele to eList[i].
  for pt in ele:parts {
    if pt = frompart { // this is an indirect test of part equals operator
      set fromElementNum to i.
    }.
    if pt = topart { // this is an indirect test of part equals operator
      set toElementNum to i.
    }.
  }.
  set i to i + 1.
}.

if fromElementNum < 0 or toElementNum < 0 {
  print "error calculating element. Dying now".
  set x to 1/0.
}
if fromElementNum = toElementNum {
  print "error: from and to element are the same element.".
  print "dying now".
  set x to 1/0.
}

print "Now transferring 90 liquidfuel from element " + fromelementNum + " to element " + toElementNum.
set foo to transfer("liquidfuel", eList[fromElementNum], eList[toElementNum], 90).
set foo:active to true.
until foo:status = "Finished" or foo:status = "Failed" {
  print "transferred " + foo:transferred + " " + foo:resource + " so far. " + foo:status + " ...".
  wait 0.2.
}.
print "transferred " + foo:transferred + " in total. " + foo:status.


print "Now transferring 110 oxidizer from element " + fromelementNum + " to element " + toElementNum.
set foo to transfer("oxidizer", eList[fromElementNum], eList[toElementNum], 110).
set foo:active to true.
until foo:status = "Finished" or foo:status = "Failed" {
  print "transferred " + foo:transferred + " " + foo:resource + " so far. " + foo:status + " ...".
  wait 0.2.
}.
print "transferred " + foo:transferred + " in total. " + foo:status.
