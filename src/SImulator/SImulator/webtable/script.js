try {
    sigame.run('reactHost', {
        selectQuestion: function (themeIndex, questionIndex) {
            window.chrome.webview.postMessage({ type: 'selectQuestion', themeIndex: themeIndex, questionIndex: questionIndex });

            return new Promise(function (resolve) {
                resolve(true);
            });
        },
        deleteTheme: function (themeIndex) {
            window.chrome.webview.postMessage({ type: 'deleteTheme', themeIndex: themeIndex });

            return new Promise(function (resolve) {
                resolve(true);
            });
        },
        sendAnswer: function (answer) {
            window.chrome.webview.postMessage({ type: 'sendAnswer', answer: answer });

            return new Promise(function (resolve) {
                resolve(true);
            });
        },
        mediaLoaded: function () { }
    });

    window.chrome.webview.postMessage({ type: 'loaded' });
} catch (e) {
    window.chrome.webview.postMessage({ type: 'loadError', error: e });
}

window.addEventListener('click', function (e) {
    const option = e.target.closest('.answerOption');

    if (option) {
        return;
    }

    window.chrome.webview.postMessage({ type: 'move' });
});