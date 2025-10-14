const nls = require('vscode-nls-dev')
const gulp = require('gulp')
const { series, parallel } = require('gulp')
const path = require('path')
const es = require('event-stream')
const ts = require('gulp-typescript')
const sourcemaps = require('gulp-sourcemaps')
const gulpFilter = require('gulp-filter')
const File = require('vinyl')
const fs = require('fs')

/*
Language Set Docs:
https://ceapex.visualstudio.com/CEINTL/_git/SoftwareLocalization?path=/src/OneBranchPackages/Localization.Languages/Localization.Languages.props&version=GBmain&line=43&lineEnd=44&lineStartColumn=1&lineEndColumn=93&lineStyle=plain&_a=contents
<!--Visual Studio languages set -->
<VS_Main_Languages>cs;de;es;fr;it;ja;ko;pl;pt-BR;ru;tr;zh-Hans;zh-Hant</VS_Main_Languages>
*/
const languages = [
	{ folderName: 'cs', id: 'cs' },
	{ folderName: 'de', id: 'de' },
	{ folderName: 'es', id: 'es' },
	{ folderName: 'fr', id: 'fr' },
	{ folderName: 'it', id: 'it' },
	{ folderName: 'ja', id: 'ja' },
	{ folderName: 'ko', id: 'ko' },
	{ folderName: 'pl', id: 'pl' },
	{ folderName: 'pt-br', id: 'pt-br' },
	{ folderName: 'ru', id: 'ru' },
	{ folderName: 'tr', id: 'tr' },
	{ folderName: 'zh-hans', id: 'zh-cn' },
	{ folderName: 'zh-hant', id: 'zh-tw' },
]

const extensionName = 'aspire-vscode'
const rootDir = __dirname
const xliffDir = path.join(rootDir, '/loc/xliff');
const encoding = 'utf8'
const locIntermediates = path.join(rootDir, '/.localization')
const bundleDefaultFile = path.join(locIntermediates, 'out', 'nls.bundle.json')
// Creates *.i18n.json files in out/loc/LANG directory with translated strings from loc/xliff/**/*.xlf files
const xlifftoi18n = () => {
	// Match all language-prefixed xlf files (e.g., aspire-vscode.cs.xlf) but not the base English file (aspire-vscode.xlf)
	const xlfFiles = `${xliffDir}/${extensionName}.*.xlf`;
	return (
		gulp
			.src([xlfFiles])
			.pipe(prepareJsonFiles())
			.pipe(gulp.dest(locIntermediates))
	)
}

// Takes package.nls.json and out/loc/*LANG*/package.i18n.json and creates package.nls.*LANG*.json
// This file will have any translations from */package.i18n.json and for any keys without translations
// it will just have the english value found in package.nls.json. This file is added to the root directory and ships in our VSIX
const createAndAddLocalizedPackageNlsJson = () => {
	return gulp.src(['package.nls.json']).pipe(nls.createAdditionalLanguageFiles(languages, locIntermediates)).pipe(gulp.dest(rootDir))
}

// Creates nls.bundle.LANG.json that has translations for our strings in our typescript files
// Also creates metadata files that are used to create english xliff files in the json-to-xliff task
const generateLocalizedBundlesAndMetadata = () => {
	const tsProject = ts.createProject('tsconfig.json')
	const jsFiles = tsProject
		.src()
		.pipe(sourcemaps.init()) // sourcemaps are required to rewrite loc calls https://github.com/microsoft/vscode-nls-dev/blob/38974b8975ad34aecb79e613ebee27ce3423cadf/src/main.ts#L79
		.pipe(tsProject())

	const localizedJsFiles = jsFiles
		.pipe(nls.rewriteLocalizeCalls()) // Creates a json file with the english strings that require translation and a json metadata file with the appropriate keys
		.pipe(nls.createAdditionalLanguageFiles(languages, locIntermediates, '.localization')) // Creates json files with translations for all languages from the localized strings in out/loc/*LANG*/**/*.json
		.pipe(nls.bundleMetaDataFiles(extensionName, '.localization')) // Bundles all metadata files into one metadata file which has all keys and values. Will use this file later in localize:json-to-xliff task
		.pipe(nls.bundleLanguageFiles()) // Bundles json files together for each langauge so that there's one nls.bundle.LANG.json file for each language

	const nlsFilter = gulpFilter('**/*.json')
	// list localizedJsFiles
	console.log('Localized JS files:')
	localizedJsFiles.on('data', function (file) {
		console.log(file.relative)
	})


	return localizedJsFiles
		.pipe(sourcemaps.write('.', { includeContent: false, sourceRoot: path.join(__dirname, '.localization') }))
		.pipe(nlsFilter)
		.pipe(gulp.dest('.localization/out'))
}

// Generate xlf files for all languages from package.nls.json and nls metadata.
const jsonToXliff = (done) => {
	let completedCount = 0;
	const totalCount = languages.length + 1; // +1 for English

	const checkDone = () => {
		completedCount++;
		if (completedCount === totalCount) {
			done();
		}
	};

	// Create English xlf file (no language prefix) -> aspire-vscode.xlf
	gulp
		.src(['package.nls.json', '.localization/out/nls.metadata.header.json', '.localization/out/nls.metadata.json'])
		.pipe(nls.createXlfFiles('', extensionName))
		.pipe(gulp.dest(xliffDir))
		.on('end', checkDone);

	// Create xlf files for all other languages with language prefix (e.g., aspire-vscode.cs.xlf)
	languages.forEach(lang => {
		gulp
			.src(['package.nls.json', '.localization/out/nls.metadata.header.json', '.localization/out/nls.metadata.json'])
			.pipe(nls.createXlfFiles(lang.id, extensionName))
			.pipe(es.map(function(file, cb) {
				// Rename from aspire-vscode.xlf to aspire-vscode.LANG.xlf
				file.basename = `${extensionName}.${lang.folderName}`;
				file.extname = '.xlf';
				cb(null, file);
			}))
			.pipe(gulp.dest(xliffDir))
			.on('end', checkDone);
	});
};

const createMetadataFilesOnDist = () => {
	const tsProject = ts.createProject('tsconfig.json')
	const jsFiles = tsProject
		.src()
		.pipe(sourcemaps.init()) // sourcemaps are required to rewrite loc calls https://github.com/microsoft/vscode-nls-dev/blob/38974b8975ad34aecb79e613ebee27ce3423cadf/src/main.ts#L79
		.pipe(tsProject())
	const localizedJsFiles = jsFiles.pipe(nls.rewriteLocalizeCalls()).pipe(nls.bundleMetaDataFiles(extensionName, 'dist'))
	const nlsFilter = gulpFilter('**/*.json')
	return localizedJsFiles
		.pipe(sourcemaps.write('.', { includeContent: false, sourceRoot: path.join(__dirname, 'dist') }))
		.pipe(nlsFilter)
		.pipe(gulp.dest('dist'))
}

const watchTask = series(createMetadataFilesOnDist);

// Generate .i18n.json files from translated .xlf files
// The language is extracted from the filename (e.g., aspire-vscode.cs.xlf -> cs)
function prepareJsonFiles() {
	const parsePromises = [];
	return es.through(
		function (xlf) {
			const stream = this;
			// Extract language from filename (e.g., aspire-vscode.cs.xlf -> cs)
			const filename = path.basename(xlf.relative, '.xlf');
			const parts = filename.split('.');
			// If filename is extensionName.LANG.xlf format, extract LANG; otherwise skip
			const lang = parts.length > 1 ? parts[parts.length - 1] : null;

			if (!lang) {
				// Skip files without language prefix (English base file)
				return;
			}

			const parsePromise = nls.XLF.parse(xlf.contents.toString(encoding)).then(function (resolvedFiles) {
				resolvedFiles.forEach(function (file) {
					const translatedFile = createI18nFile(path.join(lang, file.originalFilePath), file.messages, xlf.relative);
					stream.queue(translatedFile);
				});
			});
			parsePromises.push(parsePromise);
		},
		function () {
			var stream = this
			Promise.all(parsePromises)
				.then(function () {
					stream.queue(null)
				})
				.catch(function (reason) {
					throw new Error(reason)
				})
		},
	)
}

function createI18nFile(originalFilePath, messages, sourceFile) {
	var content =
		[
			'// Licensed to the .NET Foundation under one or more agreements.',
			'// The .NET Foundation licenses this file to you under the MIT license.',
			'// Do not edit this file. It is machine generated from ' + sourceFile + '.',
		].join('\n') +
		'\n' +
		JSON.stringify(messages, null, '\t').replace(/\r\n/g, '\n')
	return new File({
		path: path.join(`${originalFilePath}.i18n.json`),
		contents: Buffer.from(content, encoding),
	})
}

// This function will validate the localization output to make sure it didn't break with most recent changes.
function validateBundleJson() {
	const jsonData = JSON.parse(fs.readFileSync(bundleDefaultFile))
	for (let key in jsonData) {
		if (Array.isArray(jsonData[key]) && jsonData[key].length === 0) {
			throw new Error(`Empty bundle found for file: '${key}' this could mean that changes in this build might have broken the localization process.`)
		}
	}
	return gulp.src(bundleDefaultFile)
}

const prepareLocFiles = series(createAndAddLocalizedPackageNlsJson, generateLocalizedBundlesAndMetadata, jsonToXliff, validateBundleJson);
const generateAllLocalizationFiles = series(
	xlifftoi18n,
	parallel(createAndAddLocalizedPackageNlsJson, series(generateLocalizedBundlesAndMetadata, jsonToXliff)),
);

module.exports.prepare = prepareLocFiles;
module.exports.localizationBundle = generateAllLocalizationFiles;
module.exports.localizationWatch = watchTask;
