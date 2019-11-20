var disabledLocal = false;
var disabledServer = false;

$(document).ready(function () {
    
    $.connection.buttonHub.client.stateChanged = function (state) {
        disabledServer = state != 1;
        updateButtonState();
    };

    $.connection.hub.start()
        .done(function ()
        {
            $("#info").text("");
            updateButtonState();
        })
        .fail(function () { alert('Не удалось соединиться с сервером!'); });
});

function buttonClick() {
	try {
	    disabledLocal = true;
	    updateButtonState();
		window.setTimeout(enableButton, 3000);

		$.connection.buttonHub.server.press().done(function (res) {
		    $("#info").text(res);
		});
	}
	catch (e) {
	    $("#error").text(e.message);
	}
}

function enableButton() {
    disabledLocal = false;
    updateButtonState();
}

function updateButtonState() {
    $("#press").prop("disabled", disabledLocal || disabledServer);
}