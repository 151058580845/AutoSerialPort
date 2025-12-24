using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Abstractions;

namespace AutoSerialPort.Infrastructure.Services;

/// <summary>
/// 模拟输入服务实现，Windows 原生输入，Linux 依赖 xdotool。
/// </summary>
public class TypingService : ITypingService
{
    private readonly string? _xdotoolPath;
    private readonly string? _unavailableReason;

    /// <summary>
    /// 初始化模拟输入服务并检测可用性。
    /// </summary>
    public TypingService()
    {
        if (OperatingSystem.IsWindows())
        {
            IsAvailable = true;
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            _xdotoolPath = FindExecutable("xdotool");
            IsAvailable = !string.IsNullOrWhiteSpace(_xdotoolPath);
            if (!IsAvailable)
            {
                _unavailableReason = "xdotool not found in PATH";
            }
            return;
        }

        IsAvailable = false;
        _unavailableReason = "Typing not supported on this OS";
    }

    /// <summary>
    /// 是否可用。
    /// </summary>
    public bool IsAvailable { get; }

    /// <summary>
    /// 获取不可用原因。
    /// </summary>
    public string? GetUnavailableReason() => _unavailableReason;

    /// <summary>
    /// 尝试模拟输入文本。
    /// </summary>
    /// <param name="text">待输入文本。</param>
    /// <param name="ct">取消令牌。</param>
    public async Task<bool> TryTypeAsync(string text, CancellationToken ct)
    {
        if (!IsAvailable)
        {
            return false;
        }

        if (OperatingSystem.IsWindows())
        {
            // Windows 使用 SendInput 逐字符输入
            foreach (var ch in text)
            {
                SendUnicodeChar(ch);
            }

            return true;
        }

        if (OperatingSystem.IsLinux() && _xdotoolPath != null)
        {
            // Linux 依赖 xdotool 输入
            var psi = new ProcessStartInfo
            {
                FileName = _xdotoolPath,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            psi.ArgumentList.Add("type");
            psi.ArgumentList.Add("--delay");
            psi.ArgumentList.Add("0");
            psi.ArgumentList.Add("--clearmodifiers");
            psi.ArgumentList.Add("--");
            psi.ArgumentList.Add(text);

            using var process = Process.Start(psi);
            if (process == null)
            {
                return false;
            }

            await process.WaitForExitAsync(ct);
            return process.ExitCode == 0;
        }

        return false;
    }

    /// <summary>
    /// 在 PATH 中查找可执行文件。
    /// </summary>
    /// <param name="name">文件名。</param>
    private static string? FindExecutable(string name)
    {
        var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var path in paths)
        {
            var candidate = Path.Combine(path, name);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// 通过 Win32 SendInput 发送 Unicode 字符。
    /// </summary>
    /// <param name="ch">字符。</param>
    private static void SendUnicodeChar(char ch)
    {
        var inputs = new INPUT[2];

        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].U.ki.wScan = ch;
        inputs[0].U.ki.dwFlags = KEYEVENTF_UNICODE;

        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].U.ki.wScan = ch;
        inputs[1].U.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    /// <summary>
    /// Win32 SendInput 接口。
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    /// <summary>
    /// Win32 输入结构。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    /// <summary>
    /// Win32 输入联合体。
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    /// <summary>
    /// Win32 键盘输入结构。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
