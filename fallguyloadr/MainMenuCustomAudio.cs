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
        public bool stop = false;

        void Awake()
        {
            Plugin.CustomAudioVolume.SettingChanged += CustomAudioVolumeSettingChanged;
        }

        public void PlayMusic()
        {
            if (waveOut != null)
            {
                stop = true;
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

            stop = false;
            waveOut.Init(volumeWaveProvider);

            waveOut.Play();
            waveOut.PlaybackStopped += OnPlaybackStopped;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (volumeWaveProvider != null)
            {
                if (!hasFocus && AudioManager.Instance.MuteAudioOnFocusLostSetting)
                {
                    volumeWaveProvider.Volume = 0;
                }
                else
                {
                    volumeWaveProvider.Volume = Math.Min((float)Plugin.CustomAudioVolume.Value / 100, 100);
                }
            }
        }

        void CustomAudioVolumeSettingChanged(object sender, EventArgs eventArgs)
        {
            SettingChangedEventArgs settingChangedEventArgs = (SettingChangedEventArgs)eventArgs;
            float volume = Math.Min((float)Convert.ToInt32(settingChangedEventArgs.ChangedSetting.BoxedValue) / 100, 100);
            volumeWaveProvider.Volume = volume;
        }

        void OnPlaybackStopped(object sender, StoppedEventArgs eventArgs)
        {
            if (!stop)
            {
                waveProvider.CurrentTime = TimeSpan.Zero;
                waveOut.Play();
            }
        }
    }
}
