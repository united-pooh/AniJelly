# Cinema：Apple TV 风格界面

用户已批准的 Cinema 覆盖首次设置、登录、首页和管理配置；最新外观在深色底上使用柔和蓝紫青绿、毛玻璃表面和扁平控件。主题 ID 为 `cinema`，是本构建的默认主题；上游原有六种主题仍可选择，已有用户明确保存的主题偏好不被清除。若旧浏览器仍显示先前选择的主题，可在个人显示设置中分别选择主界面与管理界面的 Cinema。

## 源码与构建

Web 上游版本、依赖锁定及接口兼容边界见 [Web 兼容说明](Web兼容说明.md)。Docker 先应用 [接口兼容补丁](jellyfin-web-anijelly.patch)，再应用 [视觉补丁](jellyfin-web-cinema.patch)与[电视/频道移除补丁](jellyfin-web-no-livetv.patch)，然后从源码构建。三份补丁按顺序应用于干净的固定上游源码后，文件哈希必须与开发时的源码一致；不能仅复制某台机器的 `dist` 作为唯一构建来源。

| Web 源码位置 | 职责 |
| --- | --- |
| `src/themes/cinema/` | MUI palette 与 legacy CSS：静态蓝紫青绿色域、浅蓝主按钮、玻璃回退、蓝色焦点与表面层级。 |
| `src/themes/_base/theme.ts` | 以 Cinema 选择器限定的 MUI 组件外观，覆盖表单、弹窗、开关及通知。 |
| `src/themes/index.ts`、`src/config.json` | 注册主题与默认配色；初始化默认值读取配置，已有用户选择仍优先。 |
| `src/apps/wizard/` 与登录 HTML/SCSS | 限宽居中表单、真实六步进度和登录布局；控制器认证及校验流程不变。 |
| `src/apps/legacy/CinemaSidebar.tsx`、modern drawer | 复用真实路由、用户可见媒体库与权限判断的桌面侧栏；手机保留可折叠菜单。 |
| `src/styles/cinema-library.scss`、`homesections` | 无可见媒体库时的影院引导、真实媒体内容卡片与页面留白；管理员才显示建库操作。 |
| `src/hooks/api/useUserViews.ts` 与 `hometab.js` | 共享查询在侧栏卸载时继续服务首页，失败可重试；销毁后的页面不再写入或聚焦。 |
| `ViewManagerPage.tsx`、legacy/modern 首页路由 | 动态加载时排除测试模块，避免将 Vitest 和 Node 文件模块打入浏览器。 |
| `src/apps/dashboard/` | 控制台层级、用户信息和常规设置分组，保留原有字段与保存行为。 |

字体使用本机系统字体（Apple 系统为 SF / PingFang），不下载或重新分发 Apple 字体。现有浅色 `appletv` 主题保持原样，名称相似不代表与新的 Cinema 相同。

## 柔和彩色与 Liquid Glass 风格

主按钮及选中态使用浅蓝 `#8BBCFF`，配深色文字；淡紫 `#C5B6FF` 与青绿 `#75D9C4` 用于静态背景光域、向导进度及局部强调。辅助文字 `text.secondary` 为 `#BCC4D3`；它不同于仍用于焦点与交互状态的 `palette.secondary` 蓝色 `#0A84FF`。危险操作继续使用红色，按钮与导航选中项采用 14–16px 圆角并去除装饰性投影。

共享颜色与表面变量定义在 `html[data-theme="cinema"]`：`--cinema-backdrop`、`--cinema-accent-violet`、`--cinema-accent-mint`、`--cinema-glass-bg`、`--cinema-auth-glass-bg`、`--cinema-glass-sheen`、`--cinema-glass-filter`、`--cinema-glass-shadow`、`--cinema-glass-fallback` 和 `--cinema-glass-border`。文字、主按钮和焦点优先复用 `--jf-palette-*`。

- 导航、顶栏、对话框及认证表单默认使用实色 `#171C2A`。支持标准或 WebKit `backdrop-filter` 时启用 `blur(14px) saturate(160%)`、方向性反光渐变及亮暗边缘；导航与弹窗使用 `rgba(8, 12, 22, 0.78)`，固定深色背景上的认证表单使用更清透的 `rgba(12, 19, 35, 0.45)`。
- `prefers-reduced-transparency: reduce` 或 `prefers-contrast: more` 时，两个玻璃底色均回到实色，反光、滤镜和投影关闭。不支持模糊时也使用实色。现有减少动态偏好继续生效。
- 这是基于 CSS 透光、反射高光和层次的网页近似，参考 [Apple 材质指南](https://developer.apple.com/design/human-interface-guidelines/materials)；未实现原生光学折射、内容自适应着色或液态融合。
- 登录与向导的玻璃层使用隔离的背景伪元素，不参与尺寸计算、不接收点击；输入、按钮及键盘焦点位于其上。
- 配置分组保持扁平表面与细边界，不给嵌套面板重复添加模糊。首页移除细竖线及强光影，改用静态彩色背景和浅蓝建库按钮；没有引入背景动画。

本轮 Liquid Glass 相对柔和彩色版仅修改 4 个样式/主题文件，不改变已有字段、页面布局、权限或 API 行为：

| 本轮 Web 源码文件 | 外观职责 |
| --- | --- |
| `src/themes/cinema/theme.scss` | 背景光域、共享材质变量、legacy 表面与偏好回退。 |
| `src/themes/_base/theme.ts` | Cinema 范围内的 MUI 玻璃表面。 |
| `src/apps/wizard/_cinemaForm.scss` | 登录与向导共用的清透背景层。 |
| `src/styles/cinema-library.scss` | 侧栏复用材质，内容边界保持平面。 |

## 行为边界

- “首次设置”是创建服务器首位管理员，不是开放注册。实际顺序为账户、服务器偏好、媒体库、元数据、远程访问、完成；保留密码非空和两次输入一致的要求。
- 用户没有可见媒体库时提供建库引导；已有媒体库时复用现有数据与横排卡片。没有加入演示影片，也未以主题改版修复尚未兼容的音乐聚合接口。
- 外观层保留管理设置行为；电视/频道移除补丁另按用户要求删除对应字段。其余帮助文案、禁用状态及错误反馈保留；重启、关闭、删除等操作继续使用危险色。
- 最新 Liquid Glass 风格效果见[Liquid Glass 验收记录](../.agent/visual/liquid-validation.md)。前一版[毛玻璃验收记录](../.agent/visual/glass-validation.md)保留为历史。[Cinema v1 运行验收](../.agent/visual/cinema-validation.md)保留原有 13 张截图及手机布局、键盘焦点、减少动态效果的历史结果；设计稿、v1 与最新运行证据分开记录。

## 验证入口

应用三份补丁后，在 Web 仓库中执行 `npm run build:check`、`npm run build:production`。此前外观迭代的 39 项回归来自 `src/utils/jellyfin-apiclient/createApiClient.test.ts`、`src/apps/wizard/controllers/wizard.test.js`、`src/apps/wizard/controllers/navigation.test.js`、`src/hooks/api/useUserViews.test.ts`、`src/apps/legacy/controllers/hometab.test.js`，通过 `npm test -- <测试文件列表>` 运行。真实共享查询取消、失败重试、销毁后停止写入均有回归覆盖。

此前外观增量通过 39 项回归、TypeScript、变更文件 lint 和生产构建。后续功能移除的当前完整 Web 回归为 215 项；构建、容器和已知环境边界见[当前核验记录](../docs/手册核验记录.md)。玻璃支持与实色回退均保留相同内容布局；实际可读性、焦点及桌面/手机效果以最新验收记录为准。
