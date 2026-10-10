//Single file executable
//usage - dontet run Register.csx -- C:\full\path\to\FlexTuningTray.exe {enable|disable}
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

string program = "";
bool enabled = false;



try
{
    using var key = Registry.CurrentUser.OpenSubKey(
        @"Software\Microsoft\Windows\CurrentVersion\Run",
        writable: true);

    if (key == null)
        throw new InvalidOperationException(
            "시작 프로그램 레지스트리를 열 수 없습니다.");

    if (enabled)
    {
        string exe = Environment.ProcessPath
            ?? throw new InvalidOperationException(
                "실행 파일 경로를 확인할 수 없습니다.");

        key.SetValue(AppName, $"\"{exe}\"");
    }
    else
    {
        key.DeleteValue(AppName, throwOnMissingValue: false);
    }

    MessageBox.Show(
        enabled ? "자동 실행을 등록했습니다."
                : "자동 실행을 해제했습니다.",
        AppName);
}
catch (Exception ex)
{
    MessageBox.Show(ex.Message, AppName,
        MessageBoxButtons.OK, MessageBoxIcon.Error);
}