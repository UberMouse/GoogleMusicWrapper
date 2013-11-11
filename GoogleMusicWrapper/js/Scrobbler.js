var $ = jQuery;
window.scrobbler = {
    init: function() {
        window.scrobbler.lastSongTitle = "";
        window.setInterval("window.scrobbler.detectTrack()", 1000);
        
        var sliderMax = null;

        function change(event) {
            if (event.attribute == 'aria-valuenow') {
                if (sliderMax < 30 * 1000) {
                    return;
                }

                var perc = event.value / sliderMax;

                external.app.detectScrobble(perc);
            } else if (event.attribute == 'aria-valuemax') {
                sliderMax = event.value;
            }
        }

        $('#slider').attrmonitor({
            attributes: ['aria-valuenow', 'aria-valuemax'],
            interval: 1000,
            start: true,
            callback: change
        });
    },

    detectTrack: function() {
        var parsedInfo = parseInfo();
        var artist = parsedInfo['artist'];
        var track = parsedInfo['track'];
        var album = parsedInfo['album'];
        var duration = parsedInfo['duration'];

        if (this.lastSongTitle == track) return;

        this.lastSongTitle = track;
        external.app.updateNowPlaying(artist, album, track, duration);
    },
};
    
function parseInfo() {
    var artist = '';
    var track = '';
    var album = '';
    var duration = 0;

    // Get artist and song names
    var artistValue = $("div#player-artist").text();
    var trackValue = $("div#playerSongTitle").text();
    var albumValue = $("div.player-album").text();
    var durationValue = $("div#time_container_duration").text();

    try {
        if (null != artistValue) {
            artist = artistValue.replace(/^\s+|\s+$/g, '');
        }
        if (null != trackValue) {
            track = trackValue.replace(/^\s+|\s+$/g, '');
        }
        if (null != albumValue) {
            album = albumValue.replace(/^\s+|\s+$/g, '');
        }
        if (null != durationValue) {
            duration = parseDuration(durationValue);
        }
    } catch(err) {
        return { artist: '', track: '', duration: 0 };
    }

    //console.log("artist: " + artist + ", track: " + track + ", album: " + album + ", duration: " + duration);

    return { artist: artist, track: track, album: album, duration: duration };
}

function parseDuration(artistTitle) {
    try {
        var match = artistTitle.match(/\d+:\d+/g)[0];

        var mins = match.substring(0, match.indexOf(':'));
        var seconds = match.substring(match.indexOf(':') + 1);
        return parseInt(mins * 60) + parseInt(seconds);
    } catch(err) {
        return 0;
    }
}