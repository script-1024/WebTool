class Runner {
    // 搜索列表
    static searchList = [
        'ABP', 'AC1', 'AC2', 'AC3', 'AC4', 'ACA', 'ACK', 'ACM',
        'ACP', 'ACQ', 'ACT', 'ACU', 'ACX', 'AU1', 'AU2', 'AU3',
        'AU4', 'ALYA', 'ALYB', 'ALYC', 'ALYD', 'ALYF', 'ALYG',
        'ALYH', 'ALYI', 'ALYJ', 'ALYK', 'ALYL', 'ALYM', 'ALYN',
        'ALYP', 'ALYR', 'ALYS', 'ALYT', 'ALYV', 'ALYW', 'ATAA',
        'BAT', 'BM', 'CAC', 'CAF', 'CCA', 'CCI', 'CH1', 'CH2',
        'CH3', 'CH4', 'CH5', 'FI', 'FO1', 'FO2', 'FO3', 'FO4',
        'FW', 'GM1', 'GM2', 'GM3', 'GM4', 'GMK', 'HD', 'HO',
        'HU', 'HY', 'IN', 'IW', 'IZ', 'JOH', 'KI', 'LX', 'MA',
        'MB', 'MC', 'MI', 'NI', 'PLA', 'PO', 'RAD', 'RO', 'SC',
        'SP', 'ST', 'SU', 'SZ', 'TA', 'TNK', 'TO1', 'TO2', 'TO3',
        'TO4', 'VW', 'WI'
    ]

    // 若未指定延时则使用预设值 500 毫秒
    static defaultDelay = 500;

    // 运行结果
    static fetched = 0;
    static completed = 0;
    
    // 记录进程状态
    static state = 0;
    static skip = () => this.state = 1;
    static stopAll = () => this.state = 2;

    // 物件池
    static objectPool = [];

    static createObject() {
        return this.objectPool.length > 0 ? this.objectPool.pop() : {
            id: '',
            list_price: 0,
            your_price: 0,
            name: '',
            description: ''
        };
    }

    static releaseObject(obj) {
        // 重设物件属性
        obj.id = '';
        obj.list_price = 0;
        obj.your_price = 0;
        obj.name = '';
        obj.description = '';
        this.objectPool.push(obj);
    }

    static async readDataAsync(index, ensureDelay) {
        const card = await OrderKeystone.getProductCardAsync(index, ensureDelay);
        if (card === null) return false;
    
        const info = card.querySelector('.product-info');
        const cost = card.querySelector('.product-costs');
        const result = this.createObject();
        
        // 外层数据
        result.id = info.querySelector('.availability-info div a').text;
        result.name = info.querySelector('.product-title').textContent;
        result.description = info.querySelector('.part-description').innerText.replace('• ', '').replaceAll('\n• ', ', ');
        result.list_price = parseFloat(cost.querySelectorAll('.cost-row span')[1].textContent.replace(/\$|,/g, ''));
        result.your_price = parseFloat(cost.querySelectorAll('.cost-row.your-cost span')[1].textContent.replace(/\$|,/g, ''));
    
        WebTool.postMsg('WriteToFile', result);
        await delay(ensureDelay);
        this.releaseObject(result);
        return true;
    }

    static async getPageAsync(readDelay, ensureDelay) {
        const totalCount = OrderKeystone.totalItemCount;
        if (this.state == 1) this.state = 0;

        WebTool.updateProgressBar(this.fetched, totalCount, this.completed);
        while (this.fetched < totalCount) {
            if (this.state >= 1) break; // 停止或跳过
            if(!await this.readDataAsync(this.fetched, ensureDelay)) return;
            WebTool.updateProgressBar(++this.fetched, totalCount, this.completed);
            await delay(readDelay);
        }
    }

    static async runAsync(readDelay, ensureDelay, cycleDelay, completed = 0, fetched = 0) {
        if (!Type.isNumber(readDelay) || readDelay <= 0) readDelay = this.defaultDelay;
        if (!Type.isNumber(ensureDelay) || ensureDelay <= 0) ensureDelay = this.defaultDelay;
        if (!Type.isNumber(cycleDelay) || cycleDelay <= 0) cycleDelay = this.defaultDelay;

        this.state = 0;
        this.fetched = fetched;
        this.completed = completed;
        WebTool.showProgressBar();
        
        for (let i=completed; i<this.searchList.length; i++) {
            if (this.state >= 2) break;
    
            // 进行搜索
            if (OrderKeystone.currentSearched !== this.searchList[i]) OrderKeystone.search(this.searchList[i]);
    
            // 确保搜索完成
            if (!await OrderKeystone.ensureSearchedAsync(ensureDelay)) break;
    
            // 开始抓取本页
            await this.getPageAsync(readDelay, ensureDelay);
            if (this.state >= 2) break;

            // 更新计数器
            this.fetched = 0;
            this.completed++;
    
            // 每个周期结束后休息一次
            await delay(cycleDelay);
        }

        if (this.state > 0) WebTool.updateProgressBar(this.fetched, OrderKeystone.totalItemCount, this.completed);
        else {
            WebTool.hideProgressBar();
            WebTool.showTip('網頁通知', '已順利完成所有操作');
        }

        WebTool.postMsg('Finished', this.state);
    }
}
