using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.UI;

namespace AccessKit
{
    public class AccessibleNode : MonoBehaviour
    {
        static bool initialized;
        static bool destroyed;
        static IntPtr window;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnRuntimeMethodLoad()
        {
            if (!initialized)
            {
                try
                {
                    window = GetActiveWindow();
                    if (init(window))
                    {
                        var initialState = "{\"nodes\":[{\"id\":1,\"role\":\"window\",\"name\":\"Hello from Unity\"},{\"id\":2,\"role\":\"button\",\"name\":\"Click me!\"}],\"focus\":2,\"tree\":{\"root\":1}}";
                        push_update(window, toUTF8(initialState));
                        initialized = true;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (initialized && !destroyed)
            {
                var treeUpdate = "";
                if (hasFocus)
                    treeUpdate = "{\"nodes\":[],\"focus\":2}";
                else
                    treeUpdate = "{\"nodes\":[]}";
                try
                {
                    push_update(window, toUTF8(treeUpdate));
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }
            }
        }

        void OnApplicationQuit()
        {
            if (!destroyed)
            {
                try
                {
                    destroy(window);
                    destroyed = true;
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }
            }
        }

        static byte[] toUTF8(string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            var nulTerminated = new byte[bytes.Length + 1];
            Array.Copy(bytes, 0, nulTerminated, 0, bytes.Length);
            nulTerminated[bytes.Length] = 0;
            return nulTerminated;
        }
        
        [DllImport("./accesskit_unity_plugin.dll")]
        static extern bool init(IntPtr hWnd);

        [DllImport("./accesskit_unity_plugin.dll")]
        static extern void destroy(IntPtr hwnd);

        [DllImport("./accesskit_unity_plugin.dll")]
        static extern void push_update(IntPtr hwnd, byte[] tree_update);

        [DllImport("user32")]
        static extern IntPtr GetActiveWindow();
    }
}
