.. _skid:
.. _sound:

Using the Sound/Kerbal Interface Device (SKID)
==============================================

A hidden feature shippped with all kOS CPU's was uncovered recently.
It turns out all kOS CPU's come with a so-called SKID chip
(Sound/Kerbal Interface Device).  This little integrated chip can
send audio data through the terminal lines and play the sounds on
your terminal's speaker.  (Note, you can't hear the sound over
:ref:`telnet connections <telnet>`.)

This page will explain what we have discovered about how to use
this interesting little device.

.. contents::
    :local:
    :depth: 3

Quick and dirty sound example:
------------------------------

If you just want to hurry up and hear something, without understanding
all the features of the SKID chip, type the lines below into a program
file and run it::

    SET V0 TO GETVOICE(0). // Gets a reference to the zero-th voice in the chip.
    V0:PLAY( NOTE(400, 2.5) ).  // Starts a note at 400 Hz for 2.5 seconds.
                                // The note will play while the program continues.
    PRINT "The note is still playing".
    PRINT "when this prints out.".

If you wish to do something more complex than that, then read on:

Hardware Capabilities of SKID
-----------------------------

Below we describe, to the best of our knowledge, the sound capabilities
of the SKID chip.  Further below after this section, we'll describe
the programming interface we've added to Kerboscript to let you access
these abilities.  For now, don't think about how to access these
abilities, just look at what they are.

.. _skid_voice:

Voices
~~~~~~

The SKID chip contains up to 10 simultaneous "voices" that can
emit sounds together.  (The voices are numbered 0 through 9).
Each of these so-called "voices" can be set up with a particular
set of sound settings, which is like defining the properties of its
"instrument" .  Then it can be told to play one or more notes
using that set of settings.

Please don't be fooled into thinking these actually sound like
Kerbals speaking just because we called them "voices".  They're
just electronic bloopity-blopity sounds.

Frequency Range
~~~~~~~~~~~~~~~

The SKID chip appears to be capable of producing sounds in the
range of as low as 1 Hertz (not like you'd ever hear that) and
as high as up to 14000 Hertz.  Most people can hear higher notes
than that, but the chip's notes start to get a little distorted
at those frequencies.  At any rate, this range is plenty high
enough to hear the entire range of a piano's 88 notes, and then
some.

Amplitude (volume) Control
~~~~~~~~~~~~~~~~~~~~~~~~~~

The SKID chip can put out any range of volume levels between
0.0 and 1.0, within the accuracy of floating point numbers.
It is noted that it can also amplify the volume to levels
higher than 1.0, to boost the signal when needed, although
this feature is not well understood.

.. _skid_waveform:

Waveforms
~~~~~~~~~

Each voice can be set to one of several possible sound waveform
patterns.  As of this writing, our engineers have uncovered
5 such wave forms, displayed here.

These each produce a slightly different sound.

.. figure:: /_images/general/square.png
    :alt: Image of a square sound wave.

    ``"SQUARE"``: When this ``:WAVE`` setting is used, the
    sound is very electronic, and "beep"-like.  This is also
    the **default** wave a voice starts off with if you never
    changed the setting.

.. figure:: /_images/general/triangle.png
    :alt: Image of a triangle sound wave.

    ``"TRIANGLE"``: When this ``:WAVE`` setting is used, the
    sound is sort of halfway between sounding electronic and
    sounding mellow.

.. figure:: /_images/general/sawtooth.png
    :alt: Image of a sawtooth sound wave.

    ``"SAWTOOTH"``: When this ``:WAVE`` setting is used, the
    sound is a little bit like a rasping wasp.

.. figure:: /_images/general/sine.png
    :alt: Image of a sine sound wave.

    ``"SINE"``: When this ``:WAVE`` setting is used, the
    sound is a mellow and smooth tone, which also ends up
    seeming a bit quieter because of the smoother edges.

.. figure:: /_images/general/noise.png
    :alt: Image of a noise sound wave.

    ``"NOISE"``: When this ``:WAVE`` setting is used, the
    sound is like random static on a walkie-talkie.

.. _skid_envelope:

ADSR "Envelope"
~~~~~~~~~~~~~~~

The SKID chip will play a note in the background unattended
while the CPU continues with its work.  Thus you don't have
to have the main program pause while it plays a note.

When controlling the note, the SKID chip also can vary the
amplitude (volume) of the note over time, to simulate the
effects of several types of analog instruments.  It does
this using its "ADSR Envelope", (ADSR is shorthand for "Attack,
Decay, Sustain, and Release".)

.. figure:: /_images/general/envelope.png
    :alt: Image of the ADSR Envelope
    :align: right

    The ADSR envelope shown graphically.

When a natural analog instrument plays a note, there is often a
sharp "spike" of volume right at the start of the note, followed
by a slightly quieter volume while the note is being sustained,
followed by the volume fading down to zero when the note is
released.  The ADSR envelope lets you define this behavior by
adjusting these settings differently for each of the 10 voices
of the SKID chip.

Attack (a time setting)
    The Attack setting is a time, expressed in seconds (usually
    a fraction of a second), for how long a note takes to
    achieve its full volume in its initial first spike when it
    is played.  Note that the volume achieved at the top of
    this spike is the voice's default volume level.
    This time setting is not modified by :ref:`tempo <skid_tempo>`,
    because it represents the instrument's physical properties that
    don't change when the song goes faster.

Decay (a time setting)
    The Decay setting is a time, expressed in seconds (usually
    a fraction of a second), for how long a note takes to
    drop from its initial spike in volume down to its sustaining
    volume level.
    This time setting is not modified by :ref:`tempo <skid_tempo>`,
    because it represents the instrument's physical properties that
    don't change when the song goes faster.

Sustain (a volume setting)
    The Sustain setting is not a time, but a volume multiplier (
    usually some amount less than 1.0 but higher than 0.0).  It
    is the lesser volume level that the note drops to after the spike
    of volume defined by the Attack and Decay settings is over.
    The note will stay held here at this level until it is released.
    The reason this value isn't a time setting is because the
    duration of this period of the note varies depending on the note
    being played.

Release (a time setting)
    The Release setting is a time, expressed in seconds (usually
    a fraction of a second), for how long a note takes to
    fade from its Sustain volume level back down to zero again
    at the end of the note when the sustained duration of the note
    is over.
    This time setting is not modified by :ref:`tempo <skid_tempo>`,
    because it represents the instrument's physical properties that
    don't change when the song goes faster.

Default settings for the ADSR envelopes for all voices in the
SKID chip are:

* Attack = 0.0s
* Decay = 0.0s
* Sustain = 1.0
* Release = 0.1s

This produces a sound that will suddenly start but ever so slightly
fade at the end rather than dropping off immediately.  There is just
enough of a Release time to make the listener hear the sound as
slightly less harsh than a fast cutoff would sound.

It is possible to make the chip describe different shaped envelopes by
using degenerate values for some of these settings.  For some examples:

Below are settings you might use for a "staccato" type of instrument,
such as a drum, that is incapable of holding a sustained note and
instead just fades right away whenever you hit a note:

* Attack = 0.0s
* Decay = 0.2s   (Note decays immediatly on being struck)
* Sustain = 0.0  (and it decays to zero, so you can't "hold" the note).
* Release = 0.0s (Because Sustain is zero, this setting doesn't matter).

Below are settings you might use for an instrument with a strong
"wow wow" effect, where it takes time to reach full volume and you
have to hold a note in for a half second or so before you really hear it:

* Attack = 0.5s (takes a whole half second to reach full volume)
* Decay = 0s (stays at full volume once it's there)
* Sustain = 1.0 (stays at full volume once it's there)
* Release = 0.2 (and fades fairly fast when released, but not *immediately*).

.. _skid_letter_frequency:

Letter Frequency Notation
~~~~~~~~~~~~~~~~~~~~~~~~~

The SKID chip contains an interior lookup system that makes it possible,
anywhere the chip expects you to give a frequency in Hertz, to instead
specify its frequency using the more familiar
`"letter notes". <https://en.wikipedia.org/wiki/Scientific_pitch_notation>`_

To do this, you use a string in the following format:

1. Mandatory: First character is the note letter, one of
   "C","D","E","F","G","A", "B", or "R"(to mean "rest").
2. Optional: Followed by an optional character, "#" or "b" for "sharp" or
   "flat".  Note the ASCII characters hash ("#") and lower-case "B" ('b')
   are used for "sharp" and "flat" in place of the Unicode characters
   U+266F and U+266D (which are the more proper "sharp" and "flat"
   characters, but they are cumbersome to type on most keyboards.)
3. Mandatory: The last character is a digit indicating which octave
   number (0 through 7) this note is in.  (4 is the "middle" octave
   that starts with "middle C" on a piano keyboard.)

Examples:  ``"C4"`` is middle C.  ``"C#4"`` is the C-sharp just one half
step above middle C.  ``"Db4"`` is the D-flat that is in fact the same
thing as ``"C#4"``.  ``"B3"`` is the B that is just to the left of
middle C (Note the octave numbering starts with C at the bottom of the
octave, not A, thus why B3 is adjacent to C4.)

Note that in all cases where you communicate a frequency to the SKID
chip using one of these "letter notes", the SKID chip merely converts
these values into their equivalent Hertz value, and forgets the letter
string you used after that.  Thus if you set a Note's frequency to
"A4", and then immediately query its Frequency, you'll get 440 back,
not "A4".

Note that if you form a string indicating an unknown note in this
notation scheme (for example ``"E#5"``, when there is no such thing
as an E-sharp), the resulting frequency will just be zero Hertz, the
same as for a rest note.

.. _skid_note:

The Note format
~~~~~~~~~~~~~~~

When telling the SKID chip to begin a note, the following data is
passed into it, to let it know the parameters of that one note:

Frequency (In Hertz)
    Defines which note you mean, i.e. which of the keys on a
    synthesizer keyboard.  Be aware that if you set the frequency
    to zero, you end up with a note that is, essentially, a "rest",
    and makes no sound.  The frequency can also be expressed using
    :ref:`musical letter notation <skid_letter_frequency>`.
    If you define both a Frequency and an EndFrequency, then the
    Frequency is merely the initial frequency the note starts at.

EndFrequency (In Hertz)
    The SKID chip can emit "slide" notes where the note's frequency
    changes smoothly between a start and end frequency over the duration of
    the note without further intervention from the CPU's program.  To
    communicate to the chip that this is your intent, you can give it
    a note which has an EndFrequency value that differs from its
    Frequency value.  When you do this, the note's normal frequency
    value is merely its "start" frequency.  The SKID chip will change
    the note's frequency either upward or downward as needed depending
    on whether the EndFrequency is higher or lower than the initial
    frequency of the note.  It will also change the frequency as
    quickly or slowly as needed to make the frequency reach EndFrequency
    at exactly the moment the note's Duration runs out.  (So giving it
    a shorter Duration makes it change frequencies faster).

Duration (Seconds, modified by :ref:`tempo <skid_tempo>`)
    Defines how long this entire note lasts from the start of its
    Attack until the end when the next note can start.  Note that
    by default, unless you choose to set the KeyDownLength (see
    next item below) to something other than the default, the note
    won't quite fill the entire Duration, instead reserving a small
    sliver of the end of the Duration to represent the gap between
    this note and the next.

KeyDownLength (Seconds, modified by :ref:`tempo <skid_tempo>`)
    Defines how long you imagine the "key" on a synthesizer keyboard is
    being held down for to produce this note. In terms of the
    ADSR Envelope, this is the time span that includes the Attack,
    Decay, and Sustain portion of the note, but not the Release portion
    of the note.  The KeyDownLength must be less than or equal
    to the Duration (see above).  If you try to set a KeyDownLength that
    exceeds the Duration, it will be shortened to match the Duration.
    Essentially the Difference between Duration and KeyDownLength is
    that Duration is how much time the note fills up of the song, and
    KeyDownLength is how much of that time is spent with the finger
    holding the "synthesizer key" down.  The time between the end of
    the KeyDownLength and the end of the Duration is the gap of time
    from when one key is let go and the next key is begun.  If there
    is no such gap, then two adjacent notes of the same frequency would
    just bleed together into sounding like one continuous note.

    By default, the KeyDownLength is slightly shorter than the Duration
    if you don't specify it explicitly.

    The Release portion of the ADSR Envelope occurs entirely within the
    gap between the end of KeyDownLength and Duration.  If you define
    the KeyDownLength to last the entire Duration, then you won't hear
    the Release portion of the note's envelope, because the note will
    cut off before it has a chance to start the release.

Volume (between 0.0 and 1.0, although it can go higher than 1.0)
    A multiplier for the volume of this one note relative to the overall
    volume of the voice on which it is playing.  By setting it differently
    per note, it's possible to play a song in which some notes are quieter
    than others.  This can go above 1.0 if the main volume is less than 1.0.
    But it is probably a good idea to make sure that this volume times
    the voice's main volume doesn't exceed 1.0 or you might get some
    "audio compression" effects that slightly distort the sound.

.. _skid_tempo:

Tempo
~~~~~

The SKID chip allows you to set a tempo multiplier (a coefficient
multiplier that can be a fractional number like 0.5, 2.0, 1.1, etc).
This tempo multiplier causes all sound durations mentioned in specific
notes to be sped up or slowed down by multiplying them by this
number.  It can be thought of as "how long is one second's worth
of sheet music going to last when played on this chip?"  If you set
it to 0.5, then each second's worth of time in a Note's Duration
or KeyDownLength fields only lasts half a second for real, thus
causing the notes to play twice as fast.  Conversely, if you set
this to 2.0, it makes notes take twice as long to play, thus
slowing down the tempo.

It should be possible to transcribe sheet music into the note format
the SKID chip uses, by simply using a note Duration of 0.25 for
"quarter note", 0.5 for "half note", 1.0 for "whole note", and so on,
and then setting the chip's Tempo to set how long you mean for a whole
note to last.

.. _skid_song:

Songs (Lists of Notes)
~~~~~~~~~~~~~~~~~~~~~~

It is possible to point the SKID chip at an array of these
:ref:`notes <skid_note>` to make it play several notes back to
back without further intervention of the main CPU.  When the
chip is given such a list of notes, it is a bit like feeding
"sheet music" to the chip, and letting it play the song itself.

This is where the settings such as :ref:`tempo <skid_tempo>`
become quite relevant.  The SKID chip will simply play the
notes it sees in the order they're listed, waiting for one
note to finish its Duration before the next note is started.
By changing the Tempo setting, you can speed up the song
that is in the current note list without changing the
definitions of the individual notes in the song.

Note that any note with a frequency set to zero counts as a
"rest", which is useful to know when encoding a song into a
list of notes.

It is unknown if there is a limit to how long a list of notes can
be.  In our testing, the engineers haven't discovered an upper
limit yet.

.. _skid_loop:

Looping
~~~~~~~

The SKID chip contains a flag that can be used to set whether or not
it should start again with the first note in a list when it
reaches the last note in the list.  If this flag is true, then it
will continue playing the song list forever and ever until made to stop.

Note that even a single note counts as a "song" for the purpose of
this looping flag.  Yes, the SKID chip can play the same note over and
over if that's what you really want.

.. _skid_chords:

Chords
~~~~~~

Each of the 10 voices of a SKID chip can only play a single note at a
time.  But you may wish to transcribe some music into a song (list of
notes) in which the notes of the sheet music contained chords - that
is to say, cases where more than one note is supposed to be played
simultaneously.  The SKID chip can support this, but the way to do
it is a little bit messy.

In order to play something that has chords, you need to imagine that
each of the SKID chip's voices can be a different finger of a
synthesizer keyboard player.  Let's say you want to play a song that has
some 3-note chords in it.  At minimum, a keyboard player would need
3 fingers to accomplish this.  To do this with SKID, you'd need to dedicate
3 of the voices to the job, and make sure all 3 voices are given the
same settings (Waveform, ADSR Envelope, Tempo, Volume, etc).  With one
of these voices, you play all the time, all the notes.  (You give it
a :ref:`song <skid_song>` that consists of all the notes in the sheet
music).  With the other two voices, you'd give them songs (note lists)
that contain rests in all the places where there is only a singleton
note being played, but then have them join in with with the extra notes
to add to the main voice's note in the places where a chord should be
played.


Kerboscript Interface Synopsis
------------------------------

In order that you don't need to send individual bits and bytes to the
SKID chip, we've added a user interface in the Kerboscript language
that interfaces with all these features for you.  Keep the above
technical specs in mind so you know what the settings you're
changing do.

The documentation below is just a quick synopsis of how to use
the kerboscript interface to SKID.  To really fully exploit it, you
need to follow the links below and read the detailed documentation
on the :struct:`Note` structure and the :struct:`Voice` structure.


.. _skid_getvoice:

GetVoice()
~~~~~~~~~~

The basic starting point of any Kerboscript program that works with
the SKID chip is the :func:`GetVoice()` built-in function.
``GetVoice(n)``, given any N within the range of 0 through 9 for the
10 voices in the SKID chip, returns a handle you can use to send
commands to that voice of the SKID chip.

For simple easy examples, you can just use voice 0 for most of your
needs::

    SET V0 to GetVoice(0).

``GetVoice()`` returns an object of type :struct:`Voice`, that can be
used to do everything else you need after that.

Voice object
~~~~~~~~~~~~

If you take a moment to look at the documentation for :struct:`Voice`,
you can see that almost everything it lets you do has a one-to-one
correspondence to the hardware features mentioned above in this document.

All the features of the SKID chip can be set this way, and have a
suffix that corresponds to them, as given in the example below::

    SET V0:VOLUME TO 0.9.
    SET V0:WAVE to "sawtooth".
    SET V0:ATTACK to 0.1.
    SET V0:DECAY to 0.2.
    SET V0:SUSTAIN to 0.7. // 70% volume while sustaining.
    SET V0:RELEASE to 0.5. // takes half a second to fade out.

Note()
~~~~~~

When asking one of SKID's voices to play a note, you have to specify
which note you meant, and you do so by constructing a :struct:`Note`
object using the :func:`Note()` built-in function, or the
:func:`SlideNote()` built-in function::

    // N1 is a note (also at 440 Hz because that's what "A4" means)
    // that lasts 1 second overall, but only 0.8 seconds of it
    // are "key down" time (i.e. the A,D,S part of the ADSR Envelope).
    SET N1 to NOTE("A4", 0.8, 1).

    // N2 is a note that slides from the A note in one octave to the A note
    // in the next octave up, over a time span of 0.3 seconds.
    // (The last 0.05 seconds of which are "release" time you won't hear
    // if you have the voice's RELEASE value set to zero.):
    SET N2 to SLIDENOTE("A4", "A5", 0.25, 0.3).

Once a note has been constructed, it's components are not changable.  The
only way to change the note is to make a new note and use it to overwrite
the previous note.

For that reason, it's typical not to bother storing the result of a Note()
or SlideNote() constructor in a variable as shown above, and instead just
pass it right into the `Play()` method, or to make it part of a
:struct:`List` of notes for making a song.

Voice:Play()
~~~~~~~~~~~~

The heart of the Kerboscript interface to the SKID chip is the `Play()`
suffix method of the :struct:`Voice` object.

You either construct a single :struct:`Note` and tell Play() to play it,
or you construct a :struct:`List` of :struct:`Note`'s and tell Play()
to play them.

Examples::

    SET V0 TO GetVoice(0).
    V0:PLAY( NOTE( 440, 1) ).  // Play one note at 440 Hz for 1 second.

    // Play a 'song' consisting of note, note, rest, sliding note, rest:
    V0:PLAY(
        LIST(
            NOTE("A#4", 0.2,  0.25), // quarter note, of which the last 0.05s is 'release'.
            NOTE("A4",  0.2,  0.25), // quarter note, of which the last 0.05s is 'release'.
            NOTE("R",   0.2,  0.25), // rest
            SLIDENOTE("C5", "F5", 0.45, 0.5), // half note that slides from C5 to F5 as it goes.
            NOTE("R",   0.2,  0.25)  // rest.
        )
    ).
