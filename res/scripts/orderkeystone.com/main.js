/* 接受一个 `app-product-card` 物件或数字索引 */
async function readDataAsync(target, ensureDelay) {
    // 若指定的 `target` 是数字不是物件
    if (TypeChecker.isNumber(target)) target = await getProductCardAsync(target);

    const section = target.querySelector('.part-card section');
    const info = section.querySelector('.product-info');
    const cost = section.querySelector('.product-costs');
    const link = info.querySelector('.availability-info div a');
    const result = {id:'', name: '', description: '', price: {list: '', yours: ''}};
    
    // 外层数据
    result.id = link.text;
    result.name = info.querySelector('.product-title').textContent;
    result.description = info.querySelector('.part-description').innerText.replaceAll('• ', '').replaceAll('\n', ' ');
    result.price.list = cost.querySelectorAll('.cost-details .cost-row span')[1].textContent.replace('$', '');
    result.price.yours = cost.querySelectorAll('.cost-details .cost-row.your-cost span')[1].textContent.replace('$', '');
    link.click();

    // 确保窗口加载完成
    if(!await ensureDialogAsync(ensureDelay)) return null;

    const fitmentsBody = document.querySelector('mat-tab-body');
    const tile = fitmentsBody.getElementsByTagName('mat-grid-tile');
    result.fitments = new Array(tile.length);
    for (let i=0; i<tile.length; i++) {
        result.fitments[i] = tile[i].innerText;
    }

    // 关闭窗口
    await closeAllDialogAsync();

    return result;
}

/* 获取当前页面所有物品的数据 */
async function getAllDataAsync(readDelay, ensureDelay, startIndex, array = []) {
    const count = getItemCount();
    if (Runner.IsProcessKilled == 1) Runner.IsProcessKilled = 0;

    for (let i=startIndex; i<count; i++) {
        if (Runner.IsProcessKilled >= 1) break;
        Runner.LastRun.Fetched = i;

        let temp = await readDataAsync(i, ensureDelay);
        if (TypeChecker.isNull(temp)) break;

        if (i < array.length) array[i] = temp;
        else array.push(temp);

        console.log(`Fetched: ${i + 1}/${count}, Completed: ${Runner.LastRun.Completed}`);
        await delay(readDelay);
    }

    return array;
}

/* 自动化脚本主函数，将依照 search_list 进行搜索和抓取数据 */
async function main(readDelay, ensureDelay, cycleDelay, completed = 0, fetched = 0, array = []) {
    for (let i=completed; i<Runner.SearchList.length; i++) {
        if (Runner.IsProcessKilled >= 2) break;

        // 进行搜索
        if (getCurrentKeyword() !== Runner.SearchList[i]) search(Runner.SearchList[i]);
        Runner.LastRun.Completed = i;

        // 确保加载完成
        if (!await ensureLoadedAsync(ensureDelay)) break;

        let temp = (array.length > i && !TypeChecker.isNullOrUndefined(array[i])) ? array[i] : []
        await getAllDataAsync(readDelay, ensureDelay, fetched, temp);
        if (i < array.length) array[i] = temp;
        else array.push(temp);

        // 更新状态
        fetched = 0;

        // 每个周期结束后休息一次
        await delay(cycleDelay);
    }

    return array;
}

const Runner = {
    // 记录进程终止情形
    IsProcessKilled: 0,

    // 搜索列表
    SearchList: [ 'ABPCV31E',
        'AC', 'AU', 'BM', 'CH', 'FI', 'FO', 'GM',
        'HO', 'HU', 'HY', 'IN', 'IZ', 'KI', 'LX',
        'MA', 'MB', 'MC', 'MI', 'NI', 'PO', 'RO',
        'SC', 'SU', 'SZ', 'TA', 'TO', 'VW', 'WI'
    ],

    // 若不指定延时则使用预设值 1000 毫秒
    DefaultDelay: 1000,

    // 运行结果
    Result: [],
    FlatResult: [],
    LastRun: {
        Fetched: 0,
        Completed: 0,
        rd: 0, ed: 0, cd: 0, thisPageOnly: false
    },

    // 函数 跳过、中止、恢复、开始
    Skip: () => {
        Runner.IsProcessKilled = 1;
        closeAllDialogAsync();
    },
    StopAll: () => {
        Runner.IsProcessKilled = 2;
        closeAllDialogAsync();
    },
    Resume: () => {
        let lastRun = Runner.LastRun;
        if (lastRun.thisPageOnly) Runner.GetThisPage(lastRun.rd, lastRun.ed, lastRun.Fetched + 1)
        else Runner.RunAsync(lastRun.rd, lastRun.ed, lastRun.cd, lastRun.Completed, lastRun.Fetched + 1);
    },
    GetThisPage: async (readDelay, ensureDelay, fetched = 0) => {
        if (!TypeChecker.isNumber(readDelay) || readDelay <= 0) readDelay = Runner.DefaultDelay;
        if (!TypeChecker.isNumber(ensureDelay) || ensureDelay <= 0) ensureDelay = Runner.DefaultDelay;
        
        Runner.IsProcessKilled = 0;
        Runner.LastRun.rd = readDelay;
        Runner.LastRun.ed = ensureDelay;
        Runner.LastRun.thisPageOnly = true;
        if (TypeChecker.isNullOrUndefined(Runner.Result)) Runner.Result = [];
        Runner.Result = await getAllDataAsync(readDelay, ensureDelay, fetched, Runner.Result);
        Runner.FlatResult = Runner.Result.flat(1);
    },
    RunAsync: async (readDelay, ensureDelay, cycleDelay, completed = 0, fetched = 0) => {
        if (!TypeChecker.isNumber(readDelay) || readDelay <= 0) readDelay = Runner.DefaultDelay;
        if (!TypeChecker.isNumber(ensureDelay) || ensureDelay <= 0) ensureDelay = Runner.DefaultDelay;
        if (!TypeChecker.isNumber(cycleDelay) || cycleDelay <= 0) cycleDelay = Runner.DefaultDelay;

        Runner.IsProcessKilled = 0;
        Runner.LastRun.rd = readDelay;
        Runner.LastRun.ed = ensureDelay;
        Runner.LastRun.cd = cycleDelay;
        Runner.LastRun.thisPageOnly = false;
        if (TypeChecker.isNullOrUndefined(Runner.Result)) Runner.Result = [];
        Runner.Result = await main(readDelay, ensureDelay, cycleDelay, completed, fetched, Runner.Result);
        Runner.FlatResult = Runner.Result.flat(1);
    }
}
