using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.Extensions.Logging;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Input
{
	public partial class GestureRecognizer
	{
		private const int _defaultGesturesSize = 2; // Number of pointers before we have to resize the gestures dictionary

		internal const int TapMaxXDelta = 10;
		internal const int TapMaxYDelta = 10;

		internal const ulong MultiTapMaxDelayTicks = TimeSpan.TicksPerMillisecond * 1000;

		internal const long HoldMinDelayTicks = TimeSpan.TicksPerMillisecond * 800;
		internal const float HoldMinPressure = .75f;

		internal const long DragWithTouchMinDelayTicks = TimeSpan.TicksPerMillisecond * 300; // https://docs.microsoft.com/en-us/windows/uwp/design/input/drag-and-drop#open-a-context-menu-on-an-item-you-can-drag-with-touch

		private readonly ILogger _log;
		private IDictionary<uint, Gesture> _gestures = new Dictionary<uint, Gesture>(_defaultGesturesSize);
		private Manipulation _manipulation;
		private GestureSettings _gestureSettings;
		private bool _isManipulationOrDragEnabled;

		public GestureSettings GestureSettings
		{
			get => _gestureSettings;
			set
			{
				_gestureSettings = value;
				_isManipulationOrDragEnabled = (value & (GestureSettingsHelper.Manipulations | GestureSettingsHelper.DragAndDrop)) != 0;
			}
		}

		public bool IsActive => _gestures.Count > 0 || _manipulation != null;

		internal bool IsDragging => _manipulation?.IsDragManipulation ?? false;

		/// <summary>
		/// This is the owner provided in the ctor. It might be `null` if none provided.
		/// It's purpose it to allow usage of static event handlers.
		/// </summary>
		internal object Owner { get; }

		public GestureRecognizer()
		{
			_log = this.Log();
		}

		internal GestureRecognizer(object owner)
			: this()
		{
			Owner = owner;
		}

		public void ProcessDownEvent(PointerPoint value)
		{
			// Sanity validation. This is pretty important as the Gesture now has an internal state for the Holding state.
			if (_gestures.TryGetValue(value.PointerId, out var previousGesture))
			{
				if (_log.IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{Owner} Inconsistent state, we already have a pending gesture for a pointer that is going down. Abort the previous gesture.");
				}
				previousGesture.ProcessComplete();
			}

			// Create a Gesture responsible to recognize single-pointer gestures
			var gesture = new Gesture(this, value);
			if (gesture.IsCompleted)
			{
				// This usually means that the gesture already detected a double tap
				if (previousGesture != null)
				{
					_gestures.Remove(value.PointerId);
				}

				return;
			}
			_gestures[value.PointerId] = gesture;

			// Create of update a Manipulation responsible to recognize multi-pointer and drag gestures
			if (_isManipulationOrDragEnabled)
			{
				if (_manipulation == null)
				{
					_manipulation = new Manipulation(this, value);
				}
				else
				{
					_manipulation.Add(value);
				}
			}
		}

		public void ProcessMoveEvents(IList<PointerPoint> value) => ProcessMoveEvents(value, true);

		internal void ProcessMoveEvents(IList<PointerPoint> value, bool isRelevant)
		{
			// Even if the pointer was considered as irrelevant, we still buffer it as it is part of the user interaction
			// and we should considered it for the gesture recognition when processing the up.
			foreach (var point in value)
			{
				if (_gestures.TryGetValue(point.PointerId, out var gesture))
				{
					gesture.ProcessMove(point);
				}
				else if (_log.IsEnabled(LogLevel.Debug))
				{
					// debug: We might get some PointerMove for mouse even if not pressed,
					//		  or if gesture was completed by user / other gesture recognizers.
					_log.Debug($"{Owner} Received a 'Move' for a pointer which was not considered as down. Ignoring event.");
				}
			}

			_manipulation?.Update(value);
		}

		public void ProcessUpEvent(PointerPoint value) => ProcessUpEvent(value, true);

		internal void ProcessUpEvent(PointerPoint value, bool isRelevant)
		{
#if NET461 || UNO_REFERENCE_API
			if (_gestures.TryGetValue(value.PointerId, out var gesture))
			{
				_gestures.Remove(value.PointerId);
#else
			if (_gestures.Remove(value.PointerId, out var gesture))
			{
#endif
				// Note: At this point we MAY be IsActive == false, which is the expected behavior (same as UWP)
				//		 even if we will fire some events now.

				gesture.ProcessUp(value);
			}
			else if (_log.IsEnabled(LogLevel.Debug))
			{
				// debug: We might get some PointerMove for mouse even if not pressed,
				//		  or if gesture was completed by user / other gesture recognizers.
				_log.Debug($"{Owner} Received a 'Up' for a pointer which was not considered as down. Ignoring event.");
			}

			_manipulation?.Remove(value);
		}

		public void CompleteGesture()
		{
			// Capture the list in order to avoid alteration while enumerating
			var gestures = _gestures;
			_gestures = new Dictionary<uint, Gesture>(_defaultGesturesSize);

			// Note: At this point we are IsActive == false, which is the expected behavior (same as UWP)
			//		 even if we will fire some events now.

			// Complete all pointers
			foreach (var gesture in gestures.Values)
			{
				gesture.ProcessComplete();
			}

			_manipulation?.Complete();
		}

		internal void PreventHolding(uint pointerId)
		{
			if (_gestures.TryGetValue(pointerId, out var gesture))
			{
				gesture.PreventHolding();
			}
		}

		#region Manipulations
		internal event TypedEventHandler<GestureRecognizer, ManipulationStartingEventArgs> ManipulationStarting; // This is not on the public API!
		public event TypedEventHandler<GestureRecognizer, ManipulationCompletedEventArgs> ManipulationCompleted;
#pragma warning disable  // Event not raised: inertia is not supported yet
		public event TypedEventHandler<GestureRecognizer, ManipulationInertiaStartingEventArgs> ManipulationInertiaStarting;
#pragma warning restore 67
		public event TypedEventHandler<GestureRecognizer, ManipulationStartedEventArgs> ManipulationStarted;
		public event TypedEventHandler<GestureRecognizer, ManipulationUpdatedEventArgs> ManipulationUpdated;


		internal Manipulation PendingManipulation => _manipulation;
		#endregion

		#region Tap (includes DoubleTap and RightTap)
		private (ulong id, ulong ts, Point position) _lastSingleTap;

		public event TypedEventHandler<GestureRecognizer, TappedEventArgs> Tapped;
		public event TypedEventHandler<GestureRecognizer, RightTappedEventArgs> RightTapped;
		public event TypedEventHandler<GestureRecognizer, HoldingEventArgs> Holding;

		public bool CanBeDoubleTap(PointerPoint value)
			=> _gestureSettings.HasFlag(GestureSettings.DoubleTap) && Gesture.IsMultiTapGesture(_lastSingleTap, value);
		#endregion

		#region Dragging
		public event TypedEventHandler<GestureRecognizer, DraggingEventArgs> Dragging;
		#endregion

		private delegate bool CheckButton(PointerPoint point);

		private static readonly CheckButton LeftButton = (PointerPoint point) => point.Properties.IsLeftButtonPressed;
		private static readonly CheckButton RightButton = (PointerPoint point) => point.Properties.IsRightButtonPressed;
		private static readonly CheckButton BarrelButton = (PointerPoint point) => point.Properties.IsBarrelButtonPressed;

		private static ulong GetPointerIdentifier(PointerPoint point)
		{
			// For mouse, the PointerId is the same, no matter the button pressed.
			// The only thing that changes are flags in the properties.
			// Here we build a "PointerIdentifier" that fully identifies the pointer used

			// Note: We don't take in consideration props.IsHorizontalMouseWheel as it would require to also check
			//		 the (non existing) props.IsVerticalMouseWheel, and it's actually not something that should
			//		 be considered as a pointer changed.

			var props = point.Properties;

			ulong identifier = point.PointerId;

			// Mouse
			if (props.IsLeftButtonPressed)
			{
				identifier |= 1L << 32;
			}
			if (props.IsMiddleButtonPressed)
			{
				identifier |= 1L << 33;
			}
			if (props.IsRightButtonPressed)
			{
				identifier |= 1L << 34;
			}
			if (props.IsXButton1Pressed)
			{
				identifier |= 1L << 35;
			}
			if (props.IsXButton2Pressed)
			{
				identifier |= 1L << 36;
			}

			// Pen
			if (props.IsBarrelButtonPressed)
			{
				identifier |= 1L << 37;
			}
			if (props.IsEraser)
			{
				identifier |= 1L << 38;
			}

			return identifier;
		}
	}
}
