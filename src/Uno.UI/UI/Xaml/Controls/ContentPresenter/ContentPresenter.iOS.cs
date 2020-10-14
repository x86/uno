#nullable enable
using System;
using System.Drawing;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Views.Controls;
using Uno.UI.DataBinding;
using UIKit;
using Windows.UI.Xaml.Shapes;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Declares a Content presenter
	/// </summary>
	/// <remarks>
	/// The content presenter is used for compatibility with WPF concepts,
	/// but the ContentSource property is not available, because there are ControlTemplates for now.
	/// </remarks>
	public partial class ContentPresenter
	{
		private readonly BorderLayerRenderer _borderRenderer = new BorderLayerRenderer();

		public ContentPresenter()
		{
			InitializeContentPresenter();

			this.RegisterLoadActions(UpdateBorder, () => _borderRenderer.Clear());
		}

		private void SetUpdateTemplate()
		{
			UpdateContentTemplateRoot();
			SetNeedsLayout();
		}

		partial void RegisterContentTemplateRoot(UIView contentTemplateRoot)
		{
			if (Subviews.Length != 0)
			{
				throw new Exception("A Xaml control may not contain more than one child.");
			}

			contentTemplateRoot.Frame = Bounds;
			contentTemplateRoot.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			AddSubview(contentTemplateRoot);
		}

		partial void UnregisterContentTemplateRoot()
		{
			// If Content is a view it may have already been set as Content somewhere else in certain scenarios, eg virtualizing collections
			var contentTemplateRoot = ContentTemplateRoot;

			if (ReferenceEquals(contentTemplateRoot?.Superview, this))
			{
				contentTemplateRoot.RemoveFromSuperview();
			}
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();
			UpdateBorder();
		}

		private void UpdateBorder()
		{
			if (IsLoaded)
			{
				_borderRenderer.UpdateLayer(
					this,
					Background,
					BorderThickness,
					BorderBrush,
					CornerRadius,
					null
				);
			}
		}

		partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue)
		{
			UpdateBorder();
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => CornerRadius == CornerRadius.None;

		bool ICustomClippingElement.ForceClippingToLayoutSlot => false;
	}
}
