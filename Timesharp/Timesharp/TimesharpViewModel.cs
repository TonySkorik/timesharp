using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Scheduler = Microsoft.Win32.TaskScheduler;

namespace TimesharpUI {
	public class TimesharpViewModel:INotifyPropertyChanged {
		
		#region [NOTIFY PROPERTY CHANGED]
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion

		#region [P & F]

		private string _bootTaskName = "TimeSharp_boot";
		private string _taskName = "TimeSharp_sync";
		private string _taskAuthor = "TS";
		private string _taskArgs = "Scheduled_start";
		private string _adminsGroupName = "Administrators";

		private long _intervalSeconds = 10800L; // 3 hours default interval between program
		
		private DateTime _dtFetched;
		public DateTime DtFetched {
			set {
				_dtFetched = value;
				NotifyPropertyChanged();
			}
			get {
				return _dtFetched;
			}
		}
		private bool? _fetchSuccess;
		public bool? FetchSuccess {
			get { return _fetchSuccess; }
			set {
				_fetchSuccess = value;
				NotifyPropertyChanged();
			}
		}
		private bool? _setTimeSuccess;
		public bool? SetTimeSuccess {
			get { return _setTimeSuccess; }
			set {
				_setTimeSuccess = value;
				NotifyPropertyChanged();
			}
		}
		private bool _taskIsScheduled;
		public bool TaskIsScheduled {
			get { return _taskIsScheduled; }
			set {
				_taskIsScheduled = value;
				NotifyPropertyChanged();
			}
		}

		private bool _isAutoloaded;
		public bool IsAutoloaded {
			get { return _isAutoloaded; }
			set {
				_isAutoloaded = value;
				NotifyPropertyChanged();
			}
		}
		#endregion

		public TimesharpViewModel(string configPath) {

			try {
				XDocument cfg = XDocument.Load(configPath);
				Units u;
				Units.TryParse(cfg.Root.Element("TaskInterval").Attribute("Unit").Value, true, out u);
				_intervalSeconds = Int32.Parse(cfg.Root.Element("TaskInterval").Value)*(int) u;
				_taskName = cfg.Root.Element("TaskName").Value;
				_taskAuthor = cfg.Root.Element("TaskAuthor").Value;
				_taskArgs = cfg.Root.Element("TaskArgs").Value;

				_bootTaskName = cfg.Root.Element("BootTaskName").Value;

				_adminsGroupName = cfg.Root.Element("AdminsSytemUserGroup").Value;
			} catch (Exception e) {
				MessageBox.Show($"Error loading config.\nOriginal mesage: {e.Message}", "Config loading error", MessageBoxButton.OK,
								MessageBoxImage.Error);
			}

			DtFetched = DateTime.MinValue;
			FetchSuccess = null;
			SetTimeSuccess = null;
			IsAutoloaded = false;
			TaskIsScheduled = checkTaskIsScheduled();
		}

		#region [SCHEDULER]
		private bool checkTaskIsScheduled() {
			TaskIsScheduled = false;
			using (Scheduler.TaskService tsrv = new Scheduler.TaskService()) {
				if (tsrv.AllTasks.Any(t => t.Name == _taskName)) {
					TaskIsScheduled = true;
				}
			}
			return TaskIsScheduled;
		}

		private void godmodeTask(string tskName) {
			using (Scheduler.TaskService tsrv = new Scheduler.TaskService()) {
				Scheduler.TaskDefinition tskDef = tsrv.FindTask(tskName).Definition;

				tskDef.RegistrationInfo.Author = _taskAuthor;
				tskDef.Principal.RunLevel = Scheduler.TaskRunLevel.Highest;
				tskDef.Settings.MultipleInstances = Scheduler.TaskInstancesPolicy.IgnoreNew;
				tskDef.Settings.WakeToRun = false;
				tskDef.Settings.Compatibility = Scheduler.TaskCompatibility.V2_1;

				tsrv.RootFolder.RegisterTaskDefinition(tskName, tskDef,
					Scheduler.TaskCreation.CreateOrUpdate, _adminsGroupName, null,
					Scheduler.TaskLogonType.Group);
			}
		}

		public bool ScheduleTask(string exeLocation) {
			using (Scheduler.TaskService tsrv = new Scheduler.TaskService()) {
				//task for interval
				tsrv.Execute(exeLocation)
					.WithArguments(_taskArgs)
					.Once()
					.Starting(DateTime.Now)
					.RepeatingEvery(TimeSpan.FromHours(3))
					.AsTask(_taskName);

				
				godmodeTask(_taskName);
				/*
				tskDef.RegistrationInfo.Author = _taskAuthor;
				tskDef.Principal.RunLevel = Scheduler.TaskRunLevel.Highest;
				tskDef.Settings.MultipleInstances = Scheduler.TaskInstancesPolicy.IgnoreNew;
				tskDef.Settings.WakeToRun = false;
				tskDef.Settings.Volatile = false;
				tskDef.Settings.Compatibility = Scheduler.TaskCompatibility.V2_1;
				*/
				/*
				tsrv.RootFolder.RegisterTaskDefinition(_taskName, tskDef,
					Scheduler.TaskCreation.CreateOrUpdate, "Administrators", null,
					Scheduler.TaskLogonType.Group);
				*/
				//boot task
				tsrv.Execute(exeLocation)
					.WithArguments(_taskArgs)
					.OnBoot()
					.AsTask(_bootTaskName);

				//Scheduler.TaskDefinition bootTskDef = tsrv.FindTask(_bootTaskName).Definition;

				godmodeTask(_bootTaskName);
				
				/*
				bootTskDef.RegistrationInfo.Author = _taskAuthor;
				bootTskDef.Principal.RunLevel = Scheduler.TaskRunLevel.Highest;
				bootTskDef.Settings.MultipleInstances = Scheduler.TaskInstancesPolicy.IgnoreNew;
				bootTskDef.Settings.WakeToRun = false;
				bootTskDef.Settings.Volatile = false;
				bootTskDef.Settings.Compatibility = Scheduler.TaskCompatibility.V2_1;
				*/
				/*
				tsrv.RootFolder.RegisterTaskDefinition(_bootTaskName, bootTskDef,
					Scheduler.TaskCreation.CreateOrUpdate, "Administrators", null,
					Scheduler.TaskLogonType.Group);
				*/
			}
			return checkTaskIsScheduled();
		}

		public bool UnScheduleTask() {
			using(Scheduler.TaskService tsrv = new Scheduler.TaskService()) {
				tsrv.RootFolder.DeleteTask(_taskName,false);
				tsrv.RootFolder.DeleteTask(_bootTaskName, false);
			}
			return checkTaskIsScheduled();
		}
		#endregion

		#region [FETCH TIME]
		public Task FetchTimeAsync() {
			return Task.Run(() => FetchTime());
		}

		public void FetchTime() {
			DateTime dt = Timesharp.GetNistDtWeb(true);
			if (dt == DateTime.MinValue) {
				FetchSuccess = false;
				DtFetched = DateTime.MaxValue;
			} else {
				DtFetched = dt;
				FetchSuccess = true;
			}
		}
		#endregion

		#region [SET TIME]
		public Task SetTimeAsync() {
			return Task.Run(() =>SetTime());
		}

		public void SetTime() {
			FetchTime();
			if (FetchSuccess != null && FetchSuccess.Value) {
				SetTimeSuccess = Timesharp.SetTime(_dtFetched);
			} else {
				SetTimeSuccess = false;
			}
		}
		#endregion

	}
}