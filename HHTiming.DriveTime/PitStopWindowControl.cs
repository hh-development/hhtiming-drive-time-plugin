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
    public partial class PitStopWindowControl :
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
        private double _sessionTimeEndOfLap = 0;


        private double _sessionEndTime = double.MaxValue;

        private double _projectedLapTime = 0;
        private double _averageLapTime = 0;


        private bool _boxNow = false;
        private double _pitWindowOpenTime = double.MaxValue;

        private const string LongTimeFormat = @"h\:mm\:ss";

        public PitStopWindowControl()
        {
            InitializeComponent();

            Dock = DockStyle.Fill;

            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.UserPaint, true);

            tb_CarNumber.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(CarNumber), true, DataSourceUpdateMode.OnPropertyChanged));
            tb_InLapTime.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(InLapTime), true, DataSourceUpdateMode.OnPropertyChanged));
            tb_PitWindowOpen.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(StartOfPitWindow), true, DataSourceUpdateMode.OnPropertyChanged));
            tb_PitWindowClose.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(EndOfPitWindow), true, DataSourceUpdateMode.OnPropertyChanged));

            Name = "Pit Stop Window";
        }

        public PitStopWindowControl(string carNumber) : this()
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


                _sessionTime = 0;

                _projectedLapTime = 0;
                _averageLapTime = 0;

                tableLayoutPanel1.SetRowSpan(lbl_PitWindowHeading, 1);

                _carNumber = value;
                lbl_CarNumber.Text = _carNumber;
                ReinitializationFlag = true;
            }
        }

        public double StartOfPitWindow { get; set; } = 20;
        public double EndOfPitWindow { get; set; } = 40;
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
                    case "InLapTime":
                        InLapTime = int.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                        break;
                    case "MaxStintLength":
                        StartOfPitWindow = int.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                        break;
                }
            }
        }

        public void SaveToXML(XmlElement parentXMLElement)
        {
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "CarNumber", CarNumber, parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "CarColor", _carColor.ToArgb().ToString(CultureInfo.InvariantCulture), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "InLapTime", InLapTime.ToString(CultureInfo.InvariantCulture), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "MaxStintLength", StartOfPitWindow.ToString(CultureInfo.InvariantCulture), parentXMLElement);
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
                    double minRemainingTime = StartOfPitWindow * 60 - _sessionTime;
                    double maxRemainingTime = EndOfPitWindow * 60 - _sessionTime;
                    if (minRemainingTime < 0)
                    {
                        minRemainingTime = 0;
                    }
                    if (maxRemainingTime < 0)
                    {
                        maxRemainingTime = 0;
                    }

                    string minRemainingTimeString = SecondsToTimeString(minRemainingTime, LongTimeFormat);
                    string maxRemainingTimeString = SecondsToTimeString(maxRemainingTime, LongTimeFormat);

                    lbl_MinRemainingTime.Text = minRemainingTimeString;
                    lbl_MaxRemainingTime.Text = maxRemainingTimeString;

                    double lapsRemaining = (StartOfPitWindow * 60 - _sessionTimeEndOfLap - InLapTime) / _averageLapTime +1;
                    double lapsRemainingMax = (EndOfPitWindow * 60 - _sessionTimeEndOfLap - InLapTime) / _averageLapTime;

                    double sessionTimeIfPitAfterOpening = 0;
                    double sessionTimeIfPitBeforeClosing = 0;

                    if (_averageLapTime > 0)
                    {
                        sessionTimeIfPitAfterOpening = _sessionTimeEndOfLap + InLapTime;
                        while (sessionTimeIfPitAfterOpening < StartOfPitWindow * 60)
                        {
                            sessionTimeIfPitAfterOpening += _averageLapTime;
                        }
                        sessionTimeIfPitBeforeClosing = _sessionTimeEndOfLap + InLapTime;
                        while (sessionTimeIfPitBeforeClosing < (EndOfPitWindow * 60 - _averageLapTime))
                        {
                            sessionTimeIfPitBeforeClosing += _averageLapTime;
                        }

                    }
                    if (_sessionTimeEndOfLap > StartOfPitWindow * 60)
                        sessionTimeIfPitAfterOpening = 0;

                    if (sessionTimeIfPitBeforeClosing > EndOfPitWindow * 60)
                        sessionTimeIfPitBeforeClosing = 0;


                    lbl_SessionTimeBeforeEnd.Text = SecondsToTimeString(sessionTimeIfPitBeforeClosing, LongTimeFormat);
                    lbl_SessionTimeAfterStart.Text = SecondsToTimeString(sessionTimeIfPitAfterOpening, LongTimeFormat);
                    if (lapsRemainingMax != 0 && lapsRemainingMax < 1) _boxNow = true;
                    else _boxNow = false;

                    if (lapsRemaining < 0)
                    {
                        lapsRemaining = 0;
                    }
                    if (lapsRemainingMax < 0)
                    {
                        lapsRemainingMax = 0;
                    }

                    lbl_LapsRemainingBeforeStart.Text = lapsRemaining.ToString("F1");
                    lbl_LapsRemainingBeforeEnd.Text = lapsRemainingMax.ToString("F1");



                    if (minRemainingTime > 0)
                    {
                        SetBackgroundColor(_carColor);
                        lbl_PitWindowHeading.Text = "PIT WINDOW OPEN IN";
                        lbl_PitWindowContent.Text = minRemainingTimeString;
                        lbl_PitWindowContent.Visible = true;

                    }
                    else
                    {
                        var alternateColor = Color.Green;
                        if (maxRemainingTime == 0)
                        {
                            lbl_PitWindowHeading.Text = "PIT WINDOW CLOSED";

                            lbl_PitWindowContent.Visible = false;
                            SetBackgroundColor(_carColor);
                        }
                        else
                        {
                            if (lapsRemainingMax < 2)
                            {
                                alternateColor = Color.Orange;
                                lbl_PitWindowHeading.Text = "PIT WINDOW CLOSED IN";
                                tableLayoutPanel1.SetRowSpan(lbl_PitWindowHeading, 1);
                                lbl_PitWindowContent.Visible = true;
                                lbl_PitWindowContent.Text = maxRemainingTimeString;
                            }
                            else if (_boxNow)
                            {
                                tableLayoutPanel1.SetRowSpan(lbl_PitWindowHeading, 2);

                                lbl_PitWindowContent.Visible = false;

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
                                lbl_PitWindowHeading.Text = "PIT WINDOW OPEN";
                                tableLayoutPanel1.SetRowSpan(lbl_PitWindowHeading, 2);

                                lbl_PitWindowContent.Visible = false;

                            }
                            if (aFlashFlag)
                            {
                                SetBackgroundColor(alternateColor);
                            }
                            else
                            {
                                SetBackgroundColor(Color.Black);
                            }
                        }
                    }

                   
                    
                }
                else
                {
                    SetBackgroundColor(Color.LightGray);

                    pnl_PitWindow.BackColor = default(Color);
                    pnl_PitWindow.ForeColor = default(Color);

                    tableLayoutPanel1.SetRowSpan(lbl_PitWindowHeading, 1);
                    lbl_PitWindowContent.Visible = true;

                    lbl_MinRemainingTime.Text = "-";
                    lbl_MaxRemainingTime.Text = "-";
                    lbl_LapsRemainingBeforeStart.Text = "-";

                    lbl_BoxNextLapTime.Text = "-";
                    lbl_SessionTimeAfterStart.Text = "-";

                    lbl_LapsRemainingBeforeEnd.Text = "-";

                    lbl_PitWindowHeading.Text = "-";
                    lbl_PitWindowContent.Text = "-";
                }
            }
        }

        public void ReceiveUIUpdateMessage(IUIUpdateMessage anUpdateMessage)
        {
            if (anUpdateMessage is ResetUIUpdateMessage)
            {
                _carStatus = eCarStatus.OnTrackRunning;

                lbl_MinRemainingTime.Text = "0:00:00";
                lbl_DriverName.Text = "NO DRIVER NAME";
                lbl_MaxRemainingTime.Text = "0:00:00";

                tableLayoutPanel1.SetRowSpan(lbl_PitWindowHeading, 1);
                _averageLapTime = 0;
                _driverName = "";
                _projectedLapTime = 0;
                _sessionTime = 0;
                _pitWindowOpenTime = double.MaxValue;

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
            else if (anUpdateMessage is PitstopUIUpdateMessage)
            {
                HandlePitStopUIUpdateMessage((PitstopUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is SectorUIUpdateMessage)
            {
                HandleSectorUIUpdateMessage((SectorUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is EstimatedTimeRemainingUIUpdateMessage)
            {
                HandleEstimatedTimeRemainingUIUpdateMessage((EstimatedTimeRemainingUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is LapUIUpdateMessage)
            {
                HandleLapUIUpdateMessage((LapUIUpdateMessage)anUpdateMessage);
            }

        }

        public void HandleLapUIUpdateMessage(LapUIUpdateMessage message)
        {
            _sessionTimeEndOfLap = message.ElapsedTime;
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
                // Next pit window opens before the end of the current maximum stint
                _pitWindowOpenTime = firstForwardStint.EndTime - pitWindow - forwardOffset;
            }

            if (aMessage.CurrentEstimatedRace != null && aMessage.CurrentEstimatedRace.EstimatedLapTime != double.MaxValue)
            {
                _averageLapTime = aMessage.CurrentEstimatedRace.EstimatedLapTime;
            }
        }


        public void HandlePitStopUIUpdateMessage(PitstopUIUpdateMessage aMessage)
        {
            if (aMessage.MessageType == PitstopUIUpdateMessage.PitStopMessageType.PitIn) {
                itemContainer5.Refresh();

            }

            if (aMessage.MessageType == PitstopUIUpdateMessage.PitStopMessageType.NewStop)
            {
                itemContainer5.Refresh();




                DriverName = aMessage.OutDriverName;

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
