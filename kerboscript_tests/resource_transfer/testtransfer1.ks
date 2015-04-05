print "THIS SCRIPT TESTS RESOURCE TRANSFER".
print "WHEN DOING IT BY LIST() of PARTS".

set fromparts to ship:partstagged("from_us").
set toparts to ship:partstagged("to_us").

if fromparts:length = 0 {
  print "You need to pick some parts and give them nametag 'from_us'.".
  print "deliberate error to die".
  set x to 1/0.
}.

if toparts:length = 0 {
  print "You need to pick some parts and give them nametag 'to_us'.".
  print "deliberate error to die".
  set x to 1/0.
}

print "Now transferring 90 liquidfuel from parts to parts".
set foo to transfer("liquidfuel",fromparts,toparts,90).
set foo:active to true.
until foo:status = "Finished" or foo:status = "Failed" {
  print "transferred " + foo:transferred + " " + foo:resource + " so far. " + foo:status + " ...".
  wait 0.2.
}.
print "transferred " + foo:transferred + " in total.".


print "Now transferring 110 oxidizer from parts to parts".
set foo to transfer("oxidizer",fromparts,toparts,110).
set foo:active to true.
until foo:status = "Finished" or foo:status = "Failed" {
  print "transferred " + foo:transferred + " " + foo:resource + " so far. " + foo:status + " ...".
  wait 0.2.
}.
print "transferred " + foo:transferred + " in total.".
