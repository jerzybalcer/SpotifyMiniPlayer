using SpotifyAPI.Web;
using SpotifyMiniPlayer.Authentication;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SpotifyMiniPlayer;

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
            Opacity = 0.5;
        }
    }

    private void MainContainer_MouseEnter(object sender, MouseEventArgs e)
    {
        Opacity = 1.0;
    }

    private async void ShuffleMenuItem_Click(object sender, RoutedEventArgs e)
    {
        await _spotifyApiClient!.Player.SetShuffle(new PlayerShuffleRequest(ShuffleMenuItem.IsChecked));
    }

    private void QuitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
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

            if (devices.Count == 0) return;

            var deviceToPlayOn = devices.SingleOrDefault(d => d.IsActive) ?? devices[0];
            
            await _spotifyApiClient.Player.ResumePlayback(new PlayerResumePlaybackRequest { DeviceId = deviceToPlayOn.Id });
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

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        HideWindow();

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

            if (/*_currentTrackFromSpotifyApp != currentTrack &&*/ WindowState == WindowState.Normal)
            {
                UpdatePlayerView();
            }

            _currentTrackFromSpotifyApp = currentTrack;
            WindowState = WindowState.Normal;
        }
        else
        {
            HideWindow();
            UpdatePlayButtonState(false);
        }
    }

    private async void UpdatePlayerView()
    {
        var playbackState = await _spotifyApiClient!.Player.GetCurrentPlayback();

        if (playbackState is not null)
        {
            if(playbackState.Item is FullTrack track)
            {
                TitleTxt.Text = track.Name;
                ArtistTxt.Text = track.Artists[0].Name;
                CoverImg.Source = new BitmapImage(new Uri(track.Album.Images[0].Url));
            }
            else if(playbackState.Item is FullEpisode episode)
            {
                TitleTxt.Text = episode.Name;
                ArtistTxt.Text = episode.Show.Name;
                CoverImg.Source = new BitmapImage(new Uri(episode.Show.Images[0].Url));
            }

            UpdatePlayButtonState(playbackState.IsPlaying);
            ShuffleMenuItem.IsChecked = playbackState.ShuffleState;
        }

        UpdatePossiblePlayerActions(playbackState?.Actions ?? new Actions { Disallows = new System.Collections.Generic.Dictionary<string, bool>() });
    }

    private void UpdatePossiblePlayerActions(Actions actions)
    {
        actions.Disallows.TryGetValue("pausing", out bool cannotPlay);
        actions.Disallows.TryGetValue("resuming", out bool cannotResume);
        PlayBtn.IsEnabled = !(cannotPlay && cannotResume);

        actions.Disallows.TryGetValue("skipping_prev", out bool cannotSkipToPrevious);
        PreviousBtn.IsEnabled = !cannotSkipToPrevious;

        actions.Disallows.TryGetValue("skipping_next", out bool cannotSkipToNext);
        NextButton.IsEnabled = !cannotSkipToNext;

        actions.Disallows.TryGetValue("toggling_shuffle", out bool cannotShuffle);
        ShuffleMenuItem.IsEnabled = !cannotShuffle;
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

    private async void Window_StateChanged(object sender, EventArgs e)
    {
        if(WindowState == WindowState.Normal)
        {
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

            UpdatePlayerView();
        }
    }

    private void HideWindow()
    {
        // Workaround for bug that prevents window from minimizing when ShowInTaskbar is false
        ShowInTaskbar = true;
        WindowState = WindowState.Minimized;
        ShowInTaskbar = false;
    }
}
