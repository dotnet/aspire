import hljs from '/js/highlight-11.10.0.min.js'

let highlightObserver = new MutationObserver((mutations) => {
    mutations.forEach((mutation) => {
        if (mutation.addedNodes.length === 0) {
            return;
        }

        for (let i = 0; i < mutation.addedNodes.length; i++) {
            let node = mutation.addedNodes[i]
            if (node.classList && node.classList.contains("highlight-line")) {
                hljs.highlightElement(node);
            }
        }
    })
})

export function connectObserver() {
    highlightObserver.observe(document.getElementById("test"), {
        childList: true,
        subtree: true
    })

    const existingElementsToHighlight = document.getElementsByClassName("highlight-line");
    for (let i = 0; i < existingElementsToHighlight.length; i++) {
        hljs.highlightElement(existingElementsToHighlight[i]);
    }
}

export function disconnectObserver() {
    highlightObserver.disconnect()
}
