param()

$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$BalanceDir = Join-Path $RepoRoot "balance"
$CardsCsvPath = Join-Path $BalanceDir "cards.csv"
$MechanicsCsvPath = Join-Path $BalanceDir "mechanics.csv"
$WorkbookPath = Join-Path $BalanceDir "NinjaMod_Balance.xlsx"

if (-not (Test-Path -LiteralPath $CardsCsvPath)) {
    throw "找不到 $CardsCsvPath"
}

$cards = Import-Csv -LiteralPath $CardsCsvPath -Encoding UTF8
$mechanics = if (Test-Path -LiteralPath $MechanicsCsvPath) {
    Import-Csv -LiteralPath $MechanicsCsvPath -Encoding UTF8
} else {
    @()
}

function Write-Matrix {
    param(
        [Parameter(Mandatory = $true)]$Sheet,
        [Parameter(Mandatory = $true)][int]$Row,
        [Parameter(Mandatory = $true)][int]$Column,
        [Parameter(Mandatory = $true)][object[]]$Rows
    )

    if ($Rows.Count -eq 0) {
        return $null
    }

    $columnCount = ($Rows | ForEach-Object { $_.Count } | Measure-Object -Maximum).Maximum
    $matrix = New-Object "object[,]" $Rows.Count, $columnCount
    for ($r = 0; $r -lt $Rows.Count; $r++) {
        for ($c = 0; $c -lt $columnCount; $c++) {
            $matrix[$r, $c] = if ($c -lt $Rows[$r].Count) { $Rows[$r][$c] } else { "" }
        }
    }

    $range = $Sheet.Range(
        $Sheet.Cells.Item($Row, $Column),
        $Sheet.Cells.Item($Row + $Rows.Count - 1, $Column + $columnCount - 1)
    )
    $range.Value2 = $matrix
    return $range
}

function Set-TitleStyle {
    param($Range)
    $Range.Font.Bold = $true
    $Range.Font.Size = 16
    $Range.Font.Color = 0xFFFFFF
    $Range.Interior.Color = 0x5F3B1B
}

function Set-HeaderStyle {
    param($Range)
    $Range.Font.Bold = $true
    $Range.Font.Color = 0xFFFFFF
    $Range.Interior.Color = 0x6F4A1F
    $Range.WrapText = $true
}

function AutoFit-Capped {
    param($Sheet)
    $Sheet.Columns.AutoFit() | Out-Null
    for ($i = 1; $i -le $Sheet.UsedRange.Columns.Count; $i++) {
        $column = $Sheet.Columns.Item($i)
        if ($column.ColumnWidth -gt 60) {
            $column.ColumnWidth = 60
        }
        if ($column.ColumnWidth -lt 10) {
            $column.ColumnWidth = 10
        }
    }
}

function Get-CardFamily {
    param($Card)

    $className = [string]$Card.ClassName
    $chineseName = [string]$Card.ChineseName
    $rarity = [string]$Card.Rarity
    $isMusashi = ([string]$Card.IsMusashi).Trim().ToLowerInvariant()

    $fireClasses = @(
        "Ashes", "BlazeInferno", "BurningHeart", "CrimsonClaw", "DemonFlameBurst",
        "Detonation", "FlameBarrage", "FlameShield", "ForgeFlameThrust", "Lihuo", "Quenching"
    )
    $earthClasses = @(
        "CraneShield", "EarthEscape", "EarthRend", "EarthTalisman", "EarthWall",
        "Petrification", "Rashomon", "RockShatter", "StoneGatherThrust", "StoneSummon"
    )

    if ($rarity -eq "Token") {
        return "衍生牌"
    }
    if ($isMusashi -eq "true" -or $chineseName.StartsWith("武藏") -or $className.StartsWith("Musashi")) {
        return "武藏"
    }
    if ($chineseName.StartsWith("火忍") -or $fireClasses -contains $className) {
        return "火忍"
    }
    if ($chineseName.StartsWith("土忍") -or $earthClasses -contains $className) {
        return "土忍"
    }
    return "其他"
}

function Get-FamilyOrder {
    param([string]$Family)
    switch ($Family) {
        "火忍" { return 1 }
        "土忍" { return 2 }
        "武藏" { return 3 }
        "其他" { return 4 }
        "衍生牌" { return 5 }
        default { return 99 }
    }
}

function Get-RarityOrder {
    param([string]$Rarity)
    switch ($Rarity) {
        "Basic" { return 1 }
        "Common" { return 2 }
        "Uncommon" { return 3 }
        "Rare" { return 4 }
        "Token" { return 5 }
        default { return 99 }
    }
}

function Get-ExcelColumnName {
    param([Parameter(Mandatory = $true)][int]$Index)

    $name = ""
    while ($Index -gt 0) {
        $mod = ($Index - 1) % 26
        $name = [char](65 + $mod) + $name
        $Index = [math]::Floor(($Index - $mod) / 26)
    }
    return $name
}

$excel = $null
$book = $null

try {
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false

    $book = $excel.Workbooks.Add()
    while ($book.Worksheets.Count -lt 5) {
        $book.Worksheets.Add() | Out-Null
    }
    while ($book.Worksheets.Count -gt 5) {
        $book.Worksheets.Item($book.Worksheets.Count).Delete()
    }

    $sheetIntro = $book.Worksheets.Item(1)
    $sheetIndex = $book.Worksheets.Item(2)
    $sheetCards = $book.Worksheets.Item(3)
    $sheetFields = $book.Worksheets.Item(4)
    $sheetMechanics = $book.Worksheets.Item(5)

    $sheetIntro.Name = "说明"
    $sheetIndex.Name = "卡牌索引"
    $sheetCards.Name = "卡牌数值"
    $sheetFields.Name = "字段说明"
    $sheetMechanics.Name = "机制说明"

    foreach ($sheet in @($sheetIntro, $sheetIndex, $sheetCards, $sheetFields, $sheetMechanics)) {
        $sheet.Activate() | Out-Null
        $excel.ActiveWindow.DisplayGridlines = $false
        $sheet.Cells.Font.Name = "Microsoft YaHei UI"
        $sheet.Cells.Font.Size = 10
    }

    $introRows = @(
        @("NinjaMod 数值策划入口", ""),
        @("用途", "在这里查看和调整每张卡牌的描述、数值、机制备注。【卡牌数值】页已改为：卡牌作为列，属性作为行。黄色格是策划常改字段。"),
        @("找牌方式", "先打开【卡牌索引】页，可按系列、稀有度、类型、费用筛选；点击【打开】可跳到【卡牌数值】里的对应卡牌列。"),
        @("改数流程", "1. 打开本文件，修改【卡牌数值】页黄色格。"),
        @("", "2. 保存并关闭 Excel。"),
        @("", "3. 在本仓库根目录运行：powershell -ExecutionPolicy Bypass -File scripts\generate-balance.ps1"),
        @("", "4. 如果要进游戏测试，再运行：powershell -ExecutionPolicy Bypass -File scripts\build-and-install.ps1 -Configuration Release"),
        @("路径规则", "所有命令都使用相对路径；不要在表格里填写本机绝对路径。"),
        @("不要改", "第一列【属性键】是程序识别用的英文内部字段；第一行是卡牌中文名，ClassName 行是程序定位卡牌用的类名。日常改数只改卡牌列里的值。"),
        @("注意", "scripts\refresh-balance-workbook.ps1 只用于重建这个 Excel 模板，会按 cards.csv 覆盖当前工作簿。日常改数不要运行它。"),
        @("当前接入状态", "本表会生成 NinjaMod/balance/cards.json 与 NinjaModCode/Generated/CardBalance.g.cs；卡牌类迁移到该入口后，策划改表即可驱动游戏数值。")
    )
    $introRange = Write-Matrix -Sheet $sheetIntro -Row 1 -Column 1 -Rows $introRows
    $sheetIntro.Range("A1:B1").Merge() | Out-Null
    $sheetIntro.Range("A1").Value2 = "NinjaMod 数值策划入口"
    Set-TitleStyle $sheetIntro.Range("A1")
    $sheetIntro.Columns.Item(1).ColumnWidth = 18
    $sheetIntro.Columns.Item(2).ColumnWidth = 110
    $sheetIntro.Range("A2:A11").Font.Bold = $true
    $sheetIntro.Range("B2:B11").WrapText = $true
    $sheetIntro.Range("A2:B11").Borders.Color = 0xD9D9D9

    $headers = @($cards[0].PSObject.Properties | ForEach-Object { $_.Name })
    $editableFields = @(
        "ChineseName", "EnglishName", "Cost", "UpgradeCost", "Keywords",
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
        "ConstResist", "ConstStealth", "ConstVigor",
        "ExtraVarsJson", "ConstantsJson", "DescriptionZh", "DescriptionEn", "MechanicNotes", "DesignerNotes"
    )

    $fieldLabels = @{
        CardId = "卡牌ID"; ClassName = "类名"; ChineseName = "中文名"; EnglishName = "英文名"
        Rarity = "稀有度"; Type = "类型"; Target = "目标"; Cost = "费用"; UpgradeCost = "升级费用"; Keywords = "关键词"
        IsToken = "衍生牌"; IsMusashi = "武藏牌"; HasSilence = "静默"
        BaseDamage = "基础伤害"; UpgradeDamage = "升级伤害"; BaseBlock = "基础格挡"; UpgradeBlock = "升级格挡"
        BaseHeal = "基础回复"; UpgradeHeal = "升级回复"; BaseExtraDamage = "基础额外伤害"; UpgradeExtraDamage = "升级额外伤害"
        BaseRemove = "基础移除层数"; UpgradeRemove = "升级移除层数"; BaseExecutePerFive = "基础斩杀系数"; UpgradeExecutePerFive = "升级斩杀系数"
        BaseAfterimage = "残影层数"; BaseDebuffImmunity = "负面免疫层数"; BaseEightTechniquesAmount = "八法数值"
        BaseEnmei = "圆明层数"; BaseFlameShield = "火盾层数"; BaseKujiProtection = "九字护身层数"; BaseKunaiBleed = "飞刀流血"
        BaseProwl = "潜行层数"; BaseRockShatterResistLoss = "碎石移除抵挡"; BaseSeppukuHpMultiplier = "切腹失血倍率"
        BaseShadowClone = "影分身层数"; BaseSoulChaseBleed = "追魂流血"; BaseSoulReapRewardCards = "索命奖励抽牌"
        BaseSoulReapRewardEnergy = "索命奖励能量"; BaseStoneSummonMultiplier = "召石倍率"; UpgradeStoneSummonMultiplier = "升级召石倍率"
        BaseSusanooBleedPerHit = "须佐每段流血"; BaseSwallowReturnEnergy = "燕返返能"
        ConstBleed = "常量：流血"; ConstBlockPerAttack = "常量：每次攻击格挡"; ConstBurningPerCard = "常量：每张牌燃烧"
        ConstCostReduction = "常量：费用降低"; ConstCount = "常量：次数"; ConstDamageReductionPct = "常量：伤害比例%"
        ConstHpPerExecute = "常量：斩杀每几点生命"; ConstKunaiDamage = "常量：飞刀伤害"; ConstResist = "常量：抵挡"
        ConstStealth = "常量：潜行"; ConstVigor = "常量：力量"
        BaseRepeat = "基础段数"; UpgradeRepeat = "升级段数"; BaseCards = "基础抽牌/张数"; UpgradeCards = "升级抽牌/张数"
        BaseBurning = "基础燃烧"; UpgradeBurning = "升级燃烧"; BaseBleed = "基础流血"; UpgradeBleed = "升级流血"
        BaseQuench = "基础淬火"; UpgradeQuench = "升级淬火"; BaseResist = "基础抵挡"; UpgradeResist = "升级抵挡"
        BaseDex = "基础敏捷"; UpgradeDex = "升级敏捷"; BaseEnergy = "基础能量"; UpgradeEnergy = "升级能量"; BurningInfusion = "燃烧追加"
        ExtraVarsJson = "其他变量 JSON"; ConstantsJson = "常量 JSON"; DescriptionZh = "中文描述"; DescriptionEn = "英文描述"
        MechanicNotes = "机制备注"; DesignerNotes = "策划备注"; CodeFile = "代码文件"
    }
    $fieldDescriptions = @{
        CardId = "稳定卡牌 ID；不要随意修改。"; ClassName = "C# 类名，脚本用它定位卡牌；不要随意修改。"
        ChineseName = "游戏内中文显示名。"; EnglishName = "英文显示名。"; Rarity = "稀有度。"; Type = "牌类型。"; Target = "目标类型。"
        Cost = "基础费用；X 费写 X。"; UpgradeCost = "升级后费用；空表示不变。"; Keywords = "关键词备注，例如 消耗、保留、静默。"
        IsToken = "是否衍生牌。"; IsMusashi = "是否武藏系列牌。"; HasSilence = "是否带静默关键词。"
        BaseDamage = "基础伤害。"; UpgradeDamage = "升级后伤害。"; BaseBlock = "基础格挡。"; UpgradeBlock = "升级后格挡。"
        BaseHeal = "基础回复。"; UpgradeHeal = "升级后回复。"; BaseExtraDamage = "条件或额外伤害。"; UpgradeExtraDamage = "升级后条件或额外伤害。"
        BaseRemove = "基础移除层数/数量。"; UpgradeRemove = "升级后移除层数/数量。"; BaseExecutePerFive = "每损失 5 点生命的斩杀加成。"; UpgradeExecutePerFive = "升级后的斩杀加成。"
        BaseAfterimage = "残影相关层数。"; BaseDebuffImmunity = "负面免疫层数。"; BaseEightTechniquesAmount = "忍者八法统一使用的数值。"
        BaseEnmei = "圆明层数。"; BaseFlameShield = "火盾层数。"; BaseKujiProtection = "九字护身层数。"; BaseKunaiBleed = "飞刀附加流血。"
        BaseProwl = "潜行层数。"; BaseRockShatterResistLoss = "土忍：碎石移除的抵挡层数。"; BaseSeppukuHpMultiplier = "切腹按 X 失去生命的倍率。"
        BaseShadowClone = "影分身层数。"; BaseSoulChaseBleed = "追魂附加流血。"; BaseSoulReapRewardCards = "索命奖励抽牌数。"; BaseSoulReapRewardEnergy = "索命奖励能量。"
        BaseStoneSummonMultiplier = "土忍：召石基础抵挡倍率。"; UpgradeStoneSummonMultiplier = "土忍：召石升级抵挡倍率。"
        BaseSusanooBleedPerHit = "须佐能乎每段附加流血。"; BaseSwallowReturnEnergy = "燕返返还能量。"
        ConstBleed = "特殊逻辑常量：流血。"; ConstBlockPerAttack = "特殊逻辑常量：每次攻击格挡。"; ConstBurningPerCard = "特殊逻辑常量：每消耗 1 张牌施加的燃烧。"
        ConstCostReduction = "特殊逻辑常量：费用降低。"; ConstCount = "特殊逻辑常量：次数/数量。"; ConstDamageReductionPct = "特殊逻辑常量：伤害百分比。"
        ConstHpPerExecute = "特殊逻辑常量：斩杀每几点生命。"; ConstKunaiDamage = "特殊逻辑常量：飞刀伤害。"; ConstResist = "特殊逻辑常量：抵挡。"
        ConstStealth = "特殊逻辑常量：潜行。"; ConstVigor = "特殊逻辑常量：力量。"
        BaseRepeat = "基础攻击段数/重复次数。"; UpgradeRepeat = "升级后攻击段数/重复次数。"; BaseCards = "基础抽牌或生成张数。"; UpgradeCards = "升级后抽牌或生成张数。"
        BaseBurning = "基础燃烧层数。"; UpgradeBurning = "升级后燃烧层数。"; BaseBleed = "基础流血层数。"; UpgradeBleed = "升级后流血层数。"
        BaseQuench = "基础淬火层数。"; UpgradeQuench = "升级后淬火层数。"; BaseResist = "基础抵挡层数。"; UpgradeResist = "升级后抵挡层数。"
        BaseDex = "基础敏捷。"; UpgradeDex = "升级后敏捷。"; BaseEnergy = "基础能量。"; UpgradeEnergy = "升级后能量。"; BurningInfusion = "攻击附带的燃烧追加层数。"
        ExtraVarsJson = "扩展变量，保持 JSON 格式。"; ConstantsJson = "常量变量，保持 JSON 格式。"; DescriptionZh = "中文卡牌描述。"; DescriptionEn = "英文卡牌描述。"
        MechanicNotes = "机制说明，不直接影响运行时。"; DesignerNotes = "策划备注，不直接影响运行时。"; CodeFile = "对应代码文件路径。"
    }

    function Get-FieldLabel {
        param([string]$Field)
        if ($fieldLabels.ContainsKey($Field)) {
            return $fieldLabels[$Field]
        }
        return $Field
    }

    function Get-FieldDescription {
        param([string]$Field)
        if ($fieldDescriptions.ContainsKey($Field)) {
            return $fieldDescriptions[$Field]
        }
        return ""
    }

    $sortedCards = @($cards | Sort-Object `
        @{ Expression = { Get-FamilyOrder (Get-CardFamily $_) } }, `
        @{ Expression = { Get-RarityOrder ([string]$_.Rarity) } }, `
        @{ Expression = { [string]$_.ChineseName } })

    $indexRows = New-Object System.Collections.Generic.List[object]
    $indexRows.Add([object[]]@("打开", "系列", "稀有度", "类型", "费用", "中文名", "类名", "卡牌ID", "关键词", "中文描述")) | Out-Null
    foreach ($card in $sortedCards) {
        $indexRows.Add([object[]]@(
            "打开",
            (Get-CardFamily $card),
            [string]$card.Rarity,
            [string]$card.Type,
            [string]$card.Cost,
            [string]$card.ChineseName,
            [string]$card.ClassName,
            [string]$card.CardId,
            [string]$card.Keywords,
            [string]$card.DescriptionZh
        )) | Out-Null
    }
    [void](Write-Matrix -Sheet $sheetIndex -Row 1 -Column 1 -Rows $indexRows.ToArray())
    $indexRange = $sheetIndex.Range($sheetIndex.Cells.Item(1, 1), $sheetIndex.Cells.Item($indexRows.Count, 10))
    Set-HeaderStyle $sheetIndex.Range("A1:J1")
    $sheetIndex.Range($sheetIndex.Cells.Item(1, 1), $sheetIndex.Cells.Item($indexRows.Count, 10)).Borders.Color = 0xD9D9D9
    $indexRange.WrapText = $true
    $sheetIndex.Columns.Item(1).ColumnWidth = 10
    $sheetIndex.Columns.Item(2).ColumnWidth = 12
    $sheetIndex.Columns.Item(3).ColumnWidth = 12
    $sheetIndex.Columns.Item(4).ColumnWidth = 12
    $sheetIndex.Columns.Item(5).ColumnWidth = 10
    $sheetIndex.Columns.Item(6).ColumnWidth = 24
    $sheetIndex.Columns.Item(7).ColumnWidth = 28
    $sheetIndex.Columns.Item(8).ColumnWidth = 28
    $sheetIndex.Columns.Item(9).ColumnWidth = 18
    $sheetIndex.Columns.Item(10).ColumnWidth = 72
    try {
        $sheetIndex.Range($sheetIndex.Cells.Item(1, 1), $sheetIndex.Cells.Item($indexRows.Count, 10)).AutoFilter() | Out-Null
    } catch {
        Write-Warning "卡牌索引筛选按钮添加失败，但索引内容已生成。原因：$($_.Exception.Message)"
    }
    $sheetIndex.Activate() | Out-Null
    $excel.ActiveWindow.SplitRow = 1
    $excel.ActiveWindow.FreezePanes = $true
    $sheetIndex.Range("A1").Select() | Out-Null

    $cardRows = New-Object System.Collections.Generic.List[object]
    $headerRow = New-Object System.Collections.Generic.List[object]
    $headerRow.Add("属性键") | Out-Null
    $headerRow.Add("中文属性") | Out-Null
    $headerRow.Add("说明") | Out-Null
    foreach ($card in $cards) {
        $name = [string]$card.ChineseName
        if ($name.Trim() -eq "") {
            $name = [string]$card.ClassName
        }
        $headerRow.Add($name) | Out-Null
    }
    $cardRows.Add([object[]]$headerRow.ToArray()) | Out-Null

    $quickRows = @(
        @{ Key = "_Family"; Label = "系列"; Description = "快速定位辅助行；不进入运行时数据。" },
        @{ Key = "_Rarity"; Label = "稀有度"; Description = "快速定位辅助行；不进入运行时数据。" },
        @{ Key = "_Type"; Label = "类型"; Description = "快速定位辅助行；不进入运行时数据。" },
        @{ Key = "_Cost"; Label = "费用"; Description = "快速定位辅助行；不进入运行时数据。" }
    )
    foreach ($quickRow in $quickRows) {
        $row = New-Object System.Collections.Generic.List[object]
        $row.Add($quickRow.Key) | Out-Null
        $row.Add($quickRow.Label) | Out-Null
        $row.Add($quickRow.Description) | Out-Null
        foreach ($card in $cards) {
            $value = switch ($quickRow.Key) {
                "_Family" { Get-CardFamily $card }
                "_Rarity" { [string]$card.Rarity }
                "_Type" { [string]$card.Type }
                "_Cost" { [string]$card.Cost }
                default { "" }
            }
            $row.Add($value) | Out-Null
        }
        $cardRows.Add([object[]]$row.ToArray()) | Out-Null
    }

    foreach ($field in $headers) {
        $row = New-Object System.Collections.Generic.List[object]
        $row.Add($field) | Out-Null
        $row.Add((Get-FieldLabel $field)) | Out-Null
        $row.Add((Get-FieldDescription $field)) | Out-Null
        foreach ($card in $cards) {
            $row.Add([string]$card.$field) | Out-Null
        }
        $cardRows.Add([object[]]$row.ToArray()) | Out-Null
    }

    $lastCardColumn = 3 + $cards.Count
    $lastCardRow = 1 + $quickRows.Count + $headers.Count
    $cardRange = Write-Matrix -Sheet $sheetCards -Row 1 -Column 1 -Rows $cardRows.ToArray()
    Set-HeaderStyle $sheetCards.Range($sheetCards.Cells.Item(1, 1), $sheetCards.Cells.Item(1, $lastCardColumn))
    $sheetCards.Range($sheetCards.Cells.Item(2, 1), $sheetCards.Cells.Item($lastCardRow, 3)).Interior.Color = 0xEDE7DD
    $sheetCards.Range($sheetCards.Cells.Item(2, 1), $sheetCards.Cells.Item($lastCardRow, 2)).Font.Bold = $true
    for ($r = 2; $r -le $lastCardRow; $r++) {
        $field = [string]$sheetCards.Cells.Item($r, 1).Value2
        $rowRange = $sheetCards.Range($sheetCards.Cells.Item($r, 4), $sheetCards.Cells.Item($r, $lastCardColumn))
        if ($field.StartsWith("_")) {
            $rowRange.Interior.Color = 0xD9EAD3
            $sheetCards.Range($sheetCards.Cells.Item($r, 1), $sheetCards.Cells.Item($r, 3)).Interior.Color = 0xD9EAD3
        } elseif ($editableFields -contains $field) {
            $rowRange.Interior.Color = 0xCCFFFF
        } else {
            $rowRange.Interior.Color = 0xF5F5F5
        }
    }
    $sheetCards.Range($sheetCards.Cells.Item(1, 1), $sheetCards.Cells.Item($lastCardRow, $lastCardColumn)).Borders.Color = 0xD9D9D9
    $sheetCards.Activate() | Out-Null
    $excel.ActiveWindow.SplitRow = 1
    $excel.ActiveWindow.SplitColumn = 3
    $excel.ActiveWindow.FreezePanes = $true
    $sheetCards.Range("A1").Select() | Out-Null
    $sheetCards.Rows.Item(1).RowHeight = 42
    $sheetCards.Range($sheetCards.Cells.Item(1, 1), $sheetCards.Cells.Item($lastCardRow, $lastCardColumn)).WrapText = $true
    AutoFit-Capped $sheetCards
    $sheetCards.Columns.Item(1).ColumnWidth = 28
    $sheetCards.Columns.Item(2).ColumnWidth = 20
    $sheetCards.Columns.Item(3).ColumnWidth = 46
    for ($c = 4; $c -le $lastCardColumn; $c++) {
        $sheetCards.Columns.Item($c).ColumnWidth = 18
    }

    for ($i = 0; $i -lt $cards.Count; $i++) {
        $excelColumnName = Get-ExcelColumnName (4 + $i)
        $className = [string]$cards[$i].ClassName
        for ($row = 2; $row -le $indexRows.Count; $row++) {
            if ([string]$sheetIndex.Cells.Item($row, 7).Value2 -eq $className) {
                try {
                    $sheetIndex.Hyperlinks.Add(
                        $sheetIndex.Cells.Item($row, 1),
                        "",
                        "'卡牌数值'!$excelColumnName`$1",
                        "",
                        "打开"
                    ) | Out-Null
                } catch {
                    $sheetIndex.Cells.Item($row, 1).Value2 = "打开"
                }
                break
            }
        }
    }

    $fieldRows = New-Object System.Collections.Generic.List[object]
    $fieldRows.Add([object[]]@("属性键", "中文属性", "说明", "是否建议策划直接改")) | Out-Null
    foreach ($field in $headers) {
        $editable = if ($editableFields -contains $field) {
            "是"
        } elseif ($field -like "Const*" -or $field -in @("Rarity", "Type", "Target", "IsToken", "IsMusashi", "HasSilence")) {
            "谨慎"
        } else {
            "否"
        }
        $fieldRows.Add([object[]]@($field, (Get-FieldLabel $field), (Get-FieldDescription $field), $editable)) | Out-Null
    }
    $fieldRange = Write-Matrix -Sheet $sheetFields -Row 1 -Column 1 -Rows $fieldRows.ToArray()
    Set-HeaderStyle $sheetFields.Range("A1:D1")
    # Plain formatted guide range; no table object required.
    AutoFit-Capped $sheetFields
    $sheetFields.Columns.Item(3).ColumnWidth = 85
    $sheetFields.Columns.Item(3).WrapText = $true

    if ($mechanics.Count -gt 0) {
        $mechHeaders = $mechanics[0].PSObject.Properties.Name
        $mechRows = New-Object System.Collections.Generic.List[object]
        $mechRows.Add([object[]]$mechHeaders)
        foreach ($mechanic in $mechanics) {
            $mechRows.Add([object[]]($mechHeaders | ForEach-Object { [string]$mechanic.$_ }))
        }
        $mechRange = Write-Matrix -Sheet $sheetMechanics -Row 1 -Column 1 -Rows $mechRows.ToArray()
        Set-HeaderStyle $sheetMechanics.Range($sheetMechanics.Cells.Item(1, 1), $sheetMechanics.Cells.Item(1, $mechHeaders.Count))
        # Plain formatted guide range; no table object required.
        $sheetMechanics.Range("A1:E$($mechanics.Count + 1)").WrapText = $true
        AutoFit-Capped $sheetMechanics
        $sheetMechanics.Columns.Item(4).ColumnWidth = 70
        $sheetMechanics.Columns.Item(5).ColumnWidth = 45
    }

    $sheetIntro.Activate() | Out-Null
    if (Test-Path -LiteralPath $WorkbookPath) {
        Remove-Item -LiteralPath $WorkbookPath -Force
    }
    $book.SaveAs($WorkbookPath, 51)
    Write-Host "Generated workbook:"
    Write-Host "  $WorkbookPath"
}
finally {
    if ($book) { $book.Close($false) | Out-Null }
    if ($excel) { $excel.Quit() | Out-Null }
    if ($book) { [System.Runtime.InteropServices.Marshal]::ReleaseComObject($book) | Out-Null }
    if ($excel) { [System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null }
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}











