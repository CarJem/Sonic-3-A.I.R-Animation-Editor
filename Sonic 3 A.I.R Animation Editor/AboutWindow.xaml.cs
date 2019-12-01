﻿using System;
using System.Diagnostics;
using System.Windows;
using System.IO;
using System.Reflection;


namespace Sonic_3_AIR_Animation_Editor
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    partial class AboutWindow : Window
	{
		public AboutWindow()
		{
			InitializeComponent();
			Title = String.Format("About {0}", AssemblyTitle);
			labelProductName.Text = AssemblyProduct;
			labelVersion.Text = String.Format("Version {0}", App.Version);
			buildDateLabel.Text = String.Format("Build Date: {0}", GetBuildTime) + Environment.NewLine + String.Format("Architecture: {0}", GetProgramType);
			labelCopyright.Text = AssemblyCopyright;

		}

		#region Assembly Attribute Accessors

		public string AssemblyTitle
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
				if (attributes.Length > 0)
				{
					AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
					if (titleAttribute.Title != "")
					{
						return titleAttribute.Title;
					}
				}
				return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
			}
		}

		public string GetProgramType
		{
			get
			{
				if (Environment.Is64BitProcess)
				{
					return "x64";
				}
				else
				{
					return "x86";
				}
			}
		}

		public string AssemblyProduct
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
				if (attributes.Length == 0)
				{
					return "";
				}
				return ((AssemblyProductAttribute)attributes[0]).Product;
			}
		}

		public string AssemblyCopyright
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
				if (attributes.Length == 0)
				{
					return "";
				}
				return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
			}
		}

		private string GetBuildTime
		{
			get
			{
				DateTime buildDate = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).LastWriteTime;
				String buildTimeString = buildDate.ToString();
				return buildTimeString;
			}

		}
		#endregion

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{

		}

		private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}
	}
}
