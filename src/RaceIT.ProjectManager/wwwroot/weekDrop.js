export function containsPoint(element, x, y) {
    if (!element?.isConnected) return false;
    const rect = element.getBoundingClientRect();
    return x >= rect.left && x < rect.right && y >= rect.top && y < rect.bottom;
}
