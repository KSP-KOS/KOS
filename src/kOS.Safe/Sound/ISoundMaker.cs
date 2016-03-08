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
        /// Load the given sound file into the set of sounds the soundmaker knows how to play,
        /// giving it a name to refer to it in the future.
        /// </summary>
        /// <param name="soundName">name to refer to this sound in the future.</param>
        /// <param name="soundFileURL">URL of where to obtain the sound clip file from.</param>
        void LoadSound(string soundName, string soundFileURL);
        
        /// <summary>
        /// Attempt to play the given sound clip by its name.  Note that
        /// it is impossible to play the same sound clip multiple times on
        /// top of itself, with the system ISoundMaker is using.  Therefore
        /// if the sound cannot be played yet because it's arleady being played,
        /// it will return false.
        /// This call is non-blocking.  It will only begin the sound and let it
        /// play in the background, returning immediately.  It will not wait for
        /// the sound clip to finish playing.
        /// </summary>
        /// <param name="soundName">string that was given to LoadSound() earlier</param>
        /// <returns>True if the sound has begun playing.  False if it has to wait.</returns>
        bool BeginSound(string soundName);        
    }
}
