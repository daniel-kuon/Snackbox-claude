# Packs what an installation runs into one zip, attached to every GitHub release, so an update
# downloads it instead of building on the (slow) kiosk PC. The zip is laid out like the checkout
# and is copied over it: each app's published output goes where its build output would be.
#
# Not in it: the Aspire AppHost. Its build bakes in absolute paths - of the projects, and of DCP
# and the dashboard in the NuGet cache - so the updater builds that one small project on the
# machine itself. See Snackbox.Updater's ReleasePackage.
#
# Published rather than built because a build's static web assets (blazor.web.js, brand.css)
# are served through a manifest pointing into the sources, obj and the NuGet cache of the
# machine that built them; a publish copies them into its own wwwroot.
#
#   tools/package-release.ps1 -Output snackbox-v1.2.3-win-x64.zip
param(
    [Parameter(Mandatory)][string]$Output
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$stage = Join-Path ([IO.Path]::GetTempPath()) "snackbox-package-$([guid]::NewGuid().ToString('N'))"

function Publish([string]$project, [string]$binPath, [string[]]$extra = @()) {
    Write-Host "Publishing $project..."
    dotnet publish (Join-Path $root $project) -c Release -o (Join-Path $stage $binPath) @extra
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish $project failed." }
}

try {
    # Must match the paths Snackbox.Updater starts and Aspire runs ("dotnet run --no-build")
    Publish 'src/Snackbox.Api/Snackbox.Api.csproj' 'src/Snackbox.Api/bin/Release/net10.0'
    Publish 'src/Snackbox.BlazorServer/Snackbox.BlazorServer.csproj' 'src/Snackbox.BlazorServer/bin/Release/net10.0'
    Publish 'src/Snackbox.Web/Snackbox.Web.csproj' 'src/Snackbox.Web/bin/Release/net10.0-windows10.0.19041.0/win-x64' `
            @('-f', 'net10.0-windows10.0.19041.0', '-r', 'win-x64')
    Publish 'tools/Snackbox.Updater/Snackbox.Updater.csproj' 'tools/Snackbox.Updater/bin/Release/net10.0'

    # PDBs stay: they put line numbers into the stack traces SigNoz collects
    $Output = [IO.Path]::GetFullPath($Output, (Get-Location).Path)
    if (Test-Path $Output) { Remove-Item $Output }
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $Output
    Write-Host "Packed $Output ($([math]::Round((Get-Item $Output).Length / 1MB)) MB)."
}
finally {
    Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
}
