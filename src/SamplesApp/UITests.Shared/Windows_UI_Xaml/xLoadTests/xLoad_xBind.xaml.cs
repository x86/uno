﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml.xLoadTests
{
	[SampleControlInfo("xLoad", "xLoad_xBind")]
	public sealed partial class xLoad_xBind : UserControl
    {
		public bool IsLoad
		{
			get { return (bool)GetValue(IsLoadProperty); }
			set { SetValue(IsLoadProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsLoad.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsLoadProperty =
			DependencyProperty.Register("IsLoad", typeof(bool), typeof(xLoad_xBind), new PropertyMetadata(false));

		public xLoad_xBind()
        {
            this.InitializeComponent();
        }
    }
}
