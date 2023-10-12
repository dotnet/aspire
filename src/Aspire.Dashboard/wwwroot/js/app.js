
// To avoid Flash of Unstyled Content, the body is hidden by default with
// the before-upgrade CSS class. Here we'll find the first web component
// and wait for it to be upgraded. When it is, we'll remove that class
// from the body. 
const firstUndefinedElement = document.body.querySelector(":not(:defined)");

if (firstUndefinedElement) {
    customElements.whenDefined(firstUndefinedElement.localName).then(() => {
        document.body.classList.remove("before-upgrade");
    });
}

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

window.resetContinuousScrollPosition = function () {
    // Reset to scrolling to the end of the content after switching.
    isScrolledToContent = false;
}

window.initializeContinuousScroll = function () {
    const container = document.querySelector('.continuous-scroll-overflow');
    if (container == null) {
        return;
    }

    // The scroll event is used to detect when the user scrolls to view content.
    container.addEventListener('scroll', () => {
        isScrolledToContent = !isScrolledToBottom(container);
    }, { passive: true });

    // The ResizeObserver reports changes in the grid size.
    // This ensures that the logs are scrolled to the bottom when there are new logs
    // unless the user has scrolled to view content.
    const observer = new ResizeObserver(function () {
        if (!isScrolledToContent) {
            container.scrollTop = container.scrollHeight;
        }
    });
    for (const child of container.children) {
        observer.observe(child);
    }
};

function isScrolledToBottom(container) {
    // Small margin of error. e.g. container is scrolled to within 5px of the bottom.
    const marginOfError = 5;

    return container.scrollHeight - container.clientHeight <= container.scrollTop + marginOfError;
}

window.copyTextToClipboard = function (id, text, precopy, postcopy) {
    let tooltipDiv = document.querySelector('fluent-tooltip[anchor="' + id + '"]').children[0];
    navigator.clipboard.writeText(text)
        .then(() => {
            tooltipDiv.innerText = postcopy;
        })
        .catch(() => {
            tooltipDiv.innerText = 'Could not access clipboard';
        });
    setTimeout(function () { tooltipDiv.innerText = precopy }, 1500);
};

window.updateFluentSelectDisplayValue = function (fluentSelect) {
    if (fluentSelect) {
        fluentSelect.updateDisplayValue();
    }
}

window.setThemeCookie = function () {
    let matched = window.matchMedia('(prefers-color-scheme: dark)').matches;

    if (matched) {
        document.cookie = "lastSystemTheme=dark";
        return true;
    } else {
        document.cookie = "lastSystemTheme=light";
        return false;
    }
};
