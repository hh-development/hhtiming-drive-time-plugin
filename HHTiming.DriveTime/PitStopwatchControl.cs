using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
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
    public partial class PitStopwatchControl :
        UserControl,
        IWorksheetControlInternal,
        IUIUpdateControl
    {
        private Guid _controlID = Guid.NewGuid();

        private string _carNumber = "1";
        private eCarStatus _carStatus = eCarStatus.OnTrackRunning;

        private double _timeOfPitIn = double.MaxValue;
        private double _timeOfLastStop = double.MaxValue;

        private double _projectedLapTime = 0;
        private double _lapStartTime = 0;

        private bool _manualMode = false;
        private Timer _manualTimer = new Timer();
        private double _manualStopTime = 0;
        private bool _manualCarInPit = false;

        private const string ShortTimeFormat = @"ss";
        private const string LongTimeFormat = @"mm\:ss";

        public PitStopwatchControl()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;

            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.UserPaint, true);

            tb_CarNumber.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(CarNumber), true, DataSourceUpdateMode.OnPropertyChanged));

            tb_InLapTime.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(InLapTime), true, DataSourceUpdateMode.OnPropertyChanged));
            tb_ToBoxTime.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(BoxEntryTime), true, DataSourceUpdateMode.OnPropertyChanged));
            tb_FromBoxTime.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(BoxExitTime), true, DataSourceUpdateMode.OnPropertyChanged));

            tb_MaxShortTime.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(MaxShortTime), true, DataSourceUpdateMode.OnPropertyChanged));
            tb_MinLongTime.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(MinLongTime), true, DataSourceUpdateMode.OnPropertyChanged));

            chk_ManualMode.DataBindings.Add(new Binding(nameof(CheckBoxItem.CheckedBindable), this, nameof(ManualMode), true, DataSourceUpdateMode.OnPropertyChanged));

            tb_CountDownDuration.DataBindings.Add(new Binding(nameof(TextBoxItem.Text), this, nameof(CountDownDuration), true, DataSourceUpdateMode.OnPropertyChanged));

            Name = "Pit Stopwatch";

            ManualMode = false;
            _manualTimer.Interval = 100;
            _manualTimer.Tick += _manualTimer_Tick;

            tableLayoutPanel1.Click += ManualStop_Click;
            lbl_CarNumber.Click += ManualStop_Click;
            lbl_CountdownStatus.Click += ManualStop_Click;
            lbl_MidHeading.Click += ManualStop_Click;
            lbl_Time.Click += ManualStop_Click;
            lbl_TopHeading.Click += ManualStop_Click;
        }

        public double MaxShortTime { get; set; } = 60;
        public double MinLongTime { get; set; } = 120;
        public double BoxExitTime { get; set; } = 10;
        public double BoxEntryTime { get; set; } = 10;
        public int InLapTime { get; set; } = 180;

        public int CountDownDuration { get; set; } = 15;


        public string CarNumber
        {
            get
            {
                return _carNumber;
            }
            set
            {
                if (_carNumber == value) return;

                _carNumber = value;
                lbl_CarNumber.Text = _carNumber;
                ReinitializationFlag = true;
            }
        }

        public bool ManualMode
        {
            get
            {
                return _manualMode;
            }
            set
            {
                _manualMode = value;

                if (value == true)
                {
                    tableLayoutPanel2.SetRowSpan(tableLayoutPanel1, 1);
                    btn_ManualStop.Show();
                }
                else
                {
                    tableLayoutPanel2.SetRowSpan(tableLayoutPanel1, 2);
                    btn_ManualStop.Hide();

                    _manualTimer.Stop();
                    _manualStopTime = 0;
                    _manualCarInPit = false;
                }
            }
        }

        #region IWorksheetControl

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
                    case "InLapTime":
                        InLapTime = int.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                        break;
                    case "BoxEntryTime":
                        BoxEntryTime = double.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                        break;
                    case "BoxExitTime":
                        BoxExitTime = double.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                        break;
                    case "MaxShortTime":
                        MaxShortTime = double.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                        break;
                    case "MinLongTime":
                        MinLongTime = double.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                        break;
                    case "ManualMode":
                        ManualMode = bool.Parse(elem.InnerText);
                        break;
                    case "CountDownDuration":
                        CountDownDuration = int.Parse(elem.InnerText, CultureInfo.InvariantCulture);
                        break;

                }
            }
        }

        public void SaveToXML(XmlElement parentXMLElement)
        {
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "CarNumber", CarNumber, parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "InLapTime", InLapTime.ToString(CultureInfo.InvariantCulture), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "BoxEntryTime", BoxEntryTime.ToString(CultureInfo.InvariantCulture), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "BoxExitTime", BoxExitTime.ToString(CultureInfo.InvariantCulture), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "MaxShortTime", MaxShortTime.ToString(CultureInfo.InvariantCulture), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "MinLongTime", MinLongTime.ToString(CultureInfo.InvariantCulture), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "ManualMode", ManualMode.ToString(), parentXMLElement);
            XMLHelperFunctions.WriteToXML(parentXMLElement.OwnerDocument, "CountDownDuration", CountDownDuration.ToString(CultureInfo.InvariantCulture), parentXMLElement);
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
            return null;
        }

        public List<IUIUpdateMessage> GetInitializationMessages(Guid aTargetControlID)
        {
            return null;
        }

        public void PaintControl(SessionStatusUIUpdateMessage aSessionUIUpdateMessage, bool aFlashFlag)
        {
            if (aSessionUIUpdateMessage.SessionTime > 0 && aSessionUIUpdateMessage.SessionTime != double.MaxValue)
            {
                if ((!ManualMode && (_carStatus == eCarStatus.OnTrackRunning || _carStatus == eCarStatus.PitOut)) ||
                    (ManualMode && !_manualCarInPit))
                {
                    BackColor = Color.LightGray;
                    ForeColor = Color.Black;

                    lbl_TopHeading.Text = "Time From Box";

                    if (_projectedLapTime != double.MaxValue)
                    {
                        double timeFromBox = InLapTime + BoxEntryTime - (aSessionUIUpdateMessage.SessionTime - _lapStartTime);
                        if (timeFromBox > 0)
                            lbl_Time.Text = SecondsToTimeString(timeFromBox, LongTimeFormat);
                        else
                            lbl_Time.Text = "00:00";
                    }
                    else
                    {
                        lbl_Time.Text = "-";
                    }


                    lbl_MidHeading.Text = "Last Stop";
                    if (_timeOfLastStop != double.MaxValue) lbl_CountdownStatus.Text = SecondsToTimeString(_timeOfLastStop, LongTimeFormat);
                    else lbl_CountdownStatus.Text = "-";
                }
                else if ((!ManualMode && (_carStatus == eCarStatus.PitIn && _timeOfPitIn != double.MaxValue)) ||
                    (ManualMode && _manualCarInPit))
                {
                    lbl_TopHeading.Text = "Stop Time";

                    ForeColor = Color.White;

                    double stopTime;
                    if (!ManualMode) stopTime = aSessionUIUpdateMessage.SessionTime - _timeOfPitIn;
                    else stopTime = _manualStopTime;

                    lbl_Time.Text = SecondsToTimeString(stopTime, LongTimeFormat);

                    if (stopTime < MaxShortTime - BoxExitTime - CountDownDuration)
                    {
                        BackColor = Color.Green;

                        lbl_MidHeading.Text = "Release Window";
                        lbl_CountdownStatus.Text = "OPEN";
                    }
                    else if (stopTime < MaxShortTime - BoxExitTime)
                    {
                        if (aFlashFlag) BackColor = Color.DarkOrange;
                        else BackColor = Color.Chocolate;

                        lbl_MidHeading.Text = "Release Window Closes In";
                        lbl_CountdownStatus.Text = SecondsToTimeString(MaxShortTime - BoxExitTime - stopTime, ShortTimeFormat);
                    }
                    else if (stopTime >= MaxShortTime - BoxExitTime && stopTime < MinLongTime - BoxExitTime - CountDownDuration)
                    {
                        BackColor = Color.Red;

                        lbl_MidHeading.Text = "Release Window";
                        lbl_CountdownStatus.Text = "CLOSED";
                    }
                    else if (stopTime < MinLongTime - BoxExitTime)
                    {
                        if (aFlashFlag) BackColor = Color.DarkOrange;
                        else BackColor = Color.Chocolate;

                        lbl_MidHeading.Text = "Release Window Opens In";
                        lbl_CountdownStatus.Text = SecondsToTimeString(MinLongTime - BoxExitTime - stopTime, ShortTimeFormat);
                    }
                    else if (stopTime >= MinLongTime - BoxExitTime)
                    {
                        BackColor = Color.Green;

                        lbl_MidHeading.Text = "Release Window";
                        lbl_CountdownStatus.Text = "OPEN";
                    }
                }
            }
        }

        public void ReceiveUIUpdateMessage(IUIUpdateMessage anUpdateMessage)
        {
            if (anUpdateMessage is ResetUIUpdateMessage)
            {
                _carStatus = eCarStatus.OnTrackRunning;
                _timeOfPitIn = double.MaxValue;
                _timeOfLastStop = double.MaxValue;
                _projectedLapTime = 0;
                _lapStartTime = 0;

                _manualTimer.Stop();
                _manualStopTime = 0;
                _manualCarInPit = false;

                lbl_MidHeading.Text = "Time From Box";
                lbl_Time.Text = "00:00";
                lbl_MidHeading.Text = "No Stops";
                lbl_CountdownStatus.Text = "00:00";

                BackColor = Color.LightGray;
                ForeColor = Color.Black;
            }

            if (((BaseUIUpdateMessage)anUpdateMessage).ItemID != CarNumber) return;

            if (anUpdateMessage is CarStatusUIUpdateMessage)
            {
                HandleCarStatusUIUpdateMessage((CarStatusUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is CarUIUpdateMessage)
            {
                HandleCarUIUpdateMessage((CarUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is LapUIUpdateMessage)
            {
                HandleLapUIUpdateMessage((LapUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is PitstopUIUpdateMessage)
            {
                HandlePitStopUIUpdateMessage((PitstopUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is SectorUIUpdateMessage)
            {
                HandleSectorUIUpdateMessage((SectorUIUpdateMessage)anUpdateMessage);
            }
        }

        #endregion

        public string SecondsToTimeString(double aTime, string format)
        {
            if (double.IsNaN(aTime) || double.IsInfinity(aTime))
                return "";

            var ts = TimeSpan.FromSeconds(aTime);
            return ts.ToString(format);
        }

        public void HandleCarUIUpdateMessage(CarUIUpdateMessage aMessage)
        {
            lbl_CarNumber.BackColor = aMessage.CarColor;
            lbl_CarNumber.ForeColor = ColorFunctionsWinForms.GetTextColor(aMessage.CarColor);
        }

        public void HandleCarStatusUIUpdateMessage(CarStatusUIUpdateMessage aMessage)
        {
            _carStatus = aMessage.CarStatus;
        }

        public void HandleLapUIUpdateMessage(LapUIUpdateMessage aMessage)
        {
            _lapStartTime = aMessage.ElapsedTime;
        }

        public void HandlePitStopUIUpdateMessage(PitstopUIUpdateMessage aMessage)
        {
            if (aMessage.MessageType == PitstopUIUpdateMessage.PitStopMessageType.PitIn)
            {
                _timeOfPitIn = aMessage.PitInSessionTime;
            }
            else if (aMessage.MessageType == PitstopUIUpdateMessage.PitStopMessageType.NewStop)
            {
                _timeOfLastStop = aMessage.StopTime;
                _lapStartTime = aMessage.PitOutSessionTime;
                ManualPitOut();
            }
        }

        public void HandleSectorUIUpdateMessage(SectorUIUpdateMessage aMessage)
        {
            _projectedLapTime = aMessage.ProjectedLapTime;
        }

        private void _manualTimer_Tick(object sender, EventArgs e)
        {
            _manualStopTime += 0.1;
        }

        private void ManualStop_Click(object sender, EventArgs e)
        {
            if (_manualMode)
            {
                if (_manualCarInPit)
                {
                    ManualPitOut();
                }
                else
                {
                    btn_ManualStop.Text = "CLICK AT PIT OUT";
                    _manualCarInPit = true;
                    _manualTimer.Start();
                }
            }
        }

        private void ManualPitOut()
        {
            btn_ManualStop.Text = "CLICK AT PIT IN";
            _manualCarInPit = false;
            _manualTimer.Stop();
            _manualStopTime = 0;
        }
    }
}
