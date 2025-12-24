using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoSerialPort.Domain.Abstractions;
using AutoSerialPort.Domain.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoSerialPort.UI.ViewModels;

/// <summary>
/// 串口收发视图模型。
/// </summary>
public partial class SerialConsoleViewModel : ObservableObject, IDisposable
{
    private const int MaxEntryCount = 5000;
    private readonly ISerialService _serialService;
    private readonly DispatcherTimer _flushTimer;
    private ConcurrentQueue<SerialRawDataEventArgs> _pending = new();
    private DeviceProfileViewModel? _selectedDevice;
    private long _selectedDeviceId;
    private long _bufferedBytes;
    private long _bufferedCount;

    public ObservableCollection<SerialMonitorEntry> Entries { get; } = new();

    public IReadOnlyList<AutoSerialPort.Application.Models.OptionItem> AppendOptions { get; }

    public IRelayCommand ClearCommand { get; }
    public IAsyncRelayCommand SendCommand { get; }

    [ObservableProperty]
    private bool _isHexDisplay;

    [ObservableProperty]
    private bool _isAutoScrollEnabled = true;

    [ObservableProperty]
    private bool _isHexSend;

    [ObservableProperty]
    private string _sendText = string.Empty;

    [ObservableProperty]
    private AutoSerialPort.Application.Models.OptionItem? _selectedAppendOption;

    [ObservableProperty]
    private string _customAppendText = string.Empty;

    [ObservableProperty]
    private bool _isCustomAppend;

    [ObservableProperty]
    private string _receiveStatsText = "0 条 / 0 字节";

    [ObservableProperty]
    private string _sendErrorText = string.Empty;

    [ObservableProperty]
    private bool _hasSendError;

    [ObservableProperty]
    private bool _isDeviceConnected;

    [ObservableProperty]
    private bool _showConnectionHint = true;

    [ObservableProperty]
    private string _connectionHintText = "请先连接串口";

    public SerialConsoleViewModel(ISerialService serialService)
    {
        _serialService = serialService;

        AppendOptions = new[]
        {
            new AutoSerialPort.Application.Models.OptionItem("None", "无"),
            new AutoSerialPort.Application.Models.OptionItem("CR", "CR(\\r)"),
            new AutoSerialPort.Application.Models.OptionItem("LF", "LF(\\n)"),
            new AutoSerialPort.Application.Models.OptionItem("CRLF", "CRLF(\\r\\n)"),
            new AutoSerialPort.Application.Models.OptionItem("Custom", "自定义")
        };

        ClearCommand = new RelayCommand(Clear);
        SendCommand = new AsyncRelayCommand(SendAsync, CanSend);
        SelectedAppendOption = AppendOptions.FirstOrDefault();

        _serialService.RawDataReceived += OnRawDataReceived;
        _flushTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _flushTimer.Tick += (_, _) => FlushPending();
        _flushTimer.Start();
    }

    public void SetActiveDevice(DeviceProfileViewModel? device)
    {
        if (_selectedDevice != null)
        {
            _selectedDevice.PropertyChanged -= OnSelectedDevicePropertyChanged;
            _selectedDevice.ParserSettings.PropertyChanged -= OnParserSettingsPropertyChanged;
        }

        _selectedDevice = device;
        Interlocked.Exchange(ref _selectedDeviceId, device?.SerialSettings.Config.Id ?? 0);

        if (_selectedDevice != null)
        {
            _selectedDevice.PropertyChanged += OnSelectedDevicePropertyChanged;
            _selectedDevice.ParserSettings.PropertyChanged += OnParserSettingsPropertyChanged;
        }

        Interlocked.Exchange(ref _pending, new ConcurrentQueue<SerialRawDataEventArgs>());
        Clear();
        UpdateConnectionState();
        RefreshEntryDisplay();
    }

    partial void OnIsHexDisplayChanged(bool value)
    {
        RefreshEntryDisplay();
    }

    partial void OnSelectedAppendOptionChanged(AutoSerialPort.Application.Models.OptionItem? value)
    {
        IsCustomAppend = string.Equals(value?.Value, "Custom", StringComparison.OrdinalIgnoreCase);
        SendCommand.NotifyCanExecuteChanged();
    }

    partial void OnSendTextChanged(string value)
    {
        SendCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsHexSendChanged(bool value)
    {
        SendCommand.NotifyCanExecuteChanged();
    }

    partial void OnCustomAppendTextChanged(string value)
    {
        SendCommand.NotifyCanExecuteChanged();
    }

    partial void OnSendErrorTextChanged(string value)
    {
        HasSendError = !string.IsNullOrWhiteSpace(value);
    }

    private void OnSelectedDevicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DeviceProfileViewModel.IsConnected))
        {
            UpdateConnectionState();
        }
    }

    private void OnParserSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ParserSettingsViewModel.Encoding) && !IsHexDisplay)
        {
            RefreshEntryDisplay();
        }
    }

    private bool CanSend()
    {
        return IsDeviceConnected && (!string.IsNullOrWhiteSpace(SendText) || HasAppendBytes());
    }

    private void UpdateConnectionState()
    {
        IsDeviceConnected = _selectedDevice?.IsConnected == true;
        ShowConnectionHint = !IsDeviceConnected;
        SendCommand.NotifyCanExecuteChanged();
    }

    private void OnRawDataReceived(object? sender, SerialRawDataEventArgs e)
    {
        var deviceId = Interlocked.Read(ref _selectedDeviceId);
        if (deviceId == 0 || e.DeviceId != deviceId)
        {
            return;
        }

        _pending.Enqueue(e);
    }

    private void FlushPending()
    {
        var deviceId = Interlocked.Read(ref _selectedDeviceId);
        if (deviceId == 0)
        {
            DrainQueue();
            return;
        }

        var encoding = ResolveDisplayEncoding();
        var appended = false;
        while (_pending.TryDequeue(out var item))
        {
            if (item.DeviceId != deviceId)
            {
                continue;
            }

            var displayText = FormatDisplay(item.Data, encoding, IsHexDisplay);
            AddEntry(new SerialMonitorEntry(item.Timestamp, item.Data, displayText));
            appended = true;
        }

        if (appended)
        {
            UpdateReceiveStats();
        }
    }

    private void DrainQueue()
    {
        while (_pending.TryDequeue(out _))
        {
        }
    }

    private void AddEntry(SerialMonitorEntry entry)
    {
        Entries.Add(entry);
        _bufferedBytes += entry.Data.Length;
        _bufferedCount++;
        TrimEntries();
    }

    private void TrimEntries()
    {
        while (Entries.Count > MaxEntryCount)
        {
            var removed = Entries[0];
            Entries.RemoveAt(0);
            _bufferedBytes -= removed.Data.Length;
            _bufferedCount--;
        }
    }

    private void UpdateReceiveStats()
    {
        ReceiveStatsText = $"{_bufferedCount} 条 / {_bufferedBytes} 字节";
    }

    private void Clear()
    {
        Entries.Clear();
        _bufferedBytes = 0;
        _bufferedCount = 0;
        ReceiveStatsText = "0 条 / 0 字节";
        SendErrorText = string.Empty;
    }

    private async Task SendAsync()
    {
        SendErrorText = string.Empty;

        var deviceId = Interlocked.Read(ref _selectedDeviceId);
        if (!IsDeviceConnected || deviceId == 0)
        {
            SendErrorText = "请先连接串口";
            return;
        }

        if (!TryBuildPayload(out var payload, out var error))
        {
            SendErrorText = error ?? "发送数据无效";
            return;
        }

        var result = await _serialService.SendAsync(deviceId, payload, CancellationToken.None);
        if (!result.Success)
        {
            SendErrorText = result.Error ?? "发送失败";
        }
    }

    private bool TryBuildPayload(out byte[] payload, out string? error)
    {
        payload = Array.Empty<byte>();
        error = null;

        var encoding = ResolveSendEncoding();
        var data = Array.Empty<byte>();

        if (IsHexSend)
        {
            if (!string.IsNullOrWhiteSpace(SendText) &&
                !TryParseHex(SendText, out data, out error))
            {
                return false;
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(SendText))
            {
                data = encoding.GetBytes(SendText);
            }
        }

        var appendBytes = BuildAppendBytes(SelectedAppendOption, CustomAppendText, encoding);
        if (data.Length == 0 && appendBytes.Length == 0)
        {
            error = "请输入要发送的数据";
            return false;
        }

        payload = Combine(data, appendBytes);
        return true;
    }

    private Encoding ResolveDisplayEncoding()
    {
        var name = _selectedDevice?.ParserSettings.Encoding;
        return ResolveEncoding(name);
    }

    private Encoding ResolveSendEncoding()
    {
        var name = _selectedDevice?.ParserSettings.Encoding;
        return ResolveEncoding(name);
    }

    private void RefreshEntryDisplay()
    {
        var encoding = ResolveDisplayEncoding();
        foreach (var entry in Entries)
        {
            entry.DisplayText = FormatDisplay(entry.Data, encoding, IsHexDisplay);
        }
    }

    private static Encoding ResolveEncoding(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Encoding.UTF8;
        }

        try
        {
            return Encoding.GetEncoding(name);
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    private static string FormatDisplay(byte[] data, Encoding encoding, bool hex)
    {
        return hex ? FormatHex(data) : encoding.GetString(data);
    }

    private static string FormatHex(byte[] data)
    {
        if (data.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(data.Length * 3);
        for (var i = 0; i < data.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            builder.Append(data[i].ToString("X2"));
        }

        return builder.ToString();
    }

    private static bool TryParseHex(string input, out byte[] data, out string? error)
    {
        data = Array.Empty<byte>();
        error = null;

        var compact = new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (compact.Length == 0)
        {
            error = "请输入要发送的数据";
            return false;
        }

        if (compact.Length % 2 != 0)
        {
            error = "十六进制长度必须为偶数";
            return false;
        }

        var buffer = new byte[compact.Length / 2];
        for (var i = 0; i < buffer.Length; i++)
        {
            var high = HexValue(compact[i * 2]);
            var low = HexValue(compact[i * 2 + 1]);
            if (high < 0 || low < 0)
            {
                error = "包含非十六进制字符";
                return false;
            }

            buffer[i] = (byte)((high << 4) | low);
        }

        data = buffer;
        return true;
    }

    private static int HexValue(char value)
    {
        if (value is >= '0' and <= '9')
        {
            return value - '0';
        }

        if (value is >= 'a' and <= 'f')
        {
            return value - 'a' + 10;
        }

        if (value is >= 'A' and <= 'F')
        {
            return value - 'A' + 10;
        }

        return -1;
    }

    private static byte[] BuildAppendBytes(AutoSerialPort.Application.Models.OptionItem? option, string custom, Encoding encoding)
    {
        switch (option?.Value)
        {
            case "CR":
                return new byte[] { 0x0D };
            case "LF":
                return new byte[] { 0x0A };
            case "CRLF":
                return new byte[] { 0x0D, 0x0A };
            case "Custom":
                return string.IsNullOrEmpty(custom) ? Array.Empty<byte>() : encoding.GetBytes(custom);
            default:
                return Array.Empty<byte>();
        }
    }

    private bool HasAppendBytes()
    {
        return SelectedAppendOption?.Value switch
        {
            "CR" => true,
            "LF" => true,
            "CRLF" => true,
            "Custom" => !string.IsNullOrEmpty(CustomAppendText),
            _ => false
        };
    }

    private static byte[] Combine(byte[] first, byte[] second)
    {
        if (first.Length == 0)
        {
            return second;
        }

        if (second.Length == 0)
        {
            return first;
        }

        var buffer = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, buffer, 0, first.Length);
        Buffer.BlockCopy(second, 0, buffer, first.Length, second.Length);
        return buffer;
    }

    public void Dispose()
    {
        _flushTimer.Stop();
        _serialService.RawDataReceived -= OnRawDataReceived;

        if (_selectedDevice != null)
        {
            _selectedDevice.PropertyChanged -= OnSelectedDevicePropertyChanged;
            _selectedDevice.ParserSettings.PropertyChanged -= OnParserSettingsPropertyChanged;
        }
    }
}
