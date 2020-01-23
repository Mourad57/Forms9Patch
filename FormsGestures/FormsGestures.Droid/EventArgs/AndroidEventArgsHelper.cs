using Android.Views;
using Xamarin.Forms;

namespace FormsGestures.Droid
{
	public static class AndroidEventArgsHelper
	{
        public static Point[] GetTouches(MotionEvent current, Android.Views.View view, Listener listener)
        {
			int pointerCount = current.PointerCount;
			var pointerCoords = new MotionEvent.PointerCoords[pointerCount];
			using (var pointerCoord = new MotionEvent.PointerCoords())
			{
				for (int i = 0; i < pointerCount; i++)
				{
					current.GetPointerCoords(i, pointerCoord);
					pointerCoords[i] = new MotionEvent.PointerCoords(pointerCoord);
				}
			}
			return GetTouches(pointerCoords, view, listener);
		}

		public static Point[] GetTouches(MotionEvent.PointerCoords[] coords, BaseGestureEventArgs previous, Android.Views.View view, Listener listener)
		{
			int pointerCount = coords.Length;
			if (pointerCount < 2 && previous != null)
			{
				System.Diagnostics.Debug.WriteLine("\tPointerCount < requiredTouches");
				return previous.ElementTouches;
			}
			return GetTouches(coords, view, listener);
		}

        
        public static Point[] GetRawTouches(MotionEvent current)
        {
			var offsetX = current.RawX/Display.Scale - current.GetX();
			var offsetY = current.RawY/Display.Scale - current.GetY();

			var result = new Point[current.PointerCount];
			MotionEvent.PointerCoords touch = new MotionEvent.PointerCoords();
            for(int i=0;i< current.PointerCount;i++)
            {
				current.GetPointerCoords(i, touch);
				var point = new Point(touch.X + offsetX, touch.Y + offsetY);
				result[i] = point;
            }
			return result;
        }
        

		public static Point[] GetTouches(MotionEvent.PointerCoords[] pointerCoords, Android.Views.View sourceView, Listener listener)
        {
			Point delta = Point.Zero;

			if (listener?.Element != null
				&& Xamarin.Forms.Platform.Android.Platform.GetRenderer(listener?.Element) is Xamarin.Forms.Platform.Android.IVisualElementRenderer renderer
				&& renderer.View != sourceView
                )
			{
				var listenerViewLocation = listener != null
					? VisualElementExtensions.LocationInWindowCoord(listener.Element)
					: Point.Zero;
				var touchViewLocation = AndroidViewExtensions.LocationInFormsCoord(sourceView);
				delta = touchViewLocation.Subtract(listenerViewLocation);
			}

			var pointerCount = pointerCoords.Length;
			var array = new Point[pointerCount];
			for (int i = 0; i < pointerCount; i++)
				array[i] = new Point(pointerCoords[i].X + delta.X, pointerCoords[i].Y + delta.Y);
			return array;
		}

	}
}
