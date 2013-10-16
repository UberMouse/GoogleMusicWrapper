$ = jQuery;
var nowPlaying = false;

var EventHelper = (function () {
    function getPrefix(obj) {
        if (obj.eventPrefix === null) {
            return 'GMS.Unknown';
        }

        return obj.eventPrefix;
    }

    return {
        trigger: function (obj, eventType, extraParameters) {
            if (obj.$object === undefined) {
                return;
            }

            obj.$object.trigger(getPrefix(obj) + '.' + eventType, extraParameters);
        },
        bind: function (obj, eventType, eventData, handler) {
            if (obj.$object === undefined) {
                return;
            }

            obj.$object.bind(getPrefix(obj) + '.' + eventType, eventData, handler);
        }
    };
})();


function dispatchEvent(type, detail) {
    var event = document.createEvent('CustomEvent');
    event.initCustomEvent(type, true, true, detail);
    document.documentElement.dispatchEvent(event);
}

var capture_events = [
    'playSong',
    'playPause',
    'songUnPaused',
];

window.gms_event = function (event) {

    if(capture_events.indexOf(event.eventName) > -1) {
        dispatchEvent('gm.' + event.eventName, event.payload);
    }
};

document.documentElement.addEventListener('gm.playSong', function(event) {
    if(event.detail === null && event.detail.song === undefined) return;
                                            
    song = event.detail.song.a;

    var title = song[1];
    var album = song[4];
    var artist = song[3];
    var durationMillis = song[13];
    external.app.nowPlaying(artist, album, title, durationMillis);
    setPlayingState(true);
    nowPlaying = true;
});

document.documentElement.addEventListener('gm.playPause', function (event) {
    external.app.onPlayPause();
    nowPlaying = !nowPlaying;
    setPlayingState(nowPlaying);
});


window.LoadingMonitor = (function () {
    this.eventPrefix = 'window.LoadingMonitor';
    this.ownerDocument = document;

    $('#loading-progress').attrmonitor({
        attributes: ['style'],
        callback: function (event) {
            if (event.attribute == 'style' &&
                event.value !== null &&
                event.value.replace(' ', '').indexOf('display:none;') !== -1) {
                EventHelper.trigger(window.LoadingMonitor, 'loaded');
                $('#loading-progress').attrmonitor('destroy');
            }
        }
    });

    return {
        $object: $(this),

        bind: function (eventType, eventData, handler) {
            EventHelper.bind(window.LoadingMonitor, eventType, eventData, handler);
        }
    };
})();

window.SliderMonitor = (function() {
    this.eventPrefix = 'window.SliderMonitor';
    this.ownerDocument = document;
  
    var sliderMin = null,
        sliderMax = null;
  
    function change(event) {
        console.log(event)
        if(event.attribute == 'aria-valuenow') {
            EventHelper.trigger(window.SliderMonitor, 'positionChange', [sliderMin, sliderMax, event.value]);
        } else if(event.attribute == 'aria-valuemin') {
            sliderMin = event.value;
        } else if(event.attribute == 'aria-valuemax') {
            sliderMax = event.value;
        }
    }
  
    window.LoadingMonitor.bind('loaded', function () {
        $('#slider').attrmonitor({
            attributes: ['aria-valuenow', 'aria-valuemin', 'aria-valuemax'],
            interval: 1000,
            start: false,
            callback: change
        });
    });
  
    return {
        $object: $(this),
  
        bind: function(eventType, eventData, handler) {
            EventHelper.bind(window.SliderMonitor, eventType, eventData, handler);
        }
    };
})();

window.SliderMonitor.bind('positionChange', function (event, min, max, now) {
    console.log('positionChange')
    // Ignore songs shorter than 30 seconds
    if(max < 30 * 1000) {
        return;
    }
 
    var perc = now / max;
 
    external.app.trackPercent(perc);
});

function setPlayingState(value) {
    playing = value;

    if (playing === true) {
        $('#slider').attrmonitor('start');
    } else if (playing === false) {
        $('#slider').attrmonitor('stop');
    }
}