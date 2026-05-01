# WinUI3 Template - Localization Guide

This template supports multi-language localization. Currently configured languages: Chinese (Simplified, Traditional) and English.

## 📁 File Structure

```
Strings/
├── zh-CN/
│   └── Resources.resw    # Simplified Chinese resources
├── zh-TW/
│   └── Resources.resw    # Traditional Chinese resources
├── en-US/
│   └── Resources.resw    # English resources
└── Localization-Guide.md  # This document

Services/
└── LocalizationHelper.cs  # Localization helper class
```

## 🎯 Usage

### 1. Localization in XAML

#### Method A: Using `x:Uid` Attribute (Recommended)

This is the standard WinUI 3 localization approach, suitable for most controls.

**Simple Text Controls:**

```xml
<!-- In XAML -->
<TextBlock x:Uid="SettingsPageTitle" Text="Settings" />
```

```xml
<!-- In Resources.resw -->
<data name="SettingsPageTitle.Text" xml:space="preserve">
  <value>Settings</value>
</data>
```

**SettingsCard Localization:**

```xml
<!-- In XAML -->
<controls:SettingsCard x:Uid="ThemeSettingsCard">
    <controls:SettingsCard.HeaderIcon>
        <FontIcon Glyph="&#xE706;"/>
    </controls:SettingsCard.HeaderIcon>
    <!-- Card content -->
</controls:SettingsCard>
```

```xml
<!-- In Resources.resw -->
<data name="ThemeSettingsCard.Header" xml:space="preserve">
  <value>App Theme</value>
</data>
<data name="ThemeSettingsCard.Description" xml:space="preserve">
  <value>Select Light, Dark, or System</value>
</data>
```

**SettingsExpander Localization:**

```xml
<!-- In XAML -->
<controls:SettingsExpander x:Uid="MaterialSettingsExpander" IsExpanded="False">
    <controls:SettingsExpander.HeaderIcon>
        <FontIcon Glyph="&#xE2B1;"/>
    </controls:SettingsExpander.HeaderIcon>
    <controls:SettingsExpander.Items>
        <!-- Child items -->
    </controls:SettingsExpander.Items>
</controls:SettingsExpander>
```

```xml
<!-- In Resources.resw -->
<data name="MaterialSettingsExpander.Header" xml:space="preserve">
  <value>Background Material</value>
</data>
<data name="MaterialSettingsExpander.Description" xml:space="preserve">
  <value>Select Mica or Acrylic</value>
</data>
```

**Multiple Properties Example:**

```xml
<!-- In XAML -->
<Button x:Uid="SaveButton" 
        Content="Save"
        ToolTipService.ToolTip="Save settings" />
```

```xml
<!-- In Resources.resw -->
<data name="SaveButton.Content" xml:space="preserve">
  <value>Save</value>
</data>
<data name="SaveButton.[using:Microsoft.UI.Xaml.Controls]ToolTipService.ToolTip" xml:space="preserve">
  <value>Save settings</value>
</data>
```

### 2. Localization in C# Code

Use the `LocalizationHelper` class to retrieve localized strings:

```csharp
using WinUI3.Services;

// Get localized string
string message = LocalizationHelper.GetString("ErrorMessage");

// Use in dialog
var dialog = new ContentDialog
{
    Title = LocalizationHelper.GetString("DialogTitle"),
    Content = LocalizationHelper.GetString("DialogContent"),
    PrimaryButtonText = LocalizationHelper.GetString("ConfirmButton"),
    CloseButtonText = LocalizationHelper.GetString("CancelButton")
};
```

## 📝 Adding New Localized Strings

### Step 1: Add Key-Value Pairs in Resource Files

In `Strings/zh-CN/Resources.resw`:

```xml
<data name="MyNewString" xml:space="preserve">
  <value>我的新字符串</value>
</data>
```

In `Strings/en-US/Resources.resw`:

```xml
<data name="MyNewString" xml:space="preserve">
  <value>My New String</value>
</data>
```

### Step 2: Use in XAML or Code

**XAML:**
```xml
<TextBlock x:Uid="MyNewString" />
```

**C#:**
```csharp
string text = LocalizationHelper.GetString("MyNewString");
```

## 🎨 Complete SettingsCard Localization Examples

### Example 1: Simple SettingsCard

```xml
<!-- XAML -->
<controls:SettingsCard x:Uid="SoundSettingsCard">
    <controls:SettingsCard.HeaderIcon>
        <FontIcon Glyph="&#xEC4F;"/>
    </controls:SettingsCard.HeaderIcon>
    <ToggleSwitch x:Name="SoundToggle" />
</controls:SettingsCard>
```

```xml
<!-- zh-CN/Resources.resw -->
<data name="SoundSettingsCard.Header" xml:space="preserve">
  <value>控件声音</value>
</data>
<data name="SoundSettingsCard.Description" xml:space="preserve">
  <value>控制应用中的交互提示音</value>
</data>
```

```xml
<!-- en-US/Resources.resw -->
<data name="SoundSettingsCard.Header" xml:space="preserve">
  <value>Control Sound</value>
</data>
<data name="SoundSettingsCard.Description" xml:space="preserve">
  <value>Control interaction sounds in the app</value>
</data>
```

### Example 2: SettingsCard with ActionIcon

```xml
<!-- XAML -->
<controls:SettingsCard x:Uid="StartupSettingsCard">
    <controls:SettingsCard.HeaderIcon>
        <FontIcon Glyph="&#xE7E8;"/>
    </controls:SettingsCard.HeaderIcon>
    <controls:SettingsCard.ActionIcon>
        <FontIcon Glyph="&#xE8A7;"/>
    </controls:SettingsCard.ActionIcon>
    <ToggleSwitch x:Name="StartupToggle" />
</controls:SettingsCard>
```

```xml
<!-- Resources.resw -->
<data name="StartupSettingsCard.Header" xml:space="preserve">
  <value>Run at Startup</value>
</data>
<data name="StartupSettingsCard.Description" xml:space="preserve">
  <value>Allow the app to run automatically when Windows starts</value>
</data>
<data name="StartupSettingsCard.ActionIconToolTip" xml:space="preserve">
  <value>Manage app startup in system settings</value>
</data>
```

### Example 3: SettingsExpander with Nested Cards

```xml
<!-- XAML -->
<controls:SettingsExpander x:Uid="LanguageSettingsExpander" IsExpanded="False">
    <controls:SettingsExpander.HeaderIcon>
        <FontIcon Glyph="&#xF2B7;"/>
    </controls:SettingsExpander.HeaderIcon>
    <controls:SettingsExpander.Items>
        <controls:SettingsCard x:Uid="LanguageSelectCard" ContentAlignment="Left">
            <ComboBox x:Name="LanguageComboBox" Width="200">
                <ComboBoxItem x:Uid="Language_SimplifiedChinese" />
                <ComboBoxItem x:Uid="Language_English" />
            </ComboBox>
        </controls:SettingsCard>
    </controls:SettingsExpander.Items>
</controls:SettingsExpander>
```

```xml
<!-- Resources.resw -->
<!-- Outer Expander -->
<data name="LanguageSettingsExpander.Header" xml:space="preserve">
  <value>Language</value>
</data>
<data name="LanguageSettingsExpander.Description" xml:space="preserve">
  <value>Select app display language</value>
</data>

<!-- Inner Card -->
<data name="LanguageSelectCard.Header" xml:space="preserve">
  <value>Select Language</value>
</data>
<data name="LanguageSelectCard.Description" xml:space="preserve">
  <value>Restart required for changes to take effect</value>
</data>

<!-- ComboBox Options -->
<data name="Language_SimplifiedChinese.Content" xml:space="preserve">
  <value>Simplified Chinese</value>
</data>
<data name="Language_English.Content" xml:space="preserve">
  <value>English</value>
</data>
```

## ⚠️ Common Issues and Notes

### 1. ❌ Error: Using Unsupported Syntax

```xml
<!-- ❌ Wrong: Don't use this syntax -->
<controls:SettingsCard x:Uid="MyCard"
                      x:Uid:Description="MyDescription" />
```

```xml
<!-- ✅ Correct: Use only one x:Uid -->
<controls:SettingsCard x:Uid="MyCard" />
```

### 2. Resource File Naming Rules

- Use `.` to separate Uid and property name: `UidName.PropertyName`
- For attached properties, use full namespace: `UidName.[using:Namespace]PropertyName`
- Maintain naming consistency, recommend `Area_Element_Property` format

### 3. Project Configuration Notes

**Do NOT manually add PRIResource items!**

The .NET SDK automatically includes all `.resw` files. Manual addition causes NETSDK1022 errors.

```xml
<!-- ❌ Don't add this -->
<ItemGroup>
  <PRIResource Include="Strings\**\*.resw" />
</ItemGroup>
```

If you encounter duplicate item errors, remove manually added PRIResource configurations.

### 4. Dynamically Created UI Elements

For UI elements created dynamically in code, you must use `LocalizationHelper`:

```csharp
var card = new SettingsCard
{
    Header = LocalizationHelper.GetString("DynamicCard.Header"),
    Description = LocalizationHelper.GetString("DynamicCard.Description")
};
```

### 5. Language Switching

If you implement language switching, call this after switching:

```csharp
LocalizationHelper.Reset();
```

Then restart the app for changes to take effect.

## 📚 Naming Convention Recommendations

### Page Element Naming

```
Format: PageName_ElementName.PropertyName

Examples:
- SettingsPage_Title.Text
- HomePage_WelcomeText.Text
- SettingsPage_ThemeCard.Header
```

### Common Element Naming

```
Format: Category_ElementName

Examples:
- Nav_Home.Content
- Dialog_Confirm.Content
- Button_Save.Content
```

### Card Naming

```
Format: PageName_CardPurpose

Examples:
- SettingsPage_ThemeCard
- SettingsPage_LanguageExpander
- HomePage_WelcomeCard
```

## 🌍 Supported Languages

Currently configured languages:

- Simplified Chinese (zh-CN)
- Traditional Chinese (zh-TW)
- English (en-US)

### Adding New Languages

1. Create a new language folder under `Strings` (e.g., `ja-JP`)
2. Copy `en-US/Resources.resw` to the new folder
3. Translate all string values
4. Rebuild the project

## 🔗 References

- [WinUI 3 Localization Official Docs](https://learn.microsoft.com/windows/apps/windows-app-sdk/localize-strings)
- [.resw File Format](https://learn.microsoft.com/windows/uwp/app-resources/localize-strings-ui-manifest)
- [x:Uid Directive Documentation](https://learn.microsoft.com/windows/uwp/xaml-platform/x-uid-directive)

## 💡 Best Practices

1. **Keep in Sync**: Update all language resource files when adding new strings
2. **Use Descriptive Keys**: Key names should clearly express purpose, e.g., `SettingsPage_ThemeCard.Header`
3. **Avoid Hardcoding**: All user-visible text should use resource files
4. **Test Multiple Languages**: Test all pages after switching languages
5. **Provide Fallback**: `LocalizationHelper` implements fallback, returning key name when resource not found
6. **Comment Important Resources**: Add comments for complex or special strings in resource files

---

**Tip**: The `SettingsPage.xaml` in this template demonstrates complete localization implementation examples!
