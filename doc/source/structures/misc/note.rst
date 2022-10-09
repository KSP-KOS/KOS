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

.. function:: NOTE(frequency, duration, keyDownLength, volume)

    This global function creates a note object from the given values.

    where:

    ``frequency``
        **Mandatory**: The frequency can be given as either a number or a
        string.  If it is a number, then it is the frequency in hertz (Hz).
        If it is a string, then it's using the letter notation
        :ref:`described here <skid_letter_frequency>`.
    ``duration``
        **Mandatory**: The total amount of time the note takes up before
        the next note can begin, *including* the small gap between the end
        of its keyDownLength and the start of the next note.
        Note that the value here gets multiplied by the voice's
        :meth:`TEMPO<Voice:TEMPO>` to decide the actual duration in seconds when
        it gets played.
    ``keyDownLength``
        **Optional**: The amount of time the note takes up before the
        "synthesizer key" is released.  In terms of the
        :ref:`ADSR Envelope <skid_envelope>`, this is the portion of
        the note's time taken up by the Attack, Decay, and Sustain part
        of the note, but not including the Release part of the note.  In
        order to hear the note fade away during its Release portion, the
        keyDownLength must be shorter than the Duration, or else there's
        no gap of time to fit the release in before the next note starts.
        By default, if you leave the KeyDownLength off, you get a default
        KeyDownLength of 90% of the Duration, leaving 10% of the Duration
        left to hear the "Release" time before the next note starts.
        If you wish to force the notes to immediately blend from one to the
        next with no audible gaps between them, then for each note you
        need to specify a keyDownLength that is equal to the Duration.
        Note that the value here gets
        multiplied by the voice's :meth:`TEMPO<Voice:TEMPO>` to decide the actual
        duration in seconds when it gets played.
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
    :meth:`PLAY()<Voice:PLAY>` suffix method::

        SET V1 TO GETVOICE(0).
        V1:PLAY( NOTE(440, 0.2, 0.25, 1) ).

.. _slidenote:

.. function:: SLIDENOTE(frequency, endFrequency, duration, keyDownLength, volume)

    This global function creates a note object that makes a sliding note
    that changes linearly from the start frequency to the end frequency
    across the duration of the note.

    where:

    ``frequency``
        **Mandatory**: This is the frequency the sliding note begins at.
        If it is a number, then it is the frequency in hertz (Hz).
        If it is a string, then it's using the letter notation
        :ref:`described here <skid_letter_frequency>`.
    ``endFrequency``
        **Mandatory**: This is the frequency the sliding note ends at.
        If it is a number, then it is the frequency in hertz (Hz).
        If it is a string, then it's using the letter notation
        :ref:`described here <skid_letter_frequency>`.
    ``duration``
        **Mandatory**: Same as the duration for the :func:`NOTE()`
        built-in function.  If it is missing it will be the same thing
        as the keyDownLength.
    ``keyDownLength``
        **Optional**: Same as the keyDownLength for the :func:`NOTE()`
        built-in function.
    ``volume``
        **Optional**: Same as the volume for the :func:`NOTE()`
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

        * - :attr:`FREQUENCY`
          - :struct:`Scalar`
          - initial frequency of the note in hertz (Hz)
        * - :attr:`ENDFREQUENCY`
          - :struct:`Scalar`
          - final frequency of the note in hertz (Hz)
        * - :attr:`KEYDOWNLENGTH`
          - :struct:`Scalar`
          - time to hold the "synthesizer key" down for
        * - :attr:`DURATION`
          - :struct:`Scalar`
          - total time of the note including
        * - :attr:`VOLUME`
          - :struct:`Scalar`
          - multiplier for how loud this note is relative to others notes

    .. attribute:: FREQUENCY

        :access: Get Only
        :type: :struct:`Scalar` (Hz)

        The initial frequency of the note in hertz (Hz).

    .. attribute:: ENDFREQUENCY

        :access: Get Only
        :type: :struct:`Scalar` (Hz)

        If the note was created using :func:`SlideNote()` this is the final
        frequency of the note, in hertz (Hz).  Otherwise the value is identical to
        :attr:`FREQUENCY`.

    .. attribute:: KEYDOWNLENGTH

        :access: Get Only
        :type: :struct:`Scalar` (s)

        The amount of time that the "synthesizer key" is held down for.  In the
        :ref:`ADSR Envelope<skid_envelope>` this represents the total of the
        "attack", "decay", and "sustain" components.

    .. attribute:: DURATION

        :access: Get Only
        :type: :struct:`Scalar` (s)

        The total time of the note, encompassing the entire
        :ref:`ADSR Envelope<skid_envelope>` including the "release" component.

    .. attribute:: VOLUME

        :access: Get Only
        :type: :struct:`Scalar`

        The multiplier which effects how loud this note is relative to other
        notes played on this voice.  Smaller values are quieter an larger values
        are louder.  While values greater than 1 are allowed, increasing this
        value excessively may result in audio distortion.
