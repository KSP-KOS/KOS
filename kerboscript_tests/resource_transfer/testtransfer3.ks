print "THIS SCRIPT TESTS RESOURCE TRANSFER".
print "WHEN DOING IT BY SINGLE PART TO SINGLE PART".

set fromparts to ship:partstagged("from_this").
set toparts to ship:partstagged("to_this").

if fromparts:length <> 1 {
  print "You need to pick exactly one part, no more no less, and give it nametag 'from_this'.".
  print "deliberate error to die".
  set x to 1/0.
}.
set frompart to fromparts[0].

if toparts:length <> 1 {
  print "You need to pick exactly one part, no more no less, and give it nametag 'to_this'.".
  print "deliberate error to die".
  set x to 1/0.
}
set topart to toparts[0].

print "Now transferring 90 liquidfuel from part to part".
set foo to transfer("liquidfuel",frompart,topart,90).
set foo:active to true.
until foo:status = "Finished" or foo:status = "Failed" {
  print "transferred " + foo:transferred + " " + foo:resource + " so far. " + foo:status + " ...".
  wait 0.2.
}.
print "transferred " + foo:transferred + " in total.".


print "Now transferring 110 oxidizer from part to part".
set foo to transfer("oxidizer",frompart,topart,110).
set foo:active to true.
until foo:status = "Finished" or foo:status = "Failed" {
  print "transferred " + foo:transferred + " " + foo:resource + " so far. " + foo:status + " ...".
  wait 0.2.
}.
print "transferred " + foo:transferred + " in total.".
