pushd ApiGateway
dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer -p:PublishSingleFile=true --self-contained true
popd

pushd CatalogService
dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer -p:PublishSingleFile=true --self-contained true
popd

pushd BasketService
dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer -p:PublishSingleFile=true --self-contained true
popd

pushd MyFrontend
dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer -p:PublishSingleFile=true --self-contained true
popd
