﻿// NAudio will only work on windows. We can use ffmpeg on Linux or MacOS.
#if !WINDOWS || FORCE_FFMPEG
#define USE_FFMPEG
#endif // !WINDOWS

using Discord;
using Discord.Audio;
using Discord.Audio.Streams;
using DiscordMusicPlayer.Music;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordMusicPlayer
{
    /// <summary>
    /// DS 2017-06-25: The current music track changed event
    /// </summary>
    /// <param name="musicFile"></param>
    internal delegate Task OnCurrentMusicFileChangedEvent(MusicFile musicFile);

    /// <summary>
    /// DS 2017-06-24: The music player
    /// DS 2019-01-20: Added a ffmpeg stream mode. This will replace the NAudio lib for Linux.
    /// </summary>
    internal class MusicPlayer
    {
        #region Init

        /// <summary>
        /// Creates the music player
        /// </summary>
        public MusicPlayer()
        {

        }

        #endregion Init

        #region Playlist

        /// <summary>
        /// The current playlist
        /// </summary>
        private Playlist m_Playlist;

        /// <summary>
        /// Gets and sets the playlist
        /// </summary>
        public Playlist Playlist
        {
            get { return m_Playlist; }
            set { m_Playlist = value; }
        }

        /// <summary>
        /// Gets the current music file
        /// </summary>
        /// <returns></returns>
        public MusicFile GetCurrentMusicFile()
        {
            if (m_Playlist == null) return null;

            return m_Playlist.CurrentMusicFile;
        }

        /// <summary>
        /// Gets the next music file
        /// </summary>
        /// <returns></returns>
        public MusicFile GetNextMusicFile()
        {
            if (m_Playlist == null) return null;

            return m_Playlist.GetNextMusicFile();
        }

        #endregion Playlist

        #region Audio client

        /// <summary>
        /// The discord audio client
        /// </summary>
        private IAudioClient m_AudioClient;

        /// <summary>
        /// The current audio channel
        /// </summary>
        private IAudioChannel m_CurrentAudioChannel;

        /// <summary>
        /// Joins a audio channel
        /// </summary>
        /// <param name="channel"></param>
        public async Task JoinAudioChannel(IAudioChannel channel)
        {
            // Leave the old channel
            if (m_AudioClient != null)
            {
                await LeaveAudioChannel().ConfigureAwait(false);
            }

            // Joins the new audio channel
            m_CurrentAudioChannel = channel;

            // Connect
            m_AudioClient = await channel.ConnectAsync().ConfigureAwait(false);

            // Starts the player loop
            await StartPlayerLoop().ConfigureAwait(false);
        }

        /// <summary>
        /// Leaves the current audio channel
        /// </summary>
        /// <returns></returns>
        public async Task LeaveAudioChannel()
        {
            // No audio client
            if (m_AudioClient == null) return;

            // Stops the player loop
            await StopPlayerLoop().ConfigureAwait(false);

            await m_AudioClient.StopAsync().ConfigureAwait(false);

            // Reset values
            m_AudioClient = null;
            m_CurrentAudioChannel = null;
        }

        #endregion Audio client

        #region Event

        /// <summary>
        /// The music track changed event
        /// </summary>
        public event OnCurrentMusicFileChangedEvent OnCurrentMusicFileChanged;

        /// <summary>
        /// Notify that the current music file changed
        /// </summary>
        /// <param name="musicFile"></param>
        private void NotifyCurrentMusicTrackChanged(MusicFile musicFile)
        {
            Task.Run(() => { OnCurrentMusicFileChanged.Invoke(musicFile).Wait(); });
        }

        #endregion Event

        #region Volume

        /// <summary>
        /// The volume
        /// </summary>
        private float m_Volume = 1f;

        /// <summary>
        /// Gets and sets the volume
        /// </summary>
        public float Volume
        {
            get { return m_Volume; }
            set
            {
                if (value < 0f) value = 0f;
                if (value > 1f) value = 1f;

                m_Volume = value;
            }
        }

        #endregion Volume

        #region Play

        /// <summary>
        /// Plays a music file
        /// </summary>
        /// <param name="musicFile"></param>
        public void Play(MusicFile musicFile)
        {
            // Stop
            Stop();

            // Seek in playlist
            if (m_Playlist != null) m_Playlist.Goto(musicFile);

            lock (m_PlayerLock)
            {
                // Close the current music file
                OpenMusicFileInPlayerLoop(musicFile);
            }
        }

        /// <summary>
        /// Play the current track from the playlist
        /// </summary>
        public void Play()
        {
            // Gets the current track
            MusicFile musicTrack = GetCurrentMusicFile();

            if (musicTrack != null)
            {
                Play(musicTrack);
            }
            else
            {
                Logger.Log("Music", "There are no music files to play!");
            }
        }

        /// <summary>
        /// Play next
        /// </summary>
        public void Next()
        {
            if (m_Playlist != null)
            {
                MusicFile musicTrack = GetNextMusicFile();

                if (musicTrack != null)
                    Play(musicTrack);
            }
        }

        /// <summary>
        /// Stops the music
        /// </summary>
        public void Stop()
        {
            lock (m_PlayerLock)
            {
                // Close the current music file
                CloseMusicFileInPlayerLoop();

                // Call the event
                NotifyCurrentMusicTrackChanged(null);
            }
        }

        #endregion Play

        #region Player loop

        /// <summary>
        /// Defines the output format
        /// </summary>
        private static readonly WaveFormat OutputFormat = new WaveFormat(OpusEncodeStream.SampleRate, 2);

        /// <summary>
        /// The player lock
        /// </summary>
        private readonly object m_PlayerLock = new object();

        /// <summary>
        /// The player loop runs until this value is false
        /// </summary>
        private bool m_RunPlayerLoop;

#if USE_FFMPEG
        /// <summary>
        /// The current FFMpeg process
        /// </summary>
        private Process m_ProcessFFMpeg;
#else
        /// <summary>
        /// The current audio player
        /// </summary>
        private AudioFileReader m_CurrentAudioPlayer;

        /// <summary>
        /// The current music re-sampler
        /// </summary>
        private MediaFoundationResampler m_CurrentMusicResampler;
#endif // USE_FFMPEG

        /// <summary>
        /// The current player loop
        /// </summary>
        private Task m_CurrentPlayerLoop;

        /// <summary>
        /// Starts the player loop
        /// </summary>
        private Task StartPlayerLoop()
        {
            // There is a loop
            if (m_CurrentPlayerLoop != null)
            {
                return Task.CompletedTask;
            }

            // Run the loop
            m_RunPlayerLoop = true;

            // Run the player loop
            m_CurrentPlayerLoop = Task.Run(() => { PlayerLoop(); });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the player loop
        /// </summary>
        /// <returns></returns>
        private async Task StopPlayerLoop()
        {
            if (m_CurrentPlayerLoop != null)
            {
                // Stop running the loop
                m_RunPlayerLoop = false;

                // Wait for the end
                await m_CurrentPlayerLoop.ConfigureAwait(false);

                // Set this to null
                m_CurrentPlayerLoop = null;
            }
        }

        /// <summary>
        /// The player loop
        /// </summary>
        /// <returns></returns>
        private void PlayerLoop()
        {
            // Sets the highest priority for the audio thread
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            // Opens the output stream
            using (var output = m_AudioClient.CreatePCMStream(AudioApplication.Music, OutputFormat.SampleRate))
            {
                // Changed the buffer to 1sec. It adds delay but sounds better on connections with higher ping.
                int blockSize = OutputFormat.AverageBytesPerSecond; // Establish the size of our AudioBuffer
                byte[] buffer = new byte[blockSize];
                int read;
                int byteCount;

                // Run the player
                while (m_RunPlayerLoop)
                {
                    // Lock this
                    lock (m_PlayerLock)
                    {
                        // Wait
                        if (m_AudioClient == null ||
#if USE_FFMPEG
                            m_ProcessFFMpeg == null
#else
                            m_CurrentMusicResampler == null
#endif // USE_FFMPEG
                            )
                        {
                            Thread.Sleep(100);
                        }
                        else
                        {
#if USE_FFMPEG
                            var stream = m_ProcessFFMpeg.StandardOutput.BaseStream;
#else
                            var stream = m_CurrentMusicResampler;
#endif // USE_FFMPEG
                            // Resets the byte count
                            byteCount = 0;
                            do
                            {
                                // Read from sampler
                                if ((read = stream.Read(buffer, byteCount, blockSize - byteCount)) <= 0)
                                {
                                    // End of song!

                                    // Close
                                    CloseMusicFileInPlayerLoop();

                                    // Play the next song
                                    Task.Run(() => { Next(); });

                                    // Break to the main loop
                                    break;
                                }
                                byteCount += read;
                            } while (byteCount < blockSize);

                            // If the volume is 100% we can entirely skip this
                            if (m_Volume != 1f)
                            {
                                // We can not control the volume with ffmpeg so we do a little trick.
                                // The audio data is a signed 16bit little-endian stream. 
                                // We can simply multiply the volume factor to the bit data.
                                var len = byteCount / 2;
                                unsafe
                                {
                                    fixed(void* b = &buffer[0])
                                    {
                                        var data = (short*)b;
                                        for (int i = 0; i < len; i ++)
                                        {
                                            data[i] = (short)(data[i] * m_Volume);
                                        }
                                    }
                                }
                            }

                            // Send the buffer to Discord
                            output.Write(buffer, 0, byteCount);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Plays the given music file in the player loop.
        /// This must be locked!
        /// </summary>
        /// <param name="musicFile"></param>
        private void OpenMusicFileInPlayerLoop(MusicFile musicFile)
        {
            try
            {
#if USE_FFMPEG
                // Creates the process info
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = string.Format("-hide_banner -loglevel panic -i \"{0}\" -ac {1} -f s16le -ar {2} -nostdin -", musicFile.File, OutputFormat.Channels, OutputFormat.SampleRate),
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                // Starts the process
                m_ProcessFFMpeg = Process.Start(processStartInfo);
#else
                // Create a new Disposable MP3FileReader, to read audio from the filePath parameter
                m_CurrentAudioPlayer = new AudioFileReader(musicFile.File);

                // Creates the re-sampler
                m_CurrentMusicResampler = new MediaFoundationResampler(m_CurrentAudioPlayer, OutputFormat);
                // Set the quality of the re-sampler to 60, the highest quality
                m_CurrentMusicResampler.ResamplerQuality = 60;
#endif // USE_FFMPEG

                // Call the event
                NotifyCurrentMusicTrackChanged(musicFile);
            }
            catch (Exception ex)
            {
                // Show error
                Logger.Log("Music", "Could not read music file: " + musicFile.File);
                Logger.Log("Music", ex.ToString());

                // Close
                CloseMusicFileInPlayerLoop();

                // Play the next song
                Task.Run(() => { Next(); });
            }
        }

        /// <summary>
        /// Close the current track in player loop.
        /// This must be locked!
        /// </summary>
        private void CloseMusicFileInPlayerLoop()
        {
#if USE_FFMPEG
            // Kill the process
            try
            {
                m_ProcessFFMpeg?.Kill();
            }
            catch
            {
                // Kill will fail if the task was already terminated.
                // We could check HasExited but this is not thread safe.
                // So the easy way is to ignore all exceptions.
            }
            m_ProcessFFMpeg = null;
#else
            // Release all
            m_CurrentAudioPlayer?.Dispose();
            m_CurrentMusicResampler?.Dispose();
            m_CurrentAudioPlayer = null;
            m_CurrentMusicResampler = null;
#endif // USE_FFMPEG
        }

        #endregion Player loop
    }
}
