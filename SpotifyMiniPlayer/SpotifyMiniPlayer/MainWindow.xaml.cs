﻿using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static SpotifyAPI.Web.Scopes;


namespace SpotifyMiniPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string CredentialsPath = "credentials.json";
        private static readonly string? clientId = "4d100c339810485a8477076ce2774cd1";
        private static readonly EmbedIOAuthServer _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
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
            if (File.Exists(CredentialsPath))
            {
                await Start();
            }
            else
            {
                await StartAuthentication();
            }
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

        public void GetLocalSpotifyInfo()
        {
            var proc = Process.GetProcessesByName("Spotify").FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.MainWindowTitle));

            if (proc == null)
            {
                return;
            }

            if(_localAppState != proc.MainWindowTitle)
            {
                UpdatePlayerView();
            }

            _localAppState = proc.MainWindowTitle;
        }

        private async Task Start()
        {
            var json = await File.ReadAllTextAsync(CredentialsPath);
            var token = JsonConvert.DeserializeObject<PKCETokenResponse>(json);

            var authenticator = new PKCEAuthenticator(clientId!, token!);
            authenticator.TokenRefreshed += (sender, token) => File.WriteAllText(CredentialsPath, JsonConvert.SerializeObject(token));

            var config = SpotifyClientConfig.CreateDefault()
              .WithAuthenticator(authenticator);

            _spotify = new SpotifyClient(config);

            UpdatePlayerView();

            DispatcherTimer localSpotifyChecker = new DispatcherTimer();
            localSpotifyChecker.Interval = TimeSpan.FromSeconds(1);
            localSpotifyChecker.Tick += (source, e) => { GetLocalSpotifyInfo(); };
            localSpotifyChecker.Start();
            
            _server.Dispose();
        }

        private async Task StartAuthentication()
        {
            var (verifier, challenge) = PKCEUtil.GenerateCodes();

            await _server.Start();
            _server.AuthorizationCodeReceived += async (sender, response) =>
            {
                await _server.Stop();
                PKCETokenResponse token = await new OAuthClient().RequestToken(
                  new PKCETokenRequest(clientId!, response.Code, _server.BaseUri, verifier)
                );

                await File.WriteAllTextAsync(CredentialsPath, JsonConvert.SerializeObject(token));
                await Start();
            };

            var request = new LoginRequest(_server.BaseUri, clientId!, LoginRequest.ResponseType.Code)
            {
                CodeChallenge = challenge,
                CodeChallengeMethod = "S256",
                Scope = new List<string> { UserReadCurrentlyPlaying, UserReadRecentlyPlayed, UserReadPlaybackState, UserReadPlaybackPosition, UserModifyPlaybackState }
            };

            Uri uri = request.ToUri();
            try
            {
                BrowserUtil.Open(uri);
            }
            catch (Exception)
            {
                Debug.WriteLine("Unable to open URL, manually open: {0}", uri);
            }
        }
    }
}
