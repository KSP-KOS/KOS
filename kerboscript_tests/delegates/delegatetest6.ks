// NOTE THIS TEST DOESN'T WORK AS EXPECTED YET
// BECAUSE THERE"S PROBABLY OTHER PLACES WHERE
// WE HAVE REFERENCES HOLDING MEMORY (NOT JUST
// WITH USERDELEGATES).  BUT IT"S A GOOD TEST TO
// KEEP AROUND SO WE CAN USE IT LATER WHEN FINDING
// THOSE PROBLEMS.


PRINT "===========================================".
PRINT "A test that delegate data orphans properly.".
PRINT "THIS TEST WILL ADD ABOUT 1 GiB to KSP's FOOTPRINT TEMPORARILY".
PRINT "===========================================".
PRINT "This test will require you to type something when it's done.".
PRINT "===========================================".
PRINT "Before you begin, go look at the memory footprint of KSP at the moment.".
PRINT "Write down what you see, down to the nearest 1 MiB.".
print "(Press any key to continue)".
terminal:input:getChar().
PRINT "Okay, now making 5 delegates, each of which has a big pile of data in it's closure".
PRINT "This could take a while".

set g_dels to LIST().
for i in range(0,4) {
  print "======= Making g_dels[" + i + "] ========".
  g_dels:add( make_delegate() ).
}

print "Okay, now this program is holding onto a large pile of data in user closures.".
print "Check the KSP memory footprint now.  It should be much bigger.".
print "Write down the new number.".
print "(Press any Key to continue).".
terminal:input:getChar().
print "Okay, now the program is going to end,".
print "But the delegates still exist in the.".
print "variable 'g_dels'.".
print "If garbage collecting works right, ".
print "you should get an error when you try ".
print "to run them (i.e. g_dels[0]:call(). )".
print "And you should notice the memory footprint".
print "of KSP fall back down again to where it".
print "started, after a minute or so when the ".
print "GC gets around to it.".

print 1/0.

function make_delegate {

   // Make this eat a lot of data quickly: by nesting lists in lists in lists:
   print "Building large pile of junk local data for the closure".
   local str is "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa".
   // Make string longer in a powers-of-2 exponentialy fast way:
   for i in range(0,20) {
     set str to str + str.
   }
   print "Made a local string of length: " + str:length.

   // Now make a user delegate that holds all of that mess in its closure:
   return { print "Proof I have a handle on the string: It is of type " + str:length. }.
}
