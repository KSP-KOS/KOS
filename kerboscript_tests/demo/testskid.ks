core:doevent("Open Terminal").
set config:stat to true.
brakes on.
set song to list().
wait 2.
song:add(note("b4", 0.25)). // Ma-
song:add(note("a4", 0.25)). // -ry
song:add(note("g4", 0.25)). // had
song:add(note("a4", 0.25)). // a
song:add(note("b4", 0.25)). // lit-
song:add(note("b4", 0.25)). // -tle
song:add(note("b4", 0.5)). // lamb,
song:add(note("a4", 0.25)). // lit-
song:add(note("a4", 0.25)). // -tle
song:add(note("a4", 0.5)). // lamb
song:add(note("b4", 0.25)). // lit-
song:add(note("b4", 0.25)). // -tle
song:add(note("b4", 0.5)). // lamb

song:add(note("b4", 0.25)). // Ma-
song:add(note("a4", 0.25)). // -ry
song:add(note("g4", 0.25)). // had
song:add(note("a4", 0.25)). // a
song:add(note("b4", 0.25)). // lit-
song:add(note("b4", 0.25)). // -tle
song:add(note("b4", 0.25)). // lamb,
song:add(note("b4", 0.25)). // Its
song:add(note("a4", 0.25)). // fleece
song:add(note("a4", 0.25)). // was
song:add(note("b4", 0.25)). // white
song:add(note("a4", 0.25)). // as
song:add(note("g4", 1)). // snow

song:add(note("b4", 0.25)). // Ev-
song:add(note("a4", 0.25)). // -ry
song:add(note("g4", 0.25)). // where
song:add(note("a4", 0.25)). // that
song:add(note("b4", 0.25)). // Mar-
song:add(note("b4", 0.25)). // -y
song:add(note("b4", 0.5)). // went,
song:add(note("a4", 0.25)). // Mar-
song:add(note("a4", 0.25)). // -y
song:add(note("a4", 0.5)). // went
song:add(note("b4", 0.25)). // Mar-
song:add(note("b4", 0.25)). // -y
song:add(note("b4", 0.5)). // went

song:add(note("b4", 0.25)). // Ev-
song:add(note("a4", 0.25)). // -ry
song:add(note("g4", 0.25)). // where
song:add(note("a4", 0.25)). // that
song:add(note("b4", 0.25)). // Mar-
song:add(note("b4", 0.25)). // -y
song:add(note("b4", 0.25)). // went,
song:add(note("b4", 0.25)). // The
song:add(note("a4", 0.25)). // lamb
song:add(note("a4", 0.25)). // was
song:add(note("b4", 0.25)). // sure
song:add(note("a4", 0.25)). // to
song:add(note("g4", 1)). // go

wait 2.

set v0 to getvoice(0).
set v0:wave to "square".
v0:play(song).
wait until not v0:isplaying.
wait 1.
set v0:wave to "triangle".
v0:play(song).
wait until not v0:isplaying.
wait 1.
set v0:wave to "sawtooth".
v0:play(song).
wait until not v0:isplaying.
wait 1.
set v0:wave to "sine".
v0:play(song).
wait until not v0:isplaying.
