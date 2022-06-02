var disabledLocal = false;
var disabledServer = false;

var connection = new signalR.HubConnectionBuilder()
    .withUrl("/buttonHost")
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on('stateChanged', (state) => {
    disabledServer = state != 1;
    updateButtonState();
});

async function start() {
    try {
        await connection.start();
        console.log("SignalR Connected.");
        document.getElementById('info').innerText = '';
        updateButtonState();
    } catch (err) {
        console.error(err);
        document.getElementById('info').innerText = err;
    }
};

function buttonClick() {
    disabledLocal = true;
    updateButtonState();

    window.setTimeout(enableButton, 3000);

    connection.invoke('press')
        .then((res) => { document.getElementById('info').innerText = res; })
        .catch((err) => { document.getElementById('error').innerText = err.message; });
}

function enableButton() {
    disabledLocal = false;
    updateButtonState();
}

function updateButtonState() {
    document.getElementById('press').disabled = disabledLocal || disabledServer;
}

start();
