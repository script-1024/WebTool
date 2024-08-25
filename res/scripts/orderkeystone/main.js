class OrderKeystone {
    static inputEvent = new UIEvent('input');
    static scrollEvent = new UIEvent('scroll');

    static get container() {
        return document.getElementById('search-results-container');
    }

    static get totalItemCount() {
        const resultText = document.querySelector('.results-text');
        const count = parseInt(resultText?.getElementsByTagName('span')[1].textContent);
        return isNaN(count) ? 0 : count;
    }

    static get currentItemCount() {
        return document.getElementsByTagName('app-product-card').length;
    }

    static get currentSearched() {
        const searchLabel = document.querySelector('.breadcrumb');
        return searchLabel === null ? '' : searchLabel.innerText;
    }

    static get isSearching() {
        return document.querySelector('.loader-alone') !== null;
    }

    static get isLoading() {
        return document.querySelector('.loader') !== null;
    }

    static search(value) {
        const searchButton = document.querySelector('.accent-button');
        const inputbox = document.querySelector('.search-input');
        if (!Type.isUndefined(value)) inputbox.value = value;
        inputbox.dispatchEvent(this.inputEvent);
        searchButton.click();
    }

    static async getProductCardAsync(index, ensureDelay) {
        if (!Type.isNumber(ensureDelay) || ensureDelay < 500)
            ensureDelay = 500; // 此处等待时间过短有可能导致网页卡死

        const totalCount = this.totalItemCount;
        const container = this.container;
        if (index >= totalCount) return null;

        let itemCount = this.currentItemCount;
        while (index >= itemCount) {
            WebTool.updateProgressBar(itemCount, totalCount, Runner.completed, '\uEDE4', true)
            container.scrollTo(0, container.scrollHeight - 600);
            await delay(ensureDelay/2);
            container.scrollTo(0, container.scrollHeight);
            container.dispatchEvent(this.scrollEvent);
            if (Runner.status >= 1) return null;
            itemCount = this.currentItemCount;
            await delay(ensureDelay/2);
        }

        let card = document.getElementsByTagName('app-product-card')[index];
        const position = getRelativePosition(card, container);
        container.scrollTo(0, position.y);
        return card;
    }

    static async ensureSearchedAsync(ensureDelay) {
        await delay(1500);
        while (this.isSearching) {
            if (Runner.status >= 2) return false;
            await delay(ensureDelay);
        }
        return true
    }

    static async ensureLoadedAsync(ensureDelay) {
        while (this.isLoading) {
            if (Runner.status >= 2) return false;
            await delay(ensureDelay);
        }
        return true
    }
}