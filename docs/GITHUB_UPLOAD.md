# 发布这份精选代码

本目录已经按一个独立 GitHub 仓库组织。首页为 `README.md`，五款游戏的说明在 `projects/`，阅读入口和代码链接使用相对路径。

## 在 GitHub 网页上传

1. 新建名为 `game-portfolio` 的公开仓库。
2. 使用 `Add file → Upload files`，上传本目录中的文件与子目录，保留原目录层级。仓库根目录应直接看到 `README.md`，不要额外包一层同名目录。
3. 首页检查五个游戏入口、源码链接和中文显示。
4. 在个人 GitHub 主页将该仓库置顶。

## 更新已有 GitHub 仓库

本次截图更新已准备为增量包 `Game-GitHub-Update.zip`。解压后，里面直接包含 `README.md`、`projects/` 和 `docs/`。

1. 打开现有仓库首页 `https://github.com/wser9527-droid/Game-Project`，选择 `Add file → Upload files`。
2. 打开解压后的更新文件夹，选中里面的 `README.md`、`projects`、`docs` 三项，一起拖入网页。不要把 ZIP 本身或外层文件夹拖入。
3. 保留目录大小写与结构。相同路径的 README 会更新，新图片会添加；原仓库里未列入本次上传的代码文件会保留。
4. 提交说明填写 `Add game screenshots and portfolio galleries`，点击 `Commit changes`（若页面先出现确认弹窗，继续在弹窗中提交）。
5. 返回首页确认速览图片，再进入四个有图片的项目，检查图片及源码链接。

本次整理不包括 Dream Repair 截图；其他待补材料见截图清单。上述步骤更新已有仓库，无需新建仓库或启用 GitHub Pages。

## 本地 Git 上传

也可以在本目录初始化 Git，通过 GitHub Desktop 或命令行提交、推送到自己的仓库。初次推送前检查实际暂存文件；这份交付没有初始化 Git、创建远端或进行推送。

## 当前可以展示的内容

- 19 份 C# 样例及其依赖、提取范围和实现说明。
- Dream Repair、Sushi 的 PlayMode 测试，以及 Enchanted Wetlands 的 EditMode 测试节选；注明本次没有执行。
- Enchanted Wetlands 的玩法拆解与一分钟面试讲解。
- 源文件行号与 SHA-256，方便核对样例出处。

完整商业工程、场景、美术、签名和账号配置均未包含。本次没有添加开源许可证，也没有代替你授权第三方素材或完整游戏工程的再分发。

这份代码集可以直接用于技术阅读；实机视频、商店入口和个人职责介绍可作为作品展示内容继续补充。上传到 GitHub 后，在未登录浏览器中检查一次可访问性和链接。

[返回作品集](../README.md)
