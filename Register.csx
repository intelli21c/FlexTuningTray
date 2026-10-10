// Single-file executable
// Usage:
//   dotnet run Register.cs -- "C:\full\path\to\FlexTuningTray.exe" enable
//   dotnet run Register.cs -- "C:\full\path\to\FlexTuningTray.exe" disable

using System;
using System.IO;
using Microsoft.Win32;

const string AppName = "FlexTuningTray";

if (args.Length != 2)
{
    Console.Error.WriteLine(
        "Usage: dotnet run Register.csx <full-path-to-exe> <enable|disable>");
    Environment.ExitCode = 1;
    return;
}

string program = args[0];
string action = args[1];

if (!string.Equals(action, "enable", StringComparison.OrdinalIgnoreCase) &&
    !string.Equals(action, "disable", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Action must be 'enable' or 'disable'.");
    Environment.ExitCode = 1;
    return;
}

bool enabled = string.Equals(
    action, "enable", StringComparison.OrdinalIgnoreCase);

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
        if (!Path.IsPathFullyQualified(program))
            throw new ArgumentException(
                "EXE 경로는 절대 경로여야 합니다.");

        if (!File.Exists(program))
            throw new FileNotFoundException(
                "실행 파일을 찾을 수 없습니다.", program);

        // 경로에 공백이 있어도 정상 실행되도록 따옴표 처리
        string exe = Path.GetFullPath(program);
        key.SetValue(AppName, $"\"{exe}\"");

        Console.WriteLine($"자동 실행 등록 완료: {exe}");
    }
    else
    {
        key.DeleteValue(AppName, throwOnMissingValue: false);
        Console.WriteLine("자동 실행 등록 해제 완료.");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"오류: {ex.Message}");
    Environment.ExitCode = 1;
}