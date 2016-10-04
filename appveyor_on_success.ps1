. ./appveyor_helpers.ps1

if ($env:APPVEYOR_REPO_BRANCH -eq "master" -and -not $env:APPVEYOR_PULL_REQUEST_NUMBER)
{
    $strPath = FindSingleFile ([System.IO.Path]::Combine($PSScriptRoot, "DeploymentArtifacts", "BuildAgentPayload", "Release")) "SonarQube.Scanner.MSBuild.exe"
    $Assembly = [Reflection.Assembly]::Loadfile($strPath)

    $AssemblyName = $Assembly.GetName()
    $Assemblyversion = $AssemblyName.version.ToString()

    $FinalVersion = $Assemblyversion + '-build' + $env:APPVEYOR_BUILD_NUMBER

    # Upload artifacts on repox
    $implZipPath = FindSingleFile ([System.IO.Path]::Combine($PSScriptRoot, "DeploymentArtifacts", "CSharpPluginPayload", "Release")) "SonarQube.MSBuild.Runner.Implementation.zip"
    #DeployOnRepox $implZipPath "impl" $FinalVersion
    $scannerZipPath = FindSingleFile ([System.IO.Path]::Combine($PSScriptRoot, "DeploymentArtifacts", "BuildAgentPayload", "Release")) "SonarQube.Scanner.MSBuild.zip"
    #DeployOnRepox $scannerZipPath "" $FinalVersion
    write-host -f green  "replace zip filenames in pom.xml"
    (Get-Content .\pom.xml) -replace 'implZipPath', "$implZipPath" | Set-Content .\pom.xml
    (Get-Content .\pom.xml) -replace 'scannerZipPath', "$scannerZipPath" | Set-Content .\pom.xml
        
    write-host -f green  "set version $FinalVersion in pom.xml"
    $command = "mvn versions:set -DgenerateBackupPoms=false -DnewVersion='$FinalVersion'"
    iex $command
    write-host -f green  "set version $FinalVersion in env VAR PROJECT_VERSION for artifactory buildinfo metadata"
    $env:PROJECT_VERSION=$FinalVersion
    write-host -f green  "set the buildnumber to this job build number"
    $env:BUILD_ID=$env:APPVEYOR_BUILD_NUMBER
    write-host -f green  "Deploy to repox with $FinalVersion"    
    $command = 'mvn deploy -Pdeploy-sonarsource -B -e -V'
    iex $command
}
