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
    public partial class CumulativeDrivingTimeControl :
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

        private double _driverTotalTimeUpdated = 0;

        private double _sessionTime = 0;
        private double _sessionEndTime = double.MaxValue;

        private double _driverStartTime = 0;

        private double _projectedLapTime = 0;
        private double _averageLapTime = 0;

        //private int _stintNumber = 0;

        private bool _boxNow = false;

        private const string LongTimeFormat = @"h\:mm\:ss";

        public CumulativeDrivingTimeControl()
        {
            InitializeComponent();

            Dock = DockStyle.Fill;

            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.UserPaint, true);

            tb_CarNumber.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(CarNumber), true, DataSourceUpdateMode.OnPropertyChanged));
            tb_InLapTime.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(InLapTime), true, DataSourceUpdateMode.OnPropertyChanged));
            tb_MinimumDrivingTime.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(MinimumDrivingTime), true, DataSourceUpdateMode.OnPropertyChanged));
            tb_MaximumDrivingTime.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(MaximumDrivingTime), true, DataSourceUpdateMode.OnPropertyChanged));


            Name = "Cumulative Driving Time";
        }

        public CumulativeDrivingTimeControl(string carNumber) : this()
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

                _projectedLapTime = 0;
                _averageLapTime = 0;

                //_stintNumber = 0;

                _carNumber = value;
                lbl_CarNumber.Text = _carNumber;
                ReinitializationFlag = true;
            }
        }

        public double MinimumDrivingTime { get; set; } = 180;
        public double MaximumDrivingTime { get; set; } = 340;

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
                    case "MinimumDrivingTime":
                        MinimumDrivingTime = double.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                        break;
                    case "MaximumDrivingTime":
                        MaximumDrivingTime = double.Parse(elem.InnerText, CultureInfo.InvariantCulture);
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
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "MinimumDrivingTime", MinimumDrivingTime.ToString(CultureInfo.InvariantCulture), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "MaximumDrivingTime", MaximumDrivingTime.ToString(CultureInfo.InvariantCulture), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "InLapTime", InLapTime.ToString(CultureInfo.InvariantCulture), parentXMLElement);
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
                return true;
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
                    double remainingMinimumTime = MinimumDrivingTime * 60 - (_driverTotalTime + aSessionUIUpdateMessage.SessionTime - _driverTotalTimeUpdated) +1;
                    double remainingMaximumTime = MaximumDrivingTime * 60 - (_driverTotalTime + aSessionUIUpdateMessage.SessionTime - _driverTotalTimeUpdated) +1;

                    lbl_DrivingTime.Text = SecondsToTimeString(_driverTotalTime + aSessionUIUpdateMessage.SessionTime - _driverTotalTimeUpdated, LongTimeFormat);
                    //lbl_TimeAtEnd.Text = SecondsToTimeString(_driverStartTime + MaxContinuousDrivingTime * 60, LongTimeFormat);

                    if (remainingMaximumTime > 0)
                    {
                        SetBackgroundColor(_carColor);
                        //lbl_ContinuousDrivingTimeRemaining.Text = remainingTimeString;
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

                        //lbl_ContinuousDrivingTimeRemaining.Text = "+" + remainingTimeString;
                    }

                    if (_driverTotalTime != double.MaxValue)
                    {

                        lbl_MinimumDrivingTimeRemaining.Text = SecondsToTimeString(Math.Max(remainingMinimumTime, 0), LongTimeFormat);
                        lbl_MaximumDrivingTimeRemaining.Text = SecondsToTimeString(Math.Max(remainingMaximumTime, 0), LongTimeFormat);


                        double projectedLapTime = _projectedLapTime;
                        if (projectedLapTime == 0) projectedLapTime = _averageLapTime;

                        double thisLap = _driverTotalTime + InLapTime;
                        lbl_BoxThisLapTime.Text = SecondsToTimeString(thisLap, LongTimeFormat);

                        if (_projectedLapTime != 0)
                        {
                            double nextLap = thisLap + projectedLapTime;

                            lbl_BoxNextLapTime.Text = SecondsToTimeString(nextLap, LongTimeFormat);

                            double lapsRemaining = (MinimumDrivingTime * 60 - _driverTotalTime - projectedLapTime) / _averageLapTime;
                            if (lapsRemaining < 1)
                                lapsRemaining = (MaximumDrivingTime * 60 - _driverTotalTime - projectedLapTime) / _averageLapTime;
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
                        lbl_MinimumDrivingTimeRemaining.Text = "-";

                        lbl_BoxThisLapTime.Text = "-";
                        lbl_BoxNextLapTime.Text = "-";
                        lbl_LapsRemaining.Text = "-";
                    }

                    if (_boxNow)
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
                    }
                    else
                    {
                        lbl_PitWindowHeading.Text = "";
                        pnl_PitWindow.BackColor = default(Color);
                        pnl_PitWindow.ForeColor = default(Color);

                        tableLayoutPanel1.SetRowSpan(lbl_PitWindowHeading, 1);
                        lbl_PitWindowContent.Visible = true;


                    }
                }
                else
                {
                    SetBackgroundColor(Color.LightGray);

                    pnl_PitWindow.BackColor = default(Color);
                    pnl_PitWindow.ForeColor = default(Color);

                    tableLayoutPanel1.SetRowSpan(lbl_PitWindowHeading, 1);
                    lbl_PitWindowContent.Visible = true;

                    lbl_DrivingTime.Text = "-";
                    //lbl_ContinuousDrivingTimeRemaining.Text = "-";
                    lbl_MaximumDrivingTimeRemaining.Text = "-";

                    lbl_BoxNextLapTime.Text = "-";
                    lbl_BoxThisLapTime.Text = "-";

                    lbl_LapsRemaining.Text = "-";

                    lbl_PitWindowHeading.Text = "";
                    lbl_PitWindowContent.Text = "";
                }
            }
        }

        public void ReceiveUIUpdateMessage(IUIUpdateMessage anUpdateMessage)
        {
            if (anUpdateMessage is BulkRefreshDataUIUpdateMessage b)
            {
                // bulk messages is the list of messages the control receives when it is loaded
                //i.e. if the user opens the control in the middle of the race, it will receive the messages from 
                //the beginning to the current time

                //uIUpdateMessages.Clear();

                foreach (IUIUpdateMessage item in b.ListOfUIUpdateMessages)
                {
                    //uIUpdateMessages.Add(item);
                    HandleUIUpdateMessage(item, false);
                }
            }
            else
                HandleUIUpdateMessage(anUpdateMessage, true);

        }

        private void HandleUIUpdateMessage(IUIUpdateMessage anUpdateMessage, bool anAllowRefresh)
        {
            if (anUpdateMessage is ResetUIUpdateMessage)
            {
                _carStatus = eCarStatus.OnTrackRunning;

                lbl_DrivingTime.Text = "0:00:00";
                lbl_DriverName.Text = "NO DRIVER NAME";
                //lbl_ContinuousDrivingTimeRemaining.Text = "0:00:00";

                _averageLapTime = 0;
                _driverName = "";
                _driverTotalTime = 0;
                _projectedLapTime = 0;
                _sessionTime = 0;
                //_stintNumber = 0;
                _driverStartTime = 0;

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
            //else if (anUpdateMessage is DriverOverrideUIUpdateMessage)
            //{
            //    _stintNumber = 0;
            //}
            else if (anUpdateMessage is PitstopUIUpdateMessage)
            {
                HandlePitStopUIUpdateMessage((PitstopUIUpdateMessage)anUpdateMessage);
            }
            //else if (anUpdateMessage is StintUIUpdateMessage)
            //{
            //    HandleStintUIUpdateMessage((StintUIUpdateMessage)anUpdateMessage);
            //}
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
            if (aMessage.AlternateEstimatedRace != null && aMessage.AlternateEstimatedRace.EstimatedStints.Count > 0 &&
                aMessage.CurrentEstimatedRace != null && aMessage.CurrentEstimatedRace.EstimatedStints.Count > 0)
            {
                EstimatedStint firstForwardStint = aMessage.CurrentEstimatedRace.EstimatedStints.First();
                EstimatedStint lastForwardStint = aMessage.CurrentEstimatedRace.EstimatedStints.Last();
                EstimatedStint lastReverseStint = aMessage.AlternateEstimatedRace.EstimatedStints.Last();

                double forwardOffset = lastForwardStint.EndTime - _sessionEndTime;
                double reverseOffset = lastReverseStint.EndTime - _sessionEndTime;

                // Pit window is difference between final stint start times adjusted for race end
                double pitWindow = lastForwardStint.StartTime - forwardOffset - (lastReverseStint.StartTime - reverseOffset);

            }

            if (aMessage.CurrentEstimatedRace != null && aMessage.CurrentEstimatedRace.EstimatedLapTime != double.MaxValue)
            {
                _averageLapTime = aMessage.CurrentEstimatedRace.EstimatedLapTime;
            }
        }


        public void HandleLapUIUpdateMessage(LapUIUpdateMessage message)
        {

            _driverTotalTime = message.DrivingTimeforCurrentDriverAtEndOfLap;
            _driverTotalTimeUpdated = message.ElapsedTime;
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

                    _driverStartTime = aMessage.PitOutSessionTime;

                    DriverName = aMessage.OutDriverName;
                }

                _boxNow = false;
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

        //public void HandleStintUIUpdateMessage(StintUIUpdateMessage aMessage)
        //{
        //    if (aMessage.StintMessageType == StintUIUpdateMessage.StintMessageTypeEnum.CurrentStint)
        //    {
        //        DriverName = aMessage.DriverID;

        //        if (aMessage.DrivingTime != double.MaxValue)
        //        {
        //            _driverStartTime = aMessage.StartTime;
        //            _continuousDrivingTime = aMessage.DrivingTime;
        //        }
        //    }
        //    else if (aMessage.StintMessageType == StintUIUpdateMessage.StintMessageTypeEnum.EndOfStint)
        //    {
        //        if (aMessage.DrivingTime != double.MaxValue && _stintNumber == aMessage.StintNumber)
        //        {
        //            DriverName = aMessage.DriverID;

        //            _stintNumber++;
        //        }
        //    }
        //}

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
