#  _____ ____   ____             __ _ _ _
# |_   _/ ___| |  _ \ _ __ ___  / _(_) (_)_ __   __ _
#   | || |     | |_) | '__/ _ \| |_| | | | '_ \ / _` |
#   | || |___ _|  __/| | | (_) |  _| | | | | | | (_| |
#   |_| \____(_)_|   |_|  \___/|_| |_|_|_|_| |_|\__, |
#                                               |___/
# Build file

$ErrorActionPreference="Stop"

#         ↓↓↓↓↓
$VERSION="2.1.1"

$BUILD_NUMBER = [System.Environment]::GetEnvironmentVariable('BUILD_NUMBER')
if([String]::IsNullOrEmpty($BUILD_NUMBER)) { $BUILD_NUMBER=999 }
$FULL_VERSION = "$VERSION.$BUILD_NUMBER"

$NUGET_SOURCE="gitea NuGet"
$GITEA_API_KEY = [System.Environment]::GetEnvironmentVariable('GITEA_API_KEY')
if([String]::IsNullOrEmpty($GITEA_API_KEY)) { Write-Output "Environment variable GITEA_API_KEY not set or empty"; exit 1 }

Push-Location src
try {
    dotnet clean
    if(!$?) { exit 1 }

    dotnet restore
    if(!$?) { exit 1 }

    dotnet build /p:Configuration=Release /p:Version=$FULL_VERSION
    if(!$?) { exit 1 }

    dotnet pack /p:Configuration=Release /p:Version=$FULL_VERSION /p:PackageVersion=$VERSION
    if(!$?) { exit 1 }

    dotnet nuget push --source $NUGET_SOURCE --api-key=$GITEA_API_KEY "TC.Profiling\bin\Release\TC.Profiling.$VERSION.nupkg"
    if(!$?) { exit 1 }
}
finally
{
    Pop-Location
}
