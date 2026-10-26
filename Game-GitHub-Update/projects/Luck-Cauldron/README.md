# Luck Cauldron

一款围绕随机材料组合、炼金产物和升级成长展开的休闲游戏。项目将材料权重、配方优先级、消耗、升级和本地存档组织为服务类，并通过协程动画强化结果反馈，适合展示规则建模、进度系统和移动端界面迭代。

**项目角色：独立开发，负责全部开发工作。项目状态：已过审（开发者提供）。**


<!-- SCREENSHOT-GALLERY-START -->
## 游戏画面

以下为开发者提供的游戏画面，保留原始像素尺寸，仅作 PNG 无损压缩。点击图片可查看大图。

### 炼金主界面

<a href="images/01-core-gameplay.png"><img src="images/01-core-gameplay.png" width="280" alt="炼金主界面"></a>

展示材料棋盘、资源计数、操作按钮与主要交互区域。

### 炼金产物反馈

<a href="images/02-recipe-result.png"><img src="images/02-recipe-result.png" width="280" alt="炼金产物反馈"></a>

展示产物图像及 Philosopher Coin +385g 结果文案。

### 升级与成长界面

<a href="images/03-upgrade-or-progress.png"><img src="images/03-upgrade-or-progress.png" width="280" alt="升级与成长界面"></a>

展示升级项、等级、MAX 状态及加成说明。

<!-- SCREENSHOT-GALLERY-END -->

## 个人贡献

- 负责玩法设计、Unity/C# 实现、数值与配方配置、UI 表现、素材整合和调试交付，完成材料生成、组合判定、产物反馈和升级成长流程。
- 实现权重表调整与累计采样，通过材料分组计数和有序规则匹配判断配方；组织钱包、消耗、售卖加成、升级和存档服务的协作。
- 实现 UI 位移、缩放、透明度及产物反馈动画，使用协程和缓动组织连续表现，并显式校正动画终点。
- 迭代手机与 iPad 布局，处理安全区、转轮图标、HUD 与升级栏参数在运行时的同步；历史聊天还包含审核设备启动异常的日志排查和提交包身份核对。

原项目命名空间仍为 `LuckyCauldron`，源码保留 `SpinCostService`、`WinTier` 等原类型名。启动异常记录只用于说明排查经历，本次不据此宣称最终根因已修复或完成 iPad 真机回归，见[资料说明](../../docs/PROJECT_FACTS.md)。

| 本地快照信息 | 内容 |
| --- | --- |
| Product Name | Luck Cauldron |
| Unity | 6000.3.11f1 |
| 本地版本 | 1.0.3 |
| 主场景 | `Assets/Scenes/Game.unity` |
| 原始主文件 | `Assets/Scripts/Slot/SlotMachineController.cs` |

## 推荐阅读顺序

1. [AlchemyRules.cs](code/AlchemyRules.cs)：从主文件抽取材料/产品/反馈等级枚举、配方结果、`ProbabilityTableService` 和 `WinLineChecker`。保留类体，只补充引用和命名空间包装。
2. [ProgressionServices.cs](code/ProgressionServices.cs)：从同一主文件抽取消耗、钱包、售卖加成、升级、存档数据和存档服务，配合上一份文件阅读。
3. [JuicyEffectSystem.cs](code/JuicyEffectSystem.cs)：完整原文件。使用协程组合缩放、位移和透明度反馈，位移/缩放使用 EaseOutBack，淡入淡出使用 SmoothStep。

## 可以讲清楚的实现

- 提升稀有材料权重后，按比例缩放其他材料的权重。`Roll` 按实际权重总和采样，不要求传入权重总和恰好等于 100。
- 配方将材料数组分组计数，再依次匹配规则。优先级由判断顺序决定，原材料排列次序不影响相同计数对应的配方。
- 升级服务通过钱包扣费，更新升级等级与关联服务。`PriceFor` 以负值表示已达上限或不可购买。
- 存档服务记录历史统计与当前局内状态，加载 JSON 失败时退回新数据。恢复过程对若干等级和计数进行截断。
- 动画循环结束后明确写入终点，避免逐帧步进未刚好到达最终位置。

## 抽取范围

`AlchemyRules.cs` 和 `ProgressionServices.cs` 是本次为了阅读从较长文件中分离的类，不能据此声称原工程已经按这些文件拆分。两份文件保留原命名空间；不要与原 `SlotMachineController.cs` 同时导入同一程序集，以免类型重复。

这两份抽取代码互相补齐直接类型依赖，但本次未运行编译。`JuicyEffectSystem` 需要挂载在启用的 MonoBehaviour 上，并由调用方传入有效 UI 对象；本目录没有提供场景或动画录屏；上方截图仅展示静态游戏画面。

## 当前实现的边界

- 配方判断依赖规则顺序，增加配方时应补重叠规则测试；当前节选没有输入长度或空值保护。
- 钱包服务假设调用方传入有效金额，`Spend` 没有自行拒绝负数，不能把它当作通用安全交易接口。
- PlayerPrefs 和 JSON 是客户端存档方案；JSON 可解析也不代表所有字段都通过业务校验。`RunStateVersion` 是已有字段，不代表存在完整的版本迁移系统。
- `JuicyEffectSystem.active` 没有在协程自然结束时移除句柄；`KillAll` 会停止记录的协程，但不恢复动画中断前的状态。这是实际代码的改进点，不能宣称已具备完善的生命周期回收。

本次仅核对抽取一致性，没有运行原工程的 Startup/Tablet validator 或设备测试。

[返回作品集](../../README.md)
