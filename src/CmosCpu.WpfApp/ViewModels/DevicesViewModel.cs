using CmosCpu.Core;

namespace CmosCpu.WpfApp.ViewModels;

public class DevicesViewModel : ViewModelBase
{
    private int _timerCounter;
    private byte _timerControl;
    private byte _timerStatus;
    private bool _timerRunning;
    private bool _timerIrqEnabled;
    private int _rtcSecond;
    private int _rtcMinute;
    private int _rtcHour;
    private int _rtcDay;
    private int _rtcMonth;
    private int _rtcYear;

    public int TimerCounter { get => _timerCounter; set => SetProperty(ref _timerCounter, value); }
    public byte TimerControl { get => _timerControl; set => SetProperty(ref _timerControl, value); }
    public byte TimerStatus { get => _timerStatus; set => SetProperty(ref _timerStatus, value); }
    public bool TimerRunning { get => _timerRunning; set => SetProperty(ref _timerRunning, value); }
    public bool TimerIrqEnabled { get => _timerIrqEnabled; set => SetProperty(ref _timerIrqEnabled, value); }
    public int RtcSecond { get => _rtcSecond; set => SetProperty(ref _rtcSecond, value); }
    public int RtcMinute { get => _rtcMinute; set => SetProperty(ref _rtcMinute, value); }
    public int RtcHour { get => _rtcHour; set => SetProperty(ref _rtcHour, value); }
    public int RtcDay { get => _rtcDay; set => SetProperty(ref _rtcDay, value); }
    public int RtcMonth { get => _rtcMonth; set => SetProperty(ref _rtcMonth, value); }
    public int RtcYear { get => _rtcYear; set => SetProperty(ref _rtcYear, value); }

    public void Update(SimulatorSnapshot snap)
    {
        if (snap.Timer != null)
        {
            TimerCounter = snap.Timer.Counter;
            TimerControl = snap.Timer.Control;
            TimerStatus = snap.Timer.Status;
            TimerRunning = snap.Timer.Running;
            TimerIrqEnabled = snap.Timer.IrqEnabled;
        }
    }
}
