//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//using Android.App;
//using Android.Content;
//using Android.OS;
//using Android.Runtime;
//using Android.Views;
//using Android.Widget;
//using Android.Preferences;
//using System.Threading.Tasks;

//namespace visvitalis.Utils
//{
//    public sealed class SessionManager
//    {
//        private ISharedPreferences _cache { get; set; }
//        private Context _context { get; set; }

//        public SessionManager(Context context)
//        {
//            _context = context;
//            _cache = PreferenceManager.GetDefaultSharedPreferences(context);
//        }

//        public async Task PutAsync<T>(string key, T value)
//        {
//            var editor = _cache.Edit();
//            var type = typeof(T);

//            await Task.Run(() =>
//            {
//                if (type == typeof(string))
//                {
//                    editor.PutString(key, value.ToString());
//                }
//                else if (type == typeof(int))
//                {
//                    editor.PutInt(key, Convert.ToInt32(value));
//                }
//                else if (type == typeof(bool))
//                {
//                    editor.PutBoolean(key, Convert.ToBoolean(value));
//                }
//            });
//        }
//    }
//}