# Face Verification API Test Script
# Usage: .\TestFaceVerification.ps1

$baseUrl = "https://localhost:7095"  # Update with your API URL
$imagePath = "C:\path\to\face-photo.jpg"  # Update with your test image path

Write-Host "=== Face Verification API Test ===" -ForegroundColor Cyan

# Step 1: Convert image to base64
Write-Host "`n[1] Converting image to base64..." -ForegroundColor Yellow
if (Test-Path $imagePath) {
    $imageBytes = [IO.File]::ReadAllBytes($imagePath)
    $base64Image = [Convert]::ToBase64String($imageBytes)
    Write-Host "✓ Image converted. Size: $($base64Image.Length) characters" -ForegroundColor Green
} else {
    Write-Host "✗ Image file not found: $imagePath" -ForegroundColor Red
    exit
}

# Step 2: Login
Write-Host "`n[2] Testing login..." -ForegroundColor Yellow
$loginBody = @{
    email = "user@example.com"
    password = "Password123!"
    udid = "TEST-DEVICE-12345"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    Write-Host "✓ Login successful" -ForegroundColor Green
    Write-Host "  - User: $($loginResponse.userData.displayName)" -ForegroundColor Gray
    Write-Host "  - Face Required: $($loginResponse.userData.isFaceVerificationRequired)" -ForegroundColor Gray
    Write-Host "  - Has Enrollment: $($loginResponse.userData.hasFaceEnrollment)" -ForegroundColor Gray
    $token = $loginResponse.token
} catch {
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit
}

# Step 3: Test attendance WITHOUT face
Write-Host "`n[3] Testing attendance WITHOUT face image..." -ForegroundColor Yellow
$attendanceBodyNoFace = @{
    username = "user@example.com"
    udid = "TEST-DEVICE-12345"
    deviceID = "DEVICE-001"
    actionType = "CheckIn"
    faceImage = $null
} | ConvertTo-Json

try {
    $headers = @{ Authorization = "Bearer $token" }
    $response = Invoke-RestMethod -Uri "$baseUrl/api/attendance/action" -Method Post -Body $attendanceBodyNoFace -ContentType "application/json" -Headers $headers
    Write-Host "✓ Response: $($response.message)" -ForegroundColor Green
} catch {
    $errorDetails = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "✗ Expected error: $($errorDetails.message)" -ForegroundColor Yellow
}

# Step 4: Test attendance WITH face
Write-Host "`n[4] Testing attendance WITH face image..." -ForegroundColor Yellow
$attendanceBodyWithFace = @{
    username = "user@example.com"
    udid = "TEST-DEVICE-12345"
    deviceID = "DEVICE-001"
    actionType = "CheckIn"
    faceImage = $base64Image
} | ConvertTo-Json

try {
    $headers = @{ Authorization = "Bearer $token" }
    $response = Invoke-RestMethod -Uri "$baseUrl/api/attendance/action" -Method Post -Body $attendanceBodyWithFace -ContentType "application/json" -Headers $headers
    Write-Host "✓ Response: $($response.message)" -ForegroundColor Green
    Write-Host "  - Success: $($response.success)" -ForegroundColor Gray
} catch {
    $errorDetails = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "✗ Face verification failed: $($errorDetails.message)" -ForegroundColor Red
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
