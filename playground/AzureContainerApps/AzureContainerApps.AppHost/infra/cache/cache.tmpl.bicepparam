using './cache.module.bicep'

param cache_password_value = '{{ securedParameter "cache_password" }}'
param infra_outputs_azure_container_apps_environment_id = '{{ .Env.INFRA_AZURE_CONTAINER_APPS_ENVIRONMENT_ID }}'
param infra_outputs_azure_container_registry_managed_identity_id = '{{ .Env.INFRA_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID }}'
param infra_outputs_volumes_cache_0 = '{{ .Env.INFRA_VOLUMES_CACHE_0 }}'
