# NinjaMod 卡牌数值表（自动生成）

> 来源：alance/cards.csv。若策划修改 alance/NinjaMod_Balance.xlsx，请先运行 powershell -ExecutionPolicy Bypass -File scripts\generate-balance.ps1。

| 卡牌 | 类名 | 稀有度 | 费用 | 类型 | 关键数值 | 说明 |
|---|---|---|---|---|---|---|
| 火忍：灰烬 | Ashes | Uncommon | 1(0) | Skill |  | 引爆所有敌人身上的【燃烧】（造成燃烧 2 倍无法格挡伤害并移除）；若成功引爆，抽 1 张牌。 |
| 火忍：豪炎 | BlazeInferno | Uncommon | 0 | Skill | BaseBurning=7<br>UpgradeBurning=9 | 对所有敌人施加 7(9) 层【燃烧】。 |
| 骨法 | BoneArt | Uncommon | 1 | Skill | BaseBlock=11<br>ConstVigor=2 | 获得 11 点格挡，并获得 2 点【活力】。 |
| 守鹤之盾 | CraneShield | Uncommon | 2(1) | Skill |  | 获得（当前已损失生命值）点格挡。动态格挡。 |
| 火忍：凤仙花爪红 | CrimsonClaw | Uncommon | 1 | Skill | ConstCount=2 | 在手牌中生成 2 张注入手里剑（燃烧追加 6、保留、消耗）。 |
| 火忍：火魔爆 | DemonFlameBurst | Uncommon | 2 | Skill | BaseDamage=12<br>UpgradeDamage=16 | 造成 12(16) 点伤害，然后引爆目标身上的所有【燃烧】。 |
| 土忍：裂地 | EarthRend | Uncommon | 1 | Skill |  | 获得等同于所有敌人负面效果（Debuff）层数之和的格挡。动态格挡。 |
| 土忍：土护符 | EarthTalisman | Uncommon | 1(0) | Skill |  | 获得（当前消耗牌堆中牌数）点格挡。动态格挡。 |
| 火忍：火焰弹幕 | FlameBarrage | Uncommon | 1 | Skill | BaseDamage=2<br>UpgradeDamage=3<br>BaseRepeat=3<br>BaseBurning=3<br>UpgradeBurning=4 | 造成 2(3) 点伤害 ×3 次，然后施加 3(4) 层【燃烧】。 |
| 火忍：火盾 | FlameShield | Uncommon | 1(0) | Power | BaseFlameShield=1 | 获得 1 层【火盾】：每当你受到攻击时，对攻击者施加等同于火盾层数的【燃烧】。 |
| 九字护身法 | KujiProtection | Uncommon | 2(1) | Power | BaseKujiProtection=1<br>ConstResist=3 | 获得 3 层【抵挡】；每回合开始额外获得当前抵挡层数 2 倍的格挡。 |
| 锁镰 | KusariGama | Uncommon | 1 | Attack | BaseDamage=9<br>UpgradeDamage=12<br>BaseExtraDamage=4<br>UpgradeExtraDamage=6 | 造成 9(12) 点伤害；若目标有【流血】，额外造成 4(6) 点伤害。 |
| 细雪 | LightSnow | Uncommon | 1 | Attack | BaseDamage=1<br>BaseHeal=2<br>UpgradeHeal=3<br>BaseRepeat=4 | 造成 1 点伤害 ×4 段，回复 2(3) 点生命。 |
| 火忍：离火符 | Lihuo | Uncommon | 0 | Skill | BaseBurning=5<br>UpgradeBurning=8 | 给予目标 5(8) 层【燃烧】。 |
| 武藏：前进喷泉 | MusashiAdvancingFountain | Uncommon | 1 | Skill | BaseHeal=4<br>BaseEnergy=1<br>UpgradeEnergy=2 | 回复 4 点生命，下个回合额外获得 1(2) 点能量。【武藏】牌。 |
| 武藏：圆明流 | MusashiEnmeiStyle | Uncommon | 1(0) | Power | BaseEnmei=1 | 获得 1 层【圆明】：每打出一张「武藏」牌，回复等同于圆明层数的生命。【武藏】牌。 |
| 武藏：神速 | MusashiGodspeed | Uncommon | 0 | Skill | BaseDamage=3<br>UpgradeDamage=5<br>BaseBlock=8<br>UpgradeBlock=11 | 造成 3(5) 点伤害，获得 8(11) 点格挡，抽 1 张牌。【武藏】牌。 |
| 武藏：迅光三角剑 | MusashiSwiftTriangle | Uncommon | 1 | Skill | BaseDamage=11<br>UpgradeDamage=15<br>BaseDex=3<br>UpgradeDex=4 | 造成 11(15) 点伤害，获得 3(4) 点敏捷。【武藏】牌。 |
| 土忍：石化术 | Petrification | Uncommon | 2(1) | Skill | BaseBlock=13 | 获得 13 点格挡，并清除自身所有负面效果。 |
| 多重罗生门 | Rashomon | Uncommon | 2 | Skill | BaseCards=3<br>UpgradeCards=4<br>ConstBlockPerAttack=9 | 抽 3(4) 张牌；抽到的牌中每有一张攻击牌，获得 9 点格挡。 |
| 起承拳 | RisingFist | Uncommon | 1 | Attack | BaseDamage=6<br>UpgradeDamage=9 | 造成 6(9) 点伤害；若手牌中有飞刀，自动免费打出一张飞刀。 |
| 追魂 | SoulChase | Uncommon | 3 | Skill | BaseSoulChaseBleed=1<br>BaseCards=2<br>UpgradeCards=3<br>ConstKunaiDamage=5 | 对目标打出消耗牌堆中的所有【飞刀】（每张造成其伤害+流血）；若成功击杀，抽 2(3) 张牌。 |
| 索命 | SoulReap | Uncommon | 2 | Skill | BaseRemove=8<br>UpgradeRemove=12<br>BaseSoulReapRewardCards=1<br>BaseSoulReapRewardEnergy=1 | 移除目标身上最多 8(12) 层【流血】，回复等同于移除层数的生命。若移除后目标没有流血，抽 1 张牌并回复 1 点能量。 |
| 八咫镜 | YataMirror | Uncommon | 1 | Power | BaseBlock=2<br>UpgradeBlock=3 | 每回合开始时，获得 2(3) 点格挡。 |
| 残影 | AfterimageAttack | Token | 0 | Attack |  | 造成（复制源攻击牌伤害减半）点伤害。动态伤害。 |
| 注入手里剑 | InfusedShuriken | Token | 1 | Attack | BaseDamage=10<br>BurningInfusion=6<br>ConstBleed=2 | 造成 10 点伤害；未被完全格挡则施加 2 层【流血】。【燃烧追加 6】。 |
| 飞刀 | Kunai | Token | 1 | Attack | BaseDamage=5<br>UpgradeDamage=7<br>BaseKunaiBleed=1 | 造成 5(7) 点伤害；未被完全格挡则施加 1 层【流血】。 |
| 回天：绝对防御 | AbsoluteDefense | Rare | 2 | Skill | BaseBlock=20<br>UpgradeBlock=30<br>BaseHeal=4<br>UpgradeHeal=6 | 获得 20(30) 点格挡，回复 4(6) 点生命。 |
| 残影术 | AfterimageArt | Rare | 2(1) | Power | BaseAfterimage=1 | 获得 1 层【残影】：每打出一张攻击牌，在弃牌堆中生成【残影层数】张 0 费的残影牌（伤害减半）。 |
| 锄刃 | BladeEdge | Rare | 2(1) | Power | ConstCostReduction=1 | 本场战斗中，所有牌堆及后续生成的手里剑、飞刀与火焰手里剑能量消耗降低 1。 |
| 火忍：燃心 | BurningHeart | Rare | X | Skill | ConstBurningPerCard=3 | 进入抽牌堆界面，选择消耗最多 X 张牌（K 张），对所有敌人施加 K × 3 层【燃烧】。 |
| 忍者八法 | EightTechniques | Rare | 1 | Skill | BaseEightTechniquesAmount=1 | 获得 1 点力量、1 层抵挡、1 点活力、1 点能量、1 点格挡、1 张飞刀、1 点最大生命，并回复 1 点生命。 |
| 武藏：承袭 | MusashiInheritance | Rare | 3(2) | Skill |  | 将各一张【神速】【空明斩】【刺】加入手牌。下回合开始获得 3 点能量。【武藏】牌。 |
| 武藏：七星光芒斩 | MusashiSevenStar | Rare | 2 | Skill | BaseDamage=7<br>BaseExecutePerFive=0<br>UpgradeExecutePerFive=1<br>BaseRepeat=7<br>ConstHpPerExecute=5 | 造成 7 点伤害 ×7 段。升级后追加第 8 段斩杀：目标每损失 5 点生命，额外造成 1 点伤害。【武藏】牌。 |
| 武藏：二天一流 | MusashiTwoHeavens | Rare | 3(2) | Skill | BaseDamage=16<br>BaseRepeat=2 | 造成 16 点伤害 ×2 段。若目标同时拥有流血与燃烧，则使其眩晕。【武藏】牌。 |
| 武藏：空明斩 | MusashiVoidSlash | Rare | 0 | Attack | BaseDamage=15<br>UpgradeDamage=21<br>BaseResist=1<br>UpgradeResist=2 | 造成 15(21) 点伤害，获得 1(2) 层【抵挡】。【武藏】牌。 |
| 切腹 | Seppuku | Rare | X | Skill | BaseSeppukuHpMultiplier=2 | 失去 2X 点生命，获得 X 点能量、抽 X 张牌、获得 X 点力量（X = 打出时剩余能量）。 |
| 影分身 | ShadowClone | Rare | 3(2) | Skill | BaseShadowClone=2<br>ConstDamageReductionPct=40 | 本回合及下回合：① 每张非影分身卡额外结算一次；② 受到的攻击伤害减少 40%；③ 荆棘反击。 |
| 影心刺 | ShadowPierce | Rare | 0 | Attack | BaseDamage=9<br>UpgradeDamage=14<br>BaseBleed=5<br>UpgradeBleed=6 | 造成 9(14) 点伤害，附加 5(6) 层【流血】。【静默】。 |
| 隐身法 | StealthArt | Rare | 2(1) | Power | ConstStealth=3<br>ConstVigor=1 | 获得 1 点【活力】，并获得 3 层【隐身】。 |
| 须佐能乎 | Susanoo | Rare | 3 | Attack | BaseDamage=7<br>UpgradeDamage=9<br>BaseSusanooBleedPerHit=1<br>BaseRepeat=6 | 造成 7(9) 点伤害 ×6 段；每段追加 1 层【流血】。 |
| 火忍：起爆符 | Detonation | Common | 0 | Skill |  | 点燃目标的【燃烧】（造成燃烧 2 倍无法格挡伤害并移除）。 |
| 土忍：土遁 | EarthEscape | Common | 0 | Power | BaseResist=1<br>UpgradeResist=2 | 获得 1(2) 层【抵挡】。 |
| 土忍：土墙 | EarthWall | Common | 1 | Skill | BaseBlock=7<br>UpgradeBlock=10<br>BaseDebuffImmunity=2 | 获得 7(10) 点格挡，并获得【免疫负面】2 个回合。 |
| 火忍：锻火刺 | ForgeFlameThrust | Common | 1 | Attack | BaseDamage=6<br>UpgradeDamage=9<br>BaseBurning=4<br>UpgradeBurning=5 | 造成 6(9) 点伤害，附加 4(5) 层【燃烧】。 |
| 居合 | IaiStrike | Common | 2 | Attack | BaseDamage=10<br>UpgradeDamage=15<br>BaseExtraDamage=5<br>UpgradeExtraDamage=8<br>ConstBleed=3 | 造成 10(15) 点伤害；若打出后仍有能量，额外造成 5(8) 点伤害并附加 3 层【流血】。 |
| 武士刀法 | KatanaArt | Common | 1 | Attack | BaseDamage=5<br>BaseRepeat=2<br>UpgradeRepeat=3 | 对所有敌人造成 5 点伤害，共 2(3) 次。 |
| 气合 | KiBreath | Common | 1 | Skill | BaseHeal=4<br>UpgradeHeal=6 | 回复 4(6) 点生命。 |
| 苦无 | KunaiThrow | Common | 1 | Attack | BaseDamage=6<br>UpgradeDamage=9 | 造成 6(9) 点伤害；若目标有【流血】，恢复 1 点能量。 |
| 武藏：刺 | MusashiThrust | Common | 0 | Attack | BaseDamage=9<br>ConstBleed=2 | 造成 9 点伤害，附加 2 层【流血】。【武藏】牌。 |
| 潜行 | Prowl | Common | 0 | Skill | BaseBlock=4<br>BaseProwl=1 | 获得 4 点格挡；下个回合开始时获得一张「暗杀」。 |
| 火忍：淬火术 | Quenching | Common | 1 | Skill | BaseQuench=3<br>UpgradeQuench=4 | 获得 3(4) 层【淬火】：本回合攻击牌每次造成伤害额外附加等同于淬火层数的【燃烧】。 |
| 土忍：碎石 | RockShatter | Common | 1 | Skill | BaseBlock=8<br>UpgradeBlock=13<br>BaseRockShatterResistLoss=1 | 获得 8(13) 点格挡；自动免费打出手牌中所有「忍者防御」，随后移除 1 层【抵挡】。 |
| 土忍：聚石刺 | StoneGatherThrust | Common | 1 | Attack | BaseDamage=6<br>UpgradeDamage=9<br>BaseBlock=6<br>UpgradeBlock=9 | 造成 6(9) 点伤害，获得 6(9) 点格挡。 |
| 土忍：唤石 | StoneSummon | Common | 1 | Skill | BaseStoneSummonMultiplier=4<br>UpgradeStoneSummonMultiplier=5 | 获得（当前抵挡层数 × 4(5)）点格挡。动态格挡。 |
| 燕返 | SwallowReturn | Common | 1 | Attack | BaseDamage=4<br>UpgradeDamage=7<br>BaseSwallowReturnEnergy=1 | 造成 4(7) 点伤害；若伤害被完全格挡，获得 1 点能量。 |
| 暗杀 | Assassination | Basic | 1 | Attack | BaseDamage=7<br>UpgradeDamage=10 | 无视格挡，造成 7(10) 点伤害。【静默】。 |
| 忍者防御 | NinjaDefend | Basic | 1 | Skill | BaseBlock=5<br>UpgradeBlock=8 | 获得 5(8) 点格挡。 |
| 忍者打击 | NinjaStrike | Basic | 1 | Attack | BaseDamage=6<br>UpgradeDamage=9 | 造成 6(9) 点伤害。 |
| 手里剑 | Shuriken | Basic | 2 | Attack | BaseDamage=10<br>UpgradeDamage=13<br>BaseBleed=2<br>UpgradeBleed=3 | 造成 10(13) 点伤害；未被完全格挡则施加 2(3) 层【流血】。 |
