using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;


namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	[Sample]
	public sealed partial class Listview_Datatemplate_Binding : Page
    {
        public Listview_Datatemplate_Binding()
        {
            this.InitializeComponent();

            this.DataContext = new ListViewDataTemplateBindingViewModel();
		}
    }
}
