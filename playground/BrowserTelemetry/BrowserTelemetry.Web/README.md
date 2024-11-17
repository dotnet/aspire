# Updating web assets

When changes to the `./package.json` or `./Scripts/index.js` are made the `./wwwwroot/scripts/bundle.js` file needs to be updated by running the following command:

```console
dotnet build /t:NpmRunBuild
```

This will install the required npm packages and run the webpack command generating this file.
Note that the `package-lock.json`
