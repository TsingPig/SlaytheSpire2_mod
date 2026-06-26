# NinjaMod 数值策划工作流

日常改数入口：

1. 打开 `balance/NinjaMod_Balance.xlsx`。
2. 先在【卡牌索引】页按系列、稀有度、类型、费用筛选，点击【打开】跳到对应卡牌列。
3. 在【卡牌数值】页修改黄色格。
   - 现在的布局是：卡牌作为列，属性作为行。
   - 顶部绿色行是快速定位辅助行，不会进入运行时数据。
   - A 列【属性键】是程序识别用字段，不要随意修改。
   - `ClassName` 行是脚本定位卡牌用的类名，不要随意修改。
4. 保存并关闭 Excel。
5. 在仓库根目录运行：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\generate-balance.ps1
```

如需进游戏测试，再运行：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-and-install.ps1 -Configuration Release
```

路径规则：

- 所有脚本都通过自身位置反推仓库根目录，不依赖本机绝对路径。
- `cards.csv` 是版本控制友好的文本源；`NinjaMod_Balance.xlsx` 是给策划编辑的入口。
- `scripts\refresh-balance-workbook.ps1` 只用于从 CSV 重建 Excel 模板，会覆盖当前工作簿；日常改数不要运行它。

生成产物：

- `NinjaMod/balance/cards.json`
- `NinjaModCode/Generated/CardBalance.g.cs`
- `balance/generated/card-balance.generated.md`

运行时接入范围：

- 已接入：费用、升级费用、类型、稀有度、目标、常见数值字段、特殊机制数值字段、常量字段。
- `DescriptionZh` / `DescriptionEn` 当前用于策划说明与导出文档；游戏内卡牌描述仍由 `.cs` 里的模板和 `{Damage:diff()}` 等动态变量占位生成，以保证升级和动态数值能正确显示。


