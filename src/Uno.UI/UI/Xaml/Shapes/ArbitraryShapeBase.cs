﻿#if !__IOS__ && !__MACOS__ && !__SKIA__
#define LEGACY_SHAPE_MEASURE
#endif

#if LEGACY_SHAPE_MEASURE
using Windows.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using Windows.Foundation;	

namespace Windows.UI.Xaml.Shapes
{
	public abstract partial class ArbitraryShapeBase : Shape
	{
		private SerialDisposable _layer = new SerialDisposable();
		private object[] _layerState;

		protected static double LimitWithUserSize(double availableSize, double userSize, double naNFallbackValue)
		{
			var hasUserSize = userSize != 0 && !double.IsNaN(userSize) && !double.IsInfinity(userSize);
			var hasAvailableSize = !double.IsNaN(availableSize);

#if UNO_REFERENCE_API
			// The measuring algorithms for shapes in Wasm and iOS/Android/macOS are not using the
			// infinity the same way.
			// Those implementation will need to be merged.
			hasAvailableSize &= !double.IsInfinity(availableSize);
#endif

			if (hasUserSize && hasAvailableSize)
			{
				return Math.Min(userSize, availableSize);
			}

			if (hasAvailableSize)
			{
				return availableSize;
			}

			//If both availableSize and userSize are NaN, use the fallback.
			return naNFallbackValue;
		}

#if !UNO_REFERENCE_API
		protected internal override void OnInvalidateMeasure()
		{
			base.OnInvalidateMeasure();
			RefreshShape(true);
		}
#endif

		/// <summary>
		/// Refreshes the current shape, considering its drawinf parameters.
		/// </summary>
		/// <param name="forceRefresh">Forces a refresh by ignoring the shape parameters.</param>
		protected override void RefreshShape(bool forceRefresh = false)
		{
			if (IsLoaded)
			{
				var newLayerState = GetShapeParameters().ToArray();

				if (forceRefresh || !(_layerState?.SequenceEqual(newLayerState) ?? false))
				{
					// Remove the previous layer
					_layer.Disposable = null;

					_layerState = newLayerState;
					_layer.Disposable = BuildDrawableLayer();
				}
			}
		}

		private protected Rect GetBounds()
		{
			var width = Width;
			var height = Height;

			if (double.IsNaN(width))
			{
				var minWidth = MinWidth;
				if (minWidth > 0.0)
				{
					width = minWidth;
				}
			}
			if (double.IsNaN(height))
			{
				var minHeight = MinHeight;
				if (minHeight > 0.0)
				{
					height = minHeight;
				}
			}

			if (double.IsNaN(width))
			{
				if (double.IsNaN(height))
				{
					return new Rect(0.0, 0.0, 0.0, 0.0);
				}

				return new Rect(0.0, 0.0, height, height);
			}

			if (double.IsNaN(height))
			{
				return new Rect(0.0, 0.0, width, width);
			}

			return new Rect(0.0, 0.0, width, height);
		}

		/// <summary>
		/// Provides a enumeration of values that are used to determine if the shape
		/// should be rebuilt. Inheritors should append the base's enumeration.
		/// </summary>
		protected internal virtual IEnumerable<object> GetShapeParameters()
		{
			yield return GetActualSize();
			yield return Fill;
			yield return Stroke;
			yield return StrokeThickness;
			yield return Stretch;
			yield return StrokeDashArray;
			yield return _scaleX;
			yield return _scaleY;
		}

		/// <summary>
		/// Gets whether the shape should preserve the path's origin (and ignore StrokeThickness)
		/// </summary>
		protected bool ShouldPreserveOrigin => this is Path && Stretch == Stretch.None;
	}
}
#endif
