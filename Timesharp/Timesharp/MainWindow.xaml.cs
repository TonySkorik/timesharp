using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using ContextMenu = System.Windows.Controls.ContextMenu;


namespace TimesharpUi {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private IntPtr _hWnd = Process.GetCurrentProcess().Handle;
		public bool AllowWindowClose { set; get; }
		private bool _closingForced = false;
		public NotifyIcon TrayIcon;
		public ContextMenu TrayMenu;
		public WindowState CurrentWindowState { set; get; }

		public MainWindow() {
			InitializeComponent();
		}

		protected override void OnSourceInitialized(EventArgs e) {
			base.OnSourceInitialized(e);
			_createNotifyIcon();
		}
		#region [WINDOW EVENTS]
		private void Window_Loaded(object sender, RoutedEventArgs e) {
			//MainUi.DataContext = new TimesharpViewModel();
			AllowWindowClose = false;
		}
		private void MainWindow_OnClosing(object sender, CancelEventArgs e) {
			if(!AllowWindowClose) {
				if(_exitDialog()) {
					AllowWindowClose = true;
				} else {
					e.Cancel = true;
				}
			}
		}

		#endregion

		#region [NOTIFICATION ICON]
		private void _createNotifyIcon() {
			if (TrayIcon == null) {

				TrayIcon = new System.Windows.Forms.NotifyIcon{

					Icon = TimesharpUi.Properties.Resources.clock,
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
		#endregion

		#region [NOTIFICATION AREA MENU]

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
		private void SyncSetMenu_OnClick(object sender, RoutedEventArgs e) {
		}
		#endregion
		
	}
}
