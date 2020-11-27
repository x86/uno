using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests
{
	[TestFixture]
	public partial class DataTemplate_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void DataTemplate_ElementName_Binding()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ListView.Listview_Datatemplate_Binding");

			var numListView = _app.Marked("NumbersListView");
			_app.WaitForElement(numListView);

			var expectedText = "UITests.Shared.Windows_UI_Xaml_Controls.ListView.ListViewDataTemplateBindingViewModel";

			var actualText = numListView.Descendant().Marked("DCElName").GetDependencyPropertyValue("Text");

			Assert.AreEqual(expectedText, actualText);
		}
	}
}
