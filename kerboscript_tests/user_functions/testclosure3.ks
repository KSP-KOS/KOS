// Test of lock closures in which the thing being locked
// is cooked control values 'steering' and 'throttle':


// silly example to set the throttle to a
// random value within a clamped range:
declare function set_throt_to_clamped_random {
  declare parameter minVal, maxVal.

  declare variance to random().

  lock throttle to minVal + (maxVal-minVal)*variance.
}.

// silly example to set the steering to a 
// random rotation:
declare function set_steering_random
{
  declare xrot to random()*360.
  declare yrot to random()*360.
  declare zrot to random()*360.

  lock steering to R(xrot,yrot,zrot).
}

print "Fiddling with the controls randomly for a few seconds:".
set iteration to 0.
until iteration >= 3 {
  set_throt_to_clamped_random(0,0.5).
  set_steering_random.
  print "Keeping these new values for 5 seconds:".
  print "  trottle = " + round(throttle, 3) + " (clamped from 0 to 0.5).".
  print "  steering = " + steering.
  wait 5.
  set iteration to iteration + 1.
}.

print "Now leaving controls alone at whatever they were.".
print "For 10 seconds before quitting program.".
wait 10.
