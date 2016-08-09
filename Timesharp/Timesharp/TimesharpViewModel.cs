using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

		private long _taskIntervalSeconds = 10800L; // 3 hours default interval between program
		private int _closeSuccessWindowAfterMiliseconds; //10 seconds

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
		public int CloseSuccessWindowAfterMiliseconds {
			get { return _closeSuccessWindowAfterMiliseconds; }
			set { _closeSuccessWindowAfterMiliseconds = value; }
		}

		#endregion

		#region [CONSTRUCTOR]
		public TimesharpViewModel(string configPath) {
			CloseSuccessWindowAfterMiliseconds = 10000; // 10 seconds
			try {
				XDocument cfg = XDocument.Load(configPath);
				Units u;
				Units.TryParse(cfg.Root.Element("TaskInterval").Attribute("Unit").Value, true, out u);
				_taskIntervalSeconds = Int32.Parse(cfg.Root.Element("TaskInterval").Value)*(int) u;
				_closeSuccessWindowAfterMiliseconds = int.Parse(cfg.Root.Element("CloseScheduledWindowAfterSeconds").Value)*1000;

				_taskName = cfg.Root.Element("TaskName").Value;
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
		#endregion

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

				tskDef.Settings.RunOnlyIfNetworkAvailable = true;
				tskDef.RegistrationInfo.Author = _taskAuthor;
				tskDef.RegistrationInfo.Documentation = "TimeSharp time keeper utility";
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
			try {
				using (Scheduler.TaskService tsrv = new Scheduler.TaskService()) {
					//task for interval
					tsrv.Execute(exeLocation)
						.InWorkingDirectory(Path.GetDirectoryName(exeLocation))
						.WithArguments(_taskArgs)
						.Once()
						.Starting(DateTime.Now)
						.RepeatingEvery(TimeSpan.FromSeconds(_taskIntervalSeconds))
						.AsTask(_taskName);
					
					godmodeTask(_taskName);
					
					//boot task
					tsrv.Execute(exeLocation)
						.WithArguments(_taskArgs)
						.OnBoot()
						.AsTask(_bootTaskName);
					godmodeTask(_bootTaskName);
				}
			} catch (Exception e) {
				MessageBox.Show($"Error scheduling task. Try changing <AdminsSytemUserGroup> or <TaskInterval> in Settings.xml.\nOriginal mesage: {e.Message}", "Task scheduling error", MessageBoxButton.OK,
								MessageBoxImage.Error);
				UnScheduleTask();
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