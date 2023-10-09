# Tips and known issues

### How can I see the logs from my services?

The logs will be part of dashboard experience that is in the works. For now the VS will open a separate terminal window for every application service.

### My containers aren't being created at all

We have seen Docker get into weird state on "DevBox" machines that get hibernated every evening. You can check if you got into this state by opening a command window and doing "docker ps". If it hangs forever without error, that is it.

The remedy is to do "Restart Docker" gesture from the Docker icon in system tray.

### If something goes wrong and you are left with a bunch of Aspire eShopLite orphaned processes and containers

This will display process IDs of all the Aspire shopping running processes

```ps1
ps | where ProcessName -cmatch '(Api)|(Catalog)|(Basket)|(AppHost)|(MyFrontend)|(OrderProcessor)' | % {Write-Output $_.Id }
```

… and this will kill them

```ps1
ps | where ProcessName -cmatch '(Api)|(Catalog)|(Basket)|(AppHost)|(MyFrontend)|(OrderProcessor)' | % {Write-Output $_.Id; kill $_.Id }
```

### If you want to check the current status of running Aspire services using the low level Kubernetes compatible API

You can find the kubeconfig for a given run of an Aspire application in the `%TEMP%/aspire/<PID>` (or equivalnet) folder, where &lt;PID&gt; is the process ID of the Aspire orchestration project that is running. For the Aspire eShopLite sample you can find the Aspire orchestration PID by running:

```ps1
ps | where ProcessName -cmatch '(AppHost)' | % {Write-Output $_.Id }
```

```ps1
kubectl --kubeconfig $env:TEMP\aspire\<PID>\kubeconfig describe exe
```

### If you want to create an alias for easier access to the low level Kubernetes API

Edit $Profile in powershell and add the following contents

```
function dcpKubectl()
{
    $procName = $args[0]
    $args = $args | Select-Object -Skip 1
    $process = Get-Process | Where-Object { $_.Name -match $procName }
    $procId = $process.Id
    Write-Output "Using $env:TEMP\aspire\session\$procId\kubeconfig"
    & kubectl --kubeconfig "$env:TEMP\aspire\session\$procId\kubeconfig" $args
}
Set-Alias kk -Value dcpKubectl
```
Usage:

```
kk <name of orchestrator app> command(s)

kk myapp get exe
kk myapp describe exe basketservice
```

For debugging tests (part of `CloudApplicationTests` suite), the name of the process is `testhost`.
