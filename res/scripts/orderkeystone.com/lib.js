function search(value) {
    const searchButton = document.querySelector('.accent-button');
    const inputbox = document.querySelector('.search-input');
    const inputUIEvent = new UIEvent('input');
    inputbox.value = value;
    inputbox.dispatchEvent(inputUIEvent);
    searchButton.click();
}

function hasDialog() {
    const dialog = document.querySelector('mat-dialog-container');
    return dialog !== null;
}

function closeDialog() {
    const closeButton = document.querySelector('.close-button');
    closeButton?.click();
}

async function closeAllDialogAsync(maxRetries = 30) {
    let retries = 0;
    while (hasDialog() && retries < maxRetries) {
        let closeButton = document.querySelector('.close-button');
        closeButton?.click();
        await delay(20);
        retries++;
    }
}

function isDialogLoading() {
    const loader = document.querySelector('.loader');
    return loader !== null;
}

function isLoading() {
    const loaderA = document.querySelector('.loader-alone');
    const loaderB = document.querySelector('.loader');
    return loaderA !== null || loaderB !== null;
}

function getContainer() {
    return document.getElementById('search-results-container');
}

function getItemCount() {
    const resultText = document.querySelector('.results-text');
    const count = parseInt(resultText?.getElementsByTagName('span')[1].textContent);
    return isNaN(count) ? 0 : count;
}

function getCurrentKeyword() {
    const searchLabel = document.querySelector('.breadcrumb');
    return TypeChecker.isNull(searchLabel) ? '' : searchLabel.innerText;
}

async function getProductCardAsync(index) {
    const count = getItemCount();
    const container = getContainer();
    if (index >= count) return null;

    let card = document.getElementsByTagName('app-product-card')[index];
    while (TypeChecker.isUndefined(card)) {
        container.scrollTo(0, container.scrollHeight - 600);
        await delay(20);
        container.scrollTo(0, container.scrollHeight);
        if (!await ensureLoadedAsync(500)) return null;
        card = document.getElementsByTagName('app-product-card')[index];
    }

    const position = getRelativePosition(card, container);
    container.scrollTo(0, position.y);
    return card;
}

async function ensureDialogAsync(ensureDelay) {
    while (!hasDialog() || isDialogLoading()) {
        if (Runner.IsProcessKilled >= 1) return false;
        await delay(ensureDelay);
    }
    return true
}

async function ensureLoadedAsync(ensureDelay) {
    while (isLoading()) {
        if (Runner.IsProcessKilled >= 2) return false;
        await delay(ensureDelay);
    }
    return true
}
