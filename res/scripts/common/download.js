class DownloadFile {
    static AsText(filename, text) {
        // 创建 Blob 对象
        const blob = new Blob([text], { type: 'text/plain;charset=utf-8' });
        const link = document.createElement('a');

        // 创建指向 Blob 对象的 URL
        const url = URL.createObjectURL(blob);
        link.href = url;
        link.setAttribute('download', filename);

        // 隐藏链接并触发下载
        link.style.display = 'none';
        document.body.appendChild(link);
        link.click();

        // 移除链接并释放 URL 对象
        document.body.removeChild(link);
        URL.revokeObjectURL(url); // 释放内存
    }

    static AsJson(filename, obj, getResultOnly = false) {
        let jsonContent = JSON.stringify(obj, null, 4);
        if (getResultOnly === true) return jsonContent;
        else this.AsText(filename, jsonContent);
    }

    static AsCsv(filename, objArray, getResultOnly = false) {
        if (!Type.isArray(objArray) || objArray.length === 0) {
            console.error("Invalid data: Must be a non-empty array of objects.");
            return;
        }

        function flattenObject(obj, parentKey = '', result = {}) {
            for (const key in obj) {
                if (obj.hasOwnProperty(key)) {
                    const newKey = parentKey ? `${parentKey}.${key}` : key;
    
                    if (Type.isObject(obj[key]) && !Type.isArray(obj[key])) {
                        // 递归展平嵌套对象
                        flattenObject(obj[key], newKey, result);
                    }
                    else if (Type.isArray(obj[key])) {
                        // 将数组转换为换行分隔的字符串
                        result[newKey] = obj[key].join('\n');
                    }
                    else result[newKey] = obj[key];
                }
            }
            return result;
        }

        // 展平每个对象，生成平面结构
        const flattenedData = objArray.map(item => flattenObject(item));

        // 获取所有的列名（键）
        const headers = [...new Set(flattenedData.flatMap(item => Object.keys(item)))];

        // 生成 CSV 内容
        let csvContent = headers.join(',') + '\n'; // 表头行
        csvContent += flattenedData.map(item =>
            headers.map(header => Type.isStringEmpty(item[header]) ? '""' : `"${item[header]}"`).join(',')
        ).join('\n'); // 数据行

        if (getResultOnly === true) return csvContent;
        else this.AsText(filename, csvContent);
    }
}
