# 本地化资源 / Localization Resources

本文件夹包含应用的多语言本地化资源文件。

This folder contains multi-language localization resource files for the application.

## 📁 结构 / Structure

```
Strings/
├── zh-CN/              # 简体中文 / Simplified Chinese
│   └── Resources.resw
├── zh-TW/              # 繁体中文 / Traditional Chinese
│   └── Resources.resw
├── en-US/              # 英文 / English
│   └── Resources.resw
├── 本地化使用指南.md    # 中文使用指南 / Chinese Guide
├── Localization-Guide.md  # English Guide
└── README.md           # 本文件 / This file
```

## 🚀 快速开始 / Quick Start

### 在 XAML 中使用 / Use in XAML

```xml
<!-- 简单文本 / Simple Text -->
<TextBlock x:Uid="SettingsPageTitle" />

<!-- SettingsCard 卡片 / SettingsCard -->
<controls:SettingsCard x:Uid="ThemeSettingsCard">
    <controls:SettingsCard.HeaderIcon>
        <FontIcon Glyph="&#xE706;"/>
    </controls:SettingsCard.HeaderIcon>
</controls:SettingsCard>
```

### 在 C# 中使用 / Use in C#

```csharp
using WinUI3.Services;

string text = LocalizationHelper.GetString("MyResourceKey");
```

## 📖 完整文档 / Full Documentation

- **中文指南**: [本地化使用指南.md](./本地化使用指南.md)
- **English Guide**: [Localization-Guide.md](./Localization-Guide.md)

## 🎯 SettingsCard 本地化示例 / SettingsCard Localization Examples

### 示例 1 / Example 1: 简单卡片 / Simple Card

**XAML:**
```xml
<controls:SettingsCard x:Uid="SoundSettingsCard">
    <controls:SettingsCard.HeaderIcon>
        <FontIcon Glyph="&#xEC4F;"/>
    </controls:SettingsCard.HeaderIcon>
    <ToggleSwitch x:Name="SoundToggle" />
</controls:SettingsCard>
```

**Resources.resw:**
```xml
<data name="SoundSettingsCard.Header" xml:space="preserve">
  <value>控件声音</value>  <!-- zh-CN -->
  <value>Control Sound</value>  <!-- en-US -->
</data>
<data name="SoundSettingsCard.Description" xml:space="preserve">
  <value>控制应用中的交互提示音</value>  <!-- zh-CN -->
  <value>Control interaction sounds in the app</value>  <!-- en-US -->
</data>
```

### 示例 2 / Example 2: 展开卡片 / Expander Card

**XAML:**
```xml
<controls:SettingsExpander x:Uid="ThemeSettingsExpander" IsExpanded="False">
    <controls:SettingsExpander.HeaderIcon>
        <FontIcon Glyph="&#xE706;"/>
    </controls:SettingsExpander.HeaderIcon>
    <controls:SettingsExpander.Items>
        <controls:SettingsCard ContentAlignment="Left">
            <RadioButtons x:Name="ThemeRadioButtons">
                <RadioButton x:Uid="ThemeSystem" />
                <RadioButton x:Uid="ThemeLight" />
                <RadioButton x:Uid="ThemeDark" />
            </RadioButtons>
        </controls:SettingsCard>
    </controls:SettingsExpander.Items>
</controls:SettingsExpander>
```

**Resources.resw:**
```xml
<!-- Expander 本身 / Expander itself -->
<data name="ThemeSettingsExpander.Header" xml:space="preserve">
  <value>应用主题</value>
</data>
<data name="ThemeSettingsExpander.Description" xml:space="preserve">
  <value>选择浅色、深色或跟随系统</value>
</data>

<!-- RadioButton 选项 / RadioButton options -->
<data name="ThemeSystem.Content" xml:space="preserve">
  <value>跟随系统</value>
</data>
<data name="ThemeLight.Content" xml:space="preserve">
  <value>浅色</value>
</data>
<data name="ThemeDark.Content" xml:space="preserve">
  <value>深色</value>
</data>
```

## ⚠️ 重要提示 / Important Notes

### ✅ 正确做法 / Correct Way

```xml
<!-- 只使用一个 x:Uid / Use only one x:Uid -->
<controls:SettingsCard x:Uid="MyCard" />
```

### ❌ 错误做法 / Wrong Way

```xml
<!-- 不要这样做 / Don't do this -->
<controls:SettingsCard x:Uid="MyCard"
                      x:Uid:Description="MyDescription" />
```

### 📝 资源文件格式 / Resource File Format

```xml
<!-- 格式 / Format: UidName.PropertyName -->
<data name="MyCard.Header" xml:space="preserve">
  <value>卡片标题</value>
</data>
<data name="MyCard.Description" xml:space="preserve">
  <value>卡片描述</value>
</data>
```

## 🌍 支持的语言 / Supported Languages

| 语言 / Language | 代码 / Code | 状态 / Status |
|----------------|-------------|---------------|
| 简体中文 | zh-CN | ✅ 已配置 / Configured |
| 繁体中文 | zh-TW | ✅ 已配置 / Configured |
| English | en-US | ✅ Configured |

## 🔧 添加新语言 / Adding New Languages

1. 在 `Strings` 文件夹下创建新的语言文件夹（如 `ja-JP`）
   Create a new language folder under `Strings` (e.g., `ja-JP`)

2. 复制 `en-US/Resources.resw` 到新文件夹
   Copy `en-US/Resources.resw` to the new folder

3. 翻译所有字符串值
   Translate all string values

4. 重新生成项目
   Rebuild the project

## 📚 参考资料 / References

- [WinUI 3 本地化官方文档 / Official Docs](https://learn.microsoft.com/windows/apps/windows-app-sdk/localize-strings)
- [x:Uid 指令文档 / x:Uid Directive](https://learn.microsoft.com/windows/uwp/xaml-platform/x-uid-directive)

## 💡 提示 / Tips

- 查看 `SettingsPage.xaml` 获取完整的实际示例
  Check `SettingsPage.xaml` for complete real-world examples

- 使用 `LocalizationHelper` 类在 C# 代码中获取本地化字符串
  Use `LocalizationHelper` class to get localized strings in C# code

- 保持所有语言资源文件同步更新
  Keep all language resource files synchronized

---

**需要帮助？/ Need Help?**

请查看详细的使用指南：
Please refer to the detailed guides:
- [本地化使用指南.md](./本地化使用指南.md) (中文)
- [Localization-Guide.md](./Localization-Guide.md) (English)
