
# ==========================================
# AI JAM - TEACHER TO STUDENT TRAINING
# ==========================================

$ErrorActionPreference = "Stop"

# 1. Set project root
$ProjectRoot = Split-Path $PSScriptRoot -Parent
Set-Location $ProjectRoot

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   AI JAM - STUDENT TRAINING" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 2. Check required files
Write-Host "`n[1/3] Checking required files..." -ForegroundColor Yellow

$RequiredFiles = @(
    ".\initialize_student.py",
    ".\Assets\robot_teacher_student.yaml"
)

foreach ($File in $RequiredFiles) {
    if (-not (Test-Path $File -PathType Leaf)) {
        Write-Host "ERROR: Missing file: $File" -ForegroundColor Red
        exit 1
    }

    Write-Host "OK: $File" -ForegroundColor Green
}

Write-Host "All required files found!" -ForegroundColor Green

# 3. Check Python
Write-Host "`n[2/3] Checking Python environment..." -ForegroundColor Yellow

if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
    throw "Python not found. Activate your Python environment."
}

python --version

if ($LASTEXITCODE -ne 0) {
    throw "Python is not working correctly."
}

# 4. Check ML-Agents
if (-not (Get-Command mlagents-learn -ErrorAction SilentlyContinue)) {
    throw "ML-Agents not found. Activate the correct environment or install ML-Agents."
}

mlagents-learn --help | Out-Null

if ($LASTEXITCODE -ne 0) {
    throw "ML-Agents is not working correctly."
}

Write-Host "Python and ML-Agents are ready!" -ForegroundColor Green

# 5. Initialize Student from Teacher
Write-Host "`n[3/3] Initializing Student from Teacher..." -ForegroundColor Yellow

python .\initialize_student.py

if ($LASTEXITCODE -ne 0) {
    throw "Student initialization failed."
}

if (-not (Test-Path ".\Student_Initialized.pt" -PathType Leaf)) {
    throw "Student_Initialized.pt was not created."
}

Write-Host "Student initialization completed!" -ForegroundColor Green

# 6. Start PPO training
$RunId = Read-Host "`nEnter a name for this training run"

if ([string]::IsNullOrWhiteSpace($RunId)) {
    throw "Training name cannot be empty."
}

Write-Host "`nStarting PPO training: $RunId" -ForegroundColor Yellow

mlagents-learn .\Assets\robot_teacher_student.yaml --run-id="$RunId"

if ($LASTEXITCODE -ne 0) {
    throw "ML-Agents training failed."
}
