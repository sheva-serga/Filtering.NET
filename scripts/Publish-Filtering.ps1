<#
.SYNOPSIS
    Publish all three Filtering.Net packages to nuget.org.

.DESCRIPTION
    Pre-flight checks -> bump version -> promote CHANGELOG.md [Unreleased] -> build ->
    test -> pack -> commit -> tag -> push to NuGet -> push git -> optional GitHub/GitLab
    release.

    Vendor-agnostic: detects `gh` (GitHub) or `glab` (GitLab) and creates the
    platform release if either is on PATH; otherwise prints the command for you
    to run manually.

.PARAMETER Bump
    Which SemVer component to increment: Major, Minor, or Patch.

.PARAMETER ApiKey
    NuGet API key. Defaults to $env:NUGET_API_KEY.

.PARAMETER DryRun
    Do everything except `dotnet nuget push`, `git push`, and platform release
    creation. File edits are still made and committed locally; remote pushes are
    skipped.

.PARAMETER NoTest
    Skip `dotnet test`.

.PARAMETER NoPush
    Pack and commit/tag locally; skip remote pushes (NuGet + git).

.PARAMETER NoCommit
    Edit version + changelog files; skip git commit + tag.

.PARAMETER Force
    Allow empty `## [Unreleased]` block; allow non-master/main branch.

.EXAMPLE
    ./scripts/Publish-Filtering.ps1 -Bump Patch
    Bump patch version, full publish.

.EXAMPLE
    ./scripts/Publish-Filtering.ps1 -Bump Minor -DryRun
    Show what would happen for a minor bump without publishing.

.EXAMPLE
    ./scripts/Publish-Filtering.ps1 -Bump Patch -NoPush
    Pack + commit + tag locally; inspect artifacts/ before pushing.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Major', 'Minor', 'Patch')]
    [string]$Bump,

    [string]$ApiKey = $env:NUGET_API_KEY,

    [switch]$DryRun,
    [switch]$NoTest,
    [switch]$NoPush,
    [switch]$NoCommit,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Repo root = parent of this script's directory.
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $RepoRoot

$PropsPath     = Join-Path $RepoRoot 'Directory.Build.props'
$ChangelogPath = Join-Path $RepoRoot 'CHANGELOG.md'
$ArtifactsDir  = Join-Path $RepoRoot 'artifacts'

$Projects = @(
    'src/Filtering.Net/Filtering.Net.csproj',
    'src/Filtering.Net.Generator/Filtering.Net.Generator.csproj',
    'src/Filtering.Net.EntityFrameworkCore/Filtering.Net.EntityFrameworkCore.csproj'
)

function Write-Phase {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Invoke-Native {
    param([string]$Command, [string[]]$Arguments)
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed: $Command $($Arguments -join ' ')"
    }
}

# -------- Phase 1: Pre-flight --------
Write-Phase "Pre-flight"

# Clean working tree.
$dirty = git status --porcelain
if ($dirty) {
    throw "Working tree is not clean. Commit or stash changes first.`n$dirty"
}

# On master or main.
$branch = (git rev-parse --abbrev-ref HEAD).Trim()
if ($branch -notin @('master', 'main') -and -not $Force) {
    throw "Current branch is '$branch'. Switch to master/main or pass -Force."
}

# dotnet on PATH.
$null = Get-Command dotnet -ErrorAction Stop

# API key (unless skipping push).
if (-not ($DryRun -or $NoPush) -and [string]::IsNullOrWhiteSpace($ApiKey)) {
    throw "NuGet API key required. Set `$env:NUGET_API_KEY or pass -ApiKey."
}

# Unreleased block has at least one real bullet.
if (-not (Test-Path $ChangelogPath)) {
    throw "CHANGELOG.md not found at $ChangelogPath."
}
$changelogText = Get-Content $ChangelogPath -Raw

# Note: |\Z in the lookahead ensures this matches even when ## [Unreleased] is the
# only heading in the file (i.e. no prior version block follows it).
$unreleasedMatch = [regex]::Match(
    $changelogText,
    '(?ms)^##\s*\[Unreleased\][^\n]*\n(.*?)(?=^##\s*\[|\Z)'
)
if (-not $unreleasedMatch.Success) {
    throw "Could not locate '## [Unreleased]' block in CHANGELOG.md."
}
$unreleasedBody = $unreleasedMatch.Groups[1].Value

$realBullets = $unreleasedBody -split "`n" | Where-Object {
    $line = $_.Trim()
    $line -match '^-\s+\S' -and $line -notmatch '^\-\s*\(your work-in-progress goes here\)\s*$'
}
if (-not $realBullets -and -not $Force) {
    throw "## [Unreleased] in CHANGELOG.md has no real entries. Add release notes or pass -Force."
}

# Warn (don't fail) if no platform CLI present.
$ghAvailable   = [bool](Get-Command gh   -ErrorAction SilentlyContinue)
$glabAvailable = [bool](Get-Command glab -ErrorAction SilentlyContinue)
if (-not ($ghAvailable -or $glabAvailable)) {
    Write-Host "  Note: neither 'gh' nor 'glab' is on PATH. Platform release will be skipped." -ForegroundColor Yellow
}

Write-Host "  Pre-flight OK. Branch: $branch."

# -------- Phase 2: Compute new version --------
Write-Phase "Compute new version"

$propsText = Get-Content $PropsPath -Raw
$versionMatch = [regex]::Match($propsText, '<Version>(\d+)\.(\d+)\.(\d+)</Version>')
if (-not $versionMatch.Success) {
    throw "Could not find <Version>X.Y.Z</Version> in Directory.Build.props."
}
$major = [int]$versionMatch.Groups[1].Value
$minor = [int]$versionMatch.Groups[2].Value
$patch = [int]$versionMatch.Groups[3].Value
$currentVersion = "$major.$minor.$patch"

switch ($Bump) {
    'Major' { $major++; $minor = 0; $patch = 0 }
    'Minor' { $minor++; $patch = 0 }
    'Patch' { $patch++ }
}
$newVersion = "$major.$minor.$patch"
$tag        = "v$newVersion"
$today      = (Get-Date).ToString('yyyy-MM-dd')

Write-Host "  $currentVersion -> $newVersion (tag: $tag, date: $today)"

# -------- Phase 3: Edit files --------
Write-Phase "Edit Directory.Build.props and CHANGELOG.md"

# Update <Version>.
$newPropsText = $propsText -replace `
    '<Version>\d+\.\d+\.\d+</Version>', `
    "<Version>$newVersion</Version>"
Set-Content -Path $PropsPath -Value $newPropsText -NoNewline:$false

# Promote [Unreleased] -> [<newVersion>] - <today>; insert fresh empty Unreleased.
# IMPORTANT: The closing "@ must be at column 0 (no leading whitespace).
$emptyUnreleased = @"
## [Unreleased]

### Added
-

"@

$promotedHeading   = "## [$newVersion] - $today"
$capturedNotes     = "## [$newVersion] - $today`n" + $unreleasedBody.TrimEnd() + "`n"
$newChangelogText  = $changelogText -replace `
    '(?m)^##\s*\[Unreleased\][^\n]*$', `
    ($emptyUnreleased.TrimEnd() + "`n`n" + $promotedHeading)
Set-Content -Path $ChangelogPath -Value $newChangelogText -NoNewline:$false

# Save release notes to a tempfile for §9 (and `gh`/`glab` --notes-file).
$notesFile = Join-Path ([System.IO.Path]::GetTempPath()) "filtering-net-release-$newVersion.md"
Set-Content -Path $notesFile -Value $capturedNotes -NoNewline:$false

Write-Host "  Updated <Version> in Directory.Build.props"
Write-Host "  Promoted [Unreleased] -> [$newVersion] - $today in CHANGELOG.md"
Write-Host "  Release notes captured to: $notesFile"

# -------- Phase 4: Build & test --------
Write-Phase "Build & test"

Invoke-Native dotnet @('build', '-c', 'Release')

if (-not $NoTest) {
    Invoke-Native dotnet @('test', '-c', 'Release', '--no-build')
} else {
    Write-Host "  Tests skipped (-NoTest)."
}

# -------- Phase 5: Pack --------
Write-Phase "Pack"

if (Test-Path $ArtifactsDir) {
    Remove-Item $ArtifactsDir -Recurse -Force
}
New-Item -ItemType Directory -Path $ArtifactsDir | Out-Null

foreach ($proj in $Projects) {
    Invoke-Native dotnet @('pack', $proj, '-c', 'Release', '--no-build', '-o', $ArtifactsDir)
}

$packages = Get-ChildItem $ArtifactsDir -Filter "*.$newVersion.nupkg"
if ($packages.Count -ne 3) {
    throw "Expected 3 .nupkg files in $ArtifactsDir, found $($packages.Count)."
}
Write-Host "  Packed $($packages.Count) packages at version $newVersion."

# -------- Phase 6: Commit & tag --------
Write-Phase "Commit & tag"

if ($NoCommit) {
    Write-Host "  Skipped (-NoCommit)."
} else {
    Invoke-Native git @('add', $PropsPath, $ChangelogPath)
    Invoke-Native git @('commit', '-m', "release: $tag")
    Invoke-Native git @('tag', '-a', $tag, '-m', "Release $tag")
    Write-Host "  Committed + tagged $tag."
}

# -------- Phase 7: Push to NuGet --------
Write-Phase "Push to NuGet"

if ($DryRun -or $NoPush) {
    Write-Host "  Skipped ($(if ($DryRun) { '-DryRun' } else { '-NoPush' }))."
} else {
    foreach ($pkg in $packages) {
        Invoke-Native dotnet @(
            'nuget', 'push', $pkg.FullName,
            '--source', 'https://api.nuget.org/v3/index.json',
            '--api-key', $ApiKey,
            '--skip-duplicate'
        )
    }
    Write-Host "  Pushed $($packages.Count) packages to nuget.org."
}

# -------- Phase 8: Push git --------
Write-Phase "Push git"

if ($DryRun -or $NoPush -or $NoCommit) {
    Write-Host "  Skipped."
} else {
    Invoke-Native git @('push', 'origin', $branch, '--follow-tags')
    Write-Host "  Pushed branch + tag to origin."
}

# -------- Phase 9: Platform release --------
Write-Phase "Platform release"

if ($DryRun -or $NoPush) {
    Write-Host "  Skipped."
} else {
    $remoteUrl = (git remote get-url origin).Trim()
    if ($remoteUrl -match 'github\.com' -and $ghAvailable) {
        Invoke-Native gh @('release', 'create', $tag, '--title', $tag, '--notes-file', $notesFile)
        Write-Host "  Created GitHub release $tag."
    } elseif ($remoteUrl -match 'gitlab\.com' -and $glabAvailable) {
        Invoke-Native glab @('release', 'create', $tag, '--name', $tag, '--notes-file', $notesFile)
        Write-Host "  Created GitLab release $tag."
    } else {
        Write-Host "  No matching CLI or unrecognised remote. Run manually:" -ForegroundColor Yellow
        Write-Host "    gh release create $tag --title `"$tag`" --notes-file `"$notesFile`""
        Write-Host "  (or the equivalent for your hosting provider.)"
    }
}

# -------- Phase 10: Cleanup --------
Write-Phase "Done"

Remove-Item $notesFile -Force -ErrorAction SilentlyContinue

Write-Host "  Version:  $newVersion"
Write-Host "  Tag:      $tag"
Write-Host "  Packages: $($packages.Count)"
if ($DryRun) {
    Write-Host "  DryRun:   no remote actions taken."
}
Write-Host ""
Write-Host "Recovery (if needed):"
Write-Host "  git tag -d $tag; git reset --soft HEAD~1; git checkout Directory.Build.props CHANGELOG.md"
