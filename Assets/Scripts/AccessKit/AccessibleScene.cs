using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace AccessKit
{
    public class AccessibleScene : MonoBehaviour
    {
        static IntPtr windowHandle;
        static bool windowHasFocus = false;
        static AccessKit.ActionHandler actionHandler = null;
        static AccessibleNodeData rootNode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnRuntimeMethodLoad()
        {
            try
            {
                windowHandle = GetActiveWindow();
                rootNode = getRootNode(windowHandle);
                var initialTreeUpdate = new TreeUpdate();
                initialTreeUpdate.nodes.Add(rootNode);
                initialTreeUpdate.tree = new AccessibleTree(rootNode.id);
                actionHandler += new AccessKit.ActionHandler(handleAction);
                AccessKit.init(windowHandle, actionHandler, initialTreeUpdate);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        static AccessibleNodeData getRootNode(IntPtr windowHandle)
        {
            var root = new AccessibleNodeData(1, AccessibleRole.window);
            int length = GetWindowTextLength(windowHandle);
            if (length > 0)
            {
                var builder = new StringBuilder(length);
                GetWindowText(windowHandle, builder, length + 1);
                root.name = builder.ToString();
            }
            return root;
        }
        
        static void handleAction(IntPtr buffer)
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

        static string fromUTF8(IntPtr nativeUtf8)
        {
            int len = 0;
            while (Marshal.ReadByte(nativeUtf8, len) != 0)
                ++len;
            byte[] buffer = new byte[len];
            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus != windowHasFocus)
            {
                windowHasFocus = hasFocus;
                pushTreeUpdate();
            }
        }

        void LateUpdate()
        {
            pushTreeUpdate();
        }

        void OnApplicationQuit()
        {
            try
            {
                AccessKit.destroy(windowHandle);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
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

        void pushTreeUpdate()
        {
            if (!AccessKit.IsInitialized)
                return;
            var treeUpdate = new TreeUpdate();
            var nodes = AccessibleNode.allAccessibles.Values;
            rootNode.children = findChildren(rootNode.id, nodes);
            foreach (var node in nodes)
            {
                if (node.parent == null)
                    rootNode.children.Add(node.id);
                if (windowHasFocus && node.GetComponent<HasKeyboardFocus>() != null)
                    treeUpdate.focus = node.id;
                treeUpdate.nodes.Add(new AccessibleNodeData(node, findChildren(node.id, nodes)));
            }
            treeUpdate.nodes.Add(rootNode);
            if (treeUpdate.focus == null && windowHasFocus)
                treeUpdate.focus = rootNode.id;
            try
            {
                AccessKit.pushUpdate(windowHandle, treeUpdate);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }
        
        List<ulong> findChildren(ulong id, IList<AccessibleNode> nodes)
        {
            var children = new List<AccessibleNode>();
            foreach (var node in nodes)
            {
                if (node.parent != null && node.parent.id == id && node.id != 0 && node.id != id)
                    children.Add(node);
            }
            return children.ConvertAll(new Converter<AccessibleNode, ulong>(node => node.id));
        }

        [DllImport("user32")]
        static extern IntPtr GetActiveWindow();

        [DllImport("USER32.DLL")]
        private static extern int GetWindowTextLength(IntPtr hwnd);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int nMaxCount);
    }
}
