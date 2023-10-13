import * as LibraryExports from "/_content/Microsoft.Fast.Components.FluentUI/Microsoft.Fast.Components.FluentUI.lib.module.js";

export function beforeServerStart(options, extensions) {
    let wcScript = document.createElement('script');
    wcScript.type = 'module';
    wcScript.src = './_content/Aspire.Dashboard/js/web-components-v2.5.16-custom.min.js';
    wcScript.async = true;
    document.body.appendChild(wcScript);

    let libraryStyle = document.createElement('link');
    libraryStyle.rel = 'stylesheet';
    libraryStyle.type = 'text/css';
    libraryStyle.href = './_content/Microsoft.Fast.Components.FluentUI/css/Microsoft.Fast.Components.FluentUI.css';
    document.head.appendChild(libraryStyle);
}

export function afterServerStarted(blazor) {
    LibraryExports.afterStarted(blazor);
}
