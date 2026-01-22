#!/usr/bin/env node

/**
 * Generates a JSON schema for Aspire settings files at build time.
 * This schema is used by VS Code to provide IntelliSense, validation, and hover documentation.
 */

const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

// Determine the Aspire CLI path based on the OS
const isWindows = process.platform === 'win32';
const cliPath = path.join(__dirname, '..', '..', 'artifacts', 'bin', 'Aspire.Cli', 'Debug', 'net10.0', isWindows ? 'aspire.exe' : 'aspire');

// Output path for the schema (relative to extension directory)
const schemaOutputPath = path.join(__dirname, '..', 'schemas', 'aspire-settings.schema.json');

console.log('Generating Aspire settings schema...');

try {
    // Check if CLI exists
    if (!fs.existsSync(cliPath)) {
        console.warn(`WARNING: Aspire CLI not found at ${cliPath}`);
        console.warn('Skipping schema generation. Run ./build.sh first to build the CLI.');
        process.exit(0); // Exit successfully to not break the build
    }

    // Get config info from CLI
    const output = execSync(`"${cliPath}" config info --json`, { encoding: 'utf8' });
    const configInfo = JSON.parse(output);

    // Generate JSON schema
    const schema = generateJsonSchema(configInfo);

    // Ensure output directory exists
    const schemaDir = path.dirname(schemaOutputPath);
    if (!fs.existsSync(schemaDir)) {
        fs.mkdirSync(schemaDir, { recursive: true });
    }

    // Write schema to file
    fs.writeFileSync(schemaOutputPath, JSON.stringify(schema, null, 2), 'utf8');

    console.log(`✓ Schema generated successfully at: ${schemaOutputPath}`);
    console.log(`  - ${configInfo.SettingsSchema.Properties.length} top-level properties`);
    console.log(`  - ${configInfo.AvailableFeatures.length} feature flags`);
} catch (error) {
    console.error('ERROR: Failed to generate schema:', error.message);
    console.warn('Skipping schema generation. This may happen if the CLI is not built yet.');
    process.exit(0); // Exit successfully to not break the build
}

function generateJsonSchema(configInfo) {
    const properties = {};
    const required = [];

    // Add each top-level property
    for (const prop of configInfo.SettingsSchema.Properties) {
        properties[prop.Name] = createPropertySchema(prop, configInfo);
        
        if (prop.Required) {
            required.push(prop.Name);
        }
    }

    return {
        $schema: 'http://json-schema.org/draft-07/schema#',
        $id: 'https://json.schemastore.org/aspire-settings.json',
        type: 'object',
        title: 'Aspire Settings',
        description: 'Configuration file for .NET Aspire application host',
        properties,
        ...(required.length > 0 ? { required } : {}),
        additionalProperties: false
    };
}

function createPropertySchema(prop, configInfo) {
    const schema = {
        description: prop.Description
    };

    const lowerType = prop.Type.toLowerCase();

    if (lowerType === 'string') {
        schema.type = 'string';
    } else if (lowerType === 'boolean') {
        schema.type = 'boolean';
    } else if (lowerType === 'number' || lowerType === 'integer') {
        schema.type = lowerType;
    } else if (lowerType === 'array') {
        schema.type = 'array';
        schema.items = {};
    } else if (lowerType === 'object') {
        schema.type = 'object';

        // Special handling for 'features' object
        if (prop.Name === 'features') {
            schema.properties = {};
            schema.additionalProperties = false;

            // Add each feature as a boolean property
            for (const feature of configInfo.AvailableFeatures) {
                schema.properties[feature.Name] = {
                    type: 'boolean',
                    description: feature.Description,
                    default: feature.DefaultValue
                };
            }
        } else if (prop.Name === 'packages') {
            // Packages is an object with string keys and string values (name: version)
            schema.description = 'Package name to version mapping';
            schema.additionalProperties = {
                type: 'string',
                description: 'Package version'
            };
        } else {
            // Generic object
            schema.additionalProperties = true;
        }
    } else {
        // Fallback to string for unknown types
        schema.type = 'string';
    }

    return schema;
}
