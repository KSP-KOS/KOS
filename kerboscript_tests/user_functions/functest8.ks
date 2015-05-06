// Prove that one file can call functions defined in another file.

run functest3. // library of functions to be used from here.

print "------------------------------------".
print "Now printing the tree starting from all parts you named 'printme'.".
print "------------------------------------".

set printMeParts to ship:partstagged("printme").
if printMeParts:length = 0 {
  print "You need to give at least one part the nametag of 'printme' to run this program.".
} else {
  for p in printMeParts {
    print "----- Branch starting with part " + p + " -----".
    printBranch(p, ""). // this function is actually defined in functest3.ks
  }
}
