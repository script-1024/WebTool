class OrderKeystone {
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
        inputbox.dispatchEvent(new Event('input'));
        searchButton.click();
    }

    static async getProductCardAsync(index, ensureDelay) {
        const totalCount = this.totalItemCount;
        const container = this.container;
        if (index >= totalCount) return null;

        let itemCount = this.currentItemCount;
        while (index >= itemCount) {
            WebTool.updateProgressBar(itemCount, totalCount, Runner.completed, '\uEDE4', true)
            container.scrollTo(0, container.scrollHeight);
            await delay(600);
            container.dispatchEvent(new Event("scroll"));
            if (Runner.state >= 1) return null;
            itemCount = this.currentItemCount;
        }
    
        let card = document.getElementsByTagName('app-product-card')[index];
        const position = getRelativePosition(card, container);
        container.scrollTo(0, position.y);
        return card;
    }

    static async ensureSearchedAsync(ensureDelay) {
        await delay(1500);
        while (this.isSearching) {
            if (Runner.state >= 2) return false;
            await delay(ensureDelay);
        }
        return true
    }

    static async ensureLoadedAsync(ensureDelay) {
        while (this.isLoading) {
            if (Runner.state >= 2) return false;
            await delay(ensureDelay);
        }
        return true
    }
}