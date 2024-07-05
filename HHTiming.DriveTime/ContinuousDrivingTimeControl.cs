using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using DevComponents.DotNetBar;
using HHDev.Core.NETStandard.Helpers;
using HHDev.Core.WinForms.Helpers;
using HHDev.ProjectFramework.Definitions;
using HHTiming.Core.Definitions.Enums;
using HHTiming.Core.Definitions.UIUpdate.Interfaces;
using HHTiming.Core.Definitions.UIUpdate.Database;
using HHTiming.Core.Definitions.UIUpdate.Implementations;
using HHTiming.Core.Definitions.UIUpdate.Implementations.Messages;
using HHTiming.Desktop.Definitions.PlugInFramework;
using HHTiming.Desktop.Definitions.Worksheet;
using HHTiming.WinFormsControls.Workbook;

namespace HHTiming.DriveTime
{
    public partial class ContinuousDrivingTimeControl :
        UserControl,
        IWorksheetControlInternal,
        IUIUpdateControl
    {
        private Guid _controlID = Guid.NewGuid();
        private string _carNumber = null;
        private eCarStatus _carStatus = eCarStatus.InPitStop;
        private Color _carColor = Color.Empty;
        private string _driverName = "";
        private double _sessionTime = 0;
        private double _sessionEndTime = double.MaxValue;
        private double _driverStartTime = 0;
        private double _projectedLapTime = 0;
        private double _averageLapTime = 0;
        private double _continuousDriveTime = 0;
        private int _stintNumber = 0;
        private double _previousStintTimes = 0;
        private double _previousPitStopTimes = 0;
        private double _stintStartTime = 0;
        private bool _boxNow = false;

        private const string LongTimeFormat = @"h\:mm\:ss";

        public ContinuousDrivingTimeControl()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

            tb_CarNumber.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(CarNumber), true, DataSourceUpdateMode.OnPropertyChanged));
            tb_InLapTime.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(InLapTime), true, DataSourceUpdateMode.OnPropertyChanged));
            tb_ContinuousDrivingTime.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(MaxContinuousDrivingTime), true, DataSourceUpdateMode.OnPropertyChanged));
            cb_IncludePitStops.Checked = false;
            cb_IncludePitStops.DataBindings.Add(nameof(CheckBoxItem.CheckedBindable), this, nameof(IncludePitStops), true, DataSourceUpdateMode.OnPropertyChanged);

            Name = "Continuous Driving Time";
        }

        public ContinuousDrivingTimeControl(string carNumber) : this()
        {
            CarNumber = carNumber;
        }

        private bool _includePitStops;
        public bool IncludePitStops
        {
            get => _includePitStops;
            set
            {
                if (_includePitStops == value) return;
                _includePitStops = value;
                // Notify property change if necessary
            }
        }

        public string CarNumber
        {
            get => _carNumber;
            set
            {
                if (_carNumber == value) return;
                _carNumber = value;
                lbl_CarNumber.Text = _carNumber;
                ResetStintData();
                ReinitializationFlag = true;
            }
        }

        public double MaxContinuousDrivingTime { get; set; } = 195;
        public int InLapTime { get; set; } = 180;

        public string DriverName
        {
            get => _driverName;
            set
            {
                if (value != _driverName && value != Globals.IGNORE_FIELD_STRING && value != "")
                {
                    _driverName = value;
                    lbl_DriverName.Text = value;
                }
            }
        }

        private void ResetStintData()
        {
            _previousStintTimes = 0;
            _previousPitStopTimes = 0;
            _stintStartTime = 0;
            _driverStartTime = 0;
            _projectedLapTime = 0;
            _averageLapTime = 0;
            _continuousDriveTime = 0;
            _stintNumber = 0;
        }

        #region IWorksheetControlInternal

        public bool CanBeSavedInLayout => true;

        public Guid ControlID => _controlID;

        public bool IsAddedToProject { get; set; } = false;

        public bool RenameAllowed => true;

        public Control WorksheetControl => this;

        public Icon WorksheetIcon => null;

        public string WorksheetName
        {
            get => Name;
            set
            {
                if (value != Name)
                {
                    Name = value;
                    WorksheetNameChanged?.Invoke(this, value);
                }
            }
        }

#pragma warning disable 0067
        public event AddNewWorksheetEventHandler AddNewWorksheet;
        public event RequestCloseWorksheetEventHandler RequestCloseWorksheet;
#pragma warning restore 0067
        public event WorksheetNameChangedEventHandler WorksheetNameChanged;

        public bool CloseWorksheet() => true;

        public HHRibbonBar[] GetRibbonBars() => null;

        public RibbonBar[] GetRibbonBar() => new RibbonBar[] { ribbonBar1 };

        public IProjectObject GetWorksheetProjectControl() => null;

        public void LoadFromXML(XmlElement parentXMLElement)
        {
            foreach (XmlElement elem in parentXMLElement.ChildNodes)
            {
                switch (elem.Name)
                {
                    case "CarNumber":
                        CarNumber = elem.InnerText;
                        break;
                    case "CarColor":
                        SetBackgroundColor(Color.FromArgb(int.Parse(elem.InnerText, CultureInfo.InvariantCulture)));
                        break;
                    case "MaxContinuousDrivingTime":
                        MaxContinuousDrivingTime = double.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                        break;
                    case "IncludePitStops":
                        IncludePitStops = bool.Parse(elem.InnerText);
                        break;
                    case "InLapTime":
                        InLapTime = int.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                        break;
                }
            }
        }

        public void SaveToXML(XmlElement parentXMLElement)
        {
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "CarNumber", CarNumber, parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "CarColor", _carColor.ToArgb().ToString(CultureInfo.InvariantCulture), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "MaxContinuousDrivingTime", MaxContinuousDrivingTime.ToString(CultureInfo.InvariantCulture), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "InLapTime", InLapTime.ToString(CultureInfo.InvariantCulture), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "IncludePitStops", IncludePitStops.ToString(), parentXMLElement);
        }

        #endregion

        #region IUIUpdateControl

        public bool ReinitializationFlag { get; set; }

        public bool RequiresPaint => true;

        public bool RunsInUIThread => true;

        public bool UseBulkInitialization => true;

        public List<IUIUpdateMessage> BroadcastUIUpdateMessages() => null;

        public List<IUIDbMessage> GetDatabaseMessages() => null;

        public DatabaseRequest[] GetDatabaseRequests() =>
            new DatabaseRequest[] { new DatabaseRequest(eDatabaseRequestType.AllStintsAllCars, new string[] { }, ControlID) };

        public List<IUIUpdateMessage> GetInitializationMessages(Guid aTargetControlID) => null;

        public void PaintControl(SessionStatusUIUpdateMessage aSessionUIUpdateMessage, bool aFlashFlag)
        {
            _sessionTime = aSessionUIUpdateMessage.SessionTime;

            if (_sessionTime > 0 && _sessionTime != double.MaxValue)
            {
                if (_carStatus == eCarStatus.OnTrackRunning || _carStatus == eCarStatus.PitOut)
                {
                    double stintTime = _sessionTime - _stintStartTime;
                    double continuousDrivingTime = _previousStintTimes + stintTime;

                    if (_includePitStops)
                    {
                        continuousDrivingTime += _previousPitStopTimes;
                    }

                    double remainingTime = MaxContinuousDrivingTime * 60 - continuousDrivingTime;

                    lbl_ContinuousDrivingTime.Text = SecondsToTimeString(continuousDrivingTime, LongTimeFormat);
                    lbl_TimeAtEnd.Text = SecondsToTimeString(_driverStartTime + MaxContinuousDrivingTime * 60 + (_includePitStops ? 0 : _previousPitStopTimes), LongTimeFormat);

                    SetBackgroundColor(remainingTime > 0 ? _carColor : (aFlashFlag ? Color.Red : Color.Black));

                    lbl_ContinuousTimeRemaining.Text = SecondsToTimeString(Math.Max(MaxContinuousDrivingTime * 60 - continuousDrivingTime, 0), LongTimeFormat);

                    if (_continuousDriveTime != double.MaxValue)
                    {
                        double continuousDriveTimeToUse = _continuousDriveTime + (_includePitStops ? _previousPitStopTimes : 0);
                        double projectedLapTime = _projectedLapTime == 0 ? _averageLapTime : _projectedLapTime;
                        double thisLap = continuousDriveTimeToUse + InLapTime;

                        lbl_BoxThisLapTime.Text = SecondsToTimeString(thisLap, LongTimeFormat);

                        if (projectedLapTime != 0)
                        {
                            double nextLap = thisLap + projectedLapTime;
                            lbl_BoxNextLapTime.Text = SecondsToTimeString(nextLap, LongTimeFormat);

                            double lapsRemaining = (MaxContinuousDrivingTime * 60 - continuousDriveTimeToUse - projectedLapTime) / _averageLapTime;
                            lbl_LapsRemaining.Text = lapsRemaining.ToString("F1");

                            _boxNow = lapsRemaining < 1;
                        }
                        else
                        {
                            lbl_BoxNextLapTime.Text = "-";
                            lbl_LapsRemaining.Text = "-";
                        }
                    }
                    else
                    {
                        lbl_ContinuousTimeRemaining.Text = "-";
                        lbl_BoxThisLapTime.Text = "-";
                        lbl_BoxNextLapTime.Text = "-";
                        lbl_LapsRemaining.Text = "-";
                    }

                    UpdatePitWindowDisplay(aFlashFlag);
                }
                else
                {
                    ResetUIForIdleState();
                }
            }
        }

        private void UpdatePitWindowDisplay(bool aFlashFlag)
        {
            if (_boxNow)
            {
                tableLayoutPanel1.SetRowSpan(lbl_PitWindowHeading, 2);
                lbl_PitWindowContent.Visible = false;
                pnl_PitWindow.BackColor = aFlashFlag ? Color.Red : Color.Black;
                pnl_PitWindow.ForeColor = aFlashFlag ? Color.Black : Color.Red;
                lbl_PitWindowHeading.Text = "BOX THIS LAP";
            }
            else
            {
                lbl_PitWindowHeading.Text = "";
                pnl_PitWindow.BackColor = default;
                pnl_PitWindow.ForeColor = default;
                tableLayoutPanel1.SetRowSpan(lbl_PitWindowHeading, 1);
                lbl_PitWindowContent.Visible = true;
            }
        }

        private void ResetUIForIdleState()
        {
            SetBackgroundColor(Color.LightGray);
            pnl_PitWindow.BackColor = default;
            pnl_PitWindow.ForeColor = default;
            tableLayoutPanel1.SetRowSpan(lbl_PitWindowHeading, 1);
            lbl_PitWindowContent.Visible = true;
            lbl_ContinuousDrivingTime.Text = "-";
            lbl_TimeAtEnd.Text = "-";
            lbl_BoxNextLapTime.Text = "-";
            lbl_BoxThisLapTime.Text = "-";
            lbl_LapsRemaining.Text = "-";
            lbl_PitWindowHeading.Text = "";
            lbl_PitWindowContent.Text = "";
        }

        public void ReceiveUIUpdateMessage(IUIUpdateMessage anUpdateMessage)
        {
            if (anUpdateMessage is BulkRefreshDataUIUpdateMessage bulkMessage)
            {
                foreach (IUIUpdateMessage item in bulkMessage.ListOfUIUpdateMessages)
                {
                    HandleUIUpdateMessage(item, false);
                }
            }
            else
            {
                HandleUIUpdateMessage(anUpdateMessage, true);
            }
        }

        private void HandleUIUpdateMessage(IUIUpdateMessage anUpdateMessage, bool anAllowRefresh)
        {
            if (anUpdateMessage is ResetUIUpdateMessage)
            {
                ResetUI();
            }
            else if (anUpdateMessage is UserDefinedSessionLengthUIUpdateMessage sessionMessage)
            {
                _sessionEndTime = sessionMessage.SessionLengthHours * 3600;
            }

            if (((BaseUIUpdateMessage)anUpdateMessage).ItemID != CarNumber) return;

            switch (anUpdateMessage)
            {
                case CarUIUpdateMessage carMessage:
                    HandleCarUIUpdateMessage(carMessage);
                    break;
                case CarStatusUIUpdateMessage statusMessage:
                    HandleCarStatusUIUpdateMessage(statusMessage);
                    break;
                case CurrentDriverUIUpdateMessage driverMessage:
                    HandleCurrentDriverUIUpdateMessage(driverMessage);
                    break;
                case DriverOverrideUIUpdateMessage _:
                    _stintNumber = 0;
                    break;
                case PitstopUIUpdateMessage pitstopMessage:
                    HandlePitStopUIUpdateMessage(pitstopMessage);
                    break;
                case StintUIUpdateMessage stintMessage:
                    HandleStintUIUpdateMessage(stintMessage);
                    break;
                case LapUIUpdateMessage lapMessage:
                    HandleLapUIUpdateMessage(lapMessage);
                    break;
                case SectorUIUpdateMessage sectorMessage:
                    HandleSectorUIUpdateMessage(sectorMessage);
                    break;
                case EstimatedTimeRemainingUIUpdateMessage estimatedMessage:
                    HandleEstimatedTimeRemainingUIUpdateMessage(estimatedMessage);
                    break;
            }
        }

        private void ResetUI()
        {
            _carStatus = eCarStatus.OnTrackRunning;
            lbl_ContinuousDrivingTime.Text = "0:00:00";
            lbl_DriverName.Text = "NO DRIVER NAME";
            _averageLapTime = 0;
            _driverName = "";
            _previousStintTimes = 0;
            _previousPitStopTimes = 0;
            _stintStartTime = 0;
            _projectedLapTime = 0;
            _sessionTime = 0;
            _stintNumber = 0;
            _driverStartTime = 0;
            _continuousDriveTime = 0;
        }

        #endregion

        public void HandleCarStatusUIUpdateMessage(CarStatusUIUpdateMessage aMessage)
        {
            _carStatus = aMessage.CarStatus;
        }

        public void HandleCarUIUpdateMessage(CarUIUpdateMessage aMessage)
        {
            _carColor = aMessage.CarColor;
            SetBackgroundColor(_carColor);
        }

        public void HandleCurrentDriverUIUpdateMessage(CurrentDriverUIUpdateMessage aMessage)
        {
            DriverName = aMessage.DriverName;
        }

        public void HandleEstimatedTimeRemainingUIUpdateMessage(EstimatedTimeRemainingUIUpdateMessage aMessage)
        {
            if (aMessage.CurrentEstimatedRace != null && aMessage.CurrentEstimatedRace.EstimatedLapTime != double.MaxValue)
            {
                _averageLapTime = aMessage.CurrentEstimatedRace.EstimatedLapTime;
            }
        }

        public void HandlePitStopUIUpdateMessage(PitstopUIUpdateMessage aMessage)
        {
            if (aMessage.MessageType == PitstopUIUpdateMessage.PitStopMessageType.PitIn)
            {
                itemContainer5.Refresh();
            }

            if (aMessage.MessageType == PitstopUIUpdateMessage.PitStopMessageType.NewStop)
            {
                itemContainer5.Refresh();

                if (aMessage.PitOutSessionTime > _driverStartTime && aMessage.InDriverName != aMessage.OutDriverName)
                {
                    _continuousDriveTime = 0;
                    _stintStartTime = aMessage.PitOutSessionTime;
                    _driverStartTime = aMessage.PitOutSessionTime;
                    DriverName = aMessage.OutDriverName;
                    _previousPitStopTimes = 0;
                }
                else if (aMessage.PitOutSessionTime > _driverStartTime)
                {
                    _stintStartTime = aMessage.PitOutSessionTime;
                    _previousPitStopTimes += aMessage.StopTime;
                }

                _boxNow = false;
            }
        }

        public void HandleLapUIUpdateMessage(LapUIUpdateMessage message)
        {
            double stintTime = message.ElapsedTime - _stintStartTime;
            _continuousDriveTime = _previousStintTimes + stintTime;
        }

        public void HandleSectorUIUpdateMessage(SectorUIUpdateMessage aMessage)
        {
            DriverName = aMessage.DriverName;
            _projectedLapTime = aMessage.ProjectedLapTime != double.MaxValue && aMessage.ProjectedLapTime != 0 ? aMessage.ProjectedLapTime : 0;
        }

        public void HandleStintUIUpdateMessage(StintUIUpdateMessage aMessage)
        {
            if (aMessage.StintMessageType == StintUIUpdateMessage.StintMessageTypeEnum.CurrentStint)
            {
                DriverName = aMessage.DriverID;

                if (aMessage.DrivingTime != double.MaxValue)
                {
                    _driverStartTime = aMessage.StartTime;
                }
            }
            else if (aMessage.StintMessageType == StintUIUpdateMessage.StintMessageTypeEnum.EndOfStint)
            {
                if (aMessage.DrivingTime != double.MaxValue && _stintNumber == aMessage.StintNumber)
                {
                    DriverName = aMessage.DriverID;
                    _previousStintTimes += aMessage.DrivingTime;
                    _stintNumber++;
                }
            }
        }

        public void SetBackgroundColor(Color carColor)
        {
            tableLayoutPanel1.BackColor = carColor;
            tableLayoutPanel1.ForeColor = ColorFunctionsWinForms.GetTextColor(carColor);
        }

        public string SecondsToTimeString(double aTime, string format)
        {
            if (double.IsNaN(aTime) || double.IsInfinity(aTime) || aTime == double.MaxValue || aTime == double.MinValue)
                return "-";

            var ts = TimeSpan.FromSeconds(aTime);
            return ts.ToString(format);
        }
    }
}