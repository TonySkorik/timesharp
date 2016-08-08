using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Scheduler = Microsoft.Win32.TaskScheduler;

namespace TimesharpUi {
	public class TimesharpViewModel:INotifyPropertyChanged {
		
		#region [NOTIFY PROPERTY CHANGED]
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion

		#region [P & F]

		public const string _taskName = "TimeSharp_sync";

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

		public TimesharpViewModel() {
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

		public bool ScheduleTask(string exeLocation) {
			using (Scheduler.TaskService tsrv = new Scheduler.TaskService()) {
				
				tsrv.Execute(exeLocation)
					.WithArguments("Sceduled_start")
					.Once()
					.Starting(DateTime.Now)
					.RepeatingEvery(TimeSpan.FromHours(3))
					.AsTask(_taskName).Definition.RegistrationInfo.Author= System.Security.Principal.WindowsIdentity.GetCurrent().Name;
				/*
				Scheduler.TaskDefinition td = tsrv.NewTask();
				td.RegistrationInfo.Description = _taskName;
				td.RegistrationInfo.Author = _taskAuthor;
				td.Triggers.Add(new Scheduler.CustomTrigger());
				*/
			}
			return checkTaskIsScheduled();
		}

		public bool UnScheduleTask() {
			using(Scheduler.TaskService tsrv = new Scheduler.TaskService()) {
				tsrv.RootFolder.DeleteTask(_taskName,true);
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
