// A small sample script to demonstrate the reference vector
// idea.

clearscreen.
print "Solar Prime Vector demo.".
print " ".
print "Use time-warp to watch things change.".
print "press action group 1 to quit.".

set refdraw to vecdraw().
set refdraw:color to white.
set refdraw:label to "SolarPrimeVector".
set refdraw:show to true.
set zerodraw to vecdraw().
set zerodraw:color to yellow.
set zerodraw:label to ship:body:name + "'s zero meridian".
set zerodraw:show to true.

until ag1 {
  set refdraw:start  to ship:body:position.
  set zerodraw:start to ship:body:position.
  set refdraw:vec  to 2500000*SOLARPRIMEVECTOR.
  set zerodraw:vec to 4*(latlng(0,0):position - ship:body:position).
  print ship:body:name + 
        ":ROTATIONANGLE = " + 
        round(ship:body:rotationangle,2) + " deg    " 
          at (0,1).
}.

set refdraw to 0.
set zerodraw to 0.
