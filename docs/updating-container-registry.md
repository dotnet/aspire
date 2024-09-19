# Updating the test container registry

Docker Hub has a rate limit on how often requests can be made from an IP. Because of this, we can't use Docker Hub in our build and automation. Instead, we have created a mirror container registry (netaspireci.azurecr.io) to mirror the public images used in our tests.

Use the following guide to add a new image, or update an existing image with a new version.

1. Install az cli - https://learn.microsoft.com/cli/azure/install-azure-cli
2. Log in to the container registry (Assumes you are permitted to login to the registry. Contact an admin if you need permissions.)
   1. `az login`
   2. `az acr login --name netaspireci --expose-token --output tsv --query accessToken | docker login netaspireci.azurecr.io -u 00000000-0000-0000-0000-000000000000 --password-stdin`
   3. See the following docs for more information
      1. https://learn.microsoft.com/azure/container-registry/container-registry-get-started-docker-cli#log-in-to-a-registry
      2. https://github.com/dotnet/dotnet-docker/blob/main/samples/push-image-to-acr.md#login-to-acr
3. Pull the image locally, tag it, and push it
   1. `docker pull docker.io/library/redis:7.2 --platform linux/amd64`
   2. `docker tag library/redis:7.2 netaspireci.azurecr.io/library/redis:7.2`
   3. `docker push netaspireci.azurecr.io/library/redis:7.2`
4. Alternatively, you can try the import command, but unless you have Docker Hub credentials it can fail due to rate limits.
   1. `az acr import --name netaspireci --source docker.io/library/redis:7.2 --image library/redis:7.2 --username <Docker Hub user name> --password <Docker Hub token>`
   2. See https://learn.microsoft.com/azure/container-registry/container-registry-import-images?tabs=azure-cli#import-from-docker-hub

> [!IMPORTANT]
> Note that the image name in netaspireci.azurecr.io needs to match exactly the name in docker.io or else the test won't be able to simply override the container registry.