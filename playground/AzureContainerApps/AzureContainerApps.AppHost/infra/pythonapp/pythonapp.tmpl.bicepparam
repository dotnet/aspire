using './pythonapp.module.bicep'

param infra_outputs_azure_container_apps_environment_id = '{{ .Env.INFRA_AZURE_CONTAINER_APPS_ENVIRONMENT_ID }}'
param infra_outputs_azure_container_registry_endpoint = '{{ .Env.INFRA_AZURE_CONTAINER_REGISTRY_ENDPOINT }}'
param infra_outputs_azure_container_registry_managed_identity_id = '{{ .Env.INFRA_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID }}'
param pythonapp_containerimage = '{{ .Image }}'
