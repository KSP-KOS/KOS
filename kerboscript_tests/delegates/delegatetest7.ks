// Tests using system callbacks with pre-bound args.
// This test was added for issue #2236

local vd is vecdraw(v(0,0,0), ship:north:vector * 10, white, "test vector", 1, true).
function update_color {
  parameter base_color.
  return rgb(base_color:r+random()*0.2, base_color:g+random()*0.2, base_color:b+random()*0.2).
}
print "Now flickering vector color blue-ish-ly for 5 seconds.".
set vd:colorupdater to update_color@:bind(rgb(0.2,0.2,0.7)).
wait 5.
print "Now flickering vector color red-ish-ly for 5 seconds.".
set vd:colorupdater to update_color@:bind(rgb(0.7,0.2,0.2)).
wait 5.
local vd is 0.
print "Done.  If it got here without dying, the issue is fixed.".
