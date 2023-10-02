const template = createRowTemplate();

/**
 * Clears all log entries from the log viewer and resets the
 * row index back to 1
 */
export function clearLogs() {
    const container = document.getElementById("logContainer");
    container.textContent = '';
}

/**
 * Adds a series of log entries to the log viewer and, if appropriate
 * scrolls the log viewer to the bottom
 * @param {LogEntry[]} logEntries
 */
export function addLogEntries(logEntries) {

    const container = document.getElementById("logContainer");

    if (container) {
        const scrollingContainer = container.parentElement;
        const isScrolledToBottom = getIsScrolledToBottom(scrollingContainer);
        const fragment = new DocumentFragment();
        for (const logEntry of logEntries) {

            const rowContainer = getNewRowContainer();
            const lineRow = rowContainer.firstElementChild;
            const lineArea = lineRow.firstElementChild;
            const timestamp = lineArea.children[1];
            if (logEntry.timestamp) {
                timestamp.textContent = logEntry.timestamp;
            } else {
                timestamp.classList.add("missing");
            }
            const content = lineArea.lastElementChild;
            content.textContent = logEntry.content;
            if (logEntry.type === "Error") {
                content.classList.add("error");
            } else if (logEntry.type === "Warning") {
                content.classList.add("warning");
            }

            fragment.appendChild(rowContainer);
        }
        container.appendChild(fragment);

        // If we were scrolled all the way to the bottom before we added the new
        // element, then keep us scrolled to the bottom. Otherwise let the user
        // stay where they are
        if (isScrolledToBottom) {
            scrollingContainer.scrollTop = scrollingContainer.scrollHeight;
        }
    }
}

/**
 * Clones the row container template for use with a new log entry
 * @returns {HTMLElement}
 */
function getNewRowContainer() {
    return template.cloneNode(true);
}

/**
 * Creates the initial row container template that will be cloned
 * for each log entry
 * @returns {HTMLElement}
 */
function createRowTemplate() {
    
    const templateString = `
        <div class="line-row-container">
            <div class="line-row">
                <span class="line-area">
                    <span class="line-number"></span>
                    <span class="timestamp"></span>
                    <span class="content"></span>
                </span>
            </div>
        </div>
    `;
    const templateElement = document.createElement("template");
    templateElement.innerHTML = templateString.trim();
    const rowTemplate = templateElement.content.firstChild;
    return rowTemplate;
}

/**
 * Checks to see if the specified scrolling container is scrolled all the way
 * to the bottom
 * @param {HTMLElement} scrollingContainer
 * @returns {boolean}
 */
function getIsScrolledToBottom(scrollingContainer) {
    return scrollingContainer.scrollHeight - scrollingContainer.clientHeight <= scrollingContainer.scrollTop + 1;
}

/**
 * @typedef LogEntry
 * @prop {string} timestamp
 * @prop {string} content
 * @prop {"Default" | "Error" | "Warning"} type
 */
