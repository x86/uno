﻿using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls;
using System.ComponentModel;

namespace Uno.UI
{
	public static class FeatureConfiguration
	{
		public static class AutomationPeer
		{
			/// <summary>
			/// Enable a mode that simplifies accessibility by automatically grouping accessible elements into top-level accessible elements. The default value is false.
			/// </summary>
			/// <remarks>
			/// When enabled, the accessibility name of top-level accessible elements (elements that return a non-null AutomationPeer in <see cref="UIElement.OnCreateAutomationPeer()"/> and/or have <see cref="AutomationProperties.Name" /> set to a non-empty string)
			/// will be an aggregate of the accessibility name of all child accessible elements.
			///
			/// For example, if you have a <see cref="Button"/> that contains 3 <see cref="TextBlock"/> "A" "B" "C", the accessibility name of the <see cref="Button"/> will be "A, B, C".
			/// These 3 <see cref="TextBlock"/> will also be automatically excluded from accessibility focus.
			///
			/// This greatly facilitates accessibility, as you would need to do this manually on UWP.
			///
			/// A limitation of this strategy is that you can't nest interactive elements, as children of an accessible elements are excluded from accessibility focus.
			/// For example, if you put a <see cref="Button"/> inside another <see cref="Button"/>, only the parent <see cref="Button"/> will be focusable.
			/// This happens to match a limitation of iOS, which does this by default and forces developers to make elements as siblings instead of nesting them.
			///
			/// To prevent a top-level accessible element from being accessible and make its children accessibility focusable, you can set <see cref="AutomationProperties.AccessibilityViewProperty"/> to <see cref="AccessibilityView.Raw"/>.
			///
			/// Note: This is incompatible with the way accessibility works on UWP.
			/// </remarks>
			public static bool UseSimpleAccessibility { get; set; } = false;
		}

		public static class ComboBox
		{
			/// <summary>
			/// This defines the default value of the <see cref="UI.Xaml.Controls.ComboBox.DropDownPreferredPlacementProperty"/>. (cf. Remarks.)
			/// </summary>
			/// <remarks>
			/// As this value is read only once when initializing the dependency property,
			/// make sure to define it in the early stages of you application initialization,
			/// before any UI related initialization (like generic styles init) and even before
			/// referencing the ** type ** ComboBox in any way.
			/// </remarks>
			public static Uno.UI.Xaml.Controls.DropDownPlacement DefaultDropDownPreferredPlacement { get; set; } = Uno.UI.Xaml.Controls.DropDownPlacement.Auto;
		}

		public static class CompositionTarget
		{
			/// <summary>
			/// The delay between invocations of the <see cref="Windows.UI.Xaml.Media.CompositionTarget.Rendering"/> event, in milliseconds.
			/// Lower values will increase the rate at which the event fires, at the expense of increased CPU usage.
			///
			/// This property is only used on WebAssembly.
			/// </summary>
			/// <remarks>The <see cref="Windows.UI.Xaml.Media.CompositionTarget.Rendering"/> event is used by Xamarin.Forms for WebAssembly for XF animations.</remarks>
			public static int RenderEventThrottle { get; set; } = 30;
		}

		public static class ContentPresenter
		{
			/// <summary>
			/// Enables the implicit binding Content of a ContentPresenter to the one of the TemplatedParent
			/// when this one is a ContentControl.
			/// It means you can put a `<ContentPresenter />` directly in the ControlTemplate and it will
			/// be bound automatically to its TemplatedPatent's Content.
			/// </summary>
			public static bool UseImplicitContentFromTemplatedParent { get; set; } = false;
		}

		public static class Control
		{
			/// <summary>
			/// Make the default value of VerticalContentAlignment and HorizontalContentAlignment be Stretch instead of Center
			/// </summary>
			public static bool UseLegacyContentAlignment { get; set; } = false;

			/// <summary>
			/// Enables the lazy materialization of <see cref="Windows.UI.Xaml.Controls.Control"/> template. This behavior
			/// is not aligned with UWP, which materializes templates immediately, making x:Name controls available
			/// in the constructor of a control.
			/// </summary>
			public static bool UseLegacyLazyApplyTemplate { get; set; } = false;

			/// <summary>
			/// If the call to "OnApplyTemplate" should be deferred to mimic UWP sequence of events.
			/// </summary>
			/// <remarks>
			/// Will never be deferred when .ApplyTemplate() is called explicitly.
			/// More information there: https://github.com/unoplatform/uno/issues/3519
			/// </remarks>
			public static bool UseDeferredOnApplyTemplate { get; set; }
#if __ANDROID__ || __IOS__ || __MACOS__
				= false; // opt-in for iOS/Android/macOS
#else
				= true;
#endif
		}

		public static class DataTemplateSelector
		{
			/// <summary>
			/// When set the false (default value), a call to `SelectTemplateCore(object, DependencyObject)`
			/// will be made as fallback when the `SelectTemplateCore(object)` returns null.
			/// When set to true, only `SelectTemplateCore(object)` is called (Uno's legacy mode).
			/// </summary>
			public static bool UseLegacyTemplateSelectorOverload { get; set; } = false;
		}

		public static class DependencyObject
		{
			/// <summary>
			/// When set to true, the <see cref="DependencyObjectStore"/> will create hard references
			/// instead of weak references for some highly used fields, in common cases to improve the
			/// overall performance.
			/// </summary>
			/// <remarks>
			/// This feature is disabled on WebAssembly as it reveals or creates a memory corruption issue
			/// in the garbage collector. This can be revisited when upgrading tests to .NET 5+.
			/// See https://github.com/unoplatform/uno/issues/4730 for details.
			/// </remarks>
			public static bool IsStoreHardReferenceEnabled { get; set; }
				= true;
		}

		public static class Font
		{
			/// <summary>
			/// Defines the default font to be used when displaying symbols, such as in SymbolIcon.
			/// </summary>
			public static string SymbolsFont { get; set; } =
#if __SKIA__
				"ms-appx:///Assets/Fonts/uno-fluentui-assets.ttf#Symbols";
#elif !__ANDROID__
				"Symbols";
#else
				"ms-appx:///Assets/Fonts/uno-fluentui-assets.ttf#Symbols";
#endif
			/// <summary>
			/// Ignores text scale factor, resulting in a font size as dictated by the control.
			/// </summary>
			public static bool IgnoreTextScaleFactor { get; set; } = false;
		}

		public static class FrameworkElement
		{
			[Obsolete("This flag is no longer used.")]
			[EditorBrowsable(EditorBrowsableState.Never)]
			public static bool UseLegacyApplyStylePhase { get; set; }

			[Obsolete("This flag is no longer used.")]
			[EditorBrowsable(EditorBrowsableState.Never)]
			public static bool ClearPreviousOnStyleChange { get; set; }

#if __ANDROID__
			/// <summary>
			/// Controls the propagation of <see cref="Windows.UI.Xaml.FrameworkElement.Loaded"/> and
			/// <see cref="Windows.UI.Xaml.FrameworkElement.Unloaded"/> events through managed
			/// or native visual tree traversal.
			/// </summary>
			/// <remarks>
			/// This setting impacts significantly the loading performance of controls on Android.
			/// Setting it to <see cref="true"/> avoids the use of costly Java->C# interop.
			/// </remarks>
			public static bool AndroidUseManagedLoadedUnloaded { get; set; } = true;
#endif

			/// <summary>
			/// [WebAssembly Only] Controls the propagation of <see cref="Windows.UI.Xaml.FrameworkElement.Loaded"/> and
			/// <see cref="Windows.UI.Xaml.FrameworkElement.Unloaded"/> events through managed
			/// or native visual tree traversal.
			/// </summary>
			/// <remarks>
			/// This setting impacts significantly the loading performance of controls on Web Assembly.
			/// Setting it to <see cref="true"/> avoids the use of costly JavaScript->C# interop.
			/// </remarks>
			public static bool WasmUseManagedLoadedUnloaded { get; set; } = true;
		}

		public static class Image
		{
			/// <summary>
			/// Use the old way to align iOS images, using the "ContentMode".
			/// New way is using the Layer to better position the image according to alignments.
			/// </summary>
			public static bool LegacyIosAlignment { get; set; } = false;
		}

		public static class Interop
		{
			/// <summary>
			/// [WebAssembly Only] Used to control the behavior of the C#/Javascript interop. Setting this
			/// flag to true forces the use of the Javascript eval mode, instead of binary interop.
			/// This flag has no effect when running in hosted mode.
			/// </summary>
			public static bool ForceJavascriptInterop { get; set; } = false;
		}

		public static class Binding
		{
			/// <summary>
			/// Determines if the binding engine should ignore identical references in binding paths.
			/// </summary>
			public static bool IgnoreINPCSameReferences { get; set; } = false;
		}

		public static class Popup
		{
#if __ANDROID__
			/// <summary>
			/// Use a native popup to display the popup content. Otherwise use the <see cref="PopupRoot"/>.
			/// </summary>
			public static bool UseNativePopup { get; set; } = true;
#endif
		}

		public static class ProgressRing
		{
			public static Uri ProgressRingAsset { get; set; } = new Uri("embedded://Uno.UI/Uno.UI.Microsoft.UI.Xaml.Controls.ProgressRing.ProgressRingIntdeterminate.json");
			public static Uri DeterminateProgressRingAsset { get; set; } = new Uri("embedded://Uno.UI/Uno.UI.Microsoft.UI.Xaml.Controls.ProgressRing.ProgressRingDeterminate.json");
		}

		public static class ListViewBase
		{
			/// <summary>
			/// Sets the value to use for <see cref="ItemsStackPanel.CacheLength"/> and <see cref="ItemsWrapGrid.CacheLength"/> if not set
			/// explicitly in Xaml or code. Higher values will cache more views either side of the visible window, improving list performance
			/// at the expense of consuming more memory. Setting this to null will leave the default value at the UWP default of 4.0.
			/// </summary>
			public static double? DefaultCacheLength = 1.0;
		}

#if __ANDROID__
		public static class NativeListViewBase
		{
			/// <summary>
			/// Sets this value to remove item animation for <see cref="UnoRecyclerView"/>. This prevents <see cref="UnoRecyclerView"/>
			/// from crashing when pressured: Tmp detached view should be removed from RecyclerView before it can be recycled
			/// </summary>
			public static bool RemoveItemAnimator = true;
		}
#endif

		public static class Page
		{
			/// <summary>
			/// Enables reuse of <see cref="Page"/> instances. Enabling can improve performance when using <see cref="Frame"/> navigation.
			/// </summary>
			public static bool IsPoolingEnabled { get; set; } = false;
		}

		public static class PointerRoutedEventArgs
		{
#if __ANDROID__
			/// <summary>
			/// Defines if the PointerPoint.Timestamp retrieved from PointerRoutedEventArgs.GetCurrentPoint(relativeTo)
			/// or PointerRoutedEventArgs.GetIntermediatePoints(relativeTo) can be relative using the Android's
			/// "SystemClock.uptimeMillis()" or if they must be converted into an absolute scale
			/// (using the "elapsedRealtime()", cf. https://developer.android.com/reference/android/os/SystemClock).
			/// Disabling it negatively impacts the performance it requires to compute the "sleep time"
			/// (i.e. [real elapsed time] - [up time]) for each event (as the up time is paused when device is in deep sleep).
			/// By default this is `true`.
			/// </summary>
			public static bool AllowRelativeTimeStamp { get; set; } = true;
#endif
		}

		public static class SelectorItem
		{
			/// <summary>
			/// <para>
			/// Determines if the visual states "PointerOver", "PointerOverSelected"
			/// are used or not. If disabled, those states will never be activated by the selector items.
			/// </para>
			/// <para>The default value is `true`.</para>
			/// </summary>
			public static bool UseOverStates { get; set; } = true;
		}

		public static class Style
		{
			/// <summary>
			/// Determines if Uno.UI should be using native styles for controls that have
			/// a native counterpart. (e.g. Button, Slider, ComboBox, ...)
			///
			/// By default this is true.
			/// </summary>
			public static bool UseUWPDefaultStyles { get; set; } = true;

			/// <summary>
			/// Override the native styles usage per control type.
			/// </summary>
			/// <remarks>
			/// Usage: 'UseUWPDefaultStylesOverride[typeof(Frame)] = false;' will result in the native style always being the default for Frame, irrespective
			/// of the value of <see cref="UseUWPDefaultStyles"/>. This is useful when an app uses the UWP default look for most controls but the native
			/// appearance/comportment for a few particular controls, or vice versa.
			/// </remarks>
			public static IDictionary<Type, bool> UseUWPDefaultStylesOverride { get; } = new Dictionary<Type, bool>();

			/// <summary>
			/// This enables native frame navigation on Android and iOS by setting related classes (<see cref="Frame"/>, <see cref="CommandBar"/>
			/// and <see cref="AppBarButton"/>) to use their native styles.
			/// </summary>
			public static void ConfigureNativeFrameNavigation()
			{
				SetUWPDefaultStylesOverride<Frame>(useUWPDefaultStyle: false);
				SetUWPDefaultStylesOverride<CommandBar>(useUWPDefaultStyle: false);
				SetUWPDefaultStylesOverride<AppBarButton>(useUWPDefaultStyle: false);
			}

			/// <summary>
			/// Override the native styles useage for control type <typeparamref name="TControl"/>.
			/// </summary>
			/// <typeparam name="TControl"></typeparam>
			/// <param name="useUWPDefaultStyle">
			/// Whether instances of <typeparamref name="TControl"/> should use the UWP default style.
			/// If false, the native default style (if one exists) will be used.
			/// </param>
			public static void SetUWPDefaultStylesOverride<TControl>(bool useUWPDefaultStyle) where TControl : Windows.UI.Xaml.Controls.Control
				=> UseUWPDefaultStylesOverride[typeof(TControl)] = useUWPDefaultStyle;
		}

		public static class TextBlock
		{
			/// <summary>
			/// [WebAssembly Only] Determines if the measure cache is enabled.
			/// </summary>
			public static bool IsMeasureCacheEnabled { get; set; } = true;
		}

		public static class TextBox
		{
			/// <summary>
			/// Determines if the caret is visible or not.
			/// </summary>
			/// <remarks>This feature is used to avoid screenshot comparisons false positives</remarks>
			public static bool HideCaret { get; set; } = false;
		}

		public static class ScrollViewer
		{
			/// <summary>
			/// This defines the default value of the <see cref="Uno.UI.Xaml.Controls.ScrollViewer.UpdatesModeProperty"/>.
			/// For backward compatibility, you should set it to Synchronous.
			/// For better compatibility with Windows, you should keep the default value 'AsynchronousIdle'.
			/// </summary>
			/// <remarks>
			/// As this value is read only once when initializing the dependency property,
			/// make sure to define it in the early stages of you application initialization,
			/// before any UI related initialization (like generic styles init) and even before
			/// referencing the ** type ** ScrollViewer in any way.
			/// </remarks>
			public static ScrollViewerUpdatesMode DefaultUpdatesMode { get; set; } = ScrollViewerUpdatesMode.AsynchronousIdle;

#if __ANDROID__
			/// <summary>
			/// This value defines an optional delay to be set for native ScrollBar thumbs to disapear. The
			/// platform default is 300ms, which can make the thumbs appear on screenshots, changing this value
			/// to <see cref="TimeSpan.Zero"/> makes those disapear faster.
			/// </summary>
			public static TimeSpan? AndroidScrollbarFadeDelay { get; set; }
#endif
		}

		public static class ThemeAnimation
		{
			/// <summary>
			/// Default duration for xxxThemeAnimation
			/// </summary>
			public static TimeSpan DefaultThemeAnimationDuration { get; set; } = TimeSpan.FromSeconds(0.75);
		}

		public static class ToolTip
		{
			public static bool UseToolTips { get; set; }
#if __WASM__
				= true;
#endif

			public static int ShowDelay { get; set; } = 1000;

			public static int ShowDuration { get; set; } = 7000;
		}

		public static class NativeFramePresenter
		{
#if __ANDROID__
			/// <summary>
			/// Determines if pages in the backstack are kept in the visual tree.
			/// Defaults to false for performance considerations.
			/// </summary>
			public static bool AndroidUnloadInactivePages { get; set; } = false;
#endif
		}

		public static class UIElement
		{
			/// <summary>
			/// [DEPRECATED]
			/// Not used anymore, does nothing.
			/// </summary>
			[NotImplemented]
			public static bool UseLegacyClipping { get; set; } = true;

			/// <summary>
			/// Enable the visualization of clipping bounds (intended for diagnostic purposes).
			/// </summary>
			/// <remarks>
			/// This feature is only supported on iOS, for now.
			/// </remarks>
			public static bool ShowClippingBounds { get; set; } = false;

			/// <summary>
			/// [WebAssembly Only] Enable the assignation of the "xamlname", "xuid" and "xamlautomationid" attributes on DOM elements created
			/// from the XAML visual tree. This enables tools such as Puppeteer to select elements
			/// in the DOM for automation purposes.
			/// </summary>
			public static bool AssignDOMXamlName { get; set; } = false;

			/// <summary>
			/// [WebAssembly Only] Enable UIElement.ToString() to return the element's unique ID
			/// </summary>
			public static bool RenderToStringWithId { get; set; } = true;

			/// <summary>
			/// [WebAssembly Only] Enables the assignation of properties from the XAML visual tree as DOM attributes: Height -> "xamlheight",
			/// HorizontalAlignment -> "xamlhorizontalalignment" etc. 
			/// </summary>
			/// <remarks>
			/// This should only be enabled for debug builds, but can greatly aid layout debugging.
			///
			/// Note: for release builds of Uno, if the flag is set, attributes will be set on loading and *not* updated if
			/// the values change subsequently. This restriction doesn't apply to debug Uno builds.
			/// </remarks>
			public static bool AssignDOMXamlProperties { get; set; } = false;

#if __ANDROID__
			/// <summary>
			/// When this is set, non-UIElements will always be clipped to their bounds (<see cref="Android.Views.ViewGroup.ClipChildren"/> will
			/// always be set to true on their parent). 
			/// </summary>
			/// <remarks>
			/// This is true by default as most native views assume that they will be clipped, and can display incorrectly otherwise.
			/// </remarks>
			public static bool AlwaysClipNativeChildren { get; set; } = true;
#endif
		}

		public static class WebView
		{
#if __ANDROID__
			/// <summary>
			/// Prevent the WebView from using hardware rendering.
			/// This was previously the default behavior in Uno to work around a keyboard-related visual glitch in Android 5.0 (http://stackoverflow.com/questions/27172217/android-systemui-glitches-in-lollipop), however it prevents video and 3d content from being rendered.
			/// </summary>
			/// <remarks>
			/// See this for more info: https://github.com/unoplatform/uno/blob/26c5cc5992cae3c8c25adf51eb77ca4b0dd34e93/src/Uno.UI/UI/Xaml/Controls/WebView/WebView.Android.cs#L251_L255
			/// </remarks>
			public static bool ForceSoftwareRendering { get; set; } = false;
#endif
		}

		public static class Xaml
		{
			/// <summary>
			/// Maximal "BasedOn" recursive resolution depth.
			/// </summary>
			/// <remarks>
			/// This is a mechanism to prevent hard-to-diagnose stack overflow when a resource name is not found.
			/// </remarks>
			[Obsolete("This flag is no longer used.")]
			[EditorBrowsable(EditorBrowsableState.Never)]
			public static int MaxRecursiveResolvingDepth { get; set; } = 12;
		}

		public static class DatePicker
		{
#if __IOS__
			public static bool UseLegacyStyle { get; set; } = false;
#endif
		}

		public static class TimePicker
		{
#if __IOS__
			//TODO: Setting the default to true because of this: https://github.com/unoplatform/uno/issues/4611
			public static bool UseLegacyStyle { get; set; } = true;
#endif
		}
	}
}
