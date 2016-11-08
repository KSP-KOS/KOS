.. _voice:

Voice
=====

This structure represents a single one of the :ref:`'voices' <skid_voice>`
in the built-in sound chip called :ref:`SKID <skid>` in the kOS CPU.
Please refer to the :ref:`SKID chip <skid>` documentation page if you
have not already done so before reading this page.  The things mentioned
here are just ways to access the features of the SKID chip so they won't
be fully explained on this page, instead just referring to the SKID
documentation with links.

.. _getvoice:

.. function:: GETVOICE(num)

    To access one of the :ref:`voices <skid_voice>` of the
    :ref:`SKID <skid>` chip, you use the ``GetVoice(num)`` built-in
    function.

    Where ``num`` is the number of the hardware voice you're interested
    in accessing.  (The numbering starts with the first voice being
    called 0).

Each voice is capable of playing one note at a time, or a series of 
notes from a song (a :struct:`List` of :struct:`Note`'s), but what
matters is that one voice can't play two notes at once.  To do that
you need to use multiple voices.  For simple one-voice situations,
you probably only need to ever use voice 0.

.. structure:: Voice

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 1 4

        * - Suffix
          - Type
          - Get/Set/Call
          - Description

        * - :attr:`ATTACK`
          - :struct:`scalar`
          - Get/Set
          - The *Attack* setting of the SKID voice's :ref:`ADSR Envelope <skid_envelope>`

        * - :attr:`DECAY`
          - :struct:`scalar`
          - Get/Set
          - The *Decay* setting of the SKID voice's :ref:`ADSR Envelope <skid_envelope>`

        * - :attr:`SUSTAIN`
          - :struct:`scalar`
          - Get/Set
          - The *Sustain* setting of the SKID voice's :ref:`ADSR Envelope <skid_envelope>`

        * - :attr:`RELEASE`
          - :struct:`scalar`
          - Get/Set
          - The *Release* setting of the SKID voice's :ref:`ADSR Envelope <skid_envelope>`

        * - :attr:`VOLUME`
          - :struct:`scalar`
          - Get/Set
          - The default volume to play the notes on this voice.

        * - :attr:`WAVE`
          - :struct:`string`
          - Get/Set
          - The name for the :ref:`waveform <skid_waveform>` you want this voice to use.

        * - :meth:`PLAY(note_or_list)`
          - n/a (call method only)
          - Call
          - The method that actually causes the voice to make some sound.

        * - :attr:`LOOP`
          - :struct:`boolean`
          - Get/Set
          - Whether or not the voice should keep re-playing the song that was queued with PLAY().

        * - :attr:`ISPLAYING`
          - :struct:`boolean`
          - Get/Set
          - Get: Is the ``PLAY()`` method still playing? Set: Make it false to abort PLAY().

        * - :attr:`TEMPO`
          - :struct:`scalar`
          - Get/Set
          - Stretches or shrinks the duration of the notes to speed up or slow down the song.


.. attribute:: Voice:ATTACK

    :access: Get/Set
    :type: :struct:`Scalar` in seconds

    The *Attack* setting of the SKID voice's
    :ref:`ADSR Envelope <skid_envelope>`.  This value is
    in seconds (usually a fractional portion of a second).

.. attribute:: Voice:DECAY

    :access: Get/Set
    :type: :struct:`Scalar` in seconds

    The *Decay* setting of the SKID voice's
    :ref:`ADSR Envelope <skid_envelope>`.  This value is
    in seconds (usually a fractional portion of a second).

.. attribute:: Voice:SUSTAIN

    :access: Get/Set
    :type: :struct:`Scalar` in the range [0..1] to multiply the volume by.

    The *Sustain* setting of the SKID voice's
    :ref:`ADSR Envelope <skid_envelope>`.  Unlike the other
    values in the ASDR Envelope, this setting is NOT a measure
    of time.  This is a coefficient to multiply the volume by
    during the sustain portion of the notes that are being played
    on this voice.  (i.e. 0.5 would mean "sustain at half volume").

.. attribute:: Voice:RELEASE

    :access: Get/Set
    :type: :struct:`Scalar` in seconds

    The *Release* setting of the SKID voice's
    :ref:`ADSR Envelope <skid_envelope>`.  This value is
    in seconds (usually a fractional portion of a second).
    Note, that in order for this setting to have any real
    effect, the notes that are being played have to
    have their ``KeyDownLength`` set to be shorter than their
    ``Duration``, otherwise the notes will still cut
    off before the Release has a chance to happen.

.. attribute:: Voice:VOLUME

    :access: Get/Set
    :type: :struct:`Scalar` 1.0 = max, 0.0 = silent.

    The "peak" volume of the notes played on this voice, when they
    hit the top of their initial spike in the
    :ref:`ADSR Envelope <skid_envelope>`.  While conceptually the
    max value is 1.0, in practice it can often go higher because
    the KSP game setting for User Interface volume is usually only
    at 50%, and in that scenario putting a 1.0 here would put the
    max at 50%, *really*.

.. attribute:: Voice:WAVE

    :access: Get/Set
    :type: :struct:`string` taken from the list of known waveforms in the hardware.

    To select which of the SKID chip's
    :ref:`waveform generators <skid_waveform>` you want this voice
    to use, set this to the string name of that waveform.  If you
    use a string that isn't one of the ones listed there (i.e.
    "triangle", "noise", "square", etc) then the attempt to set this
    value will be ignored and it will remain at its previous value.

.. method:: Voice:PLAY(note_or_list)

    :access: Call (method)
    :parameter note_or_list: Either one :struct:`Note` or a :struct:`List` of :struct:`Note`'s
    :type: n/a (method's return value isn't meaningful)

    To cause the SKID chip to actually emit a sound, you need to
    use this suffix method.  There are two ways it can be called:

    **Play just one note** : To play a single note, you can call
    PLAY(), passing it one note object.  Usually you construct
    the note object on the fly as you call Play, like so::

        SET V0 to GetVoice(0).
        V0:PLAY(NOTE(440,0.5)).

    **Play a list of notes** : To play a full list of notes (which
    could even encode an entire song), you can call PLAY, passing it
    a :struct:`List` of :struct:`Note`'s.  It will recognize that it
    is receiving a list of notes, and begin playing through them
    one at a time, only playing the next note when the previous
    note's ``:DURATION`` is finished::

        SET V0 to GetVoice(0).
        V0:PLAY(
            LIST(
                NOTE(440, 0.5),
                NOTE(400, 0.2),
                SLIDENOTE(410, 350, 0.3)
                )
            ).

    **Notes play in the background**:  In *either case*, whether
    playing a single note or a list of notes, the ``PLAY()``
    method will return immediately, *before even the first note
    has begun playing*.  It queues the note(s) to play, rather
    than waiting for them to finish.  This lets your main program
    continue doing its work without waiting for the sound to finish.

    **Calling PLAY() again on the same voice aborts the previous
    PLAY()**:  Because the notes play in the background, it's possible
    to execute another PLAY() call while a previous one hasn't
    finished its work yet.  If you do this, then the previous thing
    that was playing will quit, to be replaced by the new thing.

    **But PLAY() can be called simultaneously on different voices**:
    (In fact that's the whole point of having different voices.).
    Calling PLAY() again on a *different* voice number will not
    abort the previous call to PLAY().  It only aborts the previous
    PLAY() when it's being done on the *same* voice.

.. attribute:: Voice:LOOP

    :access: Get/Set
    :type: :struct:`boolean`

    If this is set to true, then the PLAY() method of this voice will
    keep on playing the same list of notes continually (starting over
    with the first note after the last note has finished).  Note that
    for the purpose of this, a play command that was only given a single
    note to play still counts as a 'song' that is one note long (i.e.
    it will keep repeating the same note continually).

.. attribute:: Voice:ISPLAYING

    :access: Get/Set
    :type: :struct:`boolean`

    **Get**: If this voice is currently playing a note or list of notes
    that was previously passed in to the ``PLAY()`` method, then this
    returns true.  Note that if :attr:`LOOP` is true, then this
    will never become false unless you set it to become false.

    **Set**: If you set this value to FALSE, that will force the voice
    to stop playing whatever it was playing, and shut it up.  (Setting
    it to true doesn't really mean anything.  It becomes true because
    the PLAY() method was called.  You can't restart a song just by
    setting this to true because when it becomes false, the voice
    "throws away" its memory of the song it was playing.)

.. attribute:: Voice:TEMPO

    :access: Get/Set
    :type: :struct:`scalar` (multiplier of durations of notes)

    When the voice is playing a :struct:`Note` or (more usefully) a
    :struct:`List` of :struct:`Note`'s, it will stretch or shrink the
    durations of those notes by multiplying them by this scaling
    factor.  At 1.0 (the default), that means that when a note 
    *says* it lasts for 1 second, then it really does.  But if 
    this tempo was set to, say 1.5, then that would mean that each
    time a note claims it wants to play for 1 second, it would really
    end up playing for 1.5 seconds on this voice.  (or if you set
    the tempo to 0.5, then all songs will play their notes at double 
    speed (each note only lasting half as long as it "should").)

    In other words, setting this to a value less than 1.0 will 
    speed up the song, and setting it to a value greater than 1.0
    will slow it down (which might be the opposite of what you'd
    expect with it being called "tempo", but what else should
    we have called it?  "slowpo"?)

    Changes to this value take effect as soon as the next note in
    the song starts. (You do not need to re-run the PLAY() method.
    It will change the speed in mid-song.)

    Be aware that this *only* scales the timings of the :struct:`Note`'s
    ``:KEYDOWNLENGTH`` and ``:DURATION`` timings.  It does not
    affect the timings in the :ref:`ADSR Envelope <skid_envelope>`, as
    those represent what are meant to be physical properties of the 
    "instrument" the voice is playing on.  This means if you set the
    tempo too fast, it will start cutting off the full duration of the
    "envelope" of the notes, if you are playing the notes with settings
    that have a slow attack or decay.
