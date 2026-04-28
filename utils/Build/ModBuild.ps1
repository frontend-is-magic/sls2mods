function Build-Sls2Mod {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ModRoot,

        [Parameter(Mandatory = $true)]
        [string]$ModId,

        [Parameter(Mandatory = $true)]
        [string]$ProjectFile,

        [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",

        [string[]]$CleanDistNames = @()
    )

    $ErrorActionPreference = "Stop"

    $previousLocation = Get-Location
    Set-Location $ModRoot
    try {
        $stsDll = Join-Path $GameDir "data_sts2_windows_x86_64\sts2.dll"
        if (-not (Test-Path $stsDll)) {
            throw "sts2.dll not found at $stsDll"
        }

        Copy-Item $stsDll -Destination (Join-Path $ModRoot "sts2.dll") -Force

        dotnet build (Join-Path $ModRoot $ProjectFile)
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build failed."
        }

        $dll = Join-Path $ModRoot ".godot\mono\temp\bin\Debug\$ModId.dll"
        if (-not (Test-Path $dll)) {
            throw "Built DLL not found at $dll"
        }

        $dist = Join-Path $ModRoot "dist"
        New-Item -ItemType Directory -Force -Path $dist | Out-Null

        $namesToClean = @($ModId) + $CleanDistNames
        foreach ($name in $namesToClean) {
            Remove-Item -Path (Join-Path $dist "$name.dll") -Force -ErrorAction SilentlyContinue
            Remove-Item -Path (Join-Path $dist "$name.json") -Force -ErrorAction SilentlyContinue
            Remove-Item -Path (Join-Path $dist "$name.pck") -Force -ErrorAction SilentlyContinue
            Remove-Item -Path (Join-Path $dist "$($name)Config.json") -Force -ErrorAction SilentlyContinue
        }

        Copy-Item $dll -Destination (Join-Path $dist "$ModId.dll") -Force
        Copy-Item (Join-Path $ModRoot "$ModId.json") -Destination (Join-Path $dist "$ModId.json") -Force

        $exampleConfig = Join-Path $ModRoot "$($ModId)Config.json.example"
        if (Test-Path $exampleConfig) {
            Copy-Item $exampleConfig -Destination (Join-Path $dist "$($ModId)Config.json.example") -Force
        }

        Write-Host "Built mod files in $dist"
    }
    finally {
        Set-Location $previousLocation
    }
}
