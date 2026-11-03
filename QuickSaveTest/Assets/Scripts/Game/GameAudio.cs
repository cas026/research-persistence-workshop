using UnityEngine;

public static class GameAudio
{
    private const int SampleRate = 44100;
    private const float Frequency = 880f;
    private const float Duration = 0.12f;

    private static AudioClip _beep;

    public static void PlayBeep()
    {
        if (_beep == null)
        {
            float[] samples = new float[(int)(SampleRate * Duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float fade = 1f - (float)i / samples.Length;
                samples[i] = Mathf.Sin(2f * Mathf.PI * Frequency * i / SampleRate) * fade * 0.4f;
            }

            _beep = AudioClip.Create("Beep", samples.Length, 1, SampleRate, false);
            _beep.SetData(samples, 0);
        }

        AudioSource.PlayClipAtPoint(_beep, Camera.main.transform.position);
    }
}
