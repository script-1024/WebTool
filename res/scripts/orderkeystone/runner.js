class Runner {
    // 搜索列表
    static searchList = []

    // 若未指定延时则使用预设值 500 毫秒
    static defaultDelay = 500;

    // 运行结果
    static fetched = 0;
    static completed = 0;
    
    // 记录进程状态
    static status = 0;
    static skip = () => this.status = 1;
    static stopAll = () => this.status = 2;

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
        this.releaseObject(result);
        return true;
    }

    static async getPageAsync(readDelay, ensureDelay, startIndex) {
        const totalCount = OrderKeystone.totalItemCount;
        if (this.status == 1) this.status = 0;

        if (!Type.isUndefined(startIndex)) this.fetched = startIndex;
        WebTool.updateProgressBar(this.fetched, totalCount, this.completed);
        while (this.fetched < totalCount) {
            if (this.status >= 1) break; // 停止或跳过
            if(!await this.readDataAsync(this.fetched, ensureDelay)) return;
            WebTool.updateProgressBar(++this.fetched, totalCount, this.completed);
            if (this.fetched < totalCount) await delay(readDelay);
        }
    }

    static async runAsync(readDelay, ensureDelay, cycleDelay, completed = 0, fetched = 0) {
        if (!Type.isNumber(readDelay) || readDelay <= 0) readDelay = this.defaultDelay;
        if (!Type.isNumber(ensureDelay) || ensureDelay <= 0) ensureDelay = this.defaultDelay;
        if (!Type.isNumber(cycleDelay) || cycleDelay <= 0) cycleDelay = this.defaultDelay;

        this.status = 0;
        this.fetched = fetched;
        this.completed = completed;
        WebTool.showProgressBar();
        
        const steps = this.searchList.length
        while (this.completed < steps) {
            if (this.status >= 2) break;
    
            // 进行搜索
            let searchKeyword = this.searchList[this.completed];
            if (OrderKeystone.currentSearched !== searchKeyword)
                OrderKeystone.search(searchKeyword);
    
            // 确保搜索完成
            if (!await OrderKeystone.ensureSearchedAsync(ensureDelay)) break;
    
            // 开始抓取本页
            await this.getPageAsync(readDelay, ensureDelay);
            if (this.status >= 2) break;

            // 更新计数器
            this.fetched = 0;
            this.completed++;
    
            // 每个周期结束后休息一次
            if (this.completed < steps) await delay(cycleDelay);
        }

        if (this.status > 0) {
            WebTool.updateProgressBar(this.fetched, OrderKeystone.totalItemCount, this.completed);
            WebTool.showTip('網頁通知', '已停止抓取操作');
            WebTool.postMsg('Terminated');
        }
        else {
            WebTool.hideProgressBar();
            WebTool.showTip('網頁通知', '已順利完成所有操作');
            WebTool.postMsg('Finished');
        }
    }
}
