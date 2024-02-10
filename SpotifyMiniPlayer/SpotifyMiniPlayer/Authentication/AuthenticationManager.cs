using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using static SpotifyAPI.Web.Scopes;


namespace SpotifyMiniPlayer.Authentication;

public class AuthenticationManager
{
    private const string CredentialsPath = "credentials.json";
    private static readonly string? clientId = "4d100c339810485a8477076ce2774cd1";
    private static readonly EmbedIOAuthServer _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);

    public bool IsAuthorizationCodePresent { get { return File.Exists(CredentialsPath); } }
    public bool AuthenticationFinished { get; private set; }

    public async Task StartAuthentication()
    {
        AuthenticationFinished = false;

        var (verifier, challenge) = PKCEUtil.GenerateCodes();

        await _server.Start();
        _server.AuthorizationCodeReceived += async (sender, response) =>
        {
            await _server.Stop();
            PKCETokenResponse token = await new OAuthClient().RequestToken(
              new PKCETokenRequest(clientId!, response.Code, _server.BaseUri, verifier)
            );

            await File.WriteAllTextAsync(CredentialsPath, JsonConvert.SerializeObject(token));
            AuthenticationFinished = true;
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

    public async Task<SpotifyClient> Authorize()
    {
        var json = await File.ReadAllTextAsync(CredentialsPath);
        var token = JsonConvert.DeserializeObject<PKCETokenResponse>(json);

        var authenticator = new PKCEAuthenticator(clientId!, token!);
        authenticator.TokenRefreshed += (sender, token) => File.WriteAllText(CredentialsPath, JsonConvert.SerializeObject(token));

        var config = SpotifyClientConfig.CreateDefault()
          .WithAuthenticator(authenticator);

        _server.Dispose();

        return new SpotifyClient(config);
    }
}
