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

namespace HHTiming.Blancpain
{
    public partial class StintSummaryControl :
        UserControl,
        IWorksheetControlInternal,
        IUIUpdateControl
    {

        private Guid _controlID = Guid.NewGuid();

        private string _carNumber = null;
        private eCarStatus _carStatus = eCarStatus.InPitStop;
        private Color _carColor = Color.Empty;

        private string _driverName = "";
        private double _driverTotalTime = 0;

        private double _sessionTime = 0;
        private double _sessionEndTime = double.MaxValue;

        private double _previousStintTimes = 0;
        private double _stintStartTime = 0;
        private double _previousStintStartTime = 0;
        private double _stintTime = 0;
        private double _stintTimeUpdated = 0;

        private double _projectedLapTime = 0;
        private double _averageLapTime = 0;

        private int _stintNumber = 0;

        private bool _boxNow = false;
        private double _pitWindowOpenTime = double.MaxValue;

        private const string LongTimeFormat = @"h\:mm\:ss";

        public StintSummaryControl()
        {
            InitializeComponent();

            Dock = DockStyle.Fill;

            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.UserPaint, true);

            tb_CarNumber.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(CarNumber), true, DataSourceUpdateMode.OnPropertyChanged));
            tb_InLapTime.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(InLapTime), true, DataSourceUpdateMode.OnPropertyChanged));
            tb_StintTime.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(MaxStintLength), true, DataSourceUpdateMode.OnPropertyChanged));

            cb_MergeStints.Checked = false;
            cb_MergeStints.DataBindings.Add(nameof(CheckBoxItem.CheckedBindable), this, nameof(MergeStints), true, DataSourceUpdateMode.OnPropertyChanged);

            Name = "Stint Summary";
        }

        public StintSummaryControl(string carNumber) : this()
        {
            _carNumber = carNumber;
            lbl_CarNumber.Text = carNumber;
        }

        public string CarNumber
        {
            get
            {
                return _carNumber;
            }
            set
            {
                if (_carNumber == value) return;


                _driverTotalTime = 0;
                _previousStintTimes = 0;
                _previousStintStartTime = 0;
                _stintStartTime = 0;
                _stintTimeUpdated = 0;

                _projectedLapTime = 0;
                _averageLapTime = 0;

                _stintNumber = 0;

                _carNumber = value;
                lbl_CarNumber.Text = _carNumber;
                ReinitializationFlag = true;
            }
        }

        public double MaxStintLength { get; set; } = 65;
        public double MaxContinuousDrivingTime { get; set; } = 195;
        public double MaxTotalDrivingTime { get; set; } = 840;
        public int InLapTime { get; set; } = 180;


        private bool _mergeStints;
        public bool MergeStints
        {
            get
            {
                return _mergeStints;
            }
            set
            {
                if (_mergeStints == value) return;

                _mergeStints = value;
                lbl_StintMerged.Visible = _mergeStints;

            }
        }
        public string DriverName
        {
            get
            {
                return _driverName;
            }
            set
            {
                if (value != _driverName && value != Globals.IGNORE_FIELD_STRING && value != "")
                {
                    _driverName = value;
                    lbl_DriverName.Text = value;

                    _previousStintTimes = 0;
                }
            }
        }

        #region IWorksheetControlInternal

        public bool CanBeSavedInLayout
        {
            get
            {
                return true;
            }
        }

        public Guid ControlID
        {
            get
            {
                return _controlID;
            }
        }

        public bool IsAddedToProject
        {
            get
            {
                return false;
            }

            set
            {

            }
        }

        public bool RenameAllowed
        {
            get
            {
                return true;
            }
        }

        public Control WorksheetControl
        {
            get
            {
                return this;
            }
        }

        public Icon WorksheetIcon
        {
            get
            {
                return null;
            }
        }

        public string WorksheetName
        {
            get
            {
                return Name;
            }

            set
            {
                if (value != Name)
                {
                    Name = value;
                    WorksheetNameChanged?.Invoke(this, value);
                }
            }
        }

        public event AddNewWorksheetEventHandler AddNewWorksheet;
        public event RequestCloseWorksheetEventHandler RequestCloseWorksheet;
        public event WorksheetNameChangedEventHandler WorksheetNameChanged;

        public bool CloseWorksheet()
        {
            return true;
        }

        public HHRibbonBar[] GetRibbonBars()
        {
            return null;
        }

        public RibbonBar[] GetRibbonBar()
        {
            return new RibbonBar[] { ribbonBar1 };
        }

        public IProjectObject GetWorksheetProjectControl()
        {
            return null;
        }

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
                    case "MaxTotalDrivingTime":
                        MaxTotalDrivingTime = double.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                        break;
                    case "InLapTime":
                        InLapTime = int.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                        break;
                    case "MaxStintLength":
                        MaxStintLength = int.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                        break;

                }
            }
        }

        public void SaveToXML(XmlElement parentXMLElement)
        {
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "CarNumber", CarNumber, parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "CarColor", _carColor.ToArgb().ToString(CultureInfo.InvariantCulture), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "MaxContinuousDrivingTime", MaxContinuousDrivingTime.ToString(CultureInfo.InvariantCulture), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "MaxTotalDrivingTime", MaxTotalDrivingTime.ToString(CultureInfo.InvariantCulture), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "InLapTime", InLapTime.ToString(CultureInfo.InvariantCulture), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "MaxStintLength", MaxStintLength.ToString(CultureInfo.InvariantCulture), parentXMLElement);
        }

        #endregion

        #region IUIUpdateControl

        public bool ReinitializationFlag { get; set; }

        public bool RequiresPaint
        {
            get
            {
                return true;
            }
        }

        public bool RunsInUIThread
        {
            get
            {
                return true;
            }
        }

        public bool UseBulkInitialization
        {
            get
            {
                return false;
            }
        }

        public List<IUIUpdateMessage> BroadcastUIUpdateMessages()
        {
            return null;
        }

        public List<IUIDbMessage> GetDatabaseMessages()
        {
            return null;
        }

        public DatabaseRequest[] GetDatabaseRequests()
        {
            return new DatabaseRequest[] { new DatabaseRequest(eDatabaseRequestType.AllStintsAllCars, new string[] { }, this.ControlID) };
        }

        public List<IUIUpdateMessage> GetInitializationMessages(Guid aTargetControlID)
        {
            return null;
        }

        public void PaintControl(SessionStatusUIUpdateMessage aSessionUIUpdateMessage, bool aFlashFlag)
        {
            _sessionTime = aSessionUIUpdateMessage.SessionTime;

            if (_sessionTime > 0 && _sessionTime != double.MaxValue)
            {
                if (_carStatus == eCarStatus.OnTrackRunning || _carStatus == eCarStatus.PitOut)
                {
                    double stintTime = MergeStints ? _sessionTime - _previousStintStartTime : _sessionTime - _stintStartTime;
                    double remainingTime = MaxStintLength * 60 - stintTime;
                    string remainingTimeString = SecondsToTimeString(remainingTime, LongTimeFormat);

                    lbl_StintTime.Text = SecondsToTimeString(stintTime, LongTimeFormat);
                    lbl_TimeAtEnd.Text = SecondsToTimeString((MergeStints ? _previousStintStartTime : _stintStartTime) + MaxStintLength * 60, LongTimeFormat);

                    if (remainingTime > 0)
                    {
                        SetBackgroundColor(_carColor);
                        lbl_StintTimeRemaining.Text = remainingTimeString;
                    }
                    else
                    {
                        if (aFlashFlag)
                        {
                            SetBackgroundColor(Color.Red);
                        }
                        else
                        {
                            SetBackgroundColor(Color.Black);
                        }

                        lbl_StintTimeRemaining.Text = "+" + remainingTimeString;
                    }

                    if (_stintTimeUpdated != double.MaxValue && _stintTime != double.MaxValue)
                    {
                        double drivingTime = _previousStintTimes + stintTime;
                        lbl_ContinuousTimeRemaining.Text = SecondsToTimeString(Math.Max(MaxContinuousDrivingTime * 60 - drivingTime, 0), LongTimeFormat);

                        lbl_TotalTimeRemaining.Text = SecondsToTimeString(Math.Max(MaxTotalDrivingTime * 60 - (_driverTotalTime + aSessionUIUpdateMessage.SessionTime - _stintTimeUpdated), 0), LongTimeFormat);

                        double projectedLapTime = _projectedLapTime;
                        if (projectedLapTime == 0) projectedLapTime = _averageLapTime;

                        double thisLap = _stintTime + InLapTime;
                        lbl_BoxThisLapTime.Text = SecondsToTimeString(thisLap, LongTimeFormat);

                        if (_projectedLapTime != 0)
                        {
                            double nextLap = thisLap + projectedLapTime;

                            lbl_BoxNextLapTime.Text = SecondsToTimeString(nextLap, LongTimeFormat);

                            double lapsRemaining = (MaxStintLength * 60 - _stintTime - projectedLapTime) / _averageLapTime;
                            lbl_LapsRemaining.Text = lapsRemaining.ToString("F1");

                            if (lapsRemaining != 0 && lapsRemaining < 1) _boxNow = true;
                            else _boxNow = false;
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
                        lbl_TotalTimeRemaining.Text = "-";

                        lbl_BoxThisLapTime.Text = "-";
                        lbl_BoxNextLapTime.Text = "-";
                        lbl_LapsRemaining.Text = "-";
                    }

                    if (_boxNow || _sessionTime > _pitWindowOpenTime)
                    {
                        tableLayoutPanel1.SetRowSpan(lbl_PitWindowHeading, 2);
                        lbl_PitWindowContent.Visible = false;

                        if (_boxNow)
                        {
                            if (aFlashFlag)
                            {
                                pnl_PitWindow.BackColor = Color.Red;
                                pnl_PitWindow.ForeColor = Color.Black;
                            }
                            else
                            {
                                pnl_PitWindow.BackColor = Color.Black;
                                pnl_PitWindow.ForeColor = Color.Red;
                            }

                            lbl_PitWindowHeading.Text = "BOX THIS LAP";
                        }
                        else
                        {
                            pnl_PitWindow.BackColor = Color.Green;
                            pnl_PitWindow.ForeColor = Color.White;

                            lbl_PitWindowHeading.Text = "PIT WINDOW OPEN";
                        }
                    }
                    else
                    {
                        pnl_PitWindow.BackColor = default(Color);
                        pnl_PitWindow.ForeColor = default(Color);

                        tableLayoutPanel1.SetRowSpan(lbl_PitWindowHeading, 1);
                        lbl_PitWindowContent.Visible = true;

                        lbl_PitWindowHeading.Text = "Minimum Stint Time (No Extra Stop)";
                        lbl_PitWindowContent.Text = SecondsToTimeString(_pitWindowOpenTime - (MergeStints ? _previousStintStartTime : _stintStartTime), LongTimeFormat);

                    }
                }
                else
                {
                    SetBackgroundColor(Color.LightGray);

                    pnl_PitWindow.BackColor = default(Color);
                    pnl_PitWindow.ForeColor = default(Color);

                    tableLayoutPanel1.SetRowSpan(lbl_PitWindowHeading, 1);
                    lbl_PitWindowContent.Visible = true;

                    lbl_StintTime.Text = "-";
                    lbl_StintTimeRemaining.Text = "-";
                    lbl_TimeAtEnd.Text = "-";

                    lbl_BoxNextLapTime.Text = "-";
                    lbl_BoxThisLapTime.Text = "-";

                    lbl_LapsRemaining.Text = "-";

                    lbl_PitWindowHeading.Text = "Minimum Stint Time (No Extra Stop)";
                    lbl_PitWindowContent.Text = "-";
                }
            }
        }

        public void ReceiveUIUpdateMessage(IUIUpdateMessage anUpdateMessage)
        {
            if (anUpdateMessage is ResetUIUpdateMessage)
            {
                _carStatus = eCarStatus.OnTrackRunning;

                lbl_StintTime.Text = "0:00:00";
                lbl_DriverName.Text = "NO DRIVER NAME";
                lbl_StintTimeRemaining.Text = "0:00:00";

                _averageLapTime = 0;
                _driverName = "";
                _driverTotalTime = 0;
                _previousStintTimes = 0;
                _projectedLapTime = 0;
                _sessionTime = 0;
                _stintNumber = 0;
                _stintStartTime = 0;
                _stintTime = 0;
                _stintTimeUpdated = 0;
                _pitWindowOpenTime = double.MaxValue;

                cb_MergeStints.Visible = true;
                cb_MergeStints.Checked = false;
                lbl_StintMerged.Visible = false;
            }
            else if (anUpdateMessage is TrackOptionsUIUpdateMessage)
            {
                MaxTotalDrivingTime = ((TrackOptionsUIUpdateMessage)anUpdateMessage).MaxTotalDrivingTime;
                MaxContinuousDrivingTime = ((TrackOptionsUIUpdateMessage)anUpdateMessage).MaxContinuousDrivingTime;
            }
            else if (anUpdateMessage is UserDefinedSessionLengthUIUpdateMessage sessionMessage)
            {
                _sessionEndTime = sessionMessage.SessionLengthHours * 3600;
            }

            if (((BaseUIUpdateMessage)anUpdateMessage).ItemID != CarNumber) return;

            if (anUpdateMessage is CarUIUpdateMessage)
            {
                HandleCarUIUpdateMessage((CarUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is CarStatusUIUpdateMessage)
            {
                HandleCarStatusUIUpdateMessage((CarStatusUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is CurrentDriverUIUpdateMessage)
            {
                HandleCurrentDriverUIUpdateMessage((CurrentDriverUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is DriverOverrideUIUpdateMessage)
            {
                _previousStintTimes = 0;
                _stintNumber = 0;
            }
            else if (anUpdateMessage is PitstopUIUpdateMessage)
            {
                HandlePitStopUIUpdateMessage((PitstopUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is StintUIUpdateMessage)
            {
                HandleStintUIUpdateMessage((StintUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is LapUIUpdateMessage)
            {
                HandleLapUIUpdateMessage((LapUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is SectorUIUpdateMessage)
            {
                HandleSectorUIUpdateMessage((SectorUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is EstimatedTimeRemainingUIUpdateMessage)
            {
                HandleEstimatedTimeRemainingUIUpdateMessage((EstimatedTimeRemainingUIUpdateMessage)anUpdateMessage);
            }
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
            _driverTotalTime = 0;
        }

        public void HandleEstimatedTimeRemainingUIUpdateMessage(EstimatedTimeRemainingUIUpdateMessage aMessage)
        {
            if (aMessage.AlternateEstimatedRaceFixedStintLengths != null && aMessage.AlternateEstimatedRaceFixedStintLengths.EstimatedStints.Count > 0 &&
                aMessage.EstimatedRaceFixedStintLengths != null && aMessage.EstimatedRaceFixedStintLengths.EstimatedStints.Count > 0)
            {
                EstimatedStint firstForwardStint = aMessage.EstimatedRaceFixedStintLengths.EstimatedStints.First();
                EstimatedStint lastForwardStint = aMessage.EstimatedRaceFixedStintLengths.EstimatedStints.Last();
                EstimatedStint lastReverseStint = aMessage.AlternateEstimatedRaceFixedStintLengths.EstimatedStints.Last();

                double forwardOffset = lastForwardStint.EndTime - _sessionEndTime;
                double reverseOffset = lastReverseStint.EndTime - _sessionEndTime;

                // Pit window is difference between final stint start times adjusted for race end
                double pitWindow = lastForwardStint.StartTime - forwardOffset - (lastReverseStint.StartTime - reverseOffset);

                // Next pit window opens before the end of the current maximum stint
                _pitWindowOpenTime = firstForwardStint.EndTime - pitWindow - forwardOffset;
            }

            if (aMessage.EstimatedRaceFixedStintLengths != null && aMessage.EstimatedRaceFixedStintLengths.EstimatedLapTime != double.MaxValue)
            {
                _averageLapTime = aMessage.EstimatedRaceFixedStintLengths.EstimatedLapTime;
            }
        }

        public void HandleLapUIUpdateMessage(LapUIUpdateMessage message)
        {
            _stintTimeUpdated = message.ElapsedTime;
            _driverTotalTime = message.DrivingTimeforCurrentDriverAtEndOfLap;
        }

        public void HandlePitStopUIUpdateMessage(PitstopUIUpdateMessage aMessage)
        {
            if (aMessage.MessageType == PitstopUIUpdateMessage.PitStopMessageType.PitIn)
            {
                cb_MergeStints.SetChecked(false, eEventSource.Mouse);
                cb_MergeStints.Visible = false;
                lbl_StintMerged.Visible = false;
                itemContainer5.Refresh();

            }

            if (aMessage.MessageType == PitstopUIUpdateMessage.PitStopMessageType.NewStop)
            {
                lbl_StintMerged.Visible = false;
                cb_MergeStints.Visible = true;
                cb_MergeStints.SetChecked(false, eEventSource.Mouse);
                itemContainer5.Refresh();




                if (aMessage.PitOutSessionTime > _stintStartTime)
                {

                    _stintTime = 0;
                    _previousStintStartTime = _stintStartTime;
                    _stintStartTime = aMessage.PitOutSessionTime;
                    _stintTimeUpdated = aMessage.PitOutSessionTime;

                    DriverName = aMessage.OutDriverName;
                }

                _boxNow = false;
                _pitWindowOpenTime = double.MaxValue;
            }
        }

        public void HandleSectorUIUpdateMessage(SectorUIUpdateMessage aMessage)
        {
            DriverName = aMessage.DriverName;

            if (aMessage.ProjectedLapTime != double.MaxValue && aMessage.ProjectedLapTime != 0)
                _projectedLapTime = aMessage.ProjectedLapTime;
            else
                _projectedLapTime = 0;
        }

        public void HandleStintUIUpdateMessage(StintUIUpdateMessage aMessage)
        {
            if (aMessage.StintMessageType == StintUIUpdateMessage.StintMessageTypeEnum.CurrentStint)
            {
                DriverName = aMessage.DriverID;

                if (aMessage.DrivingTime != double.MaxValue)
                {
                    _stintStartTime = aMessage.StartTime;
                    _stintTime = aMessage.DrivingTime;
                    _stintTime = MergeStints ? _stintTime + _stintStartTime - _previousStintStartTime : _stintTime;
                    _stintTimeUpdated = aMessage.StartTime + aMessage.DrivingTime;
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
