using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TimesharpUi {
	public class TimesharpViewModel:INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private DateTime _dtSyncronized;
		public DateTime DtSyncronized {
			set {
				_dtSyncronized = value;
				NotifyPropertyChanged();
			}
			get {
				return _dtSyncronized;
			}
		}
		private bool _syncFailed;
		public bool SyncFailed {
			get { return _syncFailed; }
			set {
				_syncFailed = value;
				NotifyPropertyChanged();
			}
		}
		private bool _setTimeFailed;
		public bool SetTimeFailed {
			get { return _setTimeFailed; }
			set {
				_setTimeFailed = value;
				NotifyPropertyChanged();
			}
		}

		public TimesharpViewModel() {
			DtSyncronized = DateTime.MinValue;
			SyncTime();
			SetTimeFailed = false;
		}

		public void SyncTime() {
			DateTime dt = Timesharp.GetNistDtTcp(true);
			if(dt == DateTime.MinValue) {
				//means TCP method failed
				dt = Timesharp.GetNistDtWeb(true);
				if(dt == DateTime.MinValue) {
					//web method failed
					SyncFailed = true;
				}
			}
			DtSyncronized = dt;
		}

		public void SetTime() {
			SyncTime();
			if (!Timesharp.SetTime(_dtSyncronized)) {
				//time set failed
				SetTimeFailed = true;
			}
		}
	}
}
