const dbgHelper = {
    postMsg: {
        useWhiteList: false,
        blockList: [
            'MouseEvent',
            'ShowProgressBar',
            'HideProgressBar',
            'UpdateProgressBar'
        ]
    }
};

function postMsg(type, data) {
    const msg = {Type: type, Data: data}
    window.chrome.webview?.postMessage(msg);
    const useWhiteList = dbgHelper.postMsg.useWhiteList;
    const inBlockList = (dbgHelper.postMsg.blockList.indexOf(type) > -1);
    if (!(useWhiteList ^ inBlockList)) console.log('PostMessage: ', msg);
}

document.addEventListener('mousemove', (e) => {
    postMsg('MouseEvent', {X: e.clientX, Y: e.clientY});
});
