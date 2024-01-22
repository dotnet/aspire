/*
 Announces row text of the specified indices to screen readers using an offscreen div
 */
export function announceDataGridRows(dataGridContainerId, indices) {
    const containerId = "table-announce-container";
    let container = document.getElementById(containerId);
    if (container === null) {
        container = document.createElement("div");
        container.setAttribute("id", containerId);
        container.setAttribute("class", "visually-hidden");
        container.setAttribute("role", "log");

        const list = document.createElement("ul");
        container.appendChild(list);
        document.body.appendChild(container);
    }

    const list = container.children[0];

    indices.forEach(index => {
        const rowText = getRowText(dataGridContainerId, index);
        if (rowText) {
            const newItem = document.createElement("li");
            newItem.innerHTML = rowText;
            list.appendChild(newItem);
        }
    });
}

function getRowText(dataGridContainerId, index) {
    const dataGrid = document.getElementById(dataGridContainerId).children[0];
    const row = dataGrid.getElementsByTagName("fluent-data-grid-row")[index + 1];
    if (!row) {
        return null;
    }

    const cells = row.getElementsByTagName("fluent-data-grid-cell");
    let text = "";
    for (let i = 0; i < cells.length; i++) {
        text += cells[i].textContent + " ";
    }

    return text;
}
