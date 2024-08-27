$runtimeIdentifiers = @("win-x64", "linux-x64", "osx-x64")
foreach ($rid in $runtimeIdentifiers) {
    dotnet publish -c Release -r $rid --self-contained true
}
