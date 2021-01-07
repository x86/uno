﻿#if NET461 || __WASM__
#pragma warning disable CS0067
#endif

using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.Logging;
using Uno.Disposables;
using System.Linq;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Uno.UI;
using Uno;
using Uno.UI.Controls;
using Uno.UI.Media;
using System;
using System.Collections;
using System.Numerics;
using System.Reflection;
using Windows.UI.Xaml.Markup;
using Microsoft.Extensions.Logging;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Core;

#if __IOS__
using UIKit;
#endif

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject, IXUidProvider, IUIElement
	{
		private readonly SerialDisposable _clipSubscription = new SerialDisposable();
		private XamlRoot _xamlRoot = null;
		private string _uid;

		public static void RegisterAsScrollPort(UIElement element)
			=> element.IsScrollPort = true;

		internal bool IsScrollPort { get; private set; }

		// This are fulfilled by the ScrollViewer for the EffectiveViewport computation,
		// but it should actually be computed based on clipping vs desired size.
		internal Point ScrollOffsets { get; private protected set; }

		/// <summary>
		/// Is this view set to Window.Current.Content?
		/// </summary>
		internal bool IsWindowRoot { get; set; }

		private void Initialize()
		{
			this.SetValue(KeyboardAcceleratorsProperty, new List<KeyboardAccelerator>(0), DependencyPropertyValuePrecedences.DefaultValue);
		}

		string IXUidProvider.Uid
		{
			get => _uid;
			set
			{
				_uid = value;
				OnUidChangedPartial();
			}
		}

		partial void OnUidChangedPartial();

		public XamlRoot XamlRoot
		{
			get => _xamlRoot ?? XamlRoot.Current;
			set => _xamlRoot = value;
		}

		#region Clip DependencyProperty

		public RectangleGeometry Clip
		{
			get { return (RectangleGeometry)this.GetValue(ClipProperty); }
			set { this.SetValue(ClipProperty, value); }
		}

		public static DependencyProperty ClipProperty { get; } =
			DependencyProperty.Register(
				"Clip",
				typeof(RectangleGeometry),
				typeof(UIElement),
				new FrameworkPropertyMetadata(
					null,
					(s, e) => ((UIElement)s)?.OnClipChanged(e)
				)
			);

		private void OnClipChanged(DependencyPropertyChangedEventArgs e)
		{
			var geometry = e.NewValue as RectangleGeometry;

			ApplyClip();
			_clipSubscription.Disposable = geometry.RegisterDisposableNestedPropertyChangedCallback(
				(_, __) => ApplyClip(),
				new[] { RectangleGeometry.RectProperty },
				new[] { Geometry.TransformProperty },
				new[] { Geometry.TransformProperty, TranslateTransform.XProperty },
				new[] { Geometry.TransformProperty, TranslateTransform.YProperty }
			);
		}

		#endregion

		#region RenderTransform Dependency Property

		/// <summary>
		/// This is a Transformation for a UIElement.  It binds the Render Transform to the View
		/// </summary>
		public Transform RenderTransform
		{
			get => (Transform)this.GetValue(RenderTransformProperty);
			set => this.SetValue(RenderTransformProperty, value);
		}

		/// <summary>
		/// Backing dependency property for <see cref="RenderTransform"/>
		/// </summary>
		public static DependencyProperty RenderTransformProperty { get; } =
			DependencyProperty.Register("RenderTransform", typeof(Transform), typeof(UIElement), new FrameworkPropertyMetadata(null, (s, e) => OnRenderTransformChanged(s, e)));

		private static void OnRenderTransformChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var view = (UIElement)dependencyObject;

			view._renderTransform?.Dispose();

			if (args.NewValue is Transform transform)
			{
				view._renderTransform = new NativeRenderTransformAdapter(view, transform, view.RenderTransformOrigin);
				view.OnRenderTransformSet();
			}
			else
			{
				// Sanity
				view._renderTransform = null;
			}
		}

		internal NativeRenderTransformAdapter _renderTransform;

		partial void OnRenderTransformSet();
		#endregion

		#region RenderTransformOrigin Dependency Property

		/// <summary>
		/// This is a Transformation for a UIElement.  It binds the Render Transform to the View
		/// </summary>
		public Point RenderTransformOrigin
		{
			get => (Point)this.GetValue(RenderTransformOriginProperty);
			set => this.SetValue(RenderTransformOriginProperty, value);
		}

		// Using a DependencyProperty as the backing store for RenderTransformOrigin.  This enables animation, styling, binding, etc...
		public static DependencyProperty RenderTransformOriginProperty { get; } =
			DependencyProperty.Register("RenderTransformOrigin", typeof(Point), typeof(UIElement), new FrameworkPropertyMetadata(default(Point), (s, e) => OnRenderTransformOriginChanged(s, e)));

		private static void OnRenderTransformOriginChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var view = (UIElement)dependencyObject;
			var point = (Point)args.NewValue;

			view._renderTransform?.UpdateOrigin(point);
		}
		#endregion

		public GeneralTransform TransformToVisual(UIElement visual)
			=> new MatrixTransform { Matrix = new Matrix(GetTransform(from: this, to: visual)) };

		internal static Matrix3x2 GetTransform(UIElement from, UIElement to)
		{
			if (from == to)
			{
				return Matrix3x2.Identity;
			}

#if UNO_REFERENCE_API // Depth is defined properly only on WASM and Skia
			// If possible we try to navigate the tree upward so we have a greater chance
			// to find an element in the parent hierarchy of the other element.
			if (to is { } && from.Depth < to.Depth)
			{
				return GetTransform(to, from).Inverse();
			}
#endif

			var matrix = Matrix3x2.Identity;
			double offsetX = 0.0, offsetY = 0.0;
			var elt = from;
			do
			{
				var layoutSlot = elt.LayoutSlotWithMarginsAndAlignments;
				var transform = elt.RenderTransform;
				if (transform is null)
				{
					// As this is the common case, avoid Matrix computation when a basic addition is sufficient
					offsetX += layoutSlot.X;
					offsetY += layoutSlot.Y;
				}
				else
				{
					var origin = elt.RenderTransformOrigin;
					var transformMatrix = origin == default
						? transform.MatrixCore
						: transform.ToMatrix(origin, layoutSlot.Size);

					// First apply any pending arrange offset that would have been impacted by this RenderTransform (eg. scaled)
					// Friendly reminder: Matrix multiplication is usually not commutative ;)
					matrix *= Matrix3x2.CreateTranslation((float)offsetX, (float)offsetY);
					matrix *= transformMatrix;

					offsetX = layoutSlot.X;
					offsetY = layoutSlot.Y;
				}

#if !__SKIA__
				// On Skia, the Scrolling is managed by the ScrollContentPresenter (as UWP), which is flagged as IsScrollPort.
				// Note: We should still add support for the zoom factor ... which is not yet supported on Skia.
				if (elt is ScrollViewer sv)
				{
					var zoom = sv.ZoomFactor;
					if (zoom != 1)
					{
						matrix *= Matrix3x2.CreateTranslation((float)offsetX, (float)offsetY);
						matrix *= Matrix3x2.CreateScale(zoom);

						offsetX = -sv.HorizontalOffset;
						offsetY = -sv.VerticalOffset;
					}
					else
					{
						offsetX -= sv.HorizontalOffset;
						offsetY -= sv.VerticalOffset;
					}
				}
				else
#endif
				if (elt.IsScrollPort) // Custom scroller
				{
					offsetX -= elt.ScrollOffsets.X;
					offsetY -= elt.ScrollOffsets.Y;
				}

			} while (elt.TryGetParentUIElementForTransformToVisual(out elt, ref offsetX, ref offsetY) && elt != to); // If possible we stop as soon as we reach 'to'

			matrix *= Matrix3x2.CreateTranslation((float)offsetX, (float)offsetY);

			if (to != null && elt != to)
			{
				// Unfortunately we didn't find the 'to' in the parent hierarchy,
				// so matrix == fromToRoot and we now have to compute the transform 'toToRoot'.
				// Note: We do not propagate the 'intermediatesSelector' as cached transforms would be irrelevant
				var toToRoot = GetTransform(to, null);
				var rootToTo = toToRoot.Inverse();

				matrix *= rootToTo;
			}

			return matrix;
		}

#if !__IOS__ && !__ANDROID__ // This is the default implementation, but it can be customized per platform
		/// <summary>
		/// Note: Offsets are only an approximation which does not take in consideration possible transformations
		///	applied by a 'UIView' between this element and its parent UIElement.
		/// </summary>
		private bool TryGetParentUIElementForTransformToVisual(out UIElement parentElement, ref double offsetX, ref double offsetY)
		{
			var parent = this.GetParent();
			switch (parent)
			{
				case UIElement elt:
					parentElement = elt;
					return true;

				case null:
					parentElement = null;
					return false;

				default:
					Application.Current.RaiseRecoverableUnhandledException(new InvalidOperationException("Found a parent which is NOT a UIElement."));

					parentElement = null;
					return false;
			}
		}
#endif

		protected virtual void OnIsHitTestVisibleChanged(bool oldValue, bool newValue)
		{
			OnIsHitTestVisibleChangedPartial(oldValue, newValue);
		}

		partial void OnIsHitTestVisibleChangedPartial(bool oldValue, bool newValue);

		partial void OnOpacityChanged(DependencyPropertyChangedEventArgs args);

		private protected virtual void OnContextFlyoutChanged(FlyoutBase oldValue, FlyoutBase newValue)
		{
			if (newValue != null)
			{
				RightTapped += OpenContextFlyout;
			}
			else
			{
				RightTapped -= OpenContextFlyout;
			}
		}

		private void OpenContextFlyout(object sender, RightTappedRoutedEventArgs args)
		{
			if (this is FrameworkElement fe)
			{
				ContextFlyout?.ShowAt(
					placementTarget: fe,
					showOptions: new FlyoutShowOptions()
					{
						Position = args.GetPosition(this)
					}
				);
			}
		}

		internal bool IsRenderingSuspended { get; set; }

		[ThreadStatic]
		private static bool _isInUpdateLayout;

		private const int MaxLayoutIterations = 250;

		public void UpdateLayout()
		{
			if (_isInUpdateLayout)
			{
				return;
			}

			var root = Windows.UI.Xaml.Window.Current.RootElement;
			if (root is null)
			{
				return;
			}

			try
			{
				_isInUpdateLayout = true;

#if __MACOS__ || __IOS__ // IsMeasureDirty and IsArrangeDirty are not available on iOS / macOS
				root.Measure(LayoutInformation.GetLayoutSlot(root).Size);
				root.Arrange(LayoutInformation.GetLayoutSlot(root));
#elif __ANDROID__
				for (var i = 0; i < MaxLayoutIterations; i++)
				{
					// On Android, Measure and arrange are the same
					if (root.IsMeasureDirty)
					{
						root.Measure(LayoutInformation.GetLayoutSlot(root).Size);
						root.Arrange(LayoutInformation.GetLayoutSlot(root));
					}
					else
					{
						return;
					}
				}
#else
				for (var i = 0; i < MaxLayoutIterations; i++)
				{
					if (root.IsMeasureDirty)
					{
						root.Measure(LayoutInformation.GetLayoutSlot(root).Size);
					}
					else if (root.IsArrangeDirty)
					{
						root.Arrange(LayoutInformation.GetLayoutSlot(root));
					}
					else
					{
						return;
					}
				}

				throw new InvalidOperationException("Layout cycle detected.");
#endif
			}
			finally
			{
				_isInUpdateLayout = false;
			}
		}

		internal void ApplyClip()
		{
			Rect rect;

			if (Clip == null)
			{
				rect = Rect.Empty;

				if (NeedsClipToSlot)
				{
#if UNO_REFERENCE_API
					rect = new Rect(0, 0, RenderSize.Width, RenderSize.Height);
#else
					rect = ClippedFrame ?? Rect.Empty;
#endif
				}
			}
			else
			{
				rect = Clip.Rect;

				// Apply transform to clipping mask, if any
				if (Clip.Transform != null)
				{
					rect = Clip.Transform.TransformBounds(rect);
				}
			}

			ApplyNativeClip(rect);
			OnViewportUpdated(rect);
		}

		partial void ApplyNativeClip(Rect rect);
		private protected virtual void OnViewportUpdated(Rect viewport) { } // Not "Changed" as it might be the same as previous

		internal static object GetDependencyPropertyValueInternal(DependencyObject owner, string dependencyPropertyName)
		{
			var dp = DependencyProperty.GetProperty(owner.GetType(), dependencyPropertyName);
			return dp == null ? null : owner.GetValue(dp);
		}

		/// <summary>
		/// Sets the specified dependency property value using the format "name|value"
		/// </summary>
		/// <param name="dependencyPropertyNameAndValue">The name and value of the property</param>
		/// <returns>The currenty set value at the Local precedence</returns>
		/// <remarks>
		/// The signature of this method was chosen to work around a limitation of Xamarin.UITest with regards to
		/// parameters passing on iOS, where the number of parameters follows a unconventional set of rules. Using
		/// a single parameter with a simple delimitation format fits all platforms with little overhead.
		/// </remarks>
		internal static string SetDependencyPropertyValueInternal(DependencyObject owner, string dependencyPropertyNameAndValue)
		{
			var s = dependencyPropertyNameAndValue;
			var index = s.IndexOf("|");

			if (index != -1)
			{
				var dependencyPropertyName = s.Substring(0, index);
				var value = s.Substring(index + 1);

				if (DependencyProperty.GetProperty(owner.GetType(), dependencyPropertyName) is DependencyProperty dp)
				{
					if (owner.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						owner.Log().LogDebug($"SetDependencyPropertyValue({dependencyPropertyName}) = {value}");
					}

					owner.SetValue(dp, XamlBindingHelper.ConvertValue(dp.Type, value));

					return owner.GetValue(dp)?.ToString();
				}
				else
				{
					if (owner.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						owner.Log().LogDebug($"Failed to find property [{dependencyPropertyName}] on [{owner}]");
					}
					return "**** Failed to find property";
				}
			}
			else
			{
				return "**** Invalid property and value format.";
			}
		}

		/// <summary>
		/// Backing property for <see cref="LayoutInformation.GetAvailableSize(UIElement)"/>
		/// </summary>
		Size IUIElement.LastAvailableSize { get; set; }
		/// <summary>
		/// Gets the 'availableSize' of the last Measure
		/// </summary>
		internal Size LastAvailableSize => ((IUIElement)this).LastAvailableSize;

		/// <summary>
		/// Backing property for <see cref="LayoutInformation.GetLayoutSlot(FrameworkElement)"/>
		/// </summary>
		Rect IUIElement.LayoutSlot { get; set; }
		/// <summary>
		/// Gets the 'finalSize' of the last Arrange
		/// </summary>
		internal Rect LayoutSlot => ((IUIElement)this).LayoutSlot;

		internal Rect LayoutSlotWithMarginsAndAlignments { get; set; } = default;

		internal bool NeedsClipToSlot { get; set; }

		/// <summary>
		/// Backing property for <see cref="LayoutInformation.GetDesiredSize(UIElement)"/>
		/// </summary>
		Size IUIElement.DesiredSize { get; set; }

#if !UNO_REFERENCE_API
		/// <summary>
		/// Provides the size reported during the last call to Measure.
		/// </summary>
		/// <remarks>
		/// DesiredSize INCLUDES MARGINS.
		/// </remarks>
		public Size DesiredSize => ((IUIElement)this).DesiredSize;

		/// <summary>
		/// Provides the size reported during the last call to Arrange (i.e. the ActualSize)
		/// </summary>
		public Size RenderSize { get; internal set; }

		public virtual void Measure(Size availableSize)
		{
		}

#if !UNO_REFERENCE_API
		/// <summary>
		/// This is the Frame that should be used as "available Size" for the Arrange phase.
		/// </summary>
		internal Rect? ClippedFrame;
#endif

		public virtual void Arrange(Rect finalRect)
		{
		}

		public void InvalidateMeasure()
		{
			if (this is IFrameworkElement frameworkElement)
			{
				IFrameworkElementHelper.InvalidateMeasure(frameworkElement);
			}
			else
			{
				this.Log().Warn("Calling InvalidateMeasure on a UIElement that is not a FrameworkElement has no effect.");
			}

			OnInvalidateMeasure();
		}

		internal protected virtual void OnInvalidateMeasure()
		{
		}

		[global::Uno.NotImplemented]
		public void InvalidateArrange()
		{
			InvalidateMeasure();
#if __IOS__ || __MACOS__
			IsArrangeDirty = true;
#endif
		}
#endif

		public void StartBringIntoView()
		{
			StartBringIntoView(new BringIntoViewOptions());
		}

		public void StartBringIntoView(BringIntoViewOptions options)
		{
#if __IOS__ || __ANDROID__
			Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				// This currently doesn't support nested scrolling.
				// This currently doesn't support BringIntoViewOptions.AnimationDesired.
				var scrollContentPresenter = this.FindFirstParent<IScrollContentPresenter>();
				scrollContentPresenter?.MakeVisible(this, options.TargetRect ?? Rect.Empty);
			});
#endif
		}

		internal virtual bool IsViewHit() => true;

		internal double LayoutRound(double value)
		{
#if __SKIA__
			double scaleFactor = GetScaleFactorForLayoutRounding();

			return LayoutRound(value, scaleFactor);
#else
			return value;
#endif
		}

		internal Rect LayoutRound(Rect value)
		{
#if __SKIA__
			double scaleFactor = GetScaleFactorForLayoutRounding();

			return new Rect(
				x: LayoutRound(value.X, scaleFactor),
				y: LayoutRound(value.Y, scaleFactor),
				width: LayoutRound(value.Width, scaleFactor),
				height: LayoutRound(value.Height, scaleFactor)
			);
#else
			return value;
#endif
		}

		internal Thickness LayoutRound(Thickness value)
		{
#if __SKIA__
			double scaleFactor = GetScaleFactorForLayoutRounding();

			return new Thickness(
				top: LayoutRound(value.Top, scaleFactor),
				bottom: LayoutRound(value.Bottom, scaleFactor),
				left: LayoutRound(value.Left, scaleFactor),
				right: LayoutRound(value.Right, scaleFactor)
			);
#else
			return value;
#endif
		}

		internal Vector2 LayoutRound(Vector2 value)
		{
#if __SKIA__
			double scaleFactor = GetScaleFactorForLayoutRounding();

			return new Vector2(
				x: (float)LayoutRound(value.X, scaleFactor),
				y: (float)LayoutRound(value.Y, scaleFactor)
			);
#else
			return value;
#endif
		}

		internal Size LayoutRound(Size value)
		{
#if __SKIA__
			double scaleFactor = GetScaleFactorForLayoutRounding();

			return new Size(
				width: LayoutRound(value.Width, scaleFactor),
				height: LayoutRound(value.Height, scaleFactor)
			);
#else
			return value;
#endif
		}

		private double LayoutRound(double value, double scaleFactor)
		{
			double returnValue = value;

			// Plateau scale is applied as a scale transform on the root element. All values computed by layout
			// will be multiplied by this scale. Layout assumes a plateau of 1, and values rounded to
			// integers at layout plateau of 1 will not be integer values when scaled by plateau transform, causing
			// sub-pixel rendering at plateau != 1. To correctly put element edges at device pixel boundaries, layout rounding
			// needs to take plateau into account and produce values that will be rounded after plateau scaling is applied,
			// i.e. multiples of 1/Plateau.
			if (scaleFactor != 1.0)
			{
				returnValue = XcpRound(returnValue * scaleFactor) / scaleFactor;
			}
			else
			{
				// Avoid unnecessary multiply/divide at scale factor 1.
				returnValue = XcpRound(returnValue);
			}

			return returnValue;
		}

		// GetScaleFactorForLayoutRounding() returns the plateau scale in most cases. For ScrollContentPresenter children though,
		// the plateau scale gets combined with the owning ScrollViewer's ZoomFactor if headers are present.
		private double GetScaleFactorForLayoutRounding()
		{
			// TODO use actual scaling based on current transforms.
			return global::Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi / 96.0f; // 100%
		}

		int XcpRound(double x)
			=> (int)Math.Floor(x + 0.5);

#if HAS_UNO_WINUI
		#region FocusState DependencyProperty

		public FocusState FocusState
		{
			get { return (FocusState)GetValue(FocusStateProperty); }
			internal set { SetValue(FocusStateProperty, value); }
		}

		public static DependencyProperty FocusStateProperty { get; } =
			DependencyProperty.Register(
				"FocusState",
				typeof(FocusState),
				typeof(UIElement),
				new FrameworkPropertyMetadata(
					(FocusState)FocusState.Unfocused
				)
			);

		#endregion

		#region IsTabStop DependencyProperty

		public bool IsTabStop
		{
			get { return (bool)GetValue(IsTabStopProperty); }
			set { SetValue(IsTabStopProperty, value); }
		}

		public static DependencyProperty IsTabStopProperty { get; } =
			DependencyProperty.Register(
				"IsTabStop",
				typeof(bool),
				typeof(UIElement),
				new FrameworkPropertyMetadata(
					(bool)true,
					(s, e) => ((Control)s)?.OnIsTabStopChanged((bool)e.OldValue, (bool)e.NewValue)
				)
			);
		#endregion

		private protected virtual void OnIsTabStopChanged(bool oldValue, bool newValue) { }
#endif

#if DEBUG
		/// <summary>
		/// A helper method while debugging to get the theme resource, if any, assigned to <paramref name="propertyName"/>.
		/// </summary>
		internal string GetThemeSource(string propertyName)
		{
			if (!propertyName.EndsWith("Property"))
			{
				propertyName += "Property";
			}
			var propInfo = GetType().GetTypeInfo().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
			var dp = propInfo.GetValue(null) as DependencyProperty;
			var bindings = (this as IDependencyObjectStoreProvider).Store.GetResourceBindingsForProperty(dp);
			if (bindings.Any())
			{
				var output = "";
				foreach (var binding in bindings)
				{
					output += $"{binding.ResourceKey} ({binding.Precedence}), ";
				}

				return output;
			}
			else
			{
				return "[None]";
			}
		}

		/// <summary>
		/// Lists all resource keys associated with <paramref name="resource"/>, both in local code and the framework. This is spectacularly
		/// inefficient and only useful for providing extra information while debugging.
		/// </summary>
		/// <remarks>
		/// Currently won't work with value-typed resources (eg double, Thickness) since it uses ReferencEquals() and they will be boxed.
		/// </remarks>
		internal object[] GetKeysForResource(object resource)
		{
			return Inner().ToArray();

			IEnumerable<object> Inner()
			{
				var fe = this as FrameworkElement;
				while (fe != null)
				{
					foreach (var key in TryFindResource(fe.Resources))
					{
						yield return key;
					}

					fe = fe.Parent as FrameworkElement;
				}

				foreach (var key in TryFindResource(Application.Current.Resources))
				{
					yield return key;
				}
				foreach (var key in TryFindResource(Uno.UI.GlobalStaticResources.MasterDictionary))
				{
					yield return key;
				}

				IEnumerable<object> TryFindResource(ResourceDictionary resourceDictionary)
				{
					foreach (var kvp in resourceDictionary)
					{
						if (ReferenceEquals(resource, kvp.Value)) // TODO: doesn't work for value types
						{
							yield return kvp.Key;
						}
					}

					foreach (var mergedDict in resourceDictionary.MergedDictionaries)
					{
						foreach (var key in TryFindResource(mergedDict))
						{
							yield return key;
						}
					}

					foreach (var themeDict in resourceDictionary.ThemeDictionaries.Values.OfType<ResourceDictionary>())
					{
						foreach (var key in TryFindResource(themeDict))
						{
							yield return key;
						}
					}
				}
			}
		}
#endif
	}
}
