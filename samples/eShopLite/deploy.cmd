call ./build-containers.cmd

pushd Deployment
call ./undeploy.cmd
call ./deploy.cmd
popd
