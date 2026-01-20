//@ts-check

'use strict';

const path = require('path');
const webpack = require('webpack');
const fs = require('fs');

// Copy CSS file to dist folder
const cssSourcePath = path.join(__dirname, 'src', 'webviews', 'styles.css');
const cssDestPath = path.join(__dirname, 'dist', 'styles.css');
try {
  const distDir = path.join(__dirname, 'dist');
  if (!fs.existsSync(distDir)) {
    fs.mkdirSync(distDir, { recursive: true });
  }
  fs.copyFileSync(cssSourcePath, cssDestPath);
  console.log('Copied styles.css to dist folder');
} catch (error) {
  console.warn('Could not copy styles.css:', error);
}

//@ts-check
/** @typedef {import('webpack').Configuration} WebpackConfig **/

/**
 * Configuration for the main extension code (Node.js context)
 * 
 * Note: The extension's ts-loader excludes webview React components to avoid
 * ES module/CommonJS conflicts. These components are handled by the webview configs below.
 * 
 * @type {WebpackConfig}
 */
const extensionConfig = {
  target: 'node', // VS Code extensions run in a Node.js-context 📖 -> https://webpack.js.org/configuration/node/
	mode: 'none', // this leaves the source code as close as possible to the original (when packaging we set this to 'production')

  entry: './src/extension.ts', // the entry point of this extension, 📖 -> https://webpack.js.org/configuration/entry-context/
  output: {
    // the bundle is stored in the 'dist' folder (check package.json), 📖 -> https://webpack.js.org/configuration/output/
    path: path.resolve(__dirname, 'dist'),
    filename: 'extension.js',
    libraryTarget: 'commonjs2'
  },
  externals: {
    vscode: 'commonjs vscode' // the vscode-module is created on-the-fly and must be excluded. Add other modules that cannot be webpack'ed, 📖 -> https://webpack.js.org/configuration/externals/
    // modules added here also need to be added in the .vscodeignore file
  },
  resolve: {
    // support reading TypeScript and JavaScript files, 📖 -> https://github.com/TypeStrong/ts-loader
    extensions: ['.ts', '.js']
  },
  module: {
    rules: [
      {
				test: /\.ts$/,
				exclude: [/node_modules/, /src\/webviews\/.*\/(index|.*Webview)\.tsx?$/],
				use: [
					{
						loader: 'ts-loader',
						options: {
							// Suppress TS1479 (ESM/CommonJS import conflict) - webviews handle this via Babel
							ignoreDiagnostics: [1479],
						},
					},
				],
			},
    ]
  },
  devtool: 'source-map',
  infrastructureLogging: {
    level: "log", // enables logging required for problem matchers
  },
};

/**
 * Load localization strings from package.nls.json
 * @returns {Record<string, string>}
 */
function loadLocalizations() {
  try {
    const nlsPath = path.join(__dirname, 'package.nls.json');
    const nlsContent = fs.readFileSync(nlsPath, 'utf8');
    return JSON.parse(nlsContent);
  } catch (error) {
    console.warn('Could not load localizations:', error);
    return {};
  }
}

/**
 * Creates a webpack configuration for a webview.
 * 
 * Webviews use Babel instead of ts-loader to avoid TypeScript ES module/CommonJS conflicts
 * with packages like @vscode/webview-ui-toolkit/react which are pure ESM modules.
 * 
 * Localizations from package.nls.json are automatically injected at build time.
 * 
 * To add a new webview:
 * 1. Create directory: src/webviews/{name}/
 * 2. Add index.tsx (React entry point) and {Name}Webview.tsx (React component)
 * 3. Import { l10n } from '../l10n' and use l10n('key') for localized strings
 * 4. Add {name} to the webviews array below
 * 5. Build will automatically create dist/{name}Webview.js
 * 
 * @param {string} name - The name of the webview (e.g., 'config')
 * @returns {WebpackConfig}
 */
function createWebviewConfig(name) {
  const localizations = loadLocalizations();
  
  return {
    target: 'web',
    mode: 'none',
    entry: `./src/webviews/${name}/index.tsx`,
    output: {
      path: path.resolve(__dirname, 'dist'),
      filename: `${name}Webview.js`,
    },
    resolve: {
      extensions: ['.ts', '.tsx', '.js', '.jsx'],
      extensionAlias: {
        '.js': ['.ts', '.tsx', '.js', '.jsx']
      }
    },
    module: {
      rules: [
        {
          test: /\.tsx?$/,
          exclude: /node_modules/,
          use: [
            {
              loader: 'babel-loader',
              options: {
                presets: [
                  ['@babel/preset-react', { runtime: 'automatic' }],
                  '@babel/preset-typescript'
                ]
              }
            },
          ],
        },
      ]
    },
    devtool: 'source-map',
    plugins: [
      new webpack.DefinePlugin({
        'process.env.NODE_ENV': JSON.stringify('production'),
        'process.env': JSON.stringify({}),
        '__WEBVIEW_LOCALIZATIONS__': JSON.stringify(localizations),
      }),
    ],
  };
}

// List of webviews to build.
// Add new webview names here to automatically generate webpack configs for them.
const webviews = ['config'];

module.exports = [
  extensionConfig,
  ...webviews.map(createWebviewConfig)
];