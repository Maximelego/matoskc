param(
	[Parameter(Mandatory = $true)]
	[string]$Mode
)

Write-Host "Running app in $Mode mode"
docker compose -f "./docker-compose.$Mode.yml" up -d
