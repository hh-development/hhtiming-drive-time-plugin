using HHTiming.Desktop.Definitions.PlugInFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HHDev.ProjectFramework.Definitions;
using HHTiming.Core.Definitions.UIUpdate.Interfaces;
using HHTiming.DAL;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace HHTiming.DriveTime
{
    class DriveTimePluginEntry : IHHTimingPlugin
    {
        private List<HHRibbonTab> _ribbonTabs = new List<HHRibbonTab>();

        public DriveTimePluginEntry()
        {
            BuildRibbonTabs();
        }

        #region IHHTimingPlugin

        public Func<IHHTimingContext> HHTimingContextFactory
        {
            set
            {
                
            }
        }

        public string Name
        {
            get
            {
                return "Drive Time";
            }
        }

        public Func<string, IProjectObject> OpenProjectObject
        {
            set
            {
                
            }
        }

        public IOptionsObject Options
        {
            get
            {
                return null;
            }
        }

        private Guid _pluginId = Guid.Parse("{C3B54BC1-DA75-46F5-9E6C-BE488801D42D}");
        public Guid PluginID
        {
            get
            {
                return _pluginId;
            }
        }

        public bool LoadSuccessful
        {
            get
            {
                return true;
            }
        }

#pragma warning disable 0067
        public event EventHandler<CreateNewProjectObjectEventArgs> AddNewProjectItem;
#pragma warning restore 0067
        public event EventHandler<NewWorksheetEventArgs> AddNewWorksheet;

        public List<IUIUpdateControl> GetAllBackgroundUIUpdateControls()
        {
            return null;
        }

        public List<Type> GetDataImporters()
        {
            return null;
        }

        public List<MessageParserDefinition> GetMessageParsers()
        {
            return null;
        }

        public IProjectObjectManager GetProjectObjectManager()
        {
            return null;
        }

        public List<HHRibbonTab> GetRibbonTabs()
        {
            return _ribbonTabs;
        }

        public IWorksheetControlManager GetWorksheetControlManager()
        {
            return null;
        }

        public void SoftwareClosing()
        {

        }

        public List<LapNumericTagItemDefinition> LapNumericTagsDefinitions
        {
            get
            {
                return null;
            }
        }

        #endregion

        private void BuildRibbonTabs()
        {
            var tab = new HHRibbonTab("Drive Time");
            _ribbonTabs.Add(tab);

            var bar = new HHRibbonBar("Tools");
            tab.Bars.Add(bar);

            var createNewStintSummaryButton = new HHRibbonButton("Stint Summary", Properties.Resources.StintSummary_48, HandleCreateNewStintSummary);
            bar.Buttons.Add(createNewStintSummaryButton);

            var createNewContinuousDrivingTime = new HHRibbonButton("Continuous Driving Time (Experimental)", Properties.Resources.StintSummary_48, HandleCreateNewContinuousDrivingTime);
            bar.Buttons.Add(createNewContinuousDrivingTime);

            var createNewCumulativeDrivingTime = new HHRibbonButton("Cumulative Driving Time (Experimental)", Properties.Resources.StintSummary_48, HandleCreateNewCumulativeDrivingTime);
            bar.Buttons.Add(createNewCumulativeDrivingTime);


            var createNewPitStopwatchButton = new HHRibbonButton("Pit Stopwatch", Properties.Resources.PitStopwatch_48, HandleCreateNewPitStopwatch);
            bar.Buttons.Add(createNewPitStopwatchButton);
        }

        private void HandleCreateNewPitStopwatch(object sender)
        {
            AddNewWorksheet?.Invoke(this, new NewWorksheetEventArgs()
            {
                NewWorksheet = new PitStopwatchControl(),
                TargetWorkbook = sender
            });
        }

        private void HandleCreateNewStintSummary(object sender)
        {
            var carForm = new GetCarNumberForm();
            if (carForm.ShowDialog() != DialogResult.OK)
                return;

            AddNewWorksheet?.Invoke(this, new NewWorksheetEventArgs()
            {
                NewWorksheet = new StintSummaryControl(carForm.CarID),
                TargetWorkbook = sender
            });
        }

        private void HandleCreateNewContinuousDrivingTime(object sender)
        {
            var carForm = new GetCarNumberForm();
            if (carForm.ShowDialog() != DialogResult.OK)
                return;

            AddNewWorksheet?.Invoke(this, new NewWorksheetEventArgs()
            {
                NewWorksheet = new ContinuousDrivingTimeControl(carForm.CarID),
                TargetWorkbook = sender
            });
        }

        private void HandleCreateNewCumulativeDrivingTime(object sender)
        {
            var carForm = new GetCarNumberForm();
            if (carForm.ShowDialog() != DialogResult.OK)
                return;

            AddNewWorksheet?.Invoke(this, new NewWorksheetEventArgs()
            {
                NewWorksheet = new CumulativeDrivingTimeControl(carForm.CarID),
                TargetWorkbook = sender
            });
        }

    }
}
