window.scrollToEndInTextArea = function (classSelector) {
    let fluentTextAreas = document.querySelectorAll(classSelector);
    if (fluentTextAreas && fluentTextAreas.length > 0) {
        for (const fluentTextArea of fluentTextAreas) {
            if (fluentTextArea && fluentTextArea.shadowRoot) {
                let textArea = fluentTextArea.shadowRoot.querySelector('textarea');
                textArea.scrollTop = textArea.scrollHeight;
            }
        }
    }
};

let isScrolledToContent = false;

window.switchLogsApplication = function () {
    // Reset to scrolling to the end of the content after switching.
    isScrolledToContent = false;
}

window.scollToLogsEnd = function () {
    const container = document.querySelector('.SemanticLogsOverflow');
    const grid = document.querySelector('.SemanticLogsDataGrid');
    if (container == null || grid == null) {
        return;
    }

    container.onscroll = (event) => {
        isScrolledToContent = !isScrolledToBottom(container);
    };

    // This method scrolls to the bottom of the logs data grid. It is called by blazor when rendering updates.
    // The MutationObserver is used to detect when the aria-rowcount attribute is updated, which indicates that
    // the logs have been updated and populated into the grid. At this point, we can scroll to the bottom of the grid.

    // Options for the observer (which mutations to observe)
    const config = { attributes: true, attributeFilter: ["aria-rowcount"] };

    let observer = null;
    const callback = (mutationList, observer) => {
        // Only scroll to the bottom if the current position is at the bottom.
        // Prevents scrolling to the bottom when the user has scrolled up to view previous logs.
        if (!isScrolledToContent) {
            container.scrollTop = container.scrollHeight;
        }

        // Disconnect the observer when the logs have been scrolled to the bottom.
        // Blazor rendering updates will create an observer.
        observer.disconnect();
    };

    observer = new MutationObserver(callback);
    observer.observe(grid, config);
};

function isScrolledToBottom(container) {
    // Small margin of error. e.g. container is scrolled to within 5px of the bottom.
    const marginOfError = 5;

    return container.scrollHeight - container.clientHeight <= container.scrollTop + marginOfError;
}

window.copyTextToClipboard = function (id, text, precopy, postcopy) {
    let tooltipDiv = document.querySelector('fluent-tooltip[anchor=' + id + ']').children[0];
    navigator.clipboard.writeText(text)
        .then(() => {
            tooltipDiv.innerText = postcopy;
        })
        .catch(() => {
            tooltipDiv.innerText = 'Could not access clipboard';
        });
    setTimeout(function () { tooltipDiv.innerText = precopy }, 1500);
};
