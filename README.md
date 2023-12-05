## ne14.library.startup_extensions

``` powershell
# Restore tools
dotnet tool restore

# Run unit tests (multiple test projects, no threshold)
gci **/TestResults/ | ri -r; dotnet test -c Release -s .runsettings; dotnet reportgenerator -targetdir:coveragereport -reports:**/coverage.cobertura.xml -reporttypes:"html;jsonsummary"; start coveragereport/index.html;

# Run mutation tests and show report
gci **/StrykerOutput/ | ri -r; dotnet stryker -o;
```

### Packaging locally

``` powershell
# Upload to local package repo
$ver="1.0.0-alpha-0001"; dotnet pack -c Release -o nu -p:Version=$ver; dotnet nuget push "nu\*library*.$ver.nupkg" --source localdev
```
