# Aspire OpenTelemetry Architecture

Aspire helps apps configure and export telemetry using [OpenTelemetry (OTEL)](https://opentelemetry.io/). Additionally, Aspire local development includes UI in the dashboard for viewing OTEL data.

## Telemetry types

There are three kinds of telemetry and values are recorded using .NET APIs by libraries and apps:

* Structured logging - Log entries from `ILogger`.
* Tracing - Distributed tracing from `Activity`.
* Metrics - Numeric values from `Instrument<T>`.

## OpenTelemetry SDK

The .NET OpenTelemetry SDK provides functionality to collect values from the .NET APIs listed above (`ILogger`, `Activity` and `Instrument<T>`) and then export telemetry to a data store or reporting tool. Telemetry is exported using [OpenTelemetry protocol (OTLP)](https://opentelemetry.io/docs/specs/otel/protocol/). OTLP is a standard scheme for sending telemetry data using either REST or gRPC.

The .NET OpenTelemetry SDK is configured in .NET projects in the service defaults project. The service defaults is automatically created by Aspire templates and .NET Aspire apps should use call it at startup. The service defaults enable collecting and exporting telemetry for .NET apps.

## OpenTelemetry environment variables

Almost all OTEL settings are configured using environment variables. OTEL has a [list of known environment variables](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/) that configure most important behavior for collecting and exporting telemetry. OTEL SDK's, including the .NET SDK, support reading these variables.

Aspire apps are launched with environment variables that configure the name and ID of the app in exported telemetry, and the address endpoint of the OTLP server to export data to. For example:

* `OTEL_SERVICE_NAME` = myfrontend
* `OTEL_RESOURCE_ATTRIBUTES` = service.instance.id=1a5f9c1e-e5ba-451b-95ee-ced1ee89c168
* `OTEL_EXPORTER_OTLP_ENDPOINT` = http://localhost:4318

## Aspire local development

The Aspire dashboard provides UI for viewing telemetry of apps. Telemetry data is sent to the dashboard using OTLP, and the dashboard implements an OTLP server to receive telemetry data and then store it in-memory. The dashboard UI presents telemetry stored in-memory.

Aspire F5 debugging workflow:

* Developer starts and Aspire app with debugging
* Aspire dashboard and developer control plane (DCP) start
* App configuration is run in the DevHost project.
  * During app configuration, OTEL environment variables are automatically added to .NET projects.
  * The app name and ID from DCP are the name and ID of the app in exported telemetry.
  * The OTLP endpoint is an HTTP/2 port started by the dashboard. That tells projects to export telemetry back to the dashboard.
  * Fast export intervals so data is quickly available in the dashboard.
* The DCP starts projects, containers, executables.
* Once started, apps send telemetry to the dashboard.
* Dashboard displays near real-time telemetry of all Aspire apps.
