core:doevent("Open Terminal").
set config:stat to true.
brakes on.
set song to list().
song:add(note("b4", 0.25, 0.20)). // Ma-
song:add(note("a4", 0.25, 0.20)). // -ry
song:add(note("g4", 0.25, 0.20)). // had
song:add(note("a4", 0.25, 0.20)). // a
song:add(note("b4", 0.25, 0.20)). // lit-
song:add(note("b4", 0.25, 0.20)). // -tle
song:add(note("b4", 0.5 , 0.45)). // lamb,
song:add(note("a4", 0.25, 0.20)). // lit-
song:add(note("a4", 0.25, 0.20)). // -tle
song:add(note("a4", 0.5 , 0.45)). // lamb
song:add(note("b4", 0.25, 0.20)). // lit-
song:add(note("b4", 0.25, 0.20)). // -tle
song:add(note("b4", 0.5 , 0.45)). // lamb
                              
song:add(note("b4", 0.25, 0.20)). // Ma-
song:add(note("a4", 0.25, 0.20)). // -ry
song:add(note("g4", 0.25, 0.20)). // had
song:add(note("a4", 0.25, 0.20)). // a
song:add(note("b4", 0.25, 0.20)). // lit-
song:add(note("b4", 0.25, 0.20)). // -tle
song:add(note("b4", 0.25, 0.20)). // lamb,
song:add(note("b4", 0.25, 0.20)). // Its
song:add(note("a4", 0.25, 0.20)). // fleece
song:add(note("a4", 0.25, 0.20)). // was
song:add(note("b4", 0.25, 0.20)). // white
song:add(note("a4", 0.25, 0.20)). // as
song:add(note("g4", 1   , 0.95)). // snow

set v0 to getvoice(0).

// Make the notes' starting and stopping a bit more gentle on the ear:
set v0:attack to 0.0333. // take 1/30 th of a second to max volume.
set v0:decay to 0.02.  // take 1/50th second to drop back down to sustain.
set v0:sustain to 0.80. // sustain at 80% of max vol.
set v0:release to 0.05. // taks 1/20th of a second to fall to zero volume at the end.

for wavename in LIST("square", "triangle", "sawtooth", "sine") { // Let's not do "noise" - it sounds dumb for music
  print "Playing song in waveform : " + wavename.
  set v0:wave to wavename.
  v0:play(song).
  wait until not v0:isplaying.
  wait 1.
}
