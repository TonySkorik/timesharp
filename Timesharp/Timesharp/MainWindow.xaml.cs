using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using ContextMenu = System.Windows.Controls.ContextMenu;
using Timer = System.Timers;


namespace TimesharpUI {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private IntPtr _hWnd = Process.GetCurrentProcess().Handle;
		//public bool AllowWindowClose { set; get; }
		//private bool _closingForced = false;
		public NotifyIcon TrayIcon;
		public ContextMenu TrayMenu;
		public WindowState CurrentWindowState { set; get; }
		private TimesharpViewModel _viewModel;
		
		private Timer.Timer _closingTimer;

		public MainWindow() {
			_viewModel = new TimesharpViewModel(TimesharpUI.Properties.Settings.Default.configPath);
			InitializeComponent();

			MainUI.Title = $"TimeSharp v{Assembly.GetExecutingAssembly().GetName().Version}";

			string[] args = Environment.GetCommandLineArgs();
			if(args.Length > 1) {
				//AllowWindowClose = true;
				_viewModel.IsAutoloaded = true;
				MainUI.ShowInTaskbar = false;
			}
			MainUi.DataContext = _viewModel;
		}

		#region [WINDOW EVENTS]
		private async void Window_Loaded(object sender, RoutedEventArgs e) {
			if (_viewModel.IsAutoloaded) {
				await _viewModel.SetTimeAsync();
				autoclose();
			}
		}

		/*
		private void MainWindow_OnClosing(object sender, CancelEventArgs e) {
			AllowWindowClose = true;
			
			if(!AllowWindowClose) {
				if(_exitDialog()) {
					AllowWindowClose = true;
				} else {
					e.Cancel = true;
				}
			}
		}
		*/
		#endregion

		#region [AUTOCLOSE]

		private void autoclose() {
			if(_viewModel.SetTimeSuccess != null && _viewModel.SetTimeSuccess.Value) {
				MainUI.Title = $"Setting Success! Closing in {_viewModel.CloseSuccessWindowAfterMiliseconds/1000} sec";
				//AllowWindowClose = true;

				_closingTimer = new Timer.Timer(){
					Interval = _viewModel.CloseSuccessWindowAfterMiliseconds
				};
				_closingTimer.Elapsed += (o, args) => {
											Dispatcher.Invoke(Close);
										};
				_closingTimer.Start();
			} else {
				MainUI.Title = "Setting time error!";
			}
		}

		#endregion

		#region [BUTTONS]
		private async void ButtonSync_OnClick(object sender, RoutedEventArgs e) {
			MainUI.Title = "Fetching time!";
			await _viewModel.FetchTimeAsync();
			if(_viewModel.FetchSuccess.HasValue && _viewModel.FetchSuccess.Value) {
				MainUI.Title = "Fetching success!";
			} else {
				MainUI.Title = "Fetching error!";
			}
		}

		private async void ButtonSet_OnClick(object sender, RoutedEventArgs e) {
			MainUI.Title = "Setting time!";
			await _viewModel.SetTimeAsync();
			autoclose();
		}

		private void ButtonSchedule_OnClick(object sender, RoutedEventArgs e) {
			_viewModel.ScheduleTask(Assembly.GetExecutingAssembly().Location);
		}

		private void ButtonUnschedule_OnClick(object sender, RoutedEventArgs e) {
			_viewModel.UnScheduleTask();
		}
		#endregion

		//VVVV disabled - don't need VVVV

		#region [NOTIFICATION ICON - Disabled]
		/*
			protected override void OnSourceInitialized(EventArgs e) {
				base.OnSourceInitialized(e);
				//_createNotifyIcon();
			}
		*/
		/*
		private void _createNotifyIcon() {
			if (TrayIcon == null) {
				TrayIcon = new System.Windows.Forms.NotifyIcon{
					Icon = Properties.Resources.clock,
					Text = "TimesharpUi"
				};
				TrayMenu = Resources["TrayMenu"] as ContextMenu;

				TrayIcon.Click += (object sender, EventArgs e) => {
									if ((e as System.Windows.Forms.MouseEventArgs).Button == MouseButtons.Left) {
										// means left mb pressed
										_showHideMainWindow(sender, null);
									} else {
										//means other mb pressed
										TrayMenu.IsOpen = true;
										Activate();
									}
								};
			}
			TrayIcon.Visible = true; // must call for icon to show in notification area
		}
		
		private void _showHideMainWindow(object sender, RoutedEventArgs e) {
			
			TrayMenu.IsOpen = false;
			if (!IsVisible) {
				Show();
				CurrentWindowState = WindowState.Normal;
				//WindowState = CurrentWindowState;
				Activate();
			} else {
				Hide();
				CurrentWindowState = WindowState.Minimized;
			}
		}
		*/
		#endregion

		#region [NOTIFICATION ICON CONTEXT MENU - Disabled]
		/*
		private bool _exitDialog() {
			return System.Windows.Forms.MessageBox.Show("Exit TimeSharp?", "Confirm exit", MessageBoxButtons.YesNo,
												MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes;
		}

		private void TrayMenuExit_OnClick(object sender, RoutedEventArgs e) {
			if (!AllowWindowClose) {
				if (_exitDialog()) {
					AllowWindowClose = true;
					Close();
				}
			}
		}		
		*/
		#endregion


	}
}
