﻿using SpotifyAPI.Local;
using SpotifyAPI.Local.Models;
using SpotifyAPI.Local.Enums;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Diagnostics;
using System.Globalization;
using MusicVision.Models;

namespace MusicVision.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult MusicLounge()
        {
            List<MusicLoungeModel> model;
            try
            {
                string tokenString = this.Request.QueryString.ToString();
                
                if (tokenString != null && tokenString != string.Empty)
                {
                    tokenString = tokenString.Replace("&amp;", "&");
                    tokenString = tokenString.Replace("%3d", "=");
                    List<string> stringList = new List<string>{ "access_token=","token_type=","expires_in=","state="};
                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                    for (int index = 0; index < stringList.Count; ++index)
                    {
                        int index1 = tokenString.IndexOf(stringList[index]);
                        int num = index + 1 == stringList.Count ? tokenString.Length : tokenString.IndexOf(stringList[index + 1]) - 1;
                        string str2 = tokenString.Substring(index1, num - index1);
                        string str3 = str2.Substring(str2.IndexOf("=") + 1, str2.Length - str2.IndexOf("=") - 1);
                        dictionary.Add(stringList[index], str3);
                    }

                    Token token = new Token();
                    token.AccessToken = dictionary[stringList[0]];
                    token.TokenType = dictionary[stringList[1]];
                    token.ExpiresIn = int.Parse(dictionary[stringList[2]]);

                    SpotifyWeb spotifyWeb = new SpotifyWeb();
                    spotifyWeb._spotify = new SpotifyWebAPI
                    {
                        UseAuth = true,
                        AccessToken = token.AccessToken,
                        TokenType=token.TokenType
                    };
                    
                    spotifyWeb.InitialSetup();
                    model = spotifyWeb.MusicLoungeModel;
                }
                else
                {                   
                    model = new List<MusicLoungeModel>();                                      
                }
            }
            catch (Exception ex)
            {
                model = new List<MusicLoungeModel>();                
            }
            return View(model);
        }

        [HttpPost]
        public async Task<JsonResult> InitializePlayer(string trackURI, string playlistContext)
        {
            string message = "tester12";
            SpotifyLocal spotifyLocal = new SpotifyLocal();
            spotifyLocal.Connect();
            await spotifyLocal._spotify.PlayURL(trackURI,playlistContext);
            //string[] test = playlistName.Replace("spotify:track:","").Split(',');
            //foreach (var a in test)
            //{
            //    await spotifyLocal._spotify.AddToQueue(a);
            //}
            
            //List<string> playlistNameDict = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(playlistName);
            
            //
            return Json(message);
        }

        [HttpPost]
        public async Task<JsonResult> PlayerClick(string commandName)
        {
            string message = "No message.";
            try
            {
                SpotifyLocal spotifyLocal = new SpotifyLocal();
                spotifyLocal.Connect();
                switch (commandName)
                {
                    case "play":
                        //spotifyLocal.UpdateTrack();
                        await spotifyLocal._spotify.Play();
                        message = "Play command.";
                        break;
                    case "pause":
                        await spotifyLocal._spotify.Pause();
                        message = "Pause command.";
                        break;
                    case "next":
                        spotifyLocal._spotify.Skip();
                        message = "Next command.";
                        break;
                    case "previous":
                        spotifyLocal._spotify.Previous();
                        message = "Previous command.";
                        break;
                    default:
                        message = "Improper command.";
                        break;
                }
            }
            catch(Exception ex)
            {
                message = ex.Message;
            }
            return Json(message);
        }

        [HttpPost]
        public async Task<JsonResult> SongSearch(string trackName)
        {            
            SpotifyWeb spotifyWeb = new SpotifyWeb();

            string tokenString = this.Request.QueryString.ToString();

            if (tokenString != null && tokenString != string.Empty)
            {
                tokenString = tokenString.Replace("&amp;", "&");
                tokenString = tokenString.Replace("%3d", "=");
                List<string> stringList = new List<string> { "access_token=", "token_type=", "expires_in=", "state=" };
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                for (int index = 0; index < stringList.Count; ++index)
                {
                    int index1 = tokenString.IndexOf(stringList[index]);
                    int num = index + 1 == stringList.Count ? tokenString.Length : tokenString.IndexOf(stringList[index + 1]) - 1;
                    string str2 = tokenString.Substring(index1, num - index1);
                    string str3 = str2.Substring(str2.IndexOf("=") + 1, str2.Length - str2.IndexOf("=") - 1);
                    dictionary.Add(stringList[index], str3);
                }

                Token token = new Token();
                token.AccessToken = dictionary[stringList[0]];
                token.TokenType = dictionary[stringList[1]];
                token.ExpiresIn = int.Parse(dictionary[stringList[2]]);

                spotifyWeb._spotify = new SpotifyWebAPI
                {
                    UseAuth = true,
                    AccessToken = token.AccessToken,
                    TokenType = token.TokenType
                };

                spotifyWeb.InitialSetup();
                FullTrack result = spotifyWeb.SearchTrack(trackName);
                if (result != null)
                {                   
                    return Json(result);
                }
                else
                {
                    return Json("Track not found.");
                }
            }
            else
                return Json("Track not found.");                                   
        }
    }

    public class SpotifyWeb
    {
        public SpotifyWebAPI _spotify;

        private PrivateProfile _profile;        
        private List<SimplePlaylist> _simplePlaylists;
        private List<FullPlaylist> _fullPlaylists;       
        private List<MusicLoungeModel> _musicLoungeModel;
        public List<MusicLoungeModel> MusicLoungeModel
        {
            get { return _musicLoungeModel; }
        }

        public SpotifyWeb()
        {                         
            
        }

        public void InitialSetup()
        {                            
            _profile = _spotify.GetPrivateProfile();
            _simplePlaylists = GetPlaylists(_profile.Id);
            _fullPlaylists = GetFullPlaylists(_simplePlaylists);
            _musicLoungeModel = CreateMusicLoungeModel(_fullPlaylists);                      
        }

        public FullTrack SearchTrack(string songName)
        {
            SearchItem search = _spotify.SearchItems(songName, SearchType.Track);
            if (search.Tracks != null)
            {
                if (search.Tracks.Items != null)
                {
                    return search.Tracks.Items[0];
                }
            }
            return null;
        }

        public string GetTrackURI()
        {
            Paging<PlaylistTrack> playlistTracks = _spotify.GetPlaylistTracks(_fullPlaylists[0].Owner.Id, _fullPlaylists[0].Id);
            List<FullTrack> list = playlistTracks.Items.Select(track => track.Track).ToList();
            return list[0].Uri;
        }

        private List<MusicLoungeModel> CreateMusicLoungeModel(List<FullPlaylist> fullPlaylists)
        {            
            List<MusicLoungeModel> musicLoungeModelList = new List<MusicLoungeModel>();

            foreach (var fullPlaylist in fullPlaylists)
            {
                MusicLoungeModel musicLoungeModel = new MusicLoungeModel();
                musicLoungeModel.Playlist = fullPlaylist;

                Paging<PlaylistTrack> playlistTracks = _spotify.GetPlaylistTracks(fullPlaylist.Owner.Id, fullPlaylist.Id);
                List<FullTrack> list = playlistTracks.Items.Select(track => track.Track).ToList();

                while (playlistTracks.Next != null)
                {
                    playlistTracks = _spotify.GetPlaylistTracks(fullPlaylist.Owner.Id, fullPlaylist.Id, "", 100, playlistTracks.Offset + playlistTracks.Limit);
                    list.AddRange(playlistTracks.Items.Select(track => track.Track));
                }
                musicLoungeModel.TrackList = list;

                musicLoungeModelList.Add(musicLoungeModel);
            }

            return musicLoungeModelList;
        }


        private List<SimplePlaylist> GetPlaylists(string profileID)
        {
            Paging<SimplePlaylist> playlists = _spotify.GetUserPlaylists(profileID);
            List<SimplePlaylist> list = playlists.Items.ToList();

            while (playlists.Next != null)
            {
                playlists = _spotify.GetUserPlaylists(profileID, 20, playlists.Offset + playlists.Limit);
                list.AddRange(playlists.Items);
            }

            return list;
        }

        private List<FullPlaylist> GetFullPlaylists(List<SimplePlaylist> playlistList)
        {
            //The 'i' counter is used to prevent too many playlist imports. To be removed later.
            List<FullPlaylist> list = new List<FullPlaylist>();
            int i = 0;
            foreach(var playlist in playlistList)
            {
                i++;
                if (i > 5)
                    break;
                FullPlaylist fullPlaylist = _spotify.GetPlaylist(playlist.Owner.Id, playlist.Id);
                list.Add(fullPlaylist);
            }            

            return list;
        }        

        public async void RunAuthentication()
        {
            WebAPIFactory webApiFactory = new WebAPIFactory(
                "http://localhost",
                8000,
                "26d287105e31491889f3cd293d85bfea",
                Scope.UserReadPrivate | Scope.UserReadEmail | Scope.PlaylistReadPrivate | Scope.UserLibraryRead |
                Scope.UserReadPrivate | Scope.UserFollowRead | Scope.UserReadBirthdate | Scope.UserTopRead | Scope.PlaylistReadCollaborative |
                Scope.UserReadRecentlyPlayed | Scope.UserReadPlaybackState | Scope.UserModifyPlaybackState);

            try
            {
                _spotify = await webApiFactory.GetWebApi();
            }
            catch (Exception ex)
            {
                
            }

            if (_spotify == null)
                return;

            InitialSetup();
        }
    }

    public partial class SpotifyLocal
    {
        public readonly SpotifyLocalAPI _spotify;        
        private Track _currentTrack;

        public SpotifyLocal()
        {
            SpotifyLocalAPIConfig localConfig = new SpotifyLocalAPIConfig();
            localConfig.HostUrl = GetIPAddress();
            _spotify = new SpotifyLocalAPI(localConfig);
            //_spotify.OnPlayStateChange += _spotify_OnPlayStateChange;
            //_spotify.OnTrackChange += _spotify_OnTrackChange;
            //_spotify.OnTrackTimeChange += _spotify_OnTrackTimeChange;
            //_spotify.OnVolumeChange += _spotify_OnVolumeChange;
            //_spotify.SynchronizingObject = this;
            
            //artistLinkLabel.Click += (sender, args) => Process.Start(artistLinkLabel.Tag.ToString());
            //albumLinkLabel.Click += (sender, args) => Process.Start(albumLinkLabel.Tag.ToString());
            //titleLinkLabel.Click += (sender, args) => Process.Start(titleLinkLabel.Tag.ToString());
        }

        protected string GetIPAddress()
        {
            System.Web.HttpContext context = System.Web.HttpContext.Current;
            string ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ipAddress))
            {
                string[] addresses = ipAddress.Split(',');
                if (addresses.Length != 0)
                {
                    return addresses[0];
                }
            }

            return context.Request.ServerVariables["REMOTE_ADDR"];
        }

        public void Connect()
        {
            if (!SpotifyLocalAPI.IsSpotifyRunning())
            {                
                return;
            }
            if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
            {                
                return;
            }

            bool successful = _spotify.Connect();
            if (successful)
            {
                //connectBtn.Text = @"Connection to Spotify successful";
                //connectBtn.Enabled = false;
                UpdateInfos();
                _spotify.ListenForEvents = true;
            }
            else
            {
                //DialogResult res = MessageBox.Show(@"Couldn't connect to the spotify client. Retry?", @"Spotify", MessageBoxButtons.YesNo);
                //if (res == DialogResult.Yes)
                //    Connect();
            }
        }

        public void UpdateInfos()
        {
            StatusResponse status = _spotify.GetStatus();
            if (status == null)
                return;

            //Basic Spotify Infos
            UpdatePlayingStatus(status.Playing);
            //clientVersionLabel.Text = status.ClientVersion;
            //versionLabel.Text = status.Version.ToString();
            //repeatShuffleLabel.Text = status.Repeat + @" and " + status.Shuffle;

            if (status.Track != null) //Update track infos
                UpdateTrack(status.Track);
        }

        public async void UpdateTrack(Track track)
        {
            _currentTrack = track;

            //advertLabel.Text = track.IsAd() ? "ADVERT" : "";
            //timeProgressBar.Maximum = track.Length;

            if (track.IsAd())
                return; //Don't process further, maybe null values

            //titleLinkLabel.Text = track.TrackResource.Name;
            //titleLinkLabel.Tag = track.TrackResource.Uri;

            //artistLinkLabel.Text = track.ArtistResource.Name;
            //artistLinkLabel.Tag = track.ArtistResource.Uri;

            //albumLinkLabel.Text = track.AlbumResource.Name;
            //albumLinkLabel.Tag = track.AlbumResource.Uri;

            SpotifyUri uri = track.TrackResource.ParseUri();

            //trackInfoBox.Text = $@"Track Info - {uri.Id}";

            //bigAlbumPicture.Image = await track.GetAlbumArtAsync(AlbumArtSize.Size640);
            //smallAlbumPicture.Image = await track.GetAlbumArtAsync(AlbumArtSize.Size160);
        }

        public void UpdatePlayingStatus(bool playing)
        {
            //isPlayingLabel.Text = playing.ToString();
        }

        private void _spotify_OnVolumeChange(object sender, VolumeChangeEventArgs e)
        {
            //if (InvokeRequired)
            //{
            //    Invoke(new Action(() => _spotify_OnVolumeChange(sender, e)));
            //    return;
            //}
            //volumeLabel.Text = (e.NewVolume * 100).ToString(CultureInfo.InvariantCulture);
        }

        private void _spotify_OnTrackTimeChange(object sender, TrackTimeChangeEventArgs e)
        {
            //if (InvokeRequired)
            //{
            //    Invoke(new Action(() => _spotify_OnTrackTimeChange(sender, e)));
            //    return;
            //}
            //timeLabel.Text = $@"{FormatTime(e.TrackTime)}/{FormatTime(_currentTrack.Length)}";
            //if (e.TrackTime < _currentTrack.Length)
            //    timeProgressBar.Value = (int)e.TrackTime;
        }

        private void _spotify_OnTrackChange(object sender, TrackChangeEventArgs e)
        {
            //if (InvokeRequired)
            //{
            //    Invoke(new Action(() => _spotify_OnTrackChange(sender, e)));
            //    return;
            //}
            UpdateTrack(e.NewTrack);
        }

        private void _spotify_OnPlayStateChange(object sender, PlayStateEventArgs e)
        {
            //if (InvokeRequired)
            //{
            //    Invoke(new Action(() => _spotify_OnPlayStateChange(sender, e)));
            //    return;
            //}
            UpdatePlayingStatus(e.Playing);
        }

        private void connectBtn_Click(object sender, EventArgs e)
        {
            Connect();
        }

        private async void playUrlBtn_Click(object sender, EventArgs e)
        {
            // await _spotify.PlayURL(playTextBox.Text, contextTextBox.Text);           
        }

        private async void playBtn_Click(object sender, EventArgs e)
        {
            await _spotify.Play();
        }

        private async void pauseBtn_Click(object sender, EventArgs e)
        {
            await _spotify.Pause();
        }

        private void prevBtn_Click(object sender, EventArgs e)
        {
            _spotify.Previous();
        }

        private void skipBtn_Click(object sender, EventArgs e)
        {
            _spotify.Skip();
        }

        private static String FormatTime(double sec)
        {
            TimeSpan span = TimeSpan.FromSeconds(sec);
            String secs = span.Seconds.ToString(), mins = span.Minutes.ToString();
            if (secs.Length < 2)
                secs = "0" + secs;
            return mins + ":" + secs;
        }
    }
}