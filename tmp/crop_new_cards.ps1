Add-Type -AssemblyName System.Drawing

$tmp = "d:\--UnityProject\RunminG-Lab\SlaytheSpire2_mod\tmp"
$smallDir = "d:\--UnityProject\RunminG-Lab\SlaytheSpire2_mod\NinjaMod\images\card_portraits"
$bigDir   = "d:\--UnityProject\RunminG-Lab\SlaytheSpire2_mod\NinjaMod\images\card_portraits\big"

# sheet file -> ordered panel snake_case names (left to right)
$sheets = @(
    @{ file = 'image.png';        panels = @('ninja_tool_prep','hand_swap','fire_spread') },
    @{ file = 'image copy.png';   panels = @('ember_recovery','fan_wind','stone_hide') },
    @{ file = 'image copy 2.png'; panels = @('musashi_dream_strike','musashi_crimson','musashi_peerless') },
    @{ file = 'image copy 3.png'; panels = @('blood_fire_transfer','shadow_breath','nimble_step','covert_ops') },
    @{ file = 'image copy 4.png'; panels = @('blade_flow','flame_dance','wildfire','earthquake') }
)

$cardAspect = 250.0 / 350.0  # width/height = 0.714...

function Save-Resized($srcBmp, $srcRect, $outPath, $outW, $outH) {
    $dst = New-Object System.Drawing.Bitmap($outW, $outH)
    $g = [System.Drawing.Graphics]::FromImage($dst)
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.PixelOffsetMode = 'HighQuality'
    $destRect = New-Object System.Drawing.Rectangle(0, 0, $outW, $outH)
    $g.DrawImage($srcBmp, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
    $dst.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $dst.Dispose()
}

foreach ($s in $sheets) {
    $path = Join-Path $tmp $s.file
    $img = [System.Drawing.Bitmap]::FromFile($path)
    $n = $s.panels.Count
    $panelW = $img.Width / $n
    $H = $img.Height

    for ($i = 0; $i -lt $n; $i++) {
        $name = $s.panels[$i]
        $srcX0 = $i * $panelW

        # center-crop this panel to card aspect ratio
        if (($H * $cardAspect) -le $panelW) {
            $cropW = $H * $cardAspect
            $cropH = $H
        } else {
            $cropW = $panelW
            $cropH = $panelW / $cardAspect
        }
        $cropX = [int][math]::Round($srcX0 + ($panelW - $cropW) / 2.0)
        $cropY = [int][math]::Round(($H - $cropH) / 2.0)
        $cw = [int][math]::Round($cropW)
        $ch = [int][math]::Round($cropH)
        if (($cropX + $cw) -gt $img.Width) { $cw = $img.Width - $cropX }
        if (($cropY + $ch) -gt $img.Height) { $ch = $img.Height - $cropY }

        $rect = New-Object System.Drawing.Rectangle($cropX, $cropY, $cw, $ch)

        Save-Resized $img $rect (Join-Path $smallDir "$name.png") 250 350
        Save-Resized $img $rect (Join-Path $bigDir   "$name.png") 606 852
        "{0} <- {1} panel {2}  crop=({3},{4} {5}x{6})" -f $name, $s.file, $i, $cropX, $cropY, $cw, $ch
    }
    $img.Dispose()
}
"DONE"
