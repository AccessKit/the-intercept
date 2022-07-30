using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AccessKit
{
    public static class AccessKit
    {
        public static bool IsInitialized { get; private set; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void ActionHandler(IntPtr buffer);

        public static bool init(IntPtr windowHandle, ActionHandler actionHandler, TreeUpdate initialTreeUpdate)
        {
            if (IsInitialized)
                return true;
            IsInitialized = init(windowHandle, actionHandler, toUTF8(toJSON(initialTreeUpdate)));
            return IsInitialized;
        }

        static byte[] toUTF8(string s)
        {
            return Encoding.UTF8.GetBytes(s + char.MinValue);
        }

        static string toJSON(TreeUpdate treeUpdate)
        {
            var settings = new JsonSerializerSettings()
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };
            settings.Converters.Add(new StringEnumConverter());
            return JsonConvert.SerializeObject(treeUpdate, settings);
        }

        [DllImport("accesskit_unity_plugin")]
        static extern bool init(IntPtr hwnd, ActionHandler action_handler, byte[] initial_tree_update);

        public static void destroy(IntPtr windowHandle)
        {
            if (IsInitialized)
            {
                rawDestroy(windowHandle);
                IsInitialized = false;
            }
        }

        [DllImport("accesskit_unity_plugin", EntryPoint = "destroy")]
        static extern void rawDestroy(IntPtr hwnd);

        public static bool pushUpdate(IntPtr windowHandle, TreeUpdate treeUpdate)
        {
            if (!IsInitialized)
                return false;
            return push_update(windowHandle, toUTF8(toJSON(treeUpdate)));
        }

        [DllImport("accesskit_unity_plugin")]
        static extern bool push_update(IntPtr hwnd, byte[] tree_update);
    }
}
