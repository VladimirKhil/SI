var disabledLocal = false;
var disabledServer = false;

var token = localStorage.getItem('token') || '';
var userName = localStorage.getItem('userName') || '';
var buttonBlockTime = 3000;

var connection = new signalR.HubConnectionBuilder()
    .withUrl('/buttonHost')
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
        console.log('SignalR Connected');
        document.getElementById('info').innerText = userName;
        updateButtonState();
    } catch (err) {
        console.error(err);
        document.getElementById('info').innerText = err;
    }
};

function buttonClick() {
    disabledLocal = true;
    updateButtonState();

    window.setTimeout(enableButton, buttonBlockTime);

    connection.invoke('press', token)
        .then((res) => {
            document.getElementById('info').innerText = res.userName;
            token = res.token;
            buttonBlockTime = res.buttonBlockTime || buttonBlockTime;

            localStorage.setItem('userName', res.userName);
            localStorage.setItem('token', res.token);
        })
        .catch((err) => { document.getElementById('error').innerText = err.message; });
}

function enableButton() {
    disabledLocal = false;
    updateButtonState();
}

function updateButtonState() {
    document.getElementById('press').disabled = disabledLocal || disabledServer;
}

var noSleep = new NoSleep();

document.addEventListener('click', function enableNoSleep() {
    document.removeEventListener('click', enableNoSleep, false);
    noSleep.enable();
}, false);

start();
