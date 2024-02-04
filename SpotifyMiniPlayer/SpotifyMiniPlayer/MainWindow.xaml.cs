using Microsoft.Win32;
using SpotifyAPI.Web;
using SpotifyMiniPlayer.Authentication;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SpotifyMiniPlayer
{
    public partial class MainWindow : Window
    {

        private SpotifyClient? _spotifyApiClient;
        private string _currentTrackFromSpotifyApp = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void MainContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (TransparencyMenuItem.IsChecked)
            {
                this.Opacity = 0.5;
            }
        }

        private void MainContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Opacity = 1.0;
        }

        private async void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            var playback = await _spotifyApiClient!.Player.GetCurrentPlayback();
            
            if (playback is not null && playback.IsPlaying)
            {
                await _spotifyApiClient.Player.PausePlayback();
            }
            else
            {
                var devices = (await _spotifyApiClient.Player.GetAvailableDevices()).Devices;
                
                await _spotifyApiClient.Player.ResumePlayback(new PlayerResumePlaybackRequest { DeviceId = devices[0].Id });
            }

            UpdatePlayerView();
        }

        private async void PreviousBtn_Click(object sender, RoutedEventArgs e)
        {
            await _spotifyApiClient!.Player.SkipPrevious();
            await Task.Delay(100);
            UpdatePlayerView();
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            await _spotifyApiClient!.Player.SkipNext();
            await Task.Delay(100);
            UpdatePlayerView();
        }

        private async void ShuffleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await _spotifyApiClient!.Player.SetShuffle(new PlayerShuffleRequest(ShuffleMenuItem.IsChecked));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MainContainer.Visibility = Visibility.Collapsed;

            AuthenticationManager authenticationManager = new AuthenticationManager();

            if (authenticationManager.IsAuthorizationCodePresent)
            {
                _spotifyApiClient = await authenticationManager.Authorize();
            }
            else
            {
                await authenticationManager.StartAuthentication();

                while (!authenticationManager.AuthenticationFinished)
                {
                    await Task.Delay(10);
                }

                _spotifyApiClient = await authenticationManager.Authorize();
            }

            MainContainer.Visibility = Visibility.Visible;

            UpdatePlayerView();

            DispatcherTimer localSpotifyChecker = new DispatcherTimer();
            localSpotifyChecker.Interval = TimeSpan.FromSeconds(1);
            localSpotifyChecker.Tick += GetSpotifyAppInfo;
            localSpotifyChecker.Start();
        }

        private void GetSpotifyAppInfo(object? sender, EventArgs e)
        {
            if(SpotifyAppAdapter.IsAppRunning())
            {
                var currentTrack = SpotifyAppAdapter.GetCurrentTrack();

                if (_currentTrackFromSpotifyApp != currentTrack)
                {
                    UpdatePlayerView();
                }

                _currentTrackFromSpotifyApp = currentTrack;

                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Minimized;
                UpdatePlayButtonState(false);
            }
        }

        private async void UpdatePlayerView()
        {
            var currentlyPlaying = await _spotifyApiClient!.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());

            if (currentlyPlaying is not null && currentlyPlaying.Item is FullTrack track)
            {
                TitleTxt.Text = track.Name;
                ArtistTxt.Text = track.Artists[0].Name;
                CoverImg.Source = new BitmapImage(new Uri(track.Album.Images[0].Url));
                UpdatePlayButtonState(currentlyPlaying.IsPlaying);
            }
        }

        private void UpdatePlayButtonState(bool isPlaying)
        {
            if (isPlaying)
            {
                PauseIcon.Visibility = Visibility.Visible;
                ResumeIcon.Visibility = Visibility.Hidden;
            }
            else
            {
                ResumeIcon.Visibility = Visibility.Visible;
                PauseIcon.Visibility = Visibility.Hidden;
            }
        }

        private void QuitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SetLaunchOnSystemStartup(bool isEnabled)
        {
            RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            registryKey!.SetValue("SpotifyMiniPlayer", Environment.ProcessPath!);
        }
    }
}
