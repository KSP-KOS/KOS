Sound Sources
-------------

These files are the sound effects in the terminal window
(and perhaps more later?).

In keeping with the idea that every *source* from which the final
product is made should be in github, these are the configurations
in the Audacity editor that were used to export the WAV files used.

The program *Audacity* is free and can be downloaded for Windows,
Mac, or Linux.

They are not part of the automated build system, so you will have
to manually export new WAV files if you change them.  To export
a WAV file, run Audacity, open one of the ``*.aup`` files here,
and choose File|Export.

Files so far:

*terminal-click*: A very short "pop" sound that only lasts 1/100th of
a second, so it can be used with fast typing for the keyclick feature.

*terminal-beep*: The sound for the BEL character in the GUI terminal.
It's a single-tone basic simple square waveform to simulate primitive
sound output on early comptuers.

*error*: The sound warning the user that a KOSException happens.  It is 
NOT part of the GUI terminal because it can fire off even when the
terminal is closed.  It just fires for any KOSException.  If you hear it
when you weren't expecting it, you might want to go have a look at the
log file.  The error sound is just a square waveform that starts high
pitched and then drops to a lower, near-raspberry pitch halfway through.

