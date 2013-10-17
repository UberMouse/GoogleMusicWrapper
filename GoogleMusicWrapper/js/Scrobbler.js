$ = jQuery;
var nowPlaying = false;

var capture_events = [
    'playSong',
    'playPause',
];

window.gms_event = function (event) {
    if (capture_events.indexOf(event.eventName) > -1) {
        console.log(event.payload);
        window.scrobbler[event.eventName](event.payload);
    }
};

window.scrobbler = {
    playSong: function (event) {
        if (event === null && event.song === undefined) return;

        song = event.song.a;

        var title = song[1];
        var album = song[4];
        var artist = song[3];
        var durationMillis = song[13];
        external.app.nowPlaying(artist, album, title, durationMillis);
        setPlayingState(true);
        nowPlaying = true;
    },
    
    playPause: function (event) {
        external.app.onPlayPause();
        nowPlaying = !nowPlaying;
        setPlayingState(nowPlaying);
    }
}

//window.scrobbler.playSong = function(event) {
//    if(event.detail === null && event.detail.song === undefined) return;
//                                            
//    song = event.detail.song.a;
//
//    var title = song[1];
//    var album = song[4];
//    var artist = song[3];
//    var durationMillis = song[13];
//    external.app.nowPlaying(artist, album, title, durationMillis);
//    setPlayingState(true);
//    nowPlaying = true;
//};
//
//window.scrobbler.playPause = function (event) {
//    external.app.onPlayPause();
//    nowPlaying = !nowPlaying;
//    setPlayingState(nowPlaying);
//};

$('#loading-progress').attrmonitor({
    attributes: ['style'],
    callback: function (event) {
        if (event.attribute == 'style' &&
            event.value !== null &&
            event.value.replace(' ', '').indexOf('display:none;') !== -1) {
            sliderMonitor();
            $('#loading-progress').attrmonitor('destroy');
        }
    }
});

function sliderMonitor() {
    var sliderMax = null;

    function change(event) {
        if (event.attribute == 'aria-valuenow') {
            if (sliderMax < 30 * 1000) {
                return;
            }

            var perc = event.value / sliderMax;

            external.app.trackPercent(perc);
        } else if (event.attribute == 'aria-valuemax') {
            sliderMax = event.value;
        }
    }
    
    $('#slider').attrmonitor({
        attributes: ['aria-valuenow', 'aria-valuemax'],
        interval: 1000,
        start: false,
        callback: change
    });
}

function setPlayingState(value) {
    playing = value;

    if (playing === true) {
        $('#slider').attrmonitor('start');
    } else if (playing === false) {
        $('#slider').attrmonitor('stop');
    }
}