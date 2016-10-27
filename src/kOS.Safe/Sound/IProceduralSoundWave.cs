using System.Collections.Generic;
using System;

namespace kOS.Safe.Sound
{
	public interface IProceduralSoundWave
	{
		void InitSettings();
		float SampleFunction(float t);
		int Periods { get; set; }
		/// <summary>
		/// The Unity AudioClip of the wave form.  This must be stored as generic object
		/// instead of the right type so that the kOS.Safe.Sound.IVoice can have this in the
		/// interface without being aware of the Unity type AudioClip.
		/// </summary>
		object Clip { get; set; }
	}
}
