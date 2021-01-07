using Android.Views;
using Uno.Extensions;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;

namespace Windows.UI.Xaml
{
	public partial class ElementStub : FrameworkElement
	{
		public ElementStub()
		{
			Visibility = Visibility.Collapsed;
		}

		private View SwapViews(View oldView, Func<View> newViewProvider)
		{
			var parentViewGroup = oldView?.Parent as ViewGroup;
			var currentPosition = parentViewGroup?.GetChildren().IndexOf(oldView);

			if (currentPosition != null && currentPosition.Value != -1)
			{
				var newView = newViewProvider();
				parentViewGroup.RemoveViewAt(currentPosition.Value);

				var UnoViewGroup = parentViewGroup as UnoViewGroup;

				if (UnoViewGroup != null)
				{
					var newContentAsFrameworkElement = this as IFrameworkElement;
					if (newContentAsFrameworkElement != null)
					{
						newContentAsFrameworkElement.TemplatedParent = (UnoViewGroup as IFrameworkElement)?.TemplatedParent;
					}
					UnoViewGroup.AddView(newView, currentPosition.Value);
				}
				else
				{
					parentViewGroup.AddView(newView, currentPosition.Value);
				}

				return newView;
			}

			return null;
		}
	}
}
