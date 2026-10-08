$ErrorActionPreference = 'Stop'
Push-Location -LiteralPath $PSScriptRoot
try {
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw 'Git is missing. Install Git for Windows: https://git-scm.com/download/win' }
    if (Test-Path -LiteralPath '.git') {
        $remotes = & git remote -v
        if ($remotes -match 'Altersam[/\:]Biology_VR(\.git)?\s') { throw 'This is the existing Biology_VR repository. Stop: use the separate Biology_VR_App folder.' }
    } else {
        & git init -b main
        if ($LASTEXITCODE -ne 0) { throw 'Could not initialize the local repository.' }
    }
    & git lfs install --local
    if ($LASTEXITCODE -ne 0) { throw 'Git LFS setup failed. Install Git LFS: https://git-lfs.com/' }
    'Ready. Open GitHub Desktop: File > Add local repository, choose this folder.'
    'Commit to main, then Publish repository with the NEW name Biology_VR_App.'
    'No commit, push or remote repository change was performed by this script.'
} finally {
    Pop-Location
}
