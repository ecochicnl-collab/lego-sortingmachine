using UnityEngine;

public class AudioSettingsFix
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySafeAudioSettings()
    {
        AudioConfiguration config = AudioSettings.GetConfiguration();

        config.speakerMode = AudioSpeakerMode.Stereo;
        config.dspBufferSize = 1024;
        config.sampleRate = 44100;

        AudioSettings.Reset(config);
    }
}
