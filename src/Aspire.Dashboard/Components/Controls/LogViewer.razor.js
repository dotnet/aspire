const template = createRowTemplate();
const stdErrorBadgeTemplate = createStdErrBadgeTemplate();

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

        for (const logEntry of logEntries) {

            const rowContainer = getNewRowContainer();
            rowContainer.setAttribute("data-line-index", logEntry.lineIndex);
            rowContainer.setAttribute("data-log-id", logEntry.id);
            rowContainer.setAttribute("data-timestamp", logEntry.timestamp ?? logEntry.parentTimestamp ?? "");
            const lineRow = rowContainer.firstElementChild;
            const lineArea = lineRow.firstElementChild;
            const content = lineArea.lastElementChild;

            // logEntry.content should already be HTMLEncoded other than the <span>s produced
            // by the ANSI Control Sequence Parsing, so it should be safe to set innerHTML here
            content.innerHTML = logEntry.content;

            if (logEntry.type === "Error") {
                const stdErrorBadge = getStdErrorBadge();
                // If there's a timestamp, we want to put the badge after it to keep timestamps
                // aligned. If there's not, then we just put the badge at the start of the content
                const timestampSpan = content.querySelector(".timestamp");
                if (timestampSpan) {
                    timestampSpan.after(stdErrorBadge);
                } else {
                    content.prepend(stdErrorBadge);
                }
            }

            insertSorted(container, rowContainer, logEntry.timestamp, logEntry.parentId, logEntry.lineIndex);
        }

        // If we were scrolled all the way to the bottom before we added the new
        // element, then keep us scrolled to the bottom. Otherwise let the user
        // stay where they are
        if (isScrolledToBottom) {
            scrollingContainer.scrollTop = scrollingContainer.scrollHeight;
        }
    }
}

/**
 *
 * @param {HTMLElement} container
 * @param {HTMLElement} row
 * @param {string} timestamp
 * @param {string} parentLogId
 * @param {number} lineIndex
 */
function insertSorted(container, row, timestamp, parentId, lineIndex) {

    let prior = null;

    if (parentId) {
        // If we have a parent id, then we know we're on a non-timestamped line that is part
        // of a multi-line log entry. We need to find the prior line from that entry
        prior = container.querySelector(`div[data-log-id="${parentId}"][data-line-index="${lineIndex - 1}"]`);
    } else if (timestamp) {
        // Otherwise, if we have a timestamped line, we just need to find the prior line.
        // Since the rows are always in order in the DOM, as soon as we see a timestamp
        // that is less than the one we're adding, we can insert it immediately after that
        for (let rowIndex = container.children.length - 1; rowIndex >= 0; rowIndex--) {
            const targetRow = container.children[rowIndex];
            const targetRowTimestamp = targetRow.getAttribute("data-timestamp");

            if (targetRowTimestamp && targetRowTimestamp < timestamp) {
                prior = targetRow;
                break;
            }
        }
    }

    if (prior) {
        // If we found the prior row using either method above, go ahead and insert the new row after it
        prior.after(row);
    } else {
        // If we didn't, then just append it to the end. This happens with the first entry, but
        // could also happen if the logs don't have recognized timestamps.
        container.appendChild(row);
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
 * Clones the stderr badge template for use with a new log entry
 * @returns
 */
function getStdErrorBadge() {
    return stdErrorBadgeTemplate.cloneNode(true);
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
                <span class="line-area" role="log">
                    <span class="line-number"></span>
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
 * Creates the initial stderr badge template that will be cloned
 * for each log entry
 * @returns {HTMLElement}
 */
function createStdErrBadgeTemplate() {
    const badge = document.createElement("fluent-badge");
    badge.setAttribute("appearance", "accent");
    badge.textContent = "stderr";
    return badge;
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
 * @prop {string} id
 * @prop {string} parentId
 * @prop {number} lineIndex
 * @prop {string} parentTimestamp
 * @prop {boolean} isFirstLine
 */
