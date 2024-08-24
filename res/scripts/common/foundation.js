class Type {
    static isNull = (value) => value === null;
    static isUndefined = (value) => value === undefined;
    static isObject = (value) => typeof value === 'object';
    static isNumber = (value) => typeof value === 'number';
    static isString = (value) => typeof value === 'string';
    static isBoolean = (value) => typeof value === 'boolean';
    static isArray = (value) => Array.isArray(value);
    static isStringEmpty = (str) => str === '' || (str == null && str !== 0);
}

function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function getRelativePosition(child, parent) {
    // 获取子元素的边界矩形
    const childRect = child.getBoundingClientRect();
    // 获取父元素的边界矩形
    const parentRect = parent.getBoundingClientRect();

    // 计算子元素相对于父容器的相对位置
    const relativeX = childRect.left - parentRect.left + parent.scrollLeft;
    const relativeY = childRect.top - parentRect.top + parent.scrollTop;

    return { x: relativeX, y: relativeY };
}
