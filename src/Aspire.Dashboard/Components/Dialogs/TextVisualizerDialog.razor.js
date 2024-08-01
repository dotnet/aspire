import hljs from '/js/highlight-11.10.0.min.js'

let highlightObserver = new MutationObserver((mutations) => {
    mutations.forEach((mutation) => {
        // If the data-content attribute changes, the content for this line's span has changed and so
        // we need to re-highlight it.
        if (mutation.attributeName === "data-content") {
            const target = mutation.target;
            const text = target.getAttribute("data-content");
            const language = target.getAttribute("data-language");
            target.innerHTML = hljs.highlight(language, text).value;
        }

        // On initial open, it's possible that the Virtualize component renders elements after its initial render. There is no hook
        // to know when this happens, so we need to observe the DOM for changes and highlight any new elements that are added.
        if (mutation.addedNodes.length > 0) {
            for (let i = 0; i < mutation.addedNodes.length; i++) {
                let node = mutation.addedNodes[i]
                if (node.classList && node.classList.contains("highlight-line")) {
                    hljs.highlightElement(node);
                }
            }
        }
    })
})

export function connectObserver(logContainerId) {
    highlightObserver.observe(document.getElementById(logContainerId), {
        childList: true,
        subtree: true,
        attributes: true
    })

    const existingElementsToHighlight = document.getElementsByClassName("highlight-line");
    for (let i = 0; i < existingElementsToHighlight.length; i++) {
        hljs.highlightElement(existingElementsToHighlight[i]);
    }
}

export function disconnectObserver() {
    highlightObserver.disconnect()
}
