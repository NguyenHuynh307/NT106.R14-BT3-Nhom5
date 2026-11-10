$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

$Owner   = 'NguyenHuynh307'
$Repo    = 'NT106.R14-BT3-Nhom5'
$Members = @('phu529','Liamz07','hoanganh120507','letrongphu123')

function Refresh-Path {
    $env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')
}
function Has($c) { return [bool](Get-Command $c -ErrorAction SilentlyContinue) }

# 1. Git
if (-not (Has git)) {
    Write-Host '[1/7] Cai Git bang winget...'
    winget install --id Git.Git -e --source winget --accept-package-agreements --accept-source-agreements
    Refresh-Path
    if (-not (Has git)) { throw 'Da cai Git nhung CMD chua nhan. Dong CMD, mo lai va chay lai file nay.' }
} else { Write-Host '[1/7] Git OK' }

# 2. GitHub CLI
if (-not (Has gh)) {
    Write-Host '[2/7] Cai GitHub CLI bang winget...'
    winget install --id GitHub.cli -e --source winget --accept-package-agreements --accept-source-agreements
    Refresh-Path
    if (-not (Has gh)) { throw 'Da cai gh nhung CMD chua nhan. Dong CMD, mo lai va chay lai file nay.' }
} else { Write-Host '[2/7] GitHub CLI OK' }

# 3. Dang nhap
Write-Host '[3/7] Kiem tra dang nhap GitHub...'
gh auth status 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Trinh duyet se mo de ban xac thuc (dang nhap bang tai khoan' $Owner ').'
    gh auth login --web --git-protocol https --hostname github.com
    if ($LASTEXITCODE -ne 0) { throw 'Dang nhap GitHub that bai.' }
}
gh auth setup-git 2>$null | Out-Null
$me = (gh api user --jq .login).Trim()
Write-Host "Dang nhap voi tai khoan: $me"
if ($me -ne $Owner) {
    $ans = Read-Host "Tai khoan hien tai ($me) khac $Owner. Tiep tuc tao repo duoi $me? (y/N)"
    if ($ans -ne 'y') { throw 'Da huy.' }
    $Owner = $me
}

# 4. Git init
Write-Host '[4/7] Khoi tao Git...'
if (-not (Test-Path .git)) { git init -b main | Out-Null }
git branch -M main
$name = git config user.name
if (-not $name) { git config user.name $Owner }
$mail = git config user.email
if (-not $mail) { git config user.email "$Owner@users.noreply.github.com" }

# 5. Commit
Write-Host '[5/7] Commit bo khung...'
git add -A
git diff --cached --quiet
if ($LASTEXITCODE -ne 0) { git commit -m "chore: khoi tao bo khung BT3 NT106.R14 Nhom 5" | Out-Null }

# 6. Tao repo + push
Write-Host '[6/7] Tao repo va push...'
gh repo view "$Owner/$Repo" 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    gh repo create "$Owner/$Repo" --public --description "NT106.R14 - Bai tap 3 - Nhom 5 - Client/Server TCP" --source=. --remote=origin --push
    if ($LASTEXITCODE -ne 0) { throw 'Tao repo that bai.' }
} else {
    Write-Host 'Repo da ton tai, chi push (khong force).'
    $remotes = git remote
    if ($remotes -notcontains 'origin') { git remote add origin "https://github.com/$Owner/$Repo.git" }
    else {
        $url = git remote get-url origin
        if ($url -notmatch [regex]::Escape("$Owner/$Repo")) { throw "Remote origin tro sang repo khac: $url" }
    }
    git push -u origin main
}

# 7. Moi thanh vien
Write-Host '[7/7] Gui loi moi collaborator...'
foreach ($m in $Members) {
    gh api -X PUT "repos/$Owner/$Repo/collaborators/$m" -f permission=push 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) { Write-Host "  da moi: $m" } else { Write-Host "  LOI khi moi: $m (kiem tra username)" }
}

Write-Host ''
Write-Host "XONG: https://github.com/$Owner/$Repo"
Write-Host 'Cac thanh vien can vao email/GitHub Notifications de Accept loi moi.'
