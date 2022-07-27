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
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void ActionHandler(IntPtr action);
        static ActionHandler performAction = null;
        AccessibleNodeData rootNode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnRuntimeMethodLoad()
        {
            if (!initialized)
            {
                try
                {
                    windowHandle = GetActiveWindow();
                    performAction += new ActionHandler(performActionCallback);
                    initialized = init(windowHandle, performAction);
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }
            }
        }

        static void performActionCallback(IntPtr buffer)
        {
            try
            {
                var json = fromUTF8(buffer);
                var request = JsonConvert.DeserializeObject<ActionRequest>(json);
                dispatchAction(request);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
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

        static void dispatchAction(ActionRequest request)
        {
            if (request.action == "default" && request.target != null)
            {
                foreach (var node in GameObject.FindObjectsOfType<AccessibleNode>())
                {
                    if (node.id != request.target)
                        continue;
                    if (node.defaultActionVerb != DefaultActionVerb.click)
                        return;
                    var button = node.GetComponent<Button>();
                    if (button != null)
                        button.onClick.Invoke();
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

        public static string fromUTF8(IntPtr nativeUtf8)
        {
            int len = 0;
            while (Marshal.ReadByte(nativeUtf8, len) != 0)
                ++len;
            byte[] buffer = new byte[len];
            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        [DllImport("accesskit_unity_plugin")]
        static extern bool init(IntPtr hWnd, ActionHandler actionHandler);

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
