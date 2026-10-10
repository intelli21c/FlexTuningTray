using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class Program
{
    private const string ServiceName = "esifsvc";
    private const string AppName = "FlexTuningTray";

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
        private readonly Icon laptopIcon;
        private readonly Icon tabletIcon;

        private bool? lastKnownRunning;


        public TrayApp()
        {
            using (var iconStream1 = new MemoryStream(ReadResource("laptop.ico")))
            using (var iconStream2 = new MemoryStream(ReadResource("tablet.ico")))
            {
                laptopIcon = new Icon(iconStream1);
                tabletIcon = new Icon(iconStream2);
            }

            var menu = new ContextMenuStrip();

            statusItem = new ToolStripMenuItem("상태 확인");
            statusItem.Click += (_, _) => RefreshStatus();

            menu.Items.Add(statusItem);
            menu.Items.Add("서비스 시작 (ON)", null,
                (_, _) => ChangeService("start"));
            menu.Items.Add("서비스 중지 (OFF)", null,
                (_, _) => ChangeService("stop"));

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("종료", null, (_, _) => ExitThread());

            tray = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = AppName,
                ContextMenuStrip = menu,
                Visible = true
            };

            menu.Opening += (_, _) => RefreshStatus();
            tray.DoubleClick += (_, _) => RefreshStatus();

            RefreshStatus();
        }

        private static byte[] ReadResource(string fileName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            string? resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n =>
                    n.EndsWith("." + fileName,
                        StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new InvalidOperationException(
                    $"내장 리소스를 찾을 수 없습니다: {fileName}");
            }

            using Stream stream =
                assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"리소스를 읽을 수 없습니다: {fileName}");

            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }

        private void RefreshStatus()
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"query \"{ServiceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                });

                if (p == null)
                    throw new InvalidOperationException(
                        "서비스 상태 조회 프로세스를 시작하지 못했습니다.");

                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();
                p.WaitForExit();

                if (p.ExitCode != 0)
                {
                    SetUnknownStatus(
                        string.IsNullOrWhiteSpace(error)
                            ? "서비스를 찾을 수 없음"
                            : "서비스 조회 실패");
                    return;
                }

                // STATE 줄의 숫자 코드를 기준으로 판별
                bool? running = null;

                foreach (string line in output.Split(
                    new[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmed = line.Trim();

                    if (!trimmed.StartsWith(
                        "STATE", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (trimmed.Contains("RUNNING",
                        StringComparison.OrdinalIgnoreCase))
                        running = true;
                    else if (trimmed.Contains("STOPPED",
                        StringComparison.OrdinalIgnoreCase))
                        running = false;

                    break;
                }

                if (running == true)
                {
                    lastKnownRunning = true;
                    tray.Icon = tabletIcon;
                    tray.Text = "Intel Dynamic Tuning: ON";
                    statusItem.Text = "상태: 실행 중 (ON)";
                }
                else if (running == false)
                {
                    lastKnownRunning = false;
                    tray.Icon = laptopIcon;
                    tray.Text = "Intel Dynamic Tuning: OFF";
                    statusItem.Text = "상태: 중지됨 (OFF)";
                }
                else
                {
                    SetUnknownStatus("서비스 상태 확인 불가");
                }
            }
            catch
            {
                SetUnknownStatus("서비스 상태 조회 실패");
            }
        }

        private void SetUnknownStatus(string message)
        {
            lastKnownRunning = null;

            // 상태를 알 수 없으면 ON/OFF 아이콘으로 오인시키지 않음
            tray.Icon = SystemIcons.Warning;
            tray.Text = "Intel Dynamic Tuning: 상태 확인 필요";
            statusItem.Text = message;
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
                if (p.ExitCode != 0)
                {

                    MessageBox.Show(
                        $"명령 실행 실패 (코드 {p.ExitCode}).",
                        AppName,
                        MessageBoxButtons.OK,
                        p.ExitCode == 0
                            ? MessageBoxIcon.Information
                            : MessageBoxIcon.Warning);

                }
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

        protected override void ExitThreadCore()
        {
            tray.Visible = false;
            tray.Dispose();
            laptopIcon.Dispose();
            tabletIcon.Dispose();
            base.ExitThreadCore();
        }
    }
}