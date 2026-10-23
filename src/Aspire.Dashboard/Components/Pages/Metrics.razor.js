const registrations = new WeakMap();

export function registerMetricsSplitter(element, dotNetRef) {
    unregisterMetricsSplitter(element);

    const splitter = element.querySelector(':scope > .fluent-multi-splitter');
    const bar = splitter?.querySelector(':scope > .fluent-multi-splitter-bar');
    const groupsPane = splitter?.querySelector(':scope > .fluent-multi-splitter-pane:has(> .metric-groups)');
    if (!splitter || !bar || !groupsPane) {
        return false;
    }

    let disposed = false;

    const onDoubleClick = event => {
        event.preventDefault();

        const currentWidth = groupsPane.getBoundingClientRect().width;
        const hiddenLabelWidth = [...groupsPane.querySelectorAll('.metric-group-name')]
            .reduce((maximum, label) => Math.max(maximum, label.scrollWidth - label.clientWidth), 0);
        const splitterWidth = splitter.getBoundingClientRect().width;
        if (splitterWidth <= 0) {
            return;
        }

        const targetWidth = Math.min(currentWidth + hiddenLabelWidth, splitterWidth / 2);
        const targetPercent = targetWidth / splitterWidth * 100;
        dotNetRef.invokeMethodAsync('SetMetricGroupsPaneSizeAsync', targetPercent).catch(error => {
            if (!disposed) {
                console.error('Failed to fit the metrics instrument selector.', error);
            }
        });
    };

    bar.addEventListener('dblclick', onDoubleClick);

    registrations.set(element, () => {
        disposed = true;
        bar.removeEventListener('dblclick', onDoubleClick);
    });

    return true;
}

export function unregisterMetricsSplitter(element) {
    registrations.get(element)?.();
    registrations.delete(element);
}
