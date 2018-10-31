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

namespace HHTiming.Blancpain
{
    class BlancpainPluginEntry : IHHTimingPlugin
    {
        private List<HHRibbonTab> _ribbonTabs = new List<HHRibbonTab>();

        public BlancpainPluginEntry()
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
                return "Blancpain";
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

        public event EventHandler<CreateNewProjectObjectEventArgs> AddNewProjectItem;
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
            var tab = new HHRibbonTab("Blancpain");
            _ribbonTabs.Add(tab);

            var bar = new HHRibbonBar("Strategy Tools");
            tab.Bars.Add(bar);

            var createNewStintSummaryButton = new HHRibbonButton("Stint Summary", Properties.Resources.StintSummary_48, HandleCreateNewStintSummary);
            bar.Buttons.Add(createNewStintSummaryButton);

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
    }
}
