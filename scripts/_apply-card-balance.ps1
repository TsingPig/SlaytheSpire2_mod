$ErrorActionPreference = "Stop"
$csvPath = Join-Path $PSScriptRoot "..\balance\cards.csv"
$csvPath = (Resolve-Path $csvPath).Path
$cards = Import-Csv -LiteralPath $csvPath -Encoding UTF8

# 新增列（运行时自定义数值），为所有现有行补空值。
$newColumns = @("BaseVigor","UpgradeVigor","BaseSelect","UpgradeSelect","BaseStealth","UpgradeStealth")
foreach ($row in $cards) {
    foreach ($col in $newColumns) {
        if (-not ($row.PSObject.Properties.Name -contains $col)) {
            $row | Add-Member -NotePropertyName $col -NotePropertyValue "" -Force
        }
    }
}

$headerOrder = $cards[0].PSObject.Properties.Name

function Set-Row {
    param($Row, [hashtable]$Values)
    foreach ($k in $Values.Keys) {
        if (-not ($Row.PSObject.Properties.Name -contains $k)) {
            $Row | Add-Member -NotePropertyName $k -NotePropertyValue "" -Force
        }
        $Row.$k = [string]$Values[$k]
    }
}

function New-Row {
    param([hashtable]$Values)
    $obj = [ordered]@{}
    foreach ($col in $headerOrder) { $obj[$col] = "" }
    $row = [pscustomobject]$obj
    Set-Row -Row $row -Values $Values
    return $row
}

# ---- 修改现有卡牌 ----
$byId = @{}
foreach ($row in $cards) { $byId[$row.CardId] = $row }

Set-Row $byId["KusariGama"] @{
    BaseDamage = "8"; UpgradeDamage = "11"
    BaseExtraDamage = ""; UpgradeExtraDamage = ""
    ExtraVarsJson = ""; ConstantsJson = '{"Weak": 2}'
    DescriptionZh = "造成 8(11) 点伤害；若目标拥有【流血】，额外给予 2 层【虚弱】。"
    DescriptionEn = "Deal {Damage:diff()} damage. If the target has [gold]Bleed[/gold], apply 2 [gold]Weak[/gold]."
    MechanicNotes = "造成 8(11) 点伤害；若目标拥有【流血】，额外给予 2 层【虚弱】。"
}

Set-Row $byId["SoulChase"] @{
    Cost = "2"; Keywords = "消耗（升级追加保留）"
    BaseCards = ""; UpgradeCards = ""
    DescriptionZh = "对目标打出消耗牌堆中的所有【飞刀】（含残影飞刀），每张造成其伤害并附加流血；升级后获得保留。"
    DescriptionEn = "Play all [gold]Kunai[/gold] from your exhaust pile at the target (each deals its damage). Upgrade grants Retain."
    MechanicNotes = "对目标打出消耗牌堆中的所有【飞刀】（含残影飞刀），每张造成其伤害并附加 1 层流血；升级后获得保留。"
}

Set-Row $byId["SwallowReturn"] @{
    Cost = "0"; BaseDamage = "4"; UpgradeDamage = "7"
    BaseCards = "1"; UpgradeCards = "2"; BaseSwallowReturnEnergy = ""
    DescriptionZh = "造成 4(7) 点伤害；若目标拥有【流血】，抽 1(2) 张牌。"
    DescriptionEn = "Deal {Damage:diff()} damage. If the target has [gold]Bleed[/gold], draw {Cards:diff()} cards."
    MechanicNotes = "造成 4(7) 点伤害；若目标拥有【流血】，抽 1(2) 张牌。"
}

Set-Row $byId["Susanoo"] @{
    DescriptionZh = "造成 7(9) 点伤害，共 6 段；每段伤害后立即追加 1 层【流血】（逐段结算，后续段吃到累积流血）。"
    MechanicNotes = "造成 7(9) 点伤害，共 6 段；每打出一段立即追加 1 层流血，后续段享受累积流血伤害。"
}

# ---- 新增卡牌 ----
$newCards = @(
    @{ CardId="NinjaToolPrep"; ClassName="NinjaToolPrep"; ChineseName="忍具整理"; EnglishName="Ninja Tool Prep"; Rarity="Common"; Type="Skill"; Target="Self"; Cost="0"; BaseVigor="2"; UpgradeVigor="4"; DescriptionZh="抽 1 张牌；若抽到攻击牌，获得 2(4) 点【活力】。"; DescriptionEn="Draw 1 card. If it is an Attack, gain {Vigor:diff()} [gold]Vigor[/gold]."; CodeFile="NinjaModCode/Cards/NinjaToolPrep.cs" }
    @{ CardId="HandSwap"; ClassName="HandSwap"; ChineseName="换手"; EnglishName="Hand Swap"; Rarity="Common"; Type="Skill"; Target="Self"; Cost="1"; BaseBlock="4"; UpgradeBlock="7"; ConstantsJson='{"MaxCards": 2}'; DescriptionZh="从手牌中选择最多 2 张牌放回抽牌堆顶部，获得 4(7) 点格挡。"; DescriptionEn="Put up to 2 cards from your hand on top of your draw pile. Gain {Block:diff()} Block."; CodeFile="NinjaModCode/Cards/HandSwap.cs" }
    @{ CardId="FireSpread"; ClassName="FireSpread"; ChineseName="火忍：火势蔓延"; EnglishName="Fire Ninjutsu: Fire Spread"; Rarity="Common"; Type="Skill"; Target="AnyEnemy"; Cost="1"; UpgradeCost="0"; DescriptionZh="将目标身上的【燃烧】扩散给所有其他敌人。"; DescriptionEn="Spread the target's [gold]Burning[/gold] to all other enemies."; CodeFile="NinjaModCode/Cards/FireSpread.cs" }

    @{ CardId="EmberRecovery"; ClassName="EmberRecovery"; ChineseName="火忍：余烬回收"; EnglishName="Fire Ninjutsu: Ember Recovery"; Rarity="Uncommon"; Type="Skill"; Target="AnyEnemy"; Cost="1"; UpgradeCost="0"; BaseEnergy="1"; DescriptionZh="点燃目标身上的【燃烧】；若成功点燃，获得 1 点能量并抽 1 张牌。"; DescriptionEn="Ignite the target's [gold]Burning[/gold]. If it ignited, gain 1 Energy and draw 1 card."; CodeFile="NinjaModCode/Cards/EmberRecovery.cs" }
    @{ CardId="FanWind"; ClassName="FanWind"; ChineseName="火忍：扇风"; EnglishName="Fire Ninjutsu: Fan Wind"; Rarity="Uncommon"; Type="Skill"; Target="AnyEnemy"; Cost="2"; UpgradeCost="1"; DescriptionZh="将目标的【燃烧】层数翻倍。"; DescriptionEn="Double the target's [gold]Burning[/gold]."; CodeFile="NinjaModCode/Cards/FanWind.cs" }
    @{ CardId="MusashiDreamStrike"; ClassName="MusashiDreamStrike"; ChineseName="武藏：神梦一击"; EnglishName="Musashi: Dream Strike"; Rarity="Uncommon"; Type="Attack"; Target="AnyEnemy"; Cost="4"; IsMusashi="True"; Keywords="消耗"; BaseDamage="46"; UpgradeDamage="62"; DescriptionZh="造成 46(62) 点伤害。"; DescriptionEn="Deal {Damage:diff()} damage."; CodeFile="NinjaModCode/Cards/MusashiDreamStrike.cs" }
    @{ CardId="MusashiCrimson"; ClassName="MusashiCrimson"; ChineseName="武藏：猩红"; EnglishName="Musashi: Crimson"; Rarity="Uncommon"; Type="Skill"; Target="Self"; Cost="0"; IsMusashi="True"; Keywords="消耗"; BaseQuench="2"; DescriptionZh="获得 2 层【淬火】。"; DescriptionEn="Gain {Quench:diff()} [gold]Quenching[/gold]."; CodeFile="NinjaModCode/Cards/MusashiCrimson.cs" }
    @{ CardId="BladeFlow"; ClassName="BladeFlow"; ChineseName="刀意流转"; EnglishName="Blade Flow"; Rarity="Uncommon"; Type="Skill"; Target="AnyEnemy"; Cost="2"; UpgradeCost="1"; DescriptionZh="对目标打出手牌中的所有【飞刀】与【手里剑】（含残影复制牌）。"; DescriptionEn="Play all [gold]Kunai[/gold] and [gold]Shuriken[/gold] from your hand at the target."; CodeFile="NinjaModCode/Cards/BladeFlow.cs" }
    @{ CardId="Wildfire"; ClassName="Wildfire"; ChineseName="火忍：燎原"; EnglishName="Fire Ninjutsu: Wildfire"; Rarity="Uncommon"; Type="Power"; Target="Self"; Cost="1"; BaseBurning="2"; UpgradeBurning="3"; DescriptionZh="你的回合开始时，给予所有敌人 2(3) 层【燃烧】。"; DescriptionEn="At the start of your turn, apply {Burning:diff()} [gold]Burning[/gold] to ALL enemies."; CodeFile="NinjaModCode/Cards/Wildfire.cs" }

    @{ CardId="StoneHide"; ClassName="StoneHide"; ChineseName="土忍：石隐术"; EnglishName="Earth Ninjutsu: Stone Hide"; Rarity="Rare"; Type="Power"; Target="Self"; Cost="2"; BaseResist="2"; UpgradeResist="3"; ConstStealth="1"; DescriptionZh="获得 1 层【隐身】，获得 2(3) 层【抵挡】。"; DescriptionEn="Gain 1 [gold]Stealth[/gold] and {Resist:diff()} [gold]Resist[/gold]."; CodeFile="NinjaModCode/Cards/StoneHide.cs" }
    @{ CardId="MusashiPeerless"; ClassName="MusashiPeerless"; ChineseName="武藏：无双"; EnglishName="Musashi: Peerless"; Rarity="Rare"; Type="Skill"; Target="Self"; Cost="3"; UpgradeCost="2"; IsMusashi="True"; Keywords="消耗"; ConstantsJson='{"Energy": 2}'; DescriptionZh="下回合开始获得 2 点能量，将各一张【武藏：二天一流】【武藏：猩红】【武藏：刺】放入抽牌堆顶部。"; DescriptionEn="Gain 2 Energy next turn. Put a Two Heavens, a Crimson, and a Thrust on top of your draw pile."; CodeFile="NinjaModCode/Cards/MusashiPeerless.cs" }
    @{ CardId="BloodFireTransfer"; ClassName="BloodFireTransfer"; ChineseName="火忍：血火转印"; EnglishName="Fire Ninjutsu: Blood-Fire Transfer"; Rarity="Rare"; Type="Skill"; Target="AnyEnemy"; Cost="2"; Keywords="消耗（升级移除）"; DescriptionZh="给予目标等同于其当前【燃烧】层数的【流血】，并移除目标身上的【燃烧】；升级后移除消耗。"; DescriptionEn="Apply [gold]Bleed[/gold] equal to the target's [gold]Burning[/gold], then remove its [gold]Burning[/gold]. Upgrade removes Exhaust."; CodeFile="NinjaModCode/Cards/BloodFireTransfer.cs" }
    @{ CardId="ShadowBreath"; ClassName="ShadowBreath"; ChineseName="影息术"; EnglishName="Shadow Breath"; Rarity="Rare"; Type="Skill"; Target="Self"; Cost="1"; BaseCards="1"; UpgradeCards="2"; BaseEnergy="1"; DescriptionZh="若你处于【隐身】状态，抽 1(2) 张牌并获得 1 点能量。"; DescriptionEn="If you are [gold]Stealthed[/gold], draw {Cards:diff()} cards and gain 1 Energy."; CodeFile="NinjaModCode/Cards/ShadowBreath.cs" }
    @{ CardId="NimbleStep"; ClassName="NimbleStep"; ChineseName="轻盈舞步"; EnglishName="Nimble Step"; Rarity="Rare"; Type="Power"; Target="Self"; Cost="2"; UpgradeCost="1"; DescriptionZh="每当你打出一张 0 费牌，抽 1 张牌。"; DescriptionEn="Whenever you play a 0-cost card, draw 1 card."; CodeFile="NinjaModCode/Cards/NimbleStep.cs" }
    @{ CardId="CovertOps"; ClassName="CovertOps"; ChineseName="保密行动"; EnglishName="Covert Ops"; Rarity="Rare"; Type="Skill"; Target="Self"; Cost="2"; BaseStealth="1"; UpgradeStealth="2"; BaseSelect="2"; UpgradeSelect="3"; DescriptionZh="获得 1(2) 层【隐身】；从抽牌堆中选择 2(3) 张牌，给其追加【静默】。"; DescriptionEn="Gain {Stealth:diff()} [gold]Stealth[/gold]. Choose {Select:diff()} cards from your draw pile and grant them [gold]Silence[/gold]."; CodeFile="NinjaModCode/Cards/CovertOps.cs" }
    @{ CardId="FlameDance"; ClassName="FlameDance"; ChineseName="火忍：火焰之舞"; EnglishName="Fire Ninjutsu: Flame Dance"; Rarity="Rare"; Type="Power"; Target="Self"; Cost="1"; BaseEnergy="1"; UpgradeEnergy="2"; DescriptionZh="每回合第一次打出【火忍】牌时，获得 1(2) 点能量。"; DescriptionEn="The first time you play a Fire Ninjutsu card each turn, gain {Energy:diff()} Energy."; CodeFile="NinjaModCode/Cards/FlameDance.cs" }
    @{ CardId="Earthquake"; ClassName="Earthquake"; ChineseName="土忍：地震"; EnglishName="Earth Ninjutsu: Earthquake"; Rarity="Rare"; Type="Skill"; Target="AllEnemies"; Cost="5"; BaseDamage="8"; UpgradeDamage="12"; DescriptionZh="对所有敌人造成 8(12) 点伤害，并使所有敌人【眩晕】。"; DescriptionEn="Deal {Damage:diff()} damage to ALL enemies and [gold]Stun[/gold] them."; CodeFile="NinjaModCode/Cards/Earthquake.cs" }
)

$list = New-Object System.Collections.Generic.List[object]
foreach ($row in $cards) { $list.Add($row) }
foreach ($nc in $newCards) {
    if ($byId.ContainsKey($nc.CardId)) { continue }
    # 默认 MechanicNotes = DescriptionZh
    if (-not $nc.ContainsKey("MechanicNotes")) { $nc["MechanicNotes"] = $nc["DescriptionZh"] }
    $list.Add((New-Row $nc))
}

$utf8 = New-Object System.Text.UTF8Encoding($false)
$tmp = [System.IO.Path]::GetTempFileName()
$list | Export-Csv -LiteralPath $tmp -NoTypeInformation -Encoding UTF8
$content = [System.IO.File]::ReadAllText($tmp, $utf8)
[System.IO.File]::WriteAllText($csvPath, $content, $utf8)
Remove-Item $tmp -Force
Write-Output "cards.csv updated: $($list.Count) rows"
