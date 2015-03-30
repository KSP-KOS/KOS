// Functions: testing recursive case: note this just loads the functions
// and does nothing else.  functest8.ks actually calls this.

// -------------------------------
// printBranch()
// -------------------------------
declare function printBranch {
  declare parameter thisPart.
  declare parameter indentText.

  // print that:
  print indentText + thisPart. // printing thispart's ToString().

  // recurse into the part's children:
  for childPart in thisPart:CHILDREN {
    printBranch( childPart, indentText + "  ").
  }.
}.
// -------------------------------
// printAllParts
// -------------------------------
declare function printAllParts {

  // this is a function rather than just being in the global code
  // because it's testing a feature - calling a function from another
  // function at its "sibling" nest level:  Args passed to the
  // function need to be fully dereferenced so the function being
  // called can see the values wihtout seeing the variables they were in.
  //

  // In this example, rPart is local to this function, but we're passing it
  // to printBranch(), which can't see rPart.  Therefore this only works
  // if rPart is being coorectly derferenced upon being put into an arg list,
  // which is what is being tested here.
  declare rPart to SHIP:ROOTPART.

  printBranch( rpart, ""). 

}

print "functest3 ran successfully.". // just to prove it worked. 
                                     // a real library would probably be
				     // silent and say nothing.

