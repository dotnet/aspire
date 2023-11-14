# Instructions for running in Kubernetes locally

* Install Docker Desktop
* Enable Kubernetes support in Docker Desktop
* Start a local image repository

```sh
> docker run -d -p 5001:5000 --restart always --name registry registry:2
```

* Build the containers, pushing them to the local repository, and deploy to kubernetes

```sh
> ./deploy.cmd
```
