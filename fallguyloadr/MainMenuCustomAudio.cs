using BepInEx;
using BepInEx.Configuration;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace fallguyloadr
{
    public class MainMenuCustomAudio : MonoBehaviour
    {
        public WaveOutEvent waveOut;
        VolumeWaveProvider16 volumeWaveProvider;
        WaveStream waveProvider;

        void Awake()
        {
            Plugin.CustomAudioVolume.SettingChanged += CustomAudioVolumeSettingChanged;
        }

        public void PlayMusic()
        {
            if (waveOut != null)
            {
                waveOut.Dispose();
            }
            waveOut = new WaveOutEvent();
            string path = $"{Paths.PluginPath}/fallguyloadr/Themes/{LoaderBehaviour.instance.currentTheme.Music}";
            string extension = Path.GetExtension(path);
            waveProvider = extension == ".wav" ? new WaveFileReader(path) : new Mp3FileReader(path);
            volumeWaveProvider = new VolumeWaveProvider16(waveProvider)
            {
                Volume = Math.Min((float)Plugin.CustomAudioVolume.Value / 100, 100)                
            };


            waveOut.Init(volumeWaveProvider);

            waveOut.Play();
            waveOut.PlaybackStopped += OnPlaybackStopped;
        }

        void CustomAudioVolumeSettingChanged(object sender, EventArgs eventArgs)
        {
            SettingChangedEventArgs settingChangedEventArgs = (SettingChangedEventArgs)eventArgs;
            float volume = Math.Min((float)Convert.ToInt32(settingChangedEventArgs.ChangedSetting.BoxedValue) / 100, 100);
            volumeWaveProvider.Volume = volume;
        }

        void OnPlaybackStopped(object sender, StoppedEventArgs eventArgs)
        {
            waveProvider.CurrentTime = TimeSpan.Zero;
            waveOut.Play();
        }
    }
}
