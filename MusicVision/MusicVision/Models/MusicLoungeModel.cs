using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SpotifyAPI.Local;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace MusicVision.Models
{
    public class MusicLoungeModel
    {
        public FullPlaylist Playlist { get; set; }

        public List<FullTrack> TrackList { get; set; }
    }
}