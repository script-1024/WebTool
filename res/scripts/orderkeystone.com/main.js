/* 接受一个 `app-product-card` 物件或数字索引 */
async function readDataAsync(target, ensureDelay) {
    // 若指定的 `target` 是数字不是物件
    if (TypeChecker.isNumber(target)) target = await getProductCardAsync(target);
    if (target === null) return null;

    const section = target.querySelector('.part-card section');
    const info = section.querySelector('.product-info');
    const cost = section.querySelector('.product-costs');
    const link = info.querySelector('.availability-info div a');
    const result = {id: '', list_price: 0, your_price: 0, name: '', description: ''};
    
    // 外层数据
    result.id = link.text;
    result.name = info.querySelector('.product-title').textContent;
    result.description = info.querySelector('.part-description').innerText.replace('• ', '').replaceAll('\n• ', ', ');
    result.list_price = parseFloat(cost.querySelectorAll('.cost-details .cost-row span')[1].textContent.replace(/\$|,/g, ''));
    result.your_price = parseFloat(cost.querySelectorAll('.cost-details .cost-row.your-cost span')[1].textContent.replace(/\$|,/g, ''));
    
    /*
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
    */

    return result;
}

/* 获取当前页面所有物品的数据 */
async function getAllDataAsync(readDelay, ensureDelay, startIndex) {
    const count = getItemCount();
    if (Runner.IsProcessKilled == 1) Runner.IsProcessKilled = 0;

    for (let i=startIndex; i<count; i++) {
        if (Runner.IsProcessKilled >= 1) break;
        Runner.Fetched = i;

        let data = await readDataAsync(i, ensureDelay);
        if (TypeChecker.isNull(data)) break;

        if (Runner.TempArray.length < Runner.Capacity) Runner.TempArray.push(data);
        else {
            postMsg('WriteToFile', Runner.TempArray);
            Runner.TempArray.length = 0; // 清空数组
        }

        postMsg('UpdateProgressBar', {Current: (i + 1), Total: count, Completed: Runner.Completed});
        console.log(`Fetched: ${i + 1}/${count}, Completed: ${Runner.Completed}`);
        await delay(readDelay);
    }
}

/* 自动化脚本主函数，将依照 search_list 进行搜索和抓取数据 */
async function main(readDelay, ensureDelay, cycleDelay, completed = 0, fetched = 0) {
    for (let i=completed; i<Runner.SearchList.length; i++) {
        if (Runner.IsProcessKilled >= 2) break;

        // 进行搜索
        if (getCurrentKeyword() !== Runner.SearchList[i]) search(Runner.SearchList[i]);
        Runner.Completed = i;

        // 确保加载完成
        if (!await ensureLoadedAsync(ensureDelay)) break;

        await getAllDataAsync(readDelay, ensureDelay, fetched);

        // 更新状态
        fetched = 0;

        // 每个周期结束后休息一次
        await delay(cycleDelay);
    }
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

    // 暫存结果
    Capacity: 25,
    TempArray: [],
    Fetched: 0,
    Completed: 0,

    // 函数 跳过、中止、恢复、开始
    Skip: () => {
        Runner.IsProcessKilled = 1;
        closeAllDialogAsync();
    },
    StopAll: () => {
        Runner.IsProcessKilled = 2;
        closeAllDialogAsync();
    },
    RunAsync: async (readDelay, ensureDelay, cycleDelay, completed = 0, fetched = 0) => {
        if (!TypeChecker.isNumber(readDelay) || readDelay <= 0) readDelay = Runner.DefaultDelay;
        if (!TypeChecker.isNumber(ensureDelay) || ensureDelay <= 0) ensureDelay = Runner.DefaultDelay;
        if (!TypeChecker.isNumber(cycleDelay) || cycleDelay <= 0) cycleDelay = Runner.DefaultDelay;

        Runner.TempArray = [];
        Runner.IsProcessKilled = 0;

        postMsg('ShowProgressBar');
        await main(readDelay, ensureDelay, cycleDelay, completed, fetched);
        if (Runner.TempArray.length > 0) postMsg('WriteToFile', Runner.TempArray);

        await delay(5000); // 等待五秒让用户端有充分时间写入文件
        postMsg('Finished', (Runner.IsProcessKilled !== 0));
    }
}
