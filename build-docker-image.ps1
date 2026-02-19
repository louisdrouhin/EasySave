# Build Docker image for EasyLog.Server

Write-Host "EasyLog.Server Docker Build Script" -ForegroundColor Green
Write-Host ""

# Ask for project path
$ProjectPath = Read-Host "Enter EasySave project path"

# Validate path
if (-not (Test-Path $ProjectPath)) {
    Write-Host "ERROR: Project path does not exist: $ProjectPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path "$ProjectPath\EasyLog.Server")) {
    Write-Host "ERROR: EasyLog.Server folder not found in: $ProjectPath" -ForegroundColor Red
    exit 1
}

$ImageName = "easysave/easylog-server:latest"

Write-Host ""
Write-Host "Project path: $ProjectPath" -ForegroundColor Cyan
Write-Host "Image name: $ImageName" -ForegroundColor Cyan
Write-Host ""
Write-Host "Building Docker image..." -ForegroundColor Green

# Step 1: Publish
Write-Host "[1/3] Publishing for Linux..." -ForegroundColor Cyan
cd $ProjectPath
dotnet publish EasyLog.Server/EasyLog.Server.csproj -c Release -o EasyLog.Server/bin/Release/net10.0/publish -r linux-x64 --self-contained

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Publish failed" -ForegroundColor Red
    exit 1
}

# Step 2: Build Docker image
Write-Host "[2/3] Building Docker image..." -ForegroundColor Cyan
docker build -t $ImageName -f EasyLog.Server/Dockerfile .

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Docker build failed" -ForegroundColor Red
    exit 1
}

# Step 3: Verify
Write-Host "[3/3] Verifying image..." -ForegroundColor Cyan
docker images | Select-String $ImageName

Write-Host ""
Write-Host "SUCCESS: Image created successfully" -ForegroundColor Green
Write-Host ""
Write-Host "Run the container with:"
Write-Host "  docker run -d -p 5000:5000 -v easylog-volume:/logs --name easylog-server $ImageName"
