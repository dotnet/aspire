
// To avoid Flash of Unstyled Content, the body is hidden by default with
// the before-upgrade CSS class. Here we'll find the first web component
// and wait for it to be upgraded. When it is, we'll remove that class
// from the body.
const firstUndefinedElement = document.body.querySelector(":not(:defined)");

if (firstUndefinedElement) {
    customElements.whenDefined(firstUndefinedElement.localName).then(() => {
        document.body.classList.remove("before-upgrade");
    });
} else {
    // In the event this code doesn't run until after they've all been upgraded
    document.body.classList.remove("before-upgrade");
}

function isElementTagName(element, tagName) {
    return element.tagName.toLowerCase() === tagName;
}

function getFluentMenuItemForTarget(element) {
    // User could have clicked on either a path or svg (the image on the item) or the item itself
    if (isElementTagName(element, "path")) {
        return getFluentMenuItemForTarget(element.parentElement);
    }

    // in between the svg and fluent-menu-item is a span for the icon slot
    const possibleMenuItem = element.parentElement?.parentElement;
    if (isElementTagName(element, "svg") && possibleMenuItem && isElementTagName(possibleMenuItem, "fluent-menu-item")) {
        return element.parentElement.parentElement;
    }

    if (isElementTagName(element, "fluent-menu-item")) {
        return element;
    }

    return null;
}

// Register a global click event listener to handle copy button clicks.
// Required because an "onclick" attribute is denied by CSP.
document.addEventListener("click", function (e) {
    // The copy 'button' could either be a button or a menu item.
    const targetElement = isElementTagName(e.target, "fluent-button") ? e.target : getFluentMenuItemForTarget(e.target)
    if (targetElement && targetElement.getAttribute("data-copybutton")) {
        buttonCopyTextToClipboard(targetElement);
        e.stopPropagation();
    }
});

let isScrolledToContent = false;
let lastScrollHeight = null;

window.getIsScrolledToContent = function () {
    return isScrolledToContent;
}

window.setIsScrolledToContent = function (value) {
    if (isScrolledToContent != value) {
        isScrolledToContent = value;
    }
}

window.resetContinuousScrollPosition = function () {
    // Reset to scrolling to the end of the content after switching.
    setIsScrolledToContent(false);
}

window.initializeContinuousScroll = function () {
    // Reset to scrolling to the end of the content when initializing.
    // This needs to be called because the value is remembered across Aspire pages because the browser isn't reloading.
    resetContinuousScrollPosition();

    const container = document.querySelector('.continuous-scroll-overflow');
    if (container == null) {
        return;
    }

    // The scroll event is used to detect when the user scrolls to view content.
    container.addEventListener('scroll', () => {
        var v = !isScrolledToBottom(container);
        setIsScrolledToContent(v);
   }, { passive: true });

    // The ResizeObserver reports changes in the grid size.
    // This ensures that the logs are scrolled to the bottom when there are new logs
    // unless the user has scrolled to view content.
    const observer = new ResizeObserver(function () {
        lastScrollHeight = container.scrollHeight;
        if (!getIsScrolledToContent()) {
            container.scrollTop = lastScrollHeight;
        }
    });
    for (const child of container.children) {
        observer.observe(child);
    }
};

function isScrolledToBottom(container) {
    lastScrollHeight = lastScrollHeight || container.scrollHeight

    // There can be a race between resizing and scrolling events.
    // Use the last scroll height from the resize event to figure out if we've scrolled to the bottom.
    if (!getIsScrolledToContent()) {
        if (lastScrollHeight != container.scrollHeight) {
            console.log(`lastScrollHeight ${lastScrollHeight} doesn't equal container scrollHeight ${container.scrollHeight}.`);
        }
    }

    const marginOfError = 5;
    const containerScrollBottom = lastScrollHeight - container.clientHeight;
    const difference = containerScrollBottom - container.scrollTop;

    return difference < marginOfError;
}

window.buttonCopyTextToClipboard = function(element) {
    const text = element.getAttribute("data-text");
    const precopy = element.getAttribute("data-precopy");
    const postcopy = element.getAttribute("data-postcopy");

    copyTextToClipboard(element.getAttribute("id"), text, precopy, postcopy);
}

window.copyTextToClipboard = function (id, text, precopy, postcopy) {
    const button = document.getElementById(id);

    // If there is a pending timeout then clear it. Otherwise the pending timeout will prematurely reset values.
    if (button.dataset.copyTimeout) {
        clearTimeout(button.dataset.copyTimeout);
        delete button.dataset.copyTimeout;
    }

    const copyIcon = button.querySelector('.copy-icon');
    const checkmarkIcon = button.querySelector('.checkmark-icon');
    const anchoredTooltip = document.querySelector(`fluent-tooltip[anchor="${id}"]`);
    const tooltipDiv = anchoredTooltip ? anchoredTooltip.children[0] : null;
    navigator.clipboard.writeText(text)
        .then(() => {
            if (tooltipDiv) {
                tooltipDiv.innerText = postcopy;
            }
            copyIcon.style.display = 'none';
            checkmarkIcon.style.display = 'inline';
        })
        .catch(() => {
            if (tooltipDiv) {
                tooltipDiv.innerText = 'Could not access clipboard';
            }
        });

    button.dataset.copyTimeout = setTimeout(function () {
        if (tooltipDiv) {
            tooltipDiv.innerText = precopy;
        }

        copyIcon.style.display = 'inline';
        checkmarkIcon.style.display = 'none';
        delete button.dataset.copyTimeout;
   }, 1500);
};

function isActiveElementInput() {
    const currentElement = document.activeElement;
    // fluent components may have shadow roots that contain inputs
    return currentElement.tagName.toLowerCase() === "input" || currentElement.tagName.toLowerCase().startsWith("fluent") ? isInputElement(currentElement, false) : false;
}

function isInputElement(element, isRoot, isShadowRoot) {
    const tag = element.tagName.toLowerCase();
    // comes from https://developer.mozilla.org/en-US/docs/Web/API/Element/input_event
    // fluent-select does not use <select /> element
    if (tag === "input" || tag === "textarea" || tag === "select" || tag === "fluent-select") {
        return true;
    }

    if (isShadowRoot || isRoot) {
        const elementChildren = element.children;
        for (let i = 0; i < elementChildren.length; i++) {
            if (isInputElement(elementChildren[i], false, isShadowRoot)) {
                return true;
            }
        }
    }

    const shadowRoot = element.shadowRoot;
    if (shadowRoot) {
        const shadowRootChildren = shadowRoot.children;
        for (let i = 0; i < shadowRootChildren.length; i++) {
            if (isInputElement(shadowRootChildren[i], false, true)) {
                return true;
            }
        }
    }

    return false;
}

window.registerGlobalKeydownListener = function(shortcutManager) {
    function hasNoModifiers(keyboardEvent) {
        return !keyboardEvent.altKey && !keyboardEvent.ctrlKey && !keyboardEvent.metaKey && !keyboardEvent.shiftKey;
    }

    // Shift in some but not all, keyboard layouts, is used for + and -
    function modifierKeysExceptShiftNotPressed(keyboardEvent) {
        return !keyboardEvent.altKey && !keyboardEvent.ctrlKey && !keyboardEvent.metaKey;
    }

    function calculateShortcut(e) {
        if (modifierKeysExceptShiftNotPressed(e)) {
            /* general shortcuts */
            switch (e.key) {
                case "?": // help
                    return 100;
                case "S": // settings
                    return 110;

                /* panel shortcuts */
                case "T": // toggle panel orientation
                    return 300;
                case "X": // close panel
                    return 310;
                case "R": // reset panel sizes
                    return 320;
                case "+": // increase panel size
                    return 330;
                case "_": // decrease panel size
                case "-":
                    return 340;
            }
        }

        if (hasNoModifiers(e)) {
            switch (e.key) {
                case "r": // go to resources
                    return 200;
                case "c": // go to console logs
                    return 210;
                case "s": // go to structured logs
                    return 220;
                case "t": // go to traces
                    return 230;
                case "m": // go to metrics
                    return 240;
            }
        }

        return null;
    }

    const keydownListener = function (e) {
        if (isActiveElementInput()) {
            return;
        }

        // list of shortcut enum codes is in src/Aspire.Dashboard/Model/IGlobalKeydownListener.cs
        // to serialize an enum from js->dotnet, we must pass the enum's integer value, not its name
        let shortcut = calculateShortcut(e);

        if (shortcut) {
            shortcutManager.invokeMethodAsync('OnGlobalKeyDown', shortcut);
        }
    }

    window.document.addEventListener('keydown', keydownListener);

    return {
        keydownListener: keydownListener,
    }
}

window.unregisterGlobalKeydownListener = function (obj) {
    window.document.removeEventListener('keydown', obj.keydownListener);
}

window.getBrowserTimeZone = function () {
    const options = Intl.DateTimeFormat().resolvedOptions();

    return options.timeZone;
}

window.focusElement = function(selector) {
    const element = document.getElementById(selector);
    if (element) {
        element.focus();
    }
}

window.getWindowDimensions = function() {
    return {
        width: window.innerWidth,
        height: window.innerHeight
    }
}

window.listenToWindowResize = function(dotnetHelper) {
    function throttle(func, timeout) {
        let currentTimeout = null;
        return function () {
            if (currentTimeout) {
                return;
            }
            const context = this;
            const args = arguments;
            const later = () => {
                func.call(context, ...args);
                currentTimeout = null;
            }
            currentTimeout = setTimeout(later, timeout);
        }
    }

    const throttledResizeListener = throttle(() => {
        dotnetHelper.invokeMethodAsync('OnResizeAsync', { width: window.innerWidth, height: window.innerHeight });
    }, 150)

    window.addEventListener('load', throttledResizeListener);

    window.addEventListener('resize', throttledResizeListener);
}

window.registerOpenTextVisualizerOnClick = function(layout) {
    const onClickListener = function (e) {
        const fluentMenuItem = getFluentMenuItemForTarget(e.target);

        if (!fluentMenuItem) {
            return;
        }

        const text = fluentMenuItem.getAttribute("data-text");
        const description = fluentMenuItem.getAttribute("data-textvisualizer-description");

        if (text && description) {
            e.stopPropagation();

            // data-text may be larger than the max Blazor message size limit for very large strings
            // we have to stream it
            const textAsArray = new TextEncoder().encode(text);
            const textAsStream = DotNet.createJSStreamReference(textAsArray);
            layout.invokeMethodAsync("OpenTextVisualizerAsync", textAsStream, description);
        }
    }

    document.addEventListener('click', onClickListener);

    return {
        onClickListener: onClickListener,
    }
}

window.unregisterOpenTextVisualizerOnClick = function (obj) {
    document.removeEventListener('click', obj.onClickListener);
}
