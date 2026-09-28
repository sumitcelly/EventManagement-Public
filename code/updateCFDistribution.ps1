Param(
    [Parameter(Mandatory=$true)]
    [string]$NewEc2Dns
)

# Configuration Variables
$DistributionId = "ERB1FHGKI5VJE"
$TargetOriginId = "EC2-API-Origin"
$TargetPathPattern = "/api/*"

Write-Host "🔄 Fetching current CloudFront configuration for $DistributionId..." -ForegroundColor Cyan

# 1. Fetch current configuration and ETag
$ConfigResult = aws cloudfront get-distribution-config --id $DistributionId | ConvertFrom-Json
$Etag = $ConfigResult.ETag
$Config = $ConfigResult.DistributionConfig


# Keep backups just in case
$Config | ConvertTo-Json -Depth 100 | Out-File "backup-config.json"
Write-Host "💾 Original configuration backed up to backup-config.json" -ForegroundColor DarkGray

# ==========================================
# STEP 2: REMOVE EXISTING BEHAVIOR & ORIGIN
# ==========================================

Write-Host "🗑️ Removing old behavior and origin configuration..." -ForegroundColor Yellow

# Filter out the existing API behavior
# Filter out the existing API behavior safely
if ($Config.CacheBehaviors.Items) {
    $Config.CacheBehaviors.Items = $Config.CacheBehaviors.Items | Where-Object { $_.PathPattern -ne $TargetPathPattern }
    $Config.CacheBehaviors.Quantity = @($Config.CacheBehaviors.Items).Count
    if ($Config.CacheBehaviors.Quantity -eq 0) { 
        $Config.CacheBehaviors.Items = @() 
    }
}

# Filter out the existing EC2 origin safely
if ($Config.Origins.Items) {
    $Config.Origins.Items = $Config.Origins.Items | Where-Object { $_.Id -ne $TargetOriginId }
    $Config.Origins.Quantity = @($Config.Origins.Items).Count
}


# ==========================================
# STEP 3: CREATE NEW ORIGIN & BEHAVIOR
# ==========================================

# ==========================================
# STEP 3: CREATE NEW ORIGIN & BEHAVIOR
# ==========================================

# ==========================================
# STEP 3: CREATE NEW ORIGIN & BEHAVIOR
# ==========================================

Write-Host "✨ Constructing new origin and behavior templates..." -ForegroundColor Green

# Create the new Origin object matching AWS CLI specifications
$NewOrigin = [PSCustomObject]@{
    Id                  = $TargetOriginId
    DomainName          = $NewEc2Dns
    OriginPath          = ""
    CustomHeaders       = @{ Quantity = 0; Items = @() }
    CustomOriginConfig  = [PSCustomObject]@{
        HTTPPort               = 5220  # Matches your .NET Host Port
        HTTPSPort              = 443
        OriginProtocolPolicy   = "http-only" 
        OriginSslProtocols     = @{ Quantity = 3; Items = @("TLSv1", "TLSv1.1", "TLSv1.2") }
        OriginReadTimeout      = 30
        OriginKeepaliveTimeout = 5
    }
    ConnectionAttempts  = 3
    ConnectionTimeout   = 10
    OriginShield        = @{ Enabled = $false }
}

# Create the new Cache Behavior object optimized for dynamic APIs
$NewBehavior = [PSCustomObject]@{
    PathPattern            = $TargetPathPattern
    TargetOriginId         = $TargetOriginId
    ViewerProtocolPolicy   = "redirect-to-https"
    AllowedMethods         = @{ 
        Quantity = 7
        Items = @("GET", "HEAD", "POST", "PUT", "PATCH", "OPTIONS", "DELETE")
        CachedMethods = @{ Quantity = 2; Items = @("GET", "HEAD") }
    }
    SmoothStreaming        = $false
    Compress               = $true
    LambdaFunctionAssociations = @{ Quantity = 0; Items = @() }
    FieldLevelEncryptionId = ""
    CachePolicyId          = "4135ea2d-6df8-44a3-9df3-4b5a84be39ad" 
    OriginRequestPolicyId  = "216adef6-5c7f-47e4-b989-5492eafa07d3" 
}

# --- SAFELY APPEND ORIGIN ---
if (-not (Get-Member -InputObject $Config.Origins -Name "Items") -or $null -eq $Config.Origins.Items) {
    # Force property initialization as a concrete collection array
    $Config.Origins | Add-Member -MemberType NoteProperty -Name "Items" -Value @($NewOrigin) -Force
} else {
    $Config.Origins.Items = @($Config.Origins.Items) + $NewOrigin
}
$Config.Origins.Quantity = @($Config.Origins.Items).Count

# --- SAFELY APPEND BEHAVIOR ---
if (-not (Get-Member -InputObject $Config.CacheBehaviors -Name "Items") -or $null -eq $Config.CacheBehaviors.Items) {
    # Force property initialization as a concrete collection array
    $Config.CacheBehaviors | Add-Member -MemberType NoteProperty -Name "Items" -Value @($NewBehavior) -Force
} else {
    $Config.CacheBehaviors.Items = @($Config.CacheBehaviors.Items) + $NewBehavior
}
$Config.CacheBehaviors.Quantity = @($Config.CacheBehaviors.Items).Count

# Save modified configuration to file
$ConfigJson = $Config | ConvertTo-Json -Depth 100 -Compress
[System.IO.File]::WriteAllText("$PSScriptRoot/updated-config.json", $ConfigJson)

# ==========================================
# STEP 4: UPDATE CLOUDFRONT DISTRIBUTION
# ==========================================

Write-Host "🚀 Uploading updated configuration to CloudFront..." -ForegroundColor Cyan

aws cloudfront update-distribution `
    --id $DistributionId `
    --distribution-config file://updated-config.json `
    --if-match $Etag

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Successfully updated CloudFront! Distribution is now deploying." -ForegroundColor Green
    Remove-Item "updated-config.json" -ErrorAction SilentlyContinue
} else {
    Write-Host "❌ Failed to update CloudFront. Check error message above." -ForegroundColor Red
}
