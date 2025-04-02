// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Kubernetes.Tests;

public static class ExpectedValues
{
    public const string Chart =
        """
        apiVersion: "v2"
        name: "aspire"
        version: "0.1.0"
        kubeVersion: ">= 1.18.0-0"
        description: "Aspire Helm Chart"
        type: "application"
        keywords:
          - "aspire"
          - "kubernetes"
        appVersion: "0.1.0"
        deprecated: false

        """;

    public const string Values =
        """
        parameters:
          project1:
            project1_image: "project1:latest"
        secrets:
          myapp:
            param1: ""
            param3: ""
        config:
          myapp:
            ASPNETCORE_ENVIRONMENT: "Development"
            param0: ""
            param2: "default"
          project1:
            OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES: "true"
            OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES: "true"
            OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY: "in_memory"
            services__myapp__http__0: "http://myapp:8080"
        
        """;

    public const string ProjectOneDeployment =
        """
        ---
        apiVersion: "apps/v1"
        kind: "Deployment"
        metadata:
          name: "project1-deployment"
        spec:
          template:
            metadata:
              labels:
                app: "aspire"
                component: "project1"
            spec:
              containers:
                - image: "{{ .Values.parameters.project1.project1_image }}"
                  name: "project1"
                  envFrom:
                    - configMapRef:
                        name: "project1-config"
                  imagePullPolicy: "IfNotPresent"
          selector:
            matchLabels:
              app: "aspire"
              component: "project1"
          replicas: 1
          revisionHistoryLimit: 3
          strategy:
            rollingUpdate:
              maxSurge: 1
              maxUnavailable: 1
            type: "RollingUpdate"

        """;

    public const string ProjectOneConfigMap =
        """
        ---
        apiVersion: "v1"
        kind: "ConfigMap"
        metadata:
          name: "project1-config"
          labels:
            app: "aspire"
            component: "project1"
        data:
          OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES: "{{ .Values.config.project1.OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES }}"
          OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES: "{{ .Values.config.project1.OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES }}"
          OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY: "{{ .Values.config.project1.OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY }}"
          services__myapp__http__0: "{{ .Values.config.project1.services__myapp__http__0 }}"

        """;

    public const string MyAppDeployment =
        """
        ---
        apiVersion: "apps/v1"
        kind: "Deployment"
        metadata:
          name: "myapp-deployment"
        spec:
          template:
            metadata:
              labels:
                app: "aspire"
                component: "myapp"
            spec:
              containers:
                - image: "mcr.microsoft.com/dotnet/aspnet:8.0"
                  name: "myapp"
                  envFrom:
                    - configMapRef:
                        name: "myapp-config"
                    - secretRef:
                        name: "myapp-secrets"
                  args:
                    - "--cs"
                    - "Url={{ .Values.config.myapp.param0 }}, Secret={{ .Values.secrets.myapp.param1 }}"
                  ports:
                    - name: "http"
                      protocol: "TCP"
                      containerPort: "8080"
                  volumeMounts:
                    - name: "logs"
                      mountPath: "/logs"
                  imagePullPolicy: "IfNotPresent"
              volumes:
                - name: "logs"
                  emptyDir: {}
          selector:
            matchLabels:
              app: "aspire"
              component: "myapp"
          replicas: 1
          revisionHistoryLimit: 3
          strategy:
            rollingUpdate:
              maxSurge: 1
              maxUnavailable: 1
            type: "RollingUpdate"

        """;

    public const string MyAppService =
        """
        ---
        apiVersion: "v1"
        kind: "Service"
        metadata:
          name: "myapp-service"
        spec:
          type: "ClusterIP"
          selector:
            app: "aspire"
            component: "myapp"
          ports:
            - name: "http"
              protocol: "TCP"
              port: "8080"
              targetPort: "8080"

        """;

    public const string MyAppConfigMap =
        """
        ---
        apiVersion: "v1"
        kind: "ConfigMap"
        metadata:
          name: "myapp-config"
          labels:
            app: "aspire"
            component: "myapp"
        data:
          ASPNETCORE_ENVIRONMENT: "{{ .Values.config.myapp.ASPNETCORE_ENVIRONMENT }}"
          param0: "{{ .Values.config.myapp.param0 }}"
          param2: "{{ .Values.config.myapp.param2 }}"

        """;

    public const string MyAppSecret =
        """
        ---
        apiVersion: "v1"
        kind: "Secret"
        metadata:
          name: "myapp-secrets"
          labels:
            app: "aspire"
            component: "myapp"
        stringData:
          param1: "{{ .Values.secrets.myapp.param1 }}"
          param3: "{{ .Values.secrets.myapp.param3 }}"
          ConnectionStrings__cs: "Url={{ .Values.config.myapp.param0 }}, Secret={{ .Values.secrets.myapp.param1 }}"
        type: "Opaque"

        """;
}
