using SpotifyAPI.Local;
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

using SpotifyAPI.Local.Enums;
using SpotifyAPI.Local.Models;
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

        public async Task<ActionResult> MusicLounge()
        {
            APITest tester = new APITest();
            await Task.Run(() => tester.RunAuthentication());
            PrivateProfile profile = await tester._spotify.GetPrivateProfileAsync();
            List<SimplePlaylist> simplePlaylist = tester.GetPlaylists(profile.Id);            
            List<FullPlaylist> fullPlaylist = tester.GetFullPlaylists(profile.Id,simplePlaylist);
            List<MusicLoungeModel> model = new List<MusicLoungeModel>();

            foreach(var playlist in fullPlaylist)
            {
                MusicLoungeModel el = new MusicLoungeModel();
                el.Playlist=playlist;
                
                Paging<PlaylistTrack> playlistTracks = tester._spotify.GetPlaylistTracks(playlist.Owner.Id,playlist.Id);
                List<FullTrack> list = playlistTracks.Items.Select(track => track.Track).ToList();
                
                while (playlistTracks.Next != null)
                {
                    playlistTracks = tester._spotify.GetPlaylistTracks(playlist.Owner.Id, playlist.Id,"",100, playlistTracks.Offset + playlistTracks.Limit);
                    list.AddRange(playlistTracks.Items.Select(track => track.Track));
                }
                el.TrackList = list;

                model.Add(el);
            }
            return View(model);
        }

        [HttpPost]
        public async Task<JsonResult> AjaxMethod(string name)
        {
            string test = "tester12";
            LocalControl test2 = new LocalControl();
            test2.Connect();
            await test2._spotify.Play();
            return Json(test);
        }
    }

    public class APITest
    {
        public SpotifyWebAPI _spotify;

        private PrivateProfile _profile;
        private List<FullTrack> _savedTracks;
        private List<SimplePlaylist> _playlists;

        public APITest()
        {             
            _savedTracks = new List<FullTrack>();

        }

        public async void InitialSetup()
        {
                        
            _profile = await _spotify.GetPrivateProfileAsync();
            
            _savedTracks = GetSavedTracks();
            
            //_savedTracks.ForEach(track => savedTracksListView.Items.Add(new ListViewItem()
            //{
            //    Text = track.Name,
            //    SubItems = { string.Join(",", track.Artists.Select(source => source.Name)), track.Album.Name }
            //}));

            //_playlists = GetPlaylists();            
            //_playlists.ForEach(playlist => playlistsListBox.Items.Add(playlist.Name));           

            //if (_profile.Images != null && _profile.Images.Count > 0)
            //{
            //    using (WebClient wc = new WebClient())
            //    {
            //        byte[] imageBytes = await wc.DownloadDataTaskAsync(new Uri(_profile.Images[0].Url));
            //        using (MemoryStream stream = new MemoryStream(imageBytes)) { }
            //            //avatarPictureBox.Image = Image.FromStream(stream);
            //    }
            //}
        }

        public List<FullTrack> GetSavedTracks()
        {
            Paging<SavedTrack> savedTracks = _spotify.GetSavedTracks();
            List<FullTrack> list = savedTracks.Items.Select(track => track.Track).ToList();

            while (savedTracks.Next != null)
            {
                savedTracks = _spotify.GetSavedTracks(20, savedTracks.Offset + savedTracks.Limit);
                list.AddRange(savedTracks.Items.Select(track => track.Track));
            }

            return list;
        }

        public List<SimplePlaylist> GetPlaylists(string profileID)
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

        public List<FullPlaylist> GetFullPlaylists(string profileID, List<SimplePlaylist> playlistList)
        {

            List<FullPlaylist> list = new List<FullPlaylist>();
            foreach(var playlist in playlistList)
            {
                FullPlaylist fullPlaylist = _spotify.GetPlaylist(playlist.Owner.Id, playlist.Id);
                list.Add(fullPlaylist);
            }            

            return list;
        }

        private void authButton_Click(object sender, EventArgs e)
        {
            Task.Run(() => RunAuthentication());
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

    public partial class LocalControl
    {
        public readonly SpotifyLocalAPI _spotify;
        private Track _currentTrack;

        public LocalControl()
        {
            //InitializeComponent();

            _spotify = new SpotifyLocalAPI();
            _spotify.OnPlayStateChange += _spotify_OnPlayStateChange;
            _spotify.OnTrackChange += _spotify_OnTrackChange;
            _spotify.OnTrackTimeChange += _spotify_OnTrackTimeChange;
            _spotify.OnVolumeChange += _spotify_OnVolumeChange;
            //_spotify.SynchronizingObject = this;

            //artistLinkLabel.Click += (sender, args) => Process.Start(artistLinkLabel.Tag.ToString());
            //albumLinkLabel.Click += (sender, args) => Process.Start(albumLinkLabel.Tag.ToString());
            //titleLinkLabel.Click += (sender, args) => Process.Start(titleLinkLabel.Tag.ToString());
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