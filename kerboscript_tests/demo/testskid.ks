core:doevent("Open Terminal").
set config:stat to true.
brakes on.
set song to list().
song:add(note("b4", 0.20, 0.25)). // Ma-
song:add(note("a4", 0.20, 0.25)). // -ry
song:add(note("g4", 0.20, 0.25)). // had
song:add(note("a4", 0.20, 0.25)). // a
song:add(note("b4", 0.20, 0.25)). // lit-
song:add(note("b4", 0.20, 0.25)). // -tle
song:add(note("b4", 0.45, 0.5)). // lamb,
song:add(note("a4", 0.20, 0.25)). // lit-
song:add(note("a4", 0.20, 0.25)). // -tle
song:add(note("a4", 0.45, 0.5)). // lamb
song:add(note("b4", 0.20, 0.25)). // lit-
song:add(note("b4", 0.20, 0.25)). // -tle
song:add(note("b4", 0.45, 0.5)). // lamb

song:add(note("b4", 0.20, 0.25)). // Ma-
song:add(note("a4", 0.20, 0.25)). // -ry
song:add(note("g4", 0.20, 0.25)). // had
song:add(note("a4", 0.20, 0.25)). // a
song:add(note("b4", 0.20, 0.25)). // lit-
song:add(note("b4", 0.20, 0.25)). // -tle
song:add(note("b4", 0.20, 0.25)). // lamb,
song:add(note("b4", 0.20, 0.25)). // Its
song:add(note("a4", 0.20, 0.25)). // fleece
song:add(note("a4", 0.20, 0.25)). // was
song:add(note("b4", 0.20, 0.25)). // white
song:add(note("a4", 0.20, 0.25)). // as
song:add(note("g4", 0.95, 1)). // snow

set v0 to getvoice(0).

// Make the notes' starting and stopping a bit more gentle on the ear:
set v0:attack to 0.02.
set v0:release to 0.05.

for wavename in LIST("square", "triangle", "sawtooth", "sine") { // Let's not do "noise" - it sounds dumb for music
  print "Playing song in waveform : " + wavename.
  set v0:wave to wavename.
  v0:play(song).
  wait until not v0:isplaying.
  wait 1.
}
