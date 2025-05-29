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
const xliffDir = path.join(rootDir, '/loc/xliff')
const enuDir = path.join(xliffDir, '/enu')
const encoding = 'utf8'
const locIntermediates = path.join(rootDir, '/.localization')
const bundleDefaultFile = path.join(locIntermediates, 'out', 'nls.bundle.json')
// Creates *.i18n.json files in out/loc/LANG directory with translated strings from loc/xliff/**/*.xlf files
const xlifftoi18n = () => {
	const xlfFiles = `${xliffDir}/**/${extensionName}.xlf`
	const notEnglishXlfFiles = `!${enuDir}/**/*.xlf`
	return (
		gulp
			.src([xlfFiles, notEnglishXlfFiles])
			//The reason why we can't use nls.prepareJsonFiles here https://github.com/microsoft/vscode-nls-dev/blob/38974b8975ad34aecb79e613ebee27ce3423cadf/src/main.ts#L887
			//is because it doesn't put each *.i18n.json file in a different directory for each language
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

// Generate ENU xlf file from package.nls.json and nls metadata.
const jsonToEnuXliff = () => {
	return gulp
		.src(['package.nls.json', '.localization/out/nls.metadata.header.json', '.localization/out/nls.metadata.json']) // We need the header because the createXlfFiles function uses the outdir
		.pipe(nls.createXlfFiles('', extensionName))
		.pipe(gulp.dest(enuDir))
}

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

const watchTask = series(createMetadataFilesOnDist)

// Generate .i18n.json files from translated .xlf files
// The file base is assumed to be loc/xliff, and the first directory part in the file name
// to be the language.
function prepareJsonFiles() {
	const parsePromises = []
	return es.through(
		function (xlf) {
			const stream = this
			// Get the lang of the xlf file to be localized so that the i18n json file can be put in a directory with that name
			const lang = xlf.relative.substr(0, xlf.relative.replace(/\\/g, '/').indexOf('/'))
			const parsePromise = nls.XLF.parse(xlf.contents.toString(encoding)).then(function (resolvedFiles) {
				resolvedFiles.forEach(function (file) {
					const translatedFile = createI18nFile(path.join(lang, file.originalFilePath), file.messages, xlf.relative)
					stream.queue(translatedFile)
				})
			})
			parsePromises.push(parsePromise)
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
			'/*---------------------------------------------------------------------------------------------',
			' *  Copyright (c) Microsoft Corporation. All rights reserved.',
			' *--------------------------------------------------------------------------------------------*/',
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

const prepareLocFiles = series(createAndAddLocalizedPackageNlsJson, generateLocalizedBundlesAndMetadata, jsonToEnuXliff, validateBundleJson)
const generateAllLocalizationFiles = series(
	xlifftoi18n,
	parallel(createAndAddLocalizedPackageNlsJson, series(generateLocalizedBundlesAndMetadata, jsonToEnuXliff)),
)

module.exports.prepare = prepareLocFiles
module.exports.localizationBundle = generateAllLocalizationFiles
module.exports.localizationWatch = watchTask
