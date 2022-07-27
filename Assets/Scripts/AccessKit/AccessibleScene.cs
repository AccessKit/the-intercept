using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AccessKit
{
    public class AccessibleScene : MonoBehaviour
    {
        static IntPtr windowHandle;
        static bool initialized;
        static bool destroyed;
        static bool windowHasFocus = true;
        AccessibleNodeData rootNode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnRuntimeMethodLoad()
        {
            if (!initialized)
            {
                try
                {
                    windowHandle = GetActiveWindow();
                    initialized = init(windowHandle);
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }
            }
        }

        void Start()
        {
            rootNode = new AccessibleNodeData(1, AccessibleRole.window);
            rootNode.name = "Window";
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            if (initialized && !destroyed && hasFocus != windowHasFocus)
            {
                windowHasFocus = hasFocus;
                buildTreeUpdate(false);
            }
        }

        void LateUpdate()
        {
            buildTreeUpdate(false);
        }

        void OnApplicationQuit()
        {
            if (!destroyed)
            {
                try
                {
                    destroy(windowHandle);
                    destroyed = true;
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }
            }
        }

        void buildTreeUpdate(bool forcePush)
        {
            var treeUpdate = new TreeUpdate();
            AccessibleNode[] nodes = GameObject.FindObjectsOfType<AccessibleNode>();
            rootNode.children = findChildren(rootNode.id, nodes);
            treeUpdate.nodes.Add(rootNode);
            foreach (var node in nodes)
            {
                if (node.id == 0 || node.parent == 0)
                    continue;
                if (windowHasFocus && node.GetComponent<HasKeyboardFocus>() != null)
                    treeUpdate.focus = node.id;
                treeUpdate.nodes.Add(new AccessibleNodeData(node, findChildren(node.id, nodes)));
            }
            if (treeUpdate.focus == null && windowHasFocus)
                treeUpdate.focus = rootNode.id;
            try
            {
                var settings = new JsonSerializerSettings()
                {
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };
                settings.Converters.Add(new StringEnumConverter());
                var json = JsonConvert.SerializeObject(treeUpdate, settings);
                using (var sw = new System.IO.StreamWriter("dump.json"))
                {
                    var currentlyFocused = GetComponentInChildren<HasKeyboardFocus>();
                    if (currentlyFocused != null)
                    {
                        sw.WriteLine("Someone has focus");
                        var text = currentlyFocused.GetComponentInChildren<Text>();
                        if (text != null)
                            sw.WriteLine(text.text);
                    }
                    sw.WriteLine(json);
                }
                push_update(windowHandle, toUTF8(json), false);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }
        
        List<ulong> findChildren(ulong id, AccessibleNode[] nodes)
        {
            var children = new List<AccessibleNode>();
            foreach (var node in nodes)
            {
                if (node.parent == id && node.id != 0 && node.id != id)
                    children.Add(node);
            }
            children.Sort((x, y) => x.indexInParent.CompareTo(y.indexInParent));
            return children.ConvertAll(new Converter<AccessibleNode, ulong>(node => node.id));
        }

        static byte[] toUTF8(string s)
        {
            return Encoding.UTF8.GetBytes(s + char.MinValue);
        }
        
        [DllImport("accesskit_unity_plugin")]
        static extern bool init(IntPtr hWnd);

        [DllImport("accesskit_unity_plugin")]
        static extern void destroy(IntPtr hwnd);

        [DllImport("accesskit_unity_plugin")]
        static extern void push_update(IntPtr hwnd, byte[] tree_update, bool force);

        [DllImport("user32")]
        static extern IntPtr GetActiveWindow();
    }
    
    public class TreeUpdate
    {
        public List<AccessibleNodeData> nodes;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public ulong? focus;
        
        public TreeUpdate()
        {
            nodes = new List<AccessibleNodeData>();
        }
    }
}
