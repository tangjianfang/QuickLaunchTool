# 快速启动工具 - 框架设计文档

## 一、需求分析

### 1.1 核心功能
- **文件扫描**：遍历指定文件夹，递归查找所有.exe文件
- **信息提取**：读取exe文件的名称、完整路径、图标资源
- **界面展示**：以类似Windows任务栏的方式展示应用图标
- **快速启动**：点击图标启动对应的应用程序
- **配置管理**：保存用户设置（扫描路径、界面布局等）

### 1.2 扩展功能
- 支持多个文件夹扫描
- 应用排序（按名称、使用频率、自定义）
- 右键菜单（打开文件位置、属性、删除等）
- 搜索过滤功能
- 开机自启动
- 窗口置顶、自动隐藏
- 主题切换（浅色/深色）

---

## 二、架构设计

### 2.1 分层架构

```
┌─────────────────────────────────────┐
│     表现层 (Presentation Layer)      │
│  - MainForm (主窗体)                 │
│  - SettingsForm (设置窗体)           │
│  - AppButton (自定义控件)            │
└─────────────────────────────────────┘
              ↓↑
┌─────────────────────────────────────┐
│      业务层 (Business Layer)         │
│  - FileScanner (文件扫描)            │
│  - IconExtractor (图标提取)          │
│  - ProcessLauncher (进程启动)        │
│  - ConfigManager (配置管理)          │
└─────────────────────────────────────┘
              ↓↑
┌─────────────────────────────────────┐
│      数据层 (Data Layer)             │
│  - AppInfo (应用信息模型)            │
│  - AppConfig (配置模型)              │
│  - Cache (缓存管理)                  │
└─────────────────────────────────────┘
```

### 2.2 核心模块划分

#### **模块1：数据模型层 (Models)**
- **AppInfo**：存储单个应用的信息
  - 属性：名称、路径、图标、文件大小、修改时间、使用次数
- **AppConfig**：存储全局配置
  - 属性：扫描路径列表、排序方式、界面主题、窗口位置

#### **模块2：服务层 (Services)**
- **FileScanner**：文件扫描服务
  - 职责：遍历文件夹、过滤exe文件、异常处理
  - 方法：ScanFolder(), ScanMultipleFolders()

- **IconExtractor**：图标提取服务
  - 职责：从exe文件提取图标资源
  - 方法：ExtractIcon(), GetDefaultIcon()

- **ProcessLauncher**：进程启动服务
  - 职责：启动应用程序、管理进程
  - 方法：Launch(), LaunchAsAdmin(), IsRunning()

- **ConfigManager**：配置管理服务
  - 职责：读写配置文件（JSON/XML）
  - 方法：Load(), Save(), Reset()

#### **模块3：自定义控件层 (Controls)**
- **AppButton**：应用按钮控件
  - 功能：显示图标和名称、鼠标悬停效果、右键菜单
  - 事件：Click, RightClick, DoubleClick

#### **模块4：窗体层 (Forms)**
- **MainForm**：主窗体
  - 布局：FlowLayoutPanel 或 TableLayoutPanel
  - 功能：动态加载应用按钮、搜索框、工具栏

- **SettingsForm**：设置窗体
  - 功能：选择扫描路径、配置界面选项、高级设置

#### **模块5：工具层 (Utils)**
- **Logger**：日志记录
- **CacheManager**：缓存管理（避免重复扫描）
- **ThemeManager**：主题管理

---

## 三、技术实现要点

### 3.1 关键技术
- **图标提取**：使用 `Icon.ExtractAssociatedIcon()` 或 P/Invoke 调用 Shell32.dll
- **进程启动**：使用 `Process.Start()` 配合 ProcessStartInfo
- **配置持久化**：使用 JSON 序列化（Newtonsoft.Json）
- **异步扫描**：使用 `async/await` 或 BackgroundWorker 避免界面卡顿
- **缓存机制**：使用 Dictionary 缓存已扫描的应用信息

### 3.2 界面设计
- **布局方式**：FlowLayoutPanel（自动换行）
- **按钮样式**：48x48 图标 + 文字标签
- **交互反馈**：鼠标悬停高亮、点击动画
- **窗口特性**：无边框、可拖动、置顶、半透明

### 3.3 性能优化
- **延迟加载**：首次只扫描顶层目录，按需深度扫描
- **图标缓存**：将提取的图标缓存到内存或磁盘
- **虚拟化**：应用数量过多时使用虚拟滚动
- **多线程**：文件扫描和图标提取使用独立线程

---

## 四、数据流设计

### 4.1 启动流程
```
1. 加载配置文件 (ConfigManager.Load)
2. 读取扫描路径列表
3. 异步扫描文件夹 (FileScanner.ScanFolder)
4. 提取每个exe的图标 (IconExtractor.ExtractIcon)
5. 创建AppInfo对象并缓存
6. 动态生成AppButton控件
7. 添加到MainForm的布局容器
8. 显示主窗体
```

### 4.2 点击启动流程
```
1. 用户点击AppButton
2. 触发Click事件
3. 获取对应的AppInfo对象
4. 调用ProcessLauncher.Launch(appInfo.FullPath)
5. 记录使用次数（可选）
6. 更新配置文件（可选）
```

### 4.3 配置保存流程
```
1. 用户修改设置
2. 验证输入有效性
3. 更新AppConfig对象
4. 调用ConfigManager.Save()
5. 序列化为JSON文件
6. 保存到 %AppData%/QuickLaunchTool/config.json
```

---

## 五、AI 提示词模板

### 5.1 完整项目生成提示词

```
我需要创建一个C# WinForms桌面应用程序，名为"快速启动工具"。

【核心需求】
1. 扫描指定文件夹（包括子文件夹）中的所有.exe文件
2. 提取每个exe的文件名、完整路径和图标
3. 在主窗体上以类似Windows任务栏的方式展示这些应用（图标+名称）
4. 点击图标时启动对应的应用程序
5. 支持配置管理（保存扫描路径等设置）

【技术要求】
- 框架：.NET Framework 4.7.2 或 .NET 6+ WinForms
- 架构：分层架构（Models, Services, Forms, Controls, Utils）
- 图标提取：使用Icon.ExtractAssociatedIcon或Shell32.dll
- 配置存储：使用JSON格式（Newtonsoft.Json）
- 异步处理：使用async/await避免界面卡顿

【模块设计】
1. Models层：
   - AppInfo类（存储应用信息：名称、路径、图标、修改时间）
   - AppConfig类（存储配置：扫描路径、排序方式、主题）

2. Services层：
   - FileScanner：扫描文件夹，返回List<AppInfo>
   - IconExtractor：提取exe图标，返回Icon对象
   - ProcessLauncher：启动应用程序
   - ConfigManager：读写JSON配置文件

3. Controls层：
   - AppButton：自定义按钮控件（显示图标和名称，支持点击和右键菜单）

4. Forms层：
   - MainForm：主窗体（使用FlowLayoutPanel布局，动态添加AppButton）
   - SettingsForm：设置窗体（选择扫描路径、配置选项）

【界面要求】
- 主窗体：无边框、可拖动、置顶、半透明背景
- 按钮样式：48x48图标，下方显示应用名称
- 布局：自动换行的流式布局
- 工具栏：包含"刷新"、"设置"、"最小化"按钮

【功能特性】
- 支持右键菜单（打开文件位置、以管理员身份运行、从列表移除）
- 支持搜索过滤
- 记录使用频率，支持按频率排序
- 缓存机制避免重复扫描

请提供完整的项目结构和关键代码实现。
```

### 5.2 分模块提示词

#### **提示词1：创建数据模型**
```
请为C# WinForms应用创建数据模型类：

1. AppInfo类：
   - 属性：Name(string), FullPath(string), Icon(Icon), LastModified(DateTime), UseCount(int)
   - 构造函数：接收文件路径，自动填充属性
   - 方法：Clone(), Equals()

2. AppConfig类：
   - 属性：ScanPaths(List<string>), SortMode(enum), Theme(enum), WindowPosition(Point)
   - 默认值设置
   - 验证方法

使用属性、自动属性和数据注解。
```

#### **提示词2：实现文件扫描服务**
```
创建FileScanner服务类，用于扫描文件夹中的exe文件：

要求：
- 方法：ScanFolder(string path, bool includeSubfolders)
- 返回：List<AppInfo>
- 异常处理：捕获访问被拒绝、路径不存在等异常
- 进度报告：使用事件或回调报告扫描进度
- 过滤：排除系统文件夹（Windows, System32等）
- 异步支持：提供async版本

使用Directory.GetFiles和递归遍历实现。
```

#### **提示词3：实现图标提取**
```
创建IconExtractor静态类，从exe文件提取图标：

要求：
- 方法1：使用Icon.ExtractAssociatedIcon()
- 方法2：使用P/Invoke调用Shell32.dll的ExtractIcon
- 异常处理：文件不存在或无图标时返回默认图标
- 支持提取大图标（48x48或更大）
- 资源释放：正确释放非托管资源

提供完整的P/Invoke声明和使用示例。
```

#### **提示词4：创建自定义按钮控件**
```
创建AppButton自定义控件，继承自UserControl：

界面：
- 上方：48x48的PictureBox显示图标
- 下方：Label显示应用名称（支持文本截断）

功能：
- 鼠标悬停：背景色变化
- 单击：触发LaunchApp事件
- 右键：显示ContextMenuStrip（打开、属性、删除）
- 双击：打开文件位置

属性：
- AppInfo：绑定的应用信息
- Selected：是否选中状态

使用OnPaint自定义绘制或组合控件实现。
```

#### **提示词5：实现主窗体**
```
创建MainForm主窗体：

布局：
- 顶部：工具栏（搜索框、刷新按钮、设置按钮）
- 中间：FlowLayoutPanel（自动换行，动态添加AppButton）
- 底部：状态栏（显示应用数量、最后扫描时间）

功能：
- 异步加载应用列表
- 搜索过滤（实时筛选）
- 拖动窗口（无边框窗体）
- 右键托盘图标菜单

窗体属性：
- FormBorderStyle = None
- TopMost = true
- Opacity = 0.95

使用async/await加载数据，避免界面冻结。
```

#### **提示词6：实现配置管理**
```
创建ConfigManager服务类，管理应用配置：

功能：
- Load()：从JSON文件加载配置
- Save()：保存配置到JSON文件
- Reset()：恢复默认配置
- 配置文件路径：%AppData%/QuickLaunchTool/config.json

使用Newtonsoft.Json序列化，包含：
- 异常处理（文件不存在、格式错误）
- 默认配置生成
- 配置验证

提供单例模式实现。
```

---

## 六、开发建议

### 6.1 开发顺序
1. 创建项目结构和数据模型
2. 实现文件扫描和图标提取服务
3. 开发自定义AppButton控件
4. 构建主窗体和布局
5. 实现配置管理
6. 添加高级功能（搜索、排序、右键菜单）
7. 优化性能和用户体验
8. 测试和调试

### 6.2 测试要点
- 大量文件扫描性能测试
- 不同权限文件夹的访问测试
- 图标提取失败的降级处理
- 配置文件损坏的恢复机制
- 内存泄漏检测（图标资源释放）

### 6.3 注意事项
- 使用 `using` 语句释放图标资源
- 避免在UI线程执行耗时操作
- 处理文件访问权限异常
- 配置文件使用版本号，支持升级迁移
- 提供日志记录便于调试