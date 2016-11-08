Note
====

Notes are structures that get passed in to the
:ref:`SKID chip <skid>` to tell it what tone to play
and for how long.

All the suffixes of a :struct:`Note` are read-only,
and you're only ever expected to set them by
constructing a new Note using the built-in Note()
function, or the built-in SlideNote() function:

.. _note:

.. function:: NOTE(frequency, keyDownLength, duration, volume)

    This global function creates a note object from the given values.

    where:

    ``frequency``
        **Mandatory**: The frequency can be given as either a number or a
        string.  If it is a number, then it is the frequency in Hertz.
        If it is a string, then it's using the letter notation
        :ref:`described here <skid_letter_frequency>`.
    ``keyDownLength``
        **Mandatory**: Specifies how long the "synthesizer key" is being
        held down for for this note.  Note that the value here gets
        multiplied by the voice's ``:TEMPO`` to decide the actual
        duration in seconds when it gets played.
    ``duration``
        **Optional**: If present, it means the actual duration of the note
        is longer than keyDownLength, and should include some additional
        time after the key is released (to give the note time to fade
        in its Release portion of the :ref:`ADSR envelope <skid_envelope>`.)
        If *duration* is not present, then the note cuts off instantly
        at the end of keyDownLength.  If *duration* is present, then
        it must be at least greater than or equal to *keyDownLength*,
        or else *keyDownLength* will be shortened to match the duration.
        Note that the value here gets multiplied by the voice's
        ``:TEMPO`` to decide the actual duration in seconds when it gets played.
    ``volume``
        **Optional**: If present, then the note can be given a different
        volume than the default for the voice it's being played on, to
        make it louder or quieter than the other notes this voice is
        playing.  This setting is a relative multiplier applied to the
        voice's volume. (i.e. 1.0 means play at the same volume as the
        voice's setting, 1.1 means play a bit louder than the voice's
        setting, and 0.9 means play a bit quieter than the voice's
        setting).

    This is an example of it being used in conjunction with the Voice's
    PLAY() suffix method::

        SET V1 TO GETVOICE(0).
        V1:PLAY( NOTE(440, 0.2, 0.25, 1) ).

.. _slidenote:

.. function:: SLIDENOTE(frequency, endFrequency, keyDownLength, duration, volume)

    This global function creates a note object that makes a sliding note
    that changes linearly from the start frequency to the end frequency
    across the duration of the note.

    where:

    ``frequency``
        **Mandatory**: This is the frequency the sliding note begins at.
        If it is a number, then it is the frequency in Hertz.
        If it is a string, then it's using the letter notation
        :ref:`described here <skid_letter_frequency>`.
    ``endFrequency``
        **Mandatory**: This is the frequency the sliding note ends at.
        If it is a number, then it is the frequency in Hertz.
        If it is a string, then it's using the letter notation
        :ref:`described here <skid_letter_frequency>`.
    ``keyDownLength``
        **Mandatory**: Same as the keyDownLength for the :ref:`NOTE() <note>`
        built-in function.
    ``duration``
        **Optional**: Same as the duration for the :ref:`NOTE() <note>`
        built-in function.  If it is missing it will be the same thing
        as the keyDownLength.
    ``volume``
        **Optional**: Same as the volume for the :ref:`NOTE() <note>`
        built-in function.

    The note's frequency will change linearly from the starting to
    the ending frequency over the note's duration.  (For example, If the
    duration is shorter, but all the other values are the kept the same,
    that makes the frequency change go faster so it can all fit within the
    given duration.)

    You can make the note pitch up over time or pitch down over time
    depending on whether the endFrequency is higher or lower than
    the initial frequency.

    This is an example of it being used in conjunction with the Voice's
    PLAY() suffix method::

        SET V1 TO GETVOICE(0).
        // A fast "whoop" sound that pitches up from 300 Hz to 600 Hz quickly:
        V1:PLAY( SLIDENOTE(300, 600, 0.2, 0.25, 1) ).

.. structure:: Note

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :FREQUENCY
          - :struct:`scalar`
          - frequency of the note's start in Hertz
        * - :ENDFREQUENCY
          - :struct:`scalar`
          - If a SLIDENOTE, the frequency of the note's end in Hertz
        * - :KEYDOWNLENGTH
          - :struct:`scalar`
          - time to hold the "synthesizer key" down for
        * - :DURATION
          - :struct:`scalar`
          - total time of the note including any extra for "Release" time
        * - :VOLUME
          - :struct:`scalar`
          - multiplier for how loud this note is relative to others played on
            this voice (1.0 means "same volume")

**None of the above suffixes are set-able**.  The only way to set them
is to construct a new note using the :ref:`Note <note>` function.
