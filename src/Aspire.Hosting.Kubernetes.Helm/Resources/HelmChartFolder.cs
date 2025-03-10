// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Kubernetes.Helm.Resources;

/// <summary>
/// Represents a Helm chart folder structure, which includes the chart metadata, values, and templates.
/// </summary>
/// <remarks>
/// This class provides functionality for creating, managing, and writing Helm chart folder structures.
/// It allows for setting chart information, adding templates, and reading/writing the folder contents
/// to and from a directory on disk.
/// </remarks>
/// <remarks>
/// Represents a Helm chart folder containing the chart metadata, values, and templates.
/// Provides functionality to load and write Helm chart information to and from a directory.
/// </remarks>
public sealed class HelmChartFolder(string chartName)
{
    /// <summary>
    /// Represents the Helm chart details in a Kubernetes application.
    /// </summary>
    /// <remarks>
    /// The Chart property is of type HelmChartInfo and contains metadata about the Helm chart,
    /// such as its name, version, and dependencies. It is initialized with a default chart name
    /// when the HelmChartFolder instance is created and can be updated later using the SetChart method.
    /// </remarks>
    public HelmChartInfo Chart { get; private set; } = new HelmChartInfo(chartName);

    /// <summary>
    /// Represents the values configuration for the Helm chart, typically corresponding to the content
    /// of the `values.yaml` file. This property allows getting or setting customizable key-value pairs
    /// specific to a Helm chart deployment. It is primarily used to store and manipulate Helm chart
    /// values programmatically.
    /// </summary>
    public HelmValues Values { get; set; } = new HelmValues();

    /// <summary>
    /// Represents a collection of Helm templates included in the Helm chart.
    /// These templates are used to define Kubernetes resources in YAML format and
    /// are typically stored in the `templates/` directory of a Helm chart structure.
    /// Each template in the collection is an instance of <see cref="HelmTemplate"/>.
    /// </summary>
    public List<HelmTemplate> Templates { get; } = [];

    /// <summary>
    /// Updates the current Helm chart information for the Helm chart folder.
    /// </summary>
    /// <param name="newChart">The new Helm chart information to set.</param>
    public void SetChart(HelmChartInfo newChart) => Chart = newChart;

    /// <summary>
    /// Adds a Helm template to the collection of templates in the Helm chart folder.
    /// </summary>
    /// <param name="template">The Helm template to be added to the collection.</param>
    public void AddTemplate(HelmTemplate template) => Templates.Add(template);

    /// <summary>
    /// Writes the Helm chart folder structure, including Chart.yaml, values.yaml, and templates, to the specified directory path.
    /// </summary>
    /// <param name="folderPath">The directory path where the Helm chart folder structure will be created and written.</param>
    public void WriteToDirectory(string folderPath)
    {
        Directory.CreateDirectory(folderPath);
        // write Chart.yaml
        var chartPath = Path.Combine(folderPath, "Chart.yaml");
        File.WriteAllText(chartPath, Chart.ToYamlString());

        // write values.yaml
        var valuesPath = Path.Combine(folderPath, "values.yaml");
        File.WriteAllText(valuesPath, Values.ToYamlString());

        // templates directory
        var templatesDir = Path.Combine(folderPath, "templates");
        Directory.CreateDirectory(templatesDir);
        foreach(var tmpl in Templates)
        {
            var tmplPath = Path.Combine(templatesDir, tmpl.FileName);
            File.WriteAllText(tmplPath, tmpl.ToYamlString());
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="HelmChartFolder"/> by reading the contents of a Helm chart directory.
    /// </summary>
    /// <param name="folderPath">The file path of the directory containing the Helm chart files, including Chart.yaml, values.yaml, and optional templates.</param>
    /// <returns>Returns an instance of <see cref="HelmChartFolder"/> populated with the chart information, values, and templates from the specified directory.</returns>
    public static HelmChartFolder FromDirectory(string folderPath)
    {
        // read Chart.yaml
        var chartFile = Path.Combine(folderPath, "Chart.yaml");
        var chartContent = File.ReadAllText(chartFile);
        var chartInfo = HelmChartInfo.FromYaml(chartContent);

        // read values.yaml
        var valuesFile = Path.Combine(folderPath, "values.yaml");
        HelmValues values;
        if (File.Exists(valuesFile))
        {
            var valuesContent = File.ReadAllText(valuesFile);
            values = HelmValues.FromYaml(valuesContent);
        }
        else
        {
            values = new HelmValues();
        }

        var folder = new HelmChartFolder((chartInfo.Get(HelmYamlKeys.Name) as YamlValue)?.Value.ToString() ?? "aspire-chart")
        {
            Values = values
        };
        folder.SetChart(chartInfo);

        // read templates
        var templatesDir = Path.Combine(folderPath, "templates");
        if (Directory.Exists(templatesDir))
        {
            foreach (var file in Directory.GetFiles(templatesDir, "*.yaml"))
            {
                var tmplContent = File.ReadAllText(file);
                var tmpl = HelmTemplate.FromYaml(Path.GetFileName(file), tmplContent);
                folder.AddTemplate(tmpl);
            }
        }
        return folder;
    }
}
