# Aspire OpenTelemetry architecture

An Aspire goal is apps are easy to debug and diagnose. Towards this goal, Aspire apps are configured by default to collect and export telemetry using [OpenTelemetry (OTEL)](https://opentelemetry.io/). Additionally, Aspire local development includes UI in the dashboard for viewing OTEL data.

This document details how OpenTelemtry is used in Aspire apps.

## Telemetry types

Libraries and apps record values using .NET APIs for three kinds of telemetry:

* Structured logging - Log entries from `ILogger`.
* Tracing - Distributed tracing from `Activity`.
* Metrics - Numeric values from `Meter`/`Instrument<T>`.

## OpenTelemetry SDK

The .NET OpenTelemetry SDK provides functionality to collect values from the .NET APIs listed above (`ILogger`, `Activity`, and `Instrument<T>`) and then export telemetry to a data store or reporting tool. Telemetry is exported using [OpenTelemetry protocol (OTLP)](https://opentelemetry.io/docs/specs/otel/protocol/). OTLP is a standard scheme for sending telemetry data using REST or gRPC.

.NET projects configure the .NET OpenTelemetry SDK using the service defaults project. Aspire templates automatically create the service defaults, and .NET Aspire apps call it at startup. The service defaults enable collecting and exporting telemetry for .NET apps.

## OpenTelemetry environment variables

Environment variables configure almost all OTEL settings. OTEL has a [list of known environment variables](https://opentelemetry.io/docs/specs/otel/configuration/sdk-environment-variables/) that configure the most important behavior for collecting and exporting telemetry. OTEL SDKs, including the .NET SDK, support reading these variables.

Aspire apps launch with environment variables that configure the name and ID of the app in exported telemetry and set the address endpoint of the OTLP server to export data. For example:

* `OTEL_SERVICE_NAME` = myfrontend
* `OTEL_RESOURCE_ATTRIBUTES` = service.instance.id=1a5f9c1e-e5ba-451b-95ee-ced1ee89c168
* `OTEL_EXPORTER_OTLP_ENDPOINT` = http://localhost:4318

OTLP exporting is disabled if `OTEL_EXPORTER_OTLP_ENDPOINT` isn't configured.

## Aspire local development

The Aspire dashboard provides UI for viewing the telemetry of apps. Telemetry data is sent to the dashboard using OTLP, and the dashboard implements an OTLP server to receive telemetry data and store it in memory. The dashboard UI presents telemetry stored in memory.

Aspire F5 debugging workflow:

* Developer starts and Aspire app with debugging
* Aspire dashboard and developer control plane (DCP) start
* App configuration is run in the DevHost project.
  * OTEL environment variables are automatically added to .NET projects during app configuration.
  * The app name and ID from DCP are the name and ID of the app in exported telemetry.
  * The OTLP endpoint is an HTTP/2 port started by the dashboard. That tells projects to export telemetry back to the dashboard.
  * Fast export intervals so data is quickly available in the dashboard.
* The DCP starts projects, containers, and executables.
* Once started, apps send telemetry to the dashboard.
* Dashboard displays near real-time telemetry of all Aspire apps.

## Aspire deployment

Aspire deployment environments should configure OTEL environment variables that make sense for their environment. For example, `OTEL_EXPORTER_OTLP_ENDPOINT` should be configured to the environment's local OTLP collector or monitoring service.
