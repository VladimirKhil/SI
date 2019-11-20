var disabledLocal = 0;
var disabledServer = 0;

var timerStart = 0;
var lastCheck = new Date();

function buttonClick() {
	try {
		document.getElementById('press').disabled = 'disabled';
		disabledLocal = 1;
		window.setTimeout(enableButton, 3000);
		
		createRequest("press", 1, function (t) {
			var js = JSON.parse(t);
			if (js != undefined) {
				document.getElementById('info').innerHTML = js.name;
				if (js.guid != undefined) {
					document.cookie = 'id=' + js.guid + '; expires=Fri, 31 Dec 9999 23:59:59 UTC; path=/';
				}
			}

			if (timerStart == 0) {
			    timerStart = 1;
				setTimeout(askState, 50);
				setInterval(stateCheck, 1000);
			}
		});
	}
	catch (e) {
		document.getElementById('error').innerHTML = e.message;
	}
}

function enableButton() {
    disabledLocal = 0;
	if (disabledServer == 0) {
		document.getElementById('press').disabled = '';
	}
}

function createRequest(path, err, onSuccess) {
    try
    {
        var xmlhttp = new XMLHttpRequest();

        xmlhttp.onreadystatechange = function () {
            if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
                onSuccess(xmlhttp.responseText);
            }
        };

        if (err == 1) {
            document.getElementById('error').innerHTML = '';

            xmlhttp.onerror = function (e) {
                document.getElementById('error').innerHTML = e.target.status;
            };
        }

        xmlhttp.open("GET", path, true);

		if (err == 0) { // askState
			xmlhttp.ontimeout = function () { setTimeout(askState, 50); }
		}

        xmlhttp.send();
    }
    catch (exc) {
        document.getElementById('error').innerHTML = exc;
    }
}

// Long polling
function askState() {
    lastCheck = new Date();

    createRequest("state", 0, function (t) {
        var js = JSON.parse(t);
        if (js != undefined) {
            var state = js.state;
            if (state == 1) {
				disabledServer = 0;
				if (disabledLocal == 0) {
					document.getElementById('press').disabled = '';
				}
			}
            else if (state == 0) {
				disabledServer = 1;
				document.getElementById('press').disabled = 'disabled';
			}
        }

        setTimeout(askState, 50);
    });
}

function stateCheck() {
    if ((new Date() - lastCheck) > 10000) {
        setTimeout(askState, 50);
    }
}