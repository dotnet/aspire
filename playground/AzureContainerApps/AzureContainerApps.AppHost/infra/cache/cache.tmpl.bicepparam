using './cache.module.bicep'

param cache_password_value = '{{ securedParameter "cache_password" }}'
param cache_volumes_0_storage = '{{ .Env.SERVICE_CACHE_VOLUME_AZURECONTAINERAPPSAPPHOSTA01EC9BC8DCACHEDATA_NAME }}'
param infra_outputs_azure_container_apps_environment_id = '{{ .Env.INFRA_AZURE_CONTAINER_APPS_ENVIRONMENT_ID }}'
param infra_outputs_azure_container_registry_managed_identity_id = '{{ .Env.INFRA_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID }}'
param infra_outputs_managed_identity_client_id = '{{ .Env.INFRA_MANAGED_IDENTITY_CLIENT_ID }}'
