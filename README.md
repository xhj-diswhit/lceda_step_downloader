# lceda_step_downloader

本项目基于原作者 [@seishinkouki](https://github.com/seishinkouki) 的开源项目修改而来。

原项目地址：[seishinkouki/lceda_step_downloader](https://github.com/seishinkouki/lceda_step_downloader)

感谢原作者 [@seishinkouki](https://github.com/seishinkouki) 提供的立创 EDA 3D 模型下载工具基础版本。

## 本版本更新内容

1. 增加 STEP 模型保存目录选择功能
   - 在主界面底部新增“保存到”路径显示框。
   - 支持通过“选择”按钮打开文件夹选择窗口。
   - 下载 STEP 文件时会保存到用户选择的目录。
   - 默认保存路径仍为程序目录下的 `step` 文件夹。

2. 优化 STEP 下载接口
   - 保留原有 `DownloadStep()` 按钮下载逻辑。
   - 新增可指定目录的下载方法：
     - `DownloadStepToDirectory(string saveDirectory)`
     - `DownloadStepToDirectoryAsync(string saveDirectory)`
   - 避免 Stylet 绑定时因方法重载产生 `AmbiguousMatchException`。

3. 优化本地缓存路径
   - 将 `temp` 和 `step` 路径统一为基于 `AppContext.BaseDirectory` 的程序目录路径。
   - 避免相对路径在不同启动方式下保存位置不一致。

4. 增加 Windows 图形化安装包
   - 提供可双击运行的安装程序 EXE。
   - 安装界面支持通过“浏览...”选择安装目录。
   - 安装后创建桌面快捷方式和开始菜单快捷方式。
   - 写入 Windows 当前用户卸载信息，方便后续卸载。

## 安装包

Windows 图形化安装包请在本仓库的 GitHub Releases 中下载：

`LCEDA-Step-Downloader-Setup.exe`

## 说明

本仓库为基于原项目的二次修改版本，核心模型搜索、预览和下载能力来自原项目：
[@seishinkouki/lceda_step_downloader](https://github.com/seishinkouki/lceda_step_downloader)

![img](https://github.com/seishinkouki/lceda_step_downloader/blob/master/lceda_step_downloader/doc/Snipaste_2022-03-26_19-50-10.png)
