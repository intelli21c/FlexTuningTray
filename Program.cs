using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class Program
{
    // 반드시 실제 PC의 서비스 이름을 확인하세요.
    private const string ServiceName = "esifsvc";
    private const string AppName = "TuningTray";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApp());
    }

    private sealed class TrayApp : ApplicationContext
    {
        private readonly NotifyIcon tray;
        private readonly ToolStripMenuItem statusItem;

        public TrayApp()
        {
            var menu = new ContextMenuStrip();

            statusItem = new ToolStripMenuItem("상태 확인");
            statusItem.Click += (_, _) => RefreshStatus();

            menu.Items.Add(statusItem);
            menu.Items.Add("서비스 시작 (ON)", null,
                (_, _) => ChangeService("start"));
            menu.Items.Add("서비스 중지 (OFF)", null,
                (_, _) => ChangeService("stop"));

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add("로그인 시 자동 실행 등록", null,
                (_, _) => SetStartup(true));
            menu.Items.Add("자동 실행 해제", null,
                (_, _) => SetStartup(false));

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("종료", null, (_, _) => ExitThread());

            tray = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = AppName,
                ContextMenuStrip = menu,
                Visible = true
            };

            tray.DoubleClick += (_, _) => RefreshStatus();

            RefreshStatus();
        }

        private void RefreshStatus()
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"query {ServiceName}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });

                string output = p!.StandardOutput.ReadToEnd();
                p.WaitForExit();

                statusItem.Text = output.Contains("RUNNING",
                    StringComparison.OrdinalIgnoreCase)
                    ? "상태: 실행 중"
                    : output.Contains("STOPPED",
                        StringComparison.OrdinalIgnoreCase)
                        ? "상태: 중지됨"
                        : "상태: 확인 필요";
            }
            catch
            {
                statusItem.Text = "상태: 확인 실패";
            }
        }

        private void ChangeService(string action)
        {
            // action은 메뉴에서 지정한 두 값만 허용
            if (action != "start" && action != "stop")
                return;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"{action} {ServiceName}",
                    Verb = "runas",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var p = Process.Start(psi);
                if (p == null)
                    return;

                p.WaitForExit();

                MessageBox.Show(
                    p.ExitCode == 0
                        ? "서비스 명령이 완료되었습니다."
                        : $"명령 실행 실패 (코드 {p.ExitCode}).",
                    AppName,
                    MessageBoxButtons.OK,
                    p.ExitCode == 0
                        ? MessageBoxIcon.Information
                        : MessageBoxIcon.Warning);

                RefreshStatus();
            }
            catch (System.ComponentModel.Win32Exception ex)
                when (ex.NativeErrorCode == 1223)
            {
                // 사용자가 UAC 요청을 취소함
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetStartup(bool enabled)
        {
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
        }

        protected override void ExitThreadCore()
        {
            tray.Visible = false;
            tray.Dispose();
            base.ExitThreadCore();
        }
    }
}