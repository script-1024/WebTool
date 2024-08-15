const TypeChecker = {
    isNull: (value) => value === null,
    isUndefined: (value) => value === undefined,
    isNullOrUndefined: (value) => (value == null && value !== 0),
    isObject: (value) => typeof value === 'object',
    isNumber: (value) => typeof value === 'number',
    isString: (value) => typeof value === 'string',
    isBoolean: (value) => typeof value === 'boolean',
    isArray: (value) => Array.isArray(value)
}

const string = {
    isNullOrEmpty: (str) => str === '' || (str == null && str !== 0)
}

function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function getRelativePosition(childElement, parentElement) {
    // 获取子元素的边界矩形
    const childRect = childElement.getBoundingClientRect();
    // 获取父元素的边界矩形
    const parentRect = parentElement.getBoundingClientRect();

    // 计算子元素相对于父容器的相对位置
    const relativeX = childRect.left - parentRect.left + parentElement.scrollLeft;
    const relativeY = childRect.top - parentRect.top + parentElement.scrollTop;

    return { x: relativeX, y: relativeY };
}
