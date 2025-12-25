# Cool1 Windows Packaging Script
# Strategy: Framework Dependent (Smallest size, requires .NET 9 installed on client)

$ProjectName = "Cool1Windows"
$PublishDir = "./publish"

# 1. Clean up old publish directory
if (Test-Path $PublishDir) {
    Write-Host "Cleaning old publish directory..." -ForegroundColor Gray
    # Using specialized logic to ensure files are not locked
    Remove-Item -Recurse -Force $PublishDir -ErrorAction SilentlyContinue
    if (Test-Path $PublishDir) {
        Write-Host "Warning: Could not fully clear output directory. Some files might be in use." -ForegroundColor Yellow
    }
}

Write-Host "Packing $ProjectName (GUI Mode)..." -ForegroundColor Cyan

# 2. Run Publish Command
# --self-contained false: Reduce size by not including .NET runtime
# -p:PublishSingleFile=true: Bundle into a single EXE
dotnet publish ./Cool1Windows.csproj `
    -c Release `
    -o $PublishDir `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=false `
    -p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nPacking Successful!" -ForegroundColor Green
    Write-Host "Output Directory: " -NoNewline; Write-Host (Get-Item $PublishDir).FullName -ForegroundColor Yellow
    
    $exe = Get-ChildItem "$PublishDir/*.exe" | Select-Object -First 1
    if ($exe) {
        $size = [math]::round($exe.Length / 1MB, 2)
        Write-Host "Main EXE Size: $size MB" -ForegroundColor White
    }
} else {
    Write-Host "`nPacking Failed!" -ForegroundColor Red
}
