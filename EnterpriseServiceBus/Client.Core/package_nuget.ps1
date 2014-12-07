 ..\.nuget\nuget.exe pack .\Client.Core.csproj -IncludeReferencedProjects

 $latest = Get-ChildItem .\*.nupkg | Sort-Object LastAccessTime -Descending | Select-Object -First 1

 ..\.nuget\nuget.exe push $latest.name -s http://configmgmt.geodecisions.local/NuGet/ MyS3cretK3y!