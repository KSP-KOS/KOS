using System;

namespace kOS.Safe.Sound
{
    /// <summary>
    /// The ISoundMaker is the interface which will handle all sound effects
    /// for the whole mod, no matter how many kOS PartModules there are.
    /// It consolidates all sound clips into this one instance.  A non-Unity variant
    /// can be made which will probably just ignore all these requests
    /// and do nothing with them (but still be compilable), for use with
    /// Safe testing.
    /// </summary>
    public interface ISoundMaker
    {
        /// <summary>
        /// Attempt to play the given sound clip by its name.  Note that
        /// it is impossible to play the same sound clip multiple times on
        /// top of itself, with the system ISoundMaker is using.  Therefore
        /// if the sound cannot be played yet because it's arleady being played,
        /// it will return false.
        /// This call is non-blocking.  It will only begin the sound and let it
        /// play in the background, returning immediately.  It will not wait for
        /// the sound clip to finish playing.
        /// Begin a sound without messing with its duration or frequency.
        /// This should only be used with sound samples that have finite
        /// duration encoded into them, like sound files, and not with
        /// ProceduralSoundWave sounds that emit the generated wave
        /// forever until told to stop.        /// </summary>
        /// <param name="soundName">string that was given to LoadSound() earlier</param>
        /// <returns>True if the sound has begun playing.  False if it has to wait.</returns>
        bool BeginSound(string soundName, float volume = 1f);

        /// <summary>
        /// Begin a single note from a sound source and specify its parameters.
        /// If using a ProceduralSoundWave sound, it must be played with this variant
        /// of BeginSound (not the simpler variant) because those wave generators
        /// have no specified end time and just emit the wave forever. (Thus the sound
        /// would never stop if you told it to play out the whole "clip".)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="frequency"></param>
        /// <param name="duration"></param>
        /// <param name="volume"></param>
        /// <returns></returns>
        bool BeginSound(string name, float frequency, float duration, float volume = 1f);
    }
}
