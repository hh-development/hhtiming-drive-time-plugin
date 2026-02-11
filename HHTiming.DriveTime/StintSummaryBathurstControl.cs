using DevComponents.DotNetBar;
using HHDev.Core.NETStandard.Helpers;
using HHDev.Core.WinForms.Helpers;
using HHDev.ProjectFramework.Definitions;
using HHTiming.Core.Definitions.Enums;
using HHTiming.Core.Definitions.UIUpdate.Database;
using HHTiming.Core.Definitions.UIUpdate.Implementations;
using HHTiming.Core.Definitions.UIUpdate.Implementations.Messages;
using HHTiming.Core.Definitions.UIUpdate.Interfaces;
using HHTiming.Desktop.Definitions.PlugInFramework;
using HHTiming.Desktop.Definitions.Worksheet;
using HHTiming.WinFormsControls.Workbook;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace HHTiming.DriveTime
{
    public partial class StintSummaryBathurstControl :
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
        private double _driverStartTime = 0;

        private double _sessionTime = 0;
        private double _sessionEndTime = double.MaxValue;

        private double _previousStintTimes = 0;
        private double _stintStartTime = 0;
        private double _previousStintStartTime = 0;
        private double _stintTime = 0;
        private double _stintTimeUpdated = 0;
        private double _previousPitStopTimes = 0;

        private double _projectedLapTime = 0;
        private double _averageLapTime = 0;
        private double _inLapRatio = 1.0;

        private int _stintNumber = 0;

        private bool _boxNow = false;
        private double _pitWindowOpenTime = double.MaxValue;
        private int _maxLapsInStint = int.MaxValue;
        private double _estimatedLapTimeInMaxStint = double.MaxValue;
        private double _estimatedCurrentStintEndTime = double.MaxValue;
        private int _lapsRemaining = int.MaxValue;

        private const string LongTimeFormat = @"h\:mm\:ss";

        public StintSummaryBathurstControl()
        {
            InitializeComponent();

            Dock = DockStyle.Fill;

            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.UserPaint, true);

            tb_CarNumber.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(CarNumber), true, DataSourceUpdateMode.OnPropertyChanged));
            Name = "Stint Summary Bathurst";
        }

        public StintSummaryBathurstControl(string carNumber) : this()
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
                _driverStartTime = 0;
                _previousStintTimes = 0;
                _previousStintStartTime = 0;
                _stintStartTime = 0;
                _stintTimeUpdated = 0;
                _previousPitStopTimes = 0;

                _projectedLapTime = 0;
                _averageLapTime = 0;

                _stintNumber = 0;

                _carNumber = value;
                lbl_CarNumber.Text = _carNumber;
                ReinitializationFlag = true;
            }
        }

        public double MaxStintLength { get; set; } = 60;

        public double RealMaxStintLength
        {
            get { return MaxStintLength; }
        }
        public double MaxContinuousDrivingTime { get; set; } = 195;
        public double MaxTotalDrivingTime { get; set; } = 840;
        public int InLapTime { get; set; } = 180;



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

#pragma warning disable 0067
        public event AddNewWorksheetEventHandler AddNewWorksheet;
        public event RequestCloseWorksheetEventHandler RequestCloseWorksheet;
#pragma warning restore 0067
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
                }
            }
        }

        public void SaveToXML(XmlElement parentXMLElement)
        {
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "CarNumber", CarNumber, parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "CarColor", _carColor.ToArgb().ToString(CultureInfo.InvariantCulture), parentXMLElement);
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
                    double stintTime = _sessionTime - _stintStartTime;
                    double maxStintSeconds = RealMaxStintLength * 60;
                    double lapTimeForLimit = _averageLapTime;
                    if (_estimatedLapTimeInMaxStint != double.MaxValue && _estimatedLapTimeInMaxStint > 0)
                        lapTimeForLimit = _estimatedLapTimeInMaxStint;
                    if (_maxLapsInStint != int.MaxValue &&
                        _maxLapsInStint > 0 &&
                        lapTimeForLimit > 0)
                        maxStintSeconds = _maxLapsInStint * lapTimeForLimit;

                    double remainingTime = maxStintSeconds - stintTime;
                    if (_estimatedCurrentStintEndTime != double.MaxValue)
                        remainingTime = _estimatedCurrentStintEndTime - _sessionTime;
                    lbl_StintTime.Text = SecondsToTimeString(stintTime, LongTimeFormat);
                    if (remainingTime > 0)
                    {
                        SetBackgroundColor(_carColor);
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
                    }

                    if (_stintTimeUpdated != double.MaxValue && _stintTime != double.MaxValue)
                    {
                        double drivingTime = _sessionTime - _driverStartTime;
                        double remainingContinuous = Math.Max(MaxContinuousDrivingTime * 60 - drivingTime, 0);
                        lbl_ContinuousTimeRemaining.Text = SecondsToTimeString(remainingContinuous, LongTimeFormat);

                        double remainingTotal = Math.Max(MaxTotalDrivingTime * 60 - (_driverTotalTime + aSessionUIUpdateMessage.SessionTime - _stintTimeUpdated), 0);
                        lbl_TotalTimeRemaining.Text = SecondsToTimeString(remainingTotal, LongTimeFormat);

                        double projectedLapTime = _projectedLapTime;
                        if (projectedLapTime == 0) projectedLapTime = _averageLapTime;

                        double stintTimeToUse = _stintTime + _previousPitStopTimes;
                        double thisLap = _stintTime + InLapTime;
                        lbl_BoxThisLapTime.Text = SecondsToTimeString(thisLap, LongTimeFormat);

                        if (projectedLapTime != 0)
                        {
                            double nextLap = thisLap + projectedLapTime;

                            lbl_BoxNextLapTime.Text = SecondsToTimeString(nextLap, LongTimeFormat);

                            _boxNow = (nextLap - stintTime) > remainingContinuous ||
                                      (nextLap - stintTime) > remainingTotal;
                        }
                        else
                        {
                            lbl_BoxNextLapTime.Text = "-";
                        }
                    }
                    else
                    {
                        lbl_ContinuousTimeRemaining.Text = "-";
                        lbl_TotalTimeRemaining.Text = "-";

                        lbl_BoxThisLapTime.Text = "-";
                        lbl_BoxNextLapTime.Text = "-";
                    }

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
                        pnl_PitWindow.BackColor = default(Color);
                        pnl_PitWindow.ForeColor = default(Color);

                        lbl_PitWindowHeading.Text = "";
                    }
                }
                else
                {
                    SetBackgroundColor(Color.LightGray);

                    pnl_PitWindow.BackColor = default(Color);
                    pnl_PitWindow.ForeColor = default(Color);

                    lbl_StintTime.Text = "-";

                    lbl_BoxNextLapTime.Text = "-";
                    lbl_BoxThisLapTime.Text = "-";

                    lbl_PitWindowHeading.Text = "";
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

                _averageLapTime = 0;
                _driverName = "";
                _driverTotalTime = 0;
                _previousStintTimes = 0;
                _projectedLapTime = 0;
                _sessionTime = 0;
                _stintNumber = 0;
                _driverStartTime = 0;
                _stintStartTime = 0;
                _stintTime = 0;
                _stintTimeUpdated = 0;
                _pitWindowOpenTime = double.MaxValue;
                _previousPitStopTimes = 0;

                lbl_StintMerged.Visible = false;
            }
            else if (anUpdateMessage is TrackOptionsUIUpdateMessage)
            {
                var trackOptions = (TrackOptionsUIUpdateMessage)anUpdateMessage;
                MaxTotalDrivingTime = trackOptions.MaxTotalDrivingTime;
                MaxContinuousDrivingTime = trackOptions.MaxContinuousDrivingTime;
                _inLapRatio = trackOptions.InLapRatio;
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
                _previousPitStopTimes = 0;
                _stintNumber = 0;
                _driverStartTime = 0;
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
            if (aMessage == null)
                return;

            var currentEstimatedRace = aMessage.CurrentEstimatedRace;
            var alternateEstimatedRace = aMessage.AlternateEstimatedRace;

            if (currentEstimatedRace != null)
                _maxLapsInStint = currentEstimatedRace.EstimatedNumberOfLapsInACompleteStint;

            _estimatedLapTimeInMaxStint = aMessage.EstimatedLapTimeInMaxStint;
            _estimatedCurrentStintEndTime = double.MaxValue;
            if (aMessage.AvgLapTime != double.MaxValue && aMessage.AvgLapTime > 0 && _inLapRatio > 0)
                InLapTime = (int)Math.Round(aMessage.AvgLapTime * _inLapRatio, MidpointRounding.AwayFromZero);
            if (currentEstimatedRace != null &&
                currentEstimatedRace.EstimatedNumberOfLapsInCurrentStint != double.MaxValue)
                _lapsRemaining = currentEstimatedRace.EstimatedNumberOfLapsInCurrentStint;
            if (currentEstimatedRace?.EstimatedStints != null && currentEstimatedRace.EstimatedStints.Count > 0)
                _estimatedCurrentStintEndTime = currentEstimatedRace.EstimatedStints.First().EndTime;

            if (alternateEstimatedRace?.EstimatedStints != null && alternateEstimatedRace.EstimatedStints.Count > 0 &&
                currentEstimatedRace?.EstimatedStints != null && currentEstimatedRace.EstimatedStints.Count > 0)
            {
                EstimatedStint firstForwardStint = currentEstimatedRace.EstimatedStints.First();
                EstimatedStint lastForwardStint = currentEstimatedRace.EstimatedStints.Last();
                EstimatedStint lastReverseStint = alternateEstimatedRace.EstimatedStints.Last();

                double forwardOffset = lastForwardStint.EndTime - _sessionEndTime;
                double reverseOffset = lastReverseStint.EndTime - _sessionEndTime;

                // Pit window is difference between final stint start times adjusted for race end
                double pitWindow = lastForwardStint.StartTime - forwardOffset - (lastReverseStint.StartTime - reverseOffset);
                // Next pit window opens before the end of the current maximum stint
                _pitWindowOpenTime = firstForwardStint.EndTime - pitWindow - forwardOffset;
            }

            if (currentEstimatedRace != null && currentEstimatedRace.EstimatedLapTime != double.MaxValue)
            {
                _averageLapTime = currentEstimatedRace.EstimatedLapTime;
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
                lbl_StintMerged.Visible = false;
                itemContainer5.Refresh();

            }

            if (aMessage.MessageType == PitstopUIUpdateMessage.PitStopMessageType.NewStop)
            {
                lbl_StintMerged.Visible = false;
                itemContainer5.Refresh();




                if (aMessage.PitOutSessionTime > _stintStartTime)
                {

                    _stintTime = 0;
                    _previousStintStartTime = _stintStartTime;
                    _stintStartTime = aMessage.PitOutSessionTime;
                    _stintTimeUpdated = aMessage.PitOutSessionTime;

                    DriverName = aMessage.OutDriverName;

                    if (aMessage.InDriverName != aMessage.OutDriverName)
                    {
                        _previousPitStopTimes = 0;
                        _driverStartTime = aMessage.PitOutSessionTime;
                    }
                    else
                        _previousPitStopTimes += aMessage.StopTime;
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
