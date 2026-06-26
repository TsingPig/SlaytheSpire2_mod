param(
    [switch]$SkipExcelImport
)

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$BalanceDir = Join-Path $RepoRoot "balance"
$WorkbookPath = Join-Path $BalanceDir "NinjaMod_Balance.xlsx"
$CardsCsvPath = Join-Path $BalanceDir "cards.csv"
$GeneratedDir = Join-Path $BalanceDir "generated"
$RuntimeBalanceDir = Join-Path $RepoRoot "NinjaMod\balance"
$GeneratedCodeDir = Join-Path $RepoRoot "NinjaModCode\Generated"
$CardsJsonPath = Join-Path $RuntimeBalanceDir "cards.json"
$GeneratedCodePath = Join-Path $GeneratedCodeDir "CardBalance.g.cs"
$GeneratedMarkdownPath = Join-Path $GeneratedDir "card-balance.generated.md"

function Write-Utf8NoBom {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $directory = Split-Path -Parent $Path
    if ($directory) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

function Escape-CsvValue {
    param([AllowNull()][object]$Value)

    if ($null -eq $Value) {
        return ""
    }

    $text = [string]$Value
    if ($text -match '[,"\r\n]') {
        return '"' + $text.Replace('"', '""') + '"'
    }

    return $text
}

function Export-ExcelSheetToCsv {
    param(
        [Parameter(Mandatory = $true)][string]$Workbook,
        [Parameter(Mandatory = $true)][string]$WorksheetName,
        [Parameter(Mandatory = $true)][string]$CsvPath
    )

    if (-not (Test-Path -LiteralPath $Workbook)) {
        return $false
    }

    $excel = $null
    $book = $null
    try {
        $excel = New-Object -ComObject Excel.Application
        $excel.Visible = $false
        $excel.DisplayAlerts = $false
        $book = $excel.Workbooks.Open($Workbook, $null, $true)
        $sheet = $book.Worksheets.Item($WorksheetName)
        $used = $sheet.UsedRange
        $rowCount = [int]$used.Rows.Count
        $colCount = [int]$used.Columns.Count

        $lines = New-Object System.Collections.Generic.List[string]
        for ($r = 1; $r -le $rowCount; $r++) {
            $cells = New-Object System.Collections.Generic.List[string]
            $hasAny = $false
            for ($c = 1; $c -le $colCount; $c++) {
                $value = $sheet.Cells.Item($r, $c).Value2
                if ($null -ne $value -and [string]$value -ne "") {
                    $hasAny = $true
                }
                $cells.Add((Escape-CsvValue $value))
            }
            if ($hasAny) {
                $lines.Add(($cells -join ","))
            }
        }

        Write-Utf8NoBom -Path $CsvPath -Content (($lines -join [Environment]::NewLine) + [Environment]::NewLine)
        return $true
    }
    catch {
        Write-Warning "无法从 Excel 导出【$WorksheetName】工作表，继续使用现有 CSV。原因：$($_.Exception.Message)"
        return $false
    }
    finally {
        if ($book) { $book.Close($false) | Out-Null }
        if ($excel) { $excel.Quit() | Out-Null }
        if ($book) { [System.Runtime.InteropServices.Marshal]::ReleaseComObject($book) | Out-Null }
        if ($excel) { [System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null }
        [GC]::Collect()
        [GC]::WaitForPendingFinalizers()
    }
}

function Get-ExcelCellText {
    param(
        [Parameter(Mandatory = $true)]$Sheet,
        [Parameter(Mandatory = $true)][int]$Row,
        [Parameter(Mandatory = $true)][int]$Column
    )

    $value = $Sheet.Cells.Item($Row, $Column).Value2
    if ($null -eq $value) {
        return ""
    }
    return [string]$value
}

function Export-BalanceSheetToCsv {
    param(
        [Parameter(Mandatory = $true)][string]$Workbook,
        [Parameter(Mandatory = $true)][string]$WorksheetName,
        [Parameter(Mandatory = $true)][string]$CsvPath
    )

    if (-not (Test-Path -LiteralPath $Workbook)) {
        return $false
    }

    $excel = $null
    $book = $null
    try {
        $excel = New-Object -ComObject Excel.Application
        $excel.Visible = $false
        $excel.DisplayAlerts = $false
        $book = $excel.Workbooks.Open($Workbook, $null, $true)
        $sheet = $book.Worksheets.Item($WorksheetName)
        $used = $sheet.UsedRange
        $rowCount = [int]$used.Rows.Count
        $colCount = [int]$used.Columns.Count

        $firstCell = (Get-ExcelCellText -Sheet $sheet -Row 1 -Column 1).Trim()
        if ($firstCell -eq "CardId") {
            $lines = New-Object System.Collections.Generic.List[string]
            for ($r = 1; $r -le $rowCount; $r++) {
                $cells = New-Object System.Collections.Generic.List[string]
                $hasAny = $false
                for ($c = 1; $c -le $colCount; $c++) {
                    $value = $sheet.Cells.Item($r, $c).Value2
                    if ($null -ne $value -and [string]$value -ne "") {
                        $hasAny = $true
                    }
                    $cells.Add((Escape-CsvValue $value)) | Out-Null
                }
                if ($hasAny) {
                    $lines.Add(($cells -join ",")) | Out-Null
                }
            }
            Write-Utf8NoBom -Path $CsvPath -Content (($lines -join [Environment]::NewLine) + [Environment]::NewLine)
            return $true
        }

        $classNameRow = 0
        for ($r = 1; $r -le $rowCount; $r++) {
            $key = (Get-ExcelCellText -Sheet $sheet -Row $r -Column 1).Trim()
            if ($key -eq "ClassName") {
                $classNameRow = $r
                break
            }
        }

        if ($classNameRow -eq 0) {
            throw "无法识别【卡牌数值】表结构：既不是旧版横向表，也找不到 ClassName 属性行。"
        }

        $fieldRows = New-Object System.Collections.Generic.List[object]
        for ($r = 2; $r -le $rowCount; $r++) {
            $key = (Get-ExcelCellText -Sheet $sheet -Row $r -Column 1).Trim()
            if ($key -ne "" -and -not $key.StartsWith("_")) {
                $fieldRows.Add([ordered]@{
                    Key = $key
                    Row = $r
                }) | Out-Null
            }
        }

        if (($fieldRows | Where-Object { $_.Key -eq "CardId" }).Count -eq 0) {
            throw "转置表缺少 CardId 属性行。"
        }
        if (($fieldRows | Where-Object { $_.Key -eq "ClassName" }).Count -eq 0) {
            throw "转置表缺少 ClassName 属性行。"
        }

        $cardColumns = New-Object System.Collections.Generic.List[object]
        for ($c = 4; $c -le $colCount; $c++) {
            $className = (Get-ExcelCellText -Sheet $sheet -Row $classNameRow -Column $c).Trim()
            if ($className -ne "") {
                $cardColumns.Add([ordered]@{
                    Column = $c
                    ClassName = $className
                }) | Out-Null
            }
        }

        if ($cardColumns.Count -eq 0) {
            throw "转置表没有找到任何卡牌列。"
        }

        $lines = New-Object System.Collections.Generic.List[string]
        $lines.Add((($fieldRows | ForEach-Object { Escape-CsvValue $_.Key }) -join ",")) | Out-Null
        foreach ($cardColumn in $cardColumns) {
            $cells = New-Object System.Collections.Generic.List[string]
            foreach ($fieldRow in $fieldRows) {
                $value = $sheet.Cells.Item($fieldRow.Row, $cardColumn.Column).Value2
                if ($fieldRow.Key -eq "ClassName" -and (([string]$value).Trim() -eq "")) {
                    $value = $cardColumn.ClassName
                }
                $cells.Add((Escape-CsvValue $value)) | Out-Null
            }
            $lines.Add(($cells -join ",")) | Out-Null
        }

        Write-Utf8NoBom -Path $CsvPath -Content (($lines -join [Environment]::NewLine) + [Environment]::NewLine)
        return $true
    }
    catch {
        Write-Warning "无法从 Excel 导出【$WorksheetName】工作表，继续使用现有 CSV。原因：$($_.Exception.Message)"
        return $false
    }
    finally {
        if ($book) { $book.Close($false) | Out-Null }
        if ($excel) { $excel.Quit() | Out-Null }
        if ($book) { [System.Runtime.InteropServices.Marshal]::ReleaseComObject($book) | Out-Null }
        if ($excel) { [System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null }
        [GC]::Collect()
        [GC]::WaitForPendingFinalizers()
    }
}

function Test-IntField {
    param(
        [AllowNull()][object]$Value,
        [ref]$Parsed
    )

    $text = ([string]$Value).Trim()
    if ($text -eq "") {
        return $false
    }

    $number = 0
    if (-not [int]::TryParse($text, [ref]$number)) {
        throw "字段值必须是整数，但得到：$text"
    }
    $Parsed.Value = $number
    return $true
}

function ConvertTo-BoolValue {
    param([AllowNull()][object]$Value)

    $text = ([string]$Value).Trim()
    return @("TRUE", "True", "true", "1", "是", "yes", "YES") -contains $text
}

function ConvertTo-CSharpString {
    param([AllowNull()][object]$Value)

    $text = if ($null -eq $Value) { "" } else { [string]$Value }
    $text = $text.Replace("\", "\\").Replace('"', '\"')
    $text = $text.Replace("`r", "\r").Replace("`n", "\n")
    return '"' + $text + '"'
}

function Add-JsonObjectValues {
    param(
        [Parameter(Mandatory = $true)][hashtable]$Values,
        [AllowNull()][object]$Json,
        [Parameter(Mandatory = $true)][string]$Prefix
    )

    $text = ([string]$Json).Trim()
    if ($text -eq "") {
        return
    }

    try {
        $obj = $text | ConvertFrom-Json
        foreach ($property in $obj.PSObject.Properties) {
            $number = 0
            if ([int]::TryParse([string]$property.Value, [ref]$number)) {
                $Values["$Prefix$($property.Name)"] = $number
            }
        }
    }
    catch {
        Write-Warning "无法解析 JSON 字段：$text"
    }
}

if (-not (Test-Path -LiteralPath $CardsCsvPath)) {
    throw "找不到 $CardsCsvPath。请先保留 balance/cards.csv，或运行 scripts/refresh-balance-workbook.ps1 重建工作簿。"
}

if (-not $SkipExcelImport) {
    [void](Export-BalanceSheetToCsv -Workbook $WorkbookPath -WorksheetName "卡牌数值" -CsvPath $CardsCsvPath)
}

$cards = Import-Csv -LiteralPath $CardsCsvPath -Encoding UTF8
if ($cards.Count -eq 0) {
    throw "balance/cards.csv 没有任何卡牌行。"
}

$requiredColumns = @("CardId", "ClassName", "ChineseName", "Cost", "DescriptionZh")
$headers = $cards[0].PSObject.Properties.Name
foreach ($column in $requiredColumns) {
    if ($headers -notcontains $column) {
        throw "balance/cards.csv 缺少必需列：$column"
    }
}

$seen = @{}
$numericFields = @(
    "BaseDamage", "UpgradeDamage", "BaseBlock", "UpgradeBlock", "BaseHeal", "UpgradeHeal",
    "BaseExtraDamage", "UpgradeExtraDamage", "BaseRemove", "UpgradeRemove",
    "BaseExecutePerFive", "UpgradeExecutePerFive",
    "BaseAfterimage", "BaseDebuffImmunity", "BaseEightTechniquesAmount",
    "BaseEnmei", "BaseFlameShield", "BaseKujiProtection", "BaseKunaiBleed",
    "BaseProwl", "BaseRockShatterResistLoss", "BaseSeppukuHpMultiplier",
    "BaseShadowClone", "BaseSoulChaseBleed", "BaseSoulReapRewardCards",
    "BaseSoulReapRewardEnergy", "BaseStoneSummonMultiplier", "UpgradeStoneSummonMultiplier",
    "BaseSusanooBleedPerHit", "BaseSwallowReturnEnergy",
    "BaseRepeat", "UpgradeRepeat", "BaseCards", "UpgradeCards", "BaseBurning", "UpgradeBurning",
    "BaseBleed", "UpgradeBleed", "BaseQuench", "UpgradeQuench", "BaseResist", "UpgradeResist",
    "BaseDex", "UpgradeDex", "BaseEnergy", "UpgradeEnergy", "BurningInfusion",
    "ConstBleed", "ConstBlockPerAttack", "ConstBurningPerCard", "ConstCostReduction",
    "ConstCount", "ConstDamageReductionPct", "ConstHpPerExecute", "ConstKunaiDamage",
    "ConstResist", "ConstStealth", "ConstVigor"
)

$runtimeRows = New-Object System.Collections.Generic.List[object]
foreach ($card in $cards) {
    $id = ([string]$card.CardId).Trim()
    if ($id -eq "") {
        throw "存在 CardId 为空的卡牌行。"
    }
    if ($seen.ContainsKey($id)) {
        throw "CardId 重复：$id"
    }
    $seen[$id] = $true

    $values = @{}
    foreach ($field in $numericFields) {
        if ($headers -contains $field) {
            $parsed = 0
            if (Test-IntField -Value $card.$field -Parsed ([ref]$parsed)) {
                $values[$field] = $parsed
            }
        }
    }
    Add-JsonObjectValues -Values $values -Json $card.ExtraVarsJson -Prefix "Extra."
    Add-JsonObjectValues -Values $values -Json $card.ConstantsJson -Prefix "Const."

    $runtimeRows.Add([ordered]@{
        CardId = $id
        ClassName = ([string]$card.ClassName).Trim()
        ChineseName = [string]$card.ChineseName
        EnglishName = [string]$card.EnglishName
        Rarity = [string]$card.Rarity
        Type = [string]$card.Type
        Target = [string]$card.Target
        Cost = [string]$card.Cost
        UpgradeCost = [string]$card.UpgradeCost
        Keywords = [string]$card.Keywords
        IsToken = ConvertTo-BoolValue $card.IsToken
        IsMusashi = ConvertTo-BoolValue $card.IsMusashi
        HasSilence = ConvertTo-BoolValue $card.HasSilence
        Values = $values
        Text = [ordered]@{
            DescriptionZh = [string]$card.DescriptionZh
            DescriptionEn = [string]$card.DescriptionEn
            MechanicNotes = [string]$card.MechanicNotes
            DesignerNotes = [string]$card.DesignerNotes
        }
        CodeFile = [string]$card.CodeFile
    }) | Out-Null
}

New-Item -ItemType Directory -Force -Path $RuntimeBalanceDir, $GeneratedCodeDir, $GeneratedDir | Out-Null

$json = $runtimeRows | ConvertTo-Json -Depth 8
Write-Utf8NoBom -Path $CardsJsonPath -Content ($json + [Environment]::NewLine)

$cs = New-Object System.Collections.Generic.List[string]
$cs.Add("// <auto-generated />")
$cs.Add("// Generated by scripts/generate-balance.ps1. Do not edit this file directly.")
$cs.Add("#nullable enable")
$cs.Add("using System.Collections.Generic;")
$cs.Add("")
$cs.Add("namespace NinjaMod.NinjaModCode.Generated;")
$cs.Add("")
$cs.Add("public sealed record CardBalanceEntry(")
$cs.Add("    string CardId,")
$cs.Add("    string ClassName,")
$cs.Add("    string ChineseName,")
$cs.Add("    string EnglishName,")
$cs.Add("    string Rarity,")
$cs.Add("    string Type,")
$cs.Add("    string Target,")
$cs.Add("    string Cost,")
$cs.Add("    string UpgradeCost,")
$cs.Add("    string Keywords,")
$cs.Add("    bool IsToken,")
$cs.Add("    bool IsMusashi,")
$cs.Add("    bool HasSilence,")
$cs.Add("    IReadOnlyDictionary<string, int> Values,")
$cs.Add("    IReadOnlyDictionary<string, string> Text,")
$cs.Add("    string CodeFile);")
$cs.Add("")
$cs.Add("public static partial class CardBalance")
$cs.Add("{")
$cs.Add("    public static IReadOnlyDictionary<string, CardBalanceEntry> Cards { get; } =")
$cs.Add("        new Dictionary<string, CardBalanceEntry>")
$cs.Add("        {")

foreach ($entry in $runtimeRows | Sort-Object { $_.CardId }) {
    $valuePairs = New-Object System.Collections.Generic.List[string]
    foreach ($key in ($entry.Values.Keys | Sort-Object)) {
        $valuePairs.Add("[$(ConvertTo-CSharpString $key)] = $($entry.Values[$key])")
    }
    $valuesLiteral = if ($valuePairs.Count -gt 0) {
        "new Dictionary<string, int> { " + ($valuePairs -join ", ") + " }"
    } else {
        "new Dictionary<string, int>()"
    }

    $textPairs = New-Object System.Collections.Generic.List[string]
    foreach ($key in @("DescriptionZh", "DescriptionEn", "MechanicNotes", "DesignerNotes")) {
        $textPairs.Add("[$(ConvertTo-CSharpString $key)] = $(ConvertTo-CSharpString $entry.Text[$key])")
    }
    $textLiteral = "new Dictionary<string, string> { " + ($textPairs -join ", ") + " }"

    $cs.Add("            [$(ConvertTo-CSharpString $entry.CardId)] = new(")
    $cs.Add("                CardId: $(ConvertTo-CSharpString $entry.CardId),")
    $cs.Add("                ClassName: $(ConvertTo-CSharpString $entry.ClassName),")
    $cs.Add("                ChineseName: $(ConvertTo-CSharpString $entry.ChineseName),")
    $cs.Add("                EnglishName: $(ConvertTo-CSharpString $entry.EnglishName),")
    $cs.Add("                Rarity: $(ConvertTo-CSharpString $entry.Rarity),")
    $cs.Add("                Type: $(ConvertTo-CSharpString $entry.Type),")
    $cs.Add("                Target: $(ConvertTo-CSharpString $entry.Target),")
    $cs.Add("                Cost: $(ConvertTo-CSharpString $entry.Cost),")
    $cs.Add("                UpgradeCost: $(ConvertTo-CSharpString $entry.UpgradeCost),")
    $cs.Add("                Keywords: $(ConvertTo-CSharpString $entry.Keywords),")
    $cs.Add("                IsToken: $(([string]$entry.IsToken).ToLowerInvariant()),")
    $cs.Add("                IsMusashi: $(([string]$entry.IsMusashi).ToLowerInvariant()),")
    $cs.Add("                HasSilence: $(([string]$entry.HasSilence).ToLowerInvariant()),")
    $cs.Add("                Values: $valuesLiteral,")
    $cs.Add("                Text: $textLiteral,")
    $cs.Add("                CodeFile: $(ConvertTo-CSharpString $entry.CodeFile)),")
}

$cs.Add("        };")
$cs.Add("")
$cs.Add("    public static CardBalanceEntry? Find(string cardId) =>")
$cs.Add("        Cards.TryGetValue(cardId, out var entry) ? entry : null;")
$cs.Add("")
$cs.Add("    public static int Value(string cardId, string key, int fallback = 0) =>")
$cs.Add("        Cards.TryGetValue(cardId, out var entry) && entry.Values.TryGetValue(key, out var value)")
$cs.Add("            ? value")
$cs.Add("            : fallback;")
$cs.Add("")
$cs.Add("    public static string TextValue(string cardId, string key, string fallback = """") =>")
$cs.Add("        Cards.TryGetValue(cardId, out var entry) && entry.Text.TryGetValue(key, out var value)")
$cs.Add("            ? value")
$cs.Add("            : fallback;")
$cs.Add("}")

Write-Utf8NoBom -Path $GeneratedCodePath -Content (($cs -join [Environment]::NewLine) + [Environment]::NewLine)

$md = New-Object System.Collections.Generic.List[string]
$md.Add("# NinjaMod 卡牌数值表（自动生成）")
$md.Add("")
$md.Add("> 来源：`balance/cards.csv`。若策划修改 `balance/NinjaMod_Balance.xlsx`，请先运行 `powershell -ExecutionPolicy Bypass -File scripts\generate-balance.ps1`。")
$md.Add("")
$md.Add("| 卡牌 | 类名 | 稀有度 | 费用 | 类型 | 关键数值 | 说明 |")
$md.Add("|---|---|---|---|---|---|---|")
foreach ($card in $cards) {
    $keyValues = New-Object System.Collections.Generic.List[string]
    foreach ($field in $numericFields) {
        if ($headers -contains $field) {
            $value = ([string]$card.$field).Trim()
            if ($value -ne "") {
                $keyValues.Add("$field=$value")
            }
        }
    }
    $safeDesc = ([string]$card.DescriptionZh).Replace("|", "\|")
    $safeName = ([string]$card.ChineseName).Replace("|", "\|")
    $md.Add("| $safeName | $($card.ClassName) | $($card.Rarity) | $($card.Cost)$($(if ($card.UpgradeCost) { '(' + $card.UpgradeCost + ')' } else { '' })) | $($card.Type) | $($keyValues -join '<br>') | $safeDesc |")
}
Write-Utf8NoBom -Path $GeneratedMarkdownPath -Content (($md -join [Environment]::NewLine) + [Environment]::NewLine)

Write-Host "Generated:"
Write-Host "  $CardsJsonPath"
Write-Host "  $GeneratedCodePath"
Write-Host "  $GeneratedMarkdownPath"










