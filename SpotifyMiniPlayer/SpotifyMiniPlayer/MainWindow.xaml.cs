using SpotifyAPI.Web;
using SpotifyMiniPlayer.Authentication;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SpotifyMiniPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private SpotifyClient _spotify;
        private string _localAppState = "";
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
            var playback = await _spotify.Player.GetCurrentPlayback();
            
            if (playback is not null && playback.IsPlaying)
            {
                await _spotify.Player.PausePlayback();
            }
            else
            {
                var devices = (await _spotify.Player.GetAvailableDevices()).Devices;
                
                await _spotify.Player.ResumePlayback(new PlayerResumePlaybackRequest { DeviceId = devices[0].Id });
            }

            UpdatePlayerView();
        }
        private async void PreviousBtn_Click(object sender, RoutedEventArgs e)
        {
            await _spotify.Player.SkipPrevious();
            await Task.Delay(100);
            UpdatePlayerView();
        }
        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            await _spotify.Player.SkipNext();
            await Task.Delay(100);
            UpdatePlayerView();
        }

        private async void ShuffleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            await _spotify.Player.SetShuffle(new PlayerShuffleRequest(ShuffleMenuItem.IsChecked));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MainContainer.Visibility = Visibility.Collapsed;

            AuthenticationManager authenticationManager = new AuthenticationManager();

            if (authenticationManager.IsAuthorizationCodePresent)
            {
                _spotify = await authenticationManager.Authorize();
            }
            else
            {
                await authenticationManager.StartAuthentication();

                while (!authenticationManager.AuthenticationFinished)
                {
                    await Task.Delay(10);
                }

                _spotify = await authenticationManager.Authorize();
            }

            MainContainer.Visibility = Visibility.Visible;

            UpdatePlayerView();

            DispatcherTimer localSpotifyChecker = new DispatcherTimer();
            localSpotifyChecker.Interval = TimeSpan.FromSeconds(1);
            localSpotifyChecker.Tick += GetLocalSpotifyInfo;
            localSpotifyChecker.Start();
        }

        private void GetLocalSpotifyInfo(object? sender, EventArgs e)
        {
            var proc = Process.GetProcessesByName("Spotify").FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.MainWindowTitle));

            if (proc == null)
            {
                return;
            }

            if (_localAppState != proc.MainWindowTitle)
            {
                UpdatePlayerView();
            }

            _localAppState = proc.MainWindowTitle;
        }

        private async void UpdatePlayerView()
        {
            var currentlyPlaying = await _spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());

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
    }
}
