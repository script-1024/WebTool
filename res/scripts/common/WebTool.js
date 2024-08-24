class WebTool {
    static #useWhiteList = false;
    static #blockList = new Set(['MouseEvent', 'ShowProgressBar', 'HideProgressBar', 'UpdateProgressBar']);

    static useWhiteList(value) {
        if (typeof value === 'boolean') this.#useWhiteList = value;
        else throw new TypeError('`WebTool.useWhiteList` must be a boolean value.');
    }

    static addToBlockList(typeName) {
        if (typeof typeName === 'string') return this.#blockList.add(typeName), true;
        else return false;
    }

    static removeFromBlockList(typeName) {
        if (typeof typeName === 'string') return this.#blockList.delete(typeName);
        else return false;
    }

    static postMsg(type, data = null) {
        const msg = {Type: type, Data: data}
        window.chrome.webview?.postMessage(msg);
        const inBlockList = this.#blockList.has(type);
        if (!(this.#useWhiteList ^ inBlockList)) console.log('Post: ', msg);
    }

    static showTip(title, content, isLightDismiss = true) {
        this.postMsg('ShowTip', {
            Title: title, Content: content,
            IsLightDismiss: isLightDismiss
        });
    }

    static showProgressBar = () => this.postMsg('ShowProgressBar');

    static hideProgressBar = () => this.postMsg('HideProgressBar');

    static updateProgressBar(current, total, completed, iconGlyph = '\uEBD3', isIndeterminate = false) {
        this.postMsg('UpdateProgressBar', {
            Current: current, Total: total, Completed: completed,
            IconGlyph: iconGlyph, IsIndeterminate: isIndeterminate
        });
    }
}

document.addEventListener('mousemove', (e) => {
    WebTool.postMsg('MouseEvent', {X: e.clientX, Y: e.clientY});
});
