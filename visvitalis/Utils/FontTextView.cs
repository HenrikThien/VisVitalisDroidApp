using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Widget;

namespace visvitalis.Utils
{
    public sealed class FontTextView : TextView
    {
        private new const string Tag = "TextView";
        private readonly Dictionary<string, Typeface> _cachedTypefaces;

        private FontTextView(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public FontTextView(Context context)
            : this(context, null)
        {
        }

        public FontTextView(Context context, IAttributeSet attrs)
            : this(context, attrs, 0)
        {
        }

        public FontTextView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            if (_cachedTypefaces == null)
            {
                _cachedTypefaces = new Dictionary<string, Typeface>();
            }

            if (IsInEditMode) return;

            var array = context.ObtainStyledAttributes(attrs, Resource.Styleable.TypefaceTextView);
            if (array != null)
            {
                var typefaceAssetPath = array.GetString(Resource.Styleable.TypefaceTextView_customTypeface);

                if (typefaceAssetPath != null)
                {
                    Typeface typeface = null;

                    if (!_cachedTypefaces.TryGetValue(typefaceAssetPath, out typeface))
                    {
                        var assets = context.Assets;
                        typeface = Typeface.CreateFromAsset(assets, typefaceAssetPath);
                        _cachedTypefaces.Add(typefaceAssetPath, typeface);
                    }

                    SetTypeface(typeface, TypefaceStyle.Normal);
                }

                array.Recycle();
            }
        }
    }
}