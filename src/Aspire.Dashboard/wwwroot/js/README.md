# JavaScript Libraries

The .NET Aspire Dashboard bundles a few JavaScript libraries.

## Plotly

The default Plotly JS library is around 4MB in size (minified), as it supports many different chart types. Currently, we only use simple chart types, so can use the `basic` distribution which is around 1MB instead.

From [Plotly JS's docs](https://github.com/plotly/plotly.js/blob/22efc2fb76f4c890a2c33448e6f1485ecab77f26/dist/README.md#plotlyjs-basic):

> The `basic` partial bundle contains trace modules `bar`, `pie` and `scatter`.

If we ever want to show more chart types than those, we'll need to change the bundle we use.

## xterm.js

The [xterm.js](https://xtermjs.org/) library is used for the interactive terminal feature. We bundle:

- `xterm-5.5.0.min.js` - Core terminal emulator
- `xterm-addon-fit-0.10.0.min.js` - Addon for auto-fitting terminal to container
- `xterm-addon-web-links-0.11.0.min.js` - Addon for clickable links
- `xterm-addon-unicode11-0.8.0.min.js` - Addon for Unicode 11 support (proper box-drawing characters)

The CSS is in `../css/xterm-5.5.0.min.css`.

To update xterm.js, download new versions from npm/jsdelivr and update the file names in `terminal-interop.js`.
