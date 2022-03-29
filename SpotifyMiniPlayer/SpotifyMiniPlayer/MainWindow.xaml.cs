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
        public MainWindow()
        {
            InitializeComponent();

        }
        private void MainContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private async void PreviousBtn_Click(object sender, RoutedEventArgs e)
        {
            await _spotify.Player.SkipPrevious();
            UpdatePlayerView();
        }

        private async void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            if((await _spotify.Player.GetCurrentPlayback()).IsPlaying)
            {
                PauseIcon.Visibility = Visibility.Hidden;
                ResumeIcon.Visibility = Visibility.Visible;
                await _spotify.Player.PausePlayback();

            }
            else
            {
                ResumeIcon.Visibility = Visibility.Hidden;
                PauseIcon.Visibility = Visibility.Visible;
                await _spotify.Player.ResumePlayback();

            }

            UpdatePlayerView();
        }
        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            await _spotify.Player.SkipNext();
            UpdatePlayerView();
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
            }
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
