# =========================================================================
# AWS TEST ENVIRONMENT SHUTDOWN SCRIPT
# =========================================================================

# 1. DEFINE YOUR CONFIGURATION RESOURECES (Update your IDs here!)
$AWS_PROFILE   = "default"
$AWS_REGION    = "us-west-2"
$EC2_INSTANCE  = "i-0f881f2b840a913d1"       
$RDS_INSTANCE  = "eventmgmtdb"

Write-Host "--------------------------------------------------------" -ForegroundColor Cyan
Write-Host "  INITIATING SECURITY SHUTDOWN FOR AWS DEVELOPMENT HOSTS  " -ForegroundColor Cyan
Write-Host "--------------------------------------------------------" -ForegroundColor Cyan

# 2. STOP THE EC2 COMPUTE CONTAINER INSTANCE
Write-Host "[1/2] Signaling EC2 Instance [$EC2_INSTANCE] to halt..." -ForegroundColor Yellow
aws ec2 stop-instances --instance-ids $EC2_INSTANCE --profile $AWS_PROFILE --region $AWS_REGION --output text

# 3. STOP THE MANAGED RDS MYSQL INSTANCE
Write-Host "[2/2] Signaling RDS MySQL Database [$RDS_INSTANCE] to power down..." -ForegroundColor Yellow
aws rds stop-db-instance --db-instance-identifier $RDS_INSTANCE --profile $AWS_PROFILE --region $AWS_REGION --output text

Write-Host "--------------------------------------------------------" -ForegroundColor Green
Write-Host " SUCCESS: BOTH COMPONENT HALT COMMANDS DELIVERED SAFELY " -ForegroundColor Green
Write-Host " Remember: Storage costs apply while compute tasks are idle. " -ForegroundColor Gray
Write-Host "--------------------------------------------------------" -ForegroundColor Green
Pause
