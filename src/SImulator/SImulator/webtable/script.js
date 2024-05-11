try {
    sigame.run('reactHost');
    window.chrome.webview.postMessage({ type: 'loaded' });
} catch (e) {
    window.chrome.webview.postMessage({ type: 'loadError', error: e });
}