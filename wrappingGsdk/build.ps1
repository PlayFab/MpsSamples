$scriptRoot = Get-Location
$dropDir = "$scriptRoot\drop"
New-Item -ItemType Directory -Force -Path $dropDir

dotnet publish $scriptRoot\wrapper\wrapper.csproj -c release -o $dropDir\w10x64 --runtime win-x64 /p:PublishSingleFile=true
dotnet publish $scriptRoot\fakegame\fakegame.csproj -c release -o $dropDir\w10x64 --runtime win-x64 /p:PublishSingleFile=true
Compress-Archive -Path $dropDir\w10x64\* -DestinationPath $dropDir\gameassets.zip -Force
echo "game assets successfully zipped at $dropDir\gameassets.zip"

# open folder
Invoke-Item $PSScriptRoot\drop