print "Should simply print if trajectories mod is available.".
print "Available should mean that the mod is installed.".
print "An error thrown should mean that the addon itself is not compiled.".
print "Any other deviation means that the test is not working.".
if (addons:tr:available) {
    print "Trajectories addon is available!".
}
else {
    print "Trajectories addon not available!".
}
