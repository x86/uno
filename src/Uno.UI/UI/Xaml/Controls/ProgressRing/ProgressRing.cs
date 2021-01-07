#if XAMARIN || UNO_REFERENCE_API
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

// Keep this formatting (with the space) for the WinUI upgrade tooling.
using Microsoft .UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
{
	public partial class ProgressRing : Control
	{
		public ProgressRing()
		{
			DefaultStyleKey = typeof(ProgressRing);
		}


		/// <summary>
		/// Gets or sets a value that indicates whether the <see cref="ProgressRing"/> is showing progress.
		/// </summary>
		public bool IsActive
		{
			get { return (bool)GetValue(IsActiveProperty); }
			set { SetValue(IsActiveProperty, value); }
		}

		public static DependencyProperty IsActiveProperty { get; } =
			DependencyProperty.Register("IsActive", typeof(bool), typeof(ProgressRing), new PropertyMetadata(defaultValue: false, propertyChangedCallback: OnIsActiveChanged));

		private static void OnIsActiveChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var progressRing = (ProgressRing)dependencyObject;
			var isActive = (bool)args.NewValue;

			if (progressRing.IsLoaded)
			{
				VisualStateManager.GoToState(progressRing, isActive ? "Active" : "Inactive", false);
			}

			progressRing.OnIsActiveChangedPartial(isActive);
		}

		partial void OnUnloadedPartial();

		partial void OnIsActiveChangedPartial(bool newValue);

#if !UNO_REFERENCE_API && !__MACOS__ && !__NETSTD_REFERENCE__

		private protected override void OnLoaded()
		{
			base.OnLoaded();
			// The initial call to OnIsActiveChanged fires before ProgressRing is Loaded, so we also need to set a proper VisualState here
			VisualStateManager.GoToState(this, IsActive ? "Active" : "Inactive", false);

			OnLoadedPartial();
		}

		partial void OnLoadedPartial();

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			OnUnloadedPartial();
		}
#endif

		public ProgressRingTemplateSettings TemplateSettings
		{
			get
			{
				var result = new ProgressRingTemplateSettings()
				{
					EllipseDiameter = 3,
					MaxSideLength = 100
				};

				var size = Width.IsNaN() ? MinWidth : Width; // Strange, but ActualWidth is not working correctly here
				result.EllipseOffset = new Thickness(size * (Math.Sqrt(2) - 1) / 2); // This is the difference between inscribed and circumscribed circle, it ensures that dots will be visible after control rectangle clipping

				return result;
			}
		}
	}
}
#endif
