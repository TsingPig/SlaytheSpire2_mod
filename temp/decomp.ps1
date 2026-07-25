param([Parameter(Mandatory=$true)][string[]]$Types)
$env:DOTNET_ROLL_FORWARD = "Major"
$sts2 = "D:\SteamLibrary\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll"
$dst = "d:\--UnityProject\RunminG-Lab\SlaytheSpire2_mod\temp\decomp"
New-Item -ItemType Directory -Force -Path $dst | Out-Null
foreach ($t in $Types) {
  $short = $t.Split('.')[-1]
  ilspycmd $sts2 -t $t 2>$null | Out-File -Encoding utf8 "$dst\$short.cs"
  Write-Output ("{0}: {1} bytes -> $dst\$short.cs" -f $short, (Get-Item "$dst\$short.cs").Length)
}
