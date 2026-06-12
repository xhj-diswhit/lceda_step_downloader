using System.Diagnostics;
using System.IO.Compression;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

Forms.Application.EnableVisualStyles();
Forms.Application.SetCompatibleTextRenderingDefault(false);
Forms.Application.Run(new InstallerForm());

internal sealed class InstallerForm : Forms.Form
{
    private const string AppName = "LCEDA Step Downloader";
    private const string ExeName = "lceda_step_downloader.exe";
    private const string Publisher = "lceda_step_downloader";

    private readonly Forms.TextBox installPathTextBox = new();
    private readonly Forms.Button browseButton = new();
    private readonly Forms.Button installButton = new();
    private readonly Forms.Label statusLabel = new();
    private readonly Forms.ProgressBar progressBar = new();

    public InstallerForm()
    {
        Text = AppName + " 安装程序";
        Width = 620;
        Height = 250;
        FormBorderStyle = Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = Forms.FormStartPosition.CenterScreen;

        var titleLabel = new Forms.Label
        {
            Text = "选择安装位置",
            Left = 24,
            Top = 22,
            Width = 540,
            Height = 28,
            Font = new System.Drawing.Font(Font.FontFamily, 12, System.Drawing.FontStyle.Bold)
        };

        var hintLabel = new Forms.Label
        {
            Text = "程序将安装到下面的文件夹。点击“浏览”可以选择其他位置。",
            Left = 24,
            Top = 58,
            Width = 540,
            Height = 24
        };

        installPathTextBox.Left = 24;
        installPathTextBox.Top = 92;
        installPathTextBox.Width = 450;
        installPathTextBox.Height = 28;
        installPathTextBox.Text = DefaultInstallDir;

        browseButton.Left = 486;
        browseButton.Top = 91;
        browseButton.Width = 86;
        browseButton.Height = 30;
        browseButton.Text = "浏览...";
        browseButton.Click += BrowseButton_Click;

        progressBar.Left = 24;
        progressBar.Top = 136;
        progressBar.Width = 548;
        progressBar.Height = 18;
        progressBar.Style = Forms.ProgressBarStyle.Marquee;
        progressBar.Visible = false;

        statusLabel.Left = 24;
        statusLabel.Top = 164;
        statusLabel.Width = 548;
        statusLabel.Height = 22;
        statusLabel.Text = "准备安装";

        installButton.Left = 486;
        installButton.Top = 186;
        installButton.Width = 86;
        installButton.Height = 32;
        installButton.Text = "安装";
        installButton.Click += InstallButton_Click;

        Controls.Add(titleLabel);
        Controls.Add(hintLabel);
        Controls.Add(installPathTextBox);
        Controls.Add(browseButton);
        Controls.Add(progressBar);
        Controls.Add(statusLabel);
        Controls.Add(installButton);
    }

    private static string DefaultInstallDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs",
        AppName);

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using Forms.FolderBrowserDialog dialog = new()
        {
            Description = "选择安装文件夹",
            SelectedPath = Directory.Exists(installPathTextBox.Text) ? installPathTextBox.Text : DefaultInstallDir,
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) == Forms.DialogResult.OK)
        {
            installPathTextBox.Text = dialog.SelectedPath;
        }
    }

    private async void InstallButton_Click(object? sender, EventArgs e)
    {
        var installDir = installPathTextBox.Text.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(installDir))
        {
            Forms.MessageBox.Show(this, "请选择安装目录。", AppName, Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Warning);
            return;
        }

        if (!Path.IsPathRooted(installDir))
        {
            installDir = Path.GetFullPath(installDir);
            installPathTextBox.Text = installDir;
        }

        SetInstallingState(true, "正在安装...");

        try
        {
            await Task.Run(() => InstallToDirectory(installDir));
            SetInstallingState(false, "安装完成");
            Forms.MessageBox.Show(this, "安装完成。可以从开始菜单或桌面快捷方式启动。", AppName, Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Information);
            Close();
        }
        catch (Exception ex)
        {
            SetInstallingState(false, "安装失败");
            Forms.MessageBox.Show(this, "安装失败:\r\n" + ex.Message, AppName, Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Error);
        }
    }

    private void SetInstallingState(bool installing, string status)
    {
        installPathTextBox.Enabled = !installing;
        browseButton.Enabled = !installing;
        installButton.Enabled = !installing;
        progressBar.Visible = installing;
        statusLabel.Text = status;
    }

    private static void InstallToDirectory(string installDir)
    {
        if (IsAppRunning(installDir))
        {
            throw new InvalidOperationException("检测到程序正在运行，请先关闭后再重新安装。");
        }

        var tempRoot = Path.Combine(Path.GetTempPath(), "lceda-step-downloader-installer-" + Guid.NewGuid().ToString("N"));
        var payloadZip = Path.Combine(tempRoot, "payload.zip");
        var payloadDir = Path.Combine(tempRoot, "payload");

        Directory.CreateDirectory(tempRoot);
        Directory.CreateDirectory(payloadDir);

        using (var resource = typeof(InstallerForm).Assembly.GetManifestResourceStream("payload.zip")
            ?? throw new InvalidOperationException("安装包内没有找到 payload.zip。"))
        using (var output = File.Create(payloadZip))
        {
            resource.CopyTo(output);
        }

        ZipFile.ExtractToDirectory(payloadZip, payloadDir);

        if (Directory.Exists(installDir))
        {
            Directory.Delete(installDir, recursive: true);
        }

        CopyDirectory(payloadDir, installDir);
        CreateShortcut(AppName, Path.Combine(installDir, ExeName), installDir);
        CreateUninstaller(installDir);
        RegisterUninstallEntry(installDir);

        Directory.Delete(tempRoot, recursive: true);
    }

    private static bool IsAppRunning(string installDir)
    {
        var installedExe = Path.Combine(installDir, ExeName);
        return Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ExeName))
            .Any(process =>
            {
                try
                {
                    return string.Equals(process.MainModule?.FileName, installedExe, StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            });
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var file in Directory.GetFiles(sourceDirectory))
        {
            File.Copy(file, Path.Combine(destinationDirectory, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var directory in Directory.GetDirectories(sourceDirectory))
        {
            CopyDirectory(directory, Path.Combine(destinationDirectory, Path.GetFileName(directory)));
        }
    }

    private static void CreateShortcut(string name, string targetPath, string workingDirectory)
    {
        var startMenu = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs",
            name);
        Directory.CreateDirectory(startMenu);

        CreateShellShortcut(Path.Combine(startMenu, name + ".lnk"), targetPath, workingDirectory);

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        CreateShellShortcut(Path.Combine(desktop, name + ".lnk"), targetPath, workingDirectory);
    }

    private static void CreateShellShortcut(string shortcutPath, string targetPath, string workingDirectory)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("无法创建快捷方式。");
        dynamic shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("无法创建快捷方式。");
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.IconLocation = targetPath + ",0";
        shortcut.Save();
    }

    private static void CreateUninstaller(string installDir)
    {
        var uninstallScript = Path.Combine(installDir, "Uninstall.cmd");
        var startMenuShortcut = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs",
            AppName,
            AppName + ".lnk");
        var startMenuFolder = Path.GetDirectoryName(startMenuShortcut)!;
        var desktopShortcut = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            AppName + ".lnk");

        var script = $"""
@echo off
taskkill /IM "{ExeName}" /F >nul 2>nul
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\{AppName}" /f >nul 2>nul
del "{desktopShortcut}" >nul 2>nul
del "{startMenuShortcut}" >nul 2>nul
rmdir "{startMenuFolder}" >nul 2>nul
cd /d "%TEMP%"
rmdir /s /q "{installDir}" >nul 2>nul
echo {AppName} 已卸载。
pause
""";

        File.WriteAllText(uninstallScript, script, System.Text.Encoding.Default);
    }

    private static void RegisterUninstallEntry(string installDir)
    {
        using var key = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{AppName}");
        if (key == null)
        {
            return;
        }

        key.SetValue("DisplayName", AppName);
        key.SetValue("DisplayVersion", "1.0.0");
        key.SetValue("Publisher", Publisher);
        key.SetValue("InstallLocation", installDir);
        key.SetValue("DisplayIcon", Path.Combine(installDir, ExeName));
        key.SetValue("UninstallString", Path.Combine(installDir, "Uninstall.cmd"));
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }
}
