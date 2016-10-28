using System;
using kOS.Safe.Sound;


namespace kOS.Safe.Sound
{
	public interface IVoice
	{
		void Awake();
		bool BeginProceduralSound(IProceduralSoundWave waveGen, float frequency, float duration, float volume = 1f);
		bool BeginProceduralSound(float frequency, float duration, float volume = 1f);
		void Stop();
		void SetWave(IProceduralSoundWave waveGen);
		void FixedUpdate();
		float Frequency { get; set; }
		float Volume { get; set; }
		float Attack { get; set; }
		float Decay { get; set; }
		float Sustain { get; set; }
		float Release { get; set; }
		IProceduralSoundWave Waveform { get; set; }
		float noteAttackStart { get; set; }
		float noteReleaseStart { get; set; }
	}
}