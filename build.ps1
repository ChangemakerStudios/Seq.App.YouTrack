# This script originally (c) 2016 Serilog Contributors - license Apache 2.0
param(
    [string] $suffix
)

If ($suffix -eq '') {
    Write-Output "Please specify a suffix: E.g. 1.0"
    exit
}

echo "build: Build started"

Push-Location $PSScriptRoot

if (Test-Path .\artifacts) {
    echo "build: Cleaning .\artifacts"
    Remove-Item .\artifacts -Force -Recurse
}

& dotnet restore --no-cache
if ($LASTEXITCODE -ne 0) { exit 1 }    

echo "build: Version suffix is $suffix"

foreach ($src in ls src/*) {
    Push-Location $src

    echo "build: Packaging project in $src"

    if ($suffix) {
        & dotnet publish -c Release -o ./obj/publish --version-suffix=$suffix
        & dotnet pack -c Release -o ..\..\artifacts --no-build --version-suffix=$suffix
    }
    else {
        & dotnet publish -c Release -o ./obj/publish
        & dotnet pack -c Release -o ..\..\artifacts --no-build
    }
    if ($LASTEXITCODE -ne 0) { exit 1 }    

    Pop-Location
}

foreach ($test in ls test/*.Tests) {
    Push-Location $test

    echo "build: Testing project in $test"

    & dotnet test -c Release
    if ($LASTEXITCODE -ne 0) { exit 3 }

    Pop-Location
}

Pop-Location