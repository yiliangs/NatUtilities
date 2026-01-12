using Rhino.DocObjects;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NatBase
{
    public static partial class NatUtil
    {
        /// <summary>
        /// Creates an <see cref="ObjectAttributes"/> configured to draw from a given layer.
        /// </summary>
        /// <param name="layerName">Layer name (created if missing).</param>
        /// <param name="color">Layer color (used if created).</param>
        /// <returns>Attributes pointing to the target layer and using layer color.</returns>
        public static ObjectAttributes CreateLayerAttributes(string layerName, Color color)
        {
            int layer = GetOrCreateLayer(layerName, color);
            return new ObjectAttributes
            {
                LayerIndex = layer,
                ColorSource = ObjectColorSource.ColorFromLayer
            };
        }

        /// <summary>
        /// Overload that creates layer attributes with black as the default layer color.
        /// </summary>
        public static ObjectAttributes CreateLayerAttributes(string layerName)
        {
            return CreateLayerAttributes(layerName, Color.Black);
        }

        /// <summary>
        /// Checks whether this Rhino process is the only running instance (by process name "Rhino").
        /// </summary>
        /// <remarks>
        /// This is a heuristic based on process enumeration; it may be affected by:
        /// multiple Rhino versions, naming differences, or permissions.
        /// </remarks>
        public static bool IsSingleInstance()
        {
            int currentPid = Process.GetCurrentProcess().Id;
            var rhinoProcesses = Process.GetProcessesByName("Rhino");
            bool otherRhinosRunning = rhinoProcesses.Any(p => p.Id != currentPid);
            return !otherRhinosRunning;
        }

        /// <summary>
        /// Obfuscates a string by bitwise-NOT each UTF-8 byte, then Base64 encodes.
        /// </summary>
        /// <remarks>
        /// This is not encryption; it's reversible obfuscation.
        /// </remarks>
        public static string FlipToBase64(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)~bytes[i];

            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Reverses <see cref="FlipToBase64"/> (Base64 decode then bitwise-NOT each byte).
        /// </summary>
        public static string RegularStringOperate(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)~bytes[i];

            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Appends the full content of <paramref name="sourcePath"/> to <paramref name="targetPath"/>.
        /// </summary>
        public static void AppendLog(string targetPath, string sourcePath)
        {
            string content = File.ReadAllText(sourcePath);
            File.AppendAllText(targetPath, content);
        }

        /// <summary>
        /// Generates a 128-bit key from an input string by computing its MD5 hash.
        /// </summary>
        /// <remarks>
        /// MD5 is not collision-resistant and should not be used for security-sensitive hashing.
        /// This is suitable only for non-security identifiers / checksums.
        /// </remarks>
        public static byte[] Key128Generator(string input)
        {
            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                return md5.ComputeHash(inputBytes);
            }
        }

        /// <summary>
        /// Checks whether the current process name contains a given host name token.
        /// </summary>
        /// <param name="hostName">Substring to search (case-insensitive).</param>
        public static bool IsRunningInsideHost(string hostName)
        {
            Process current = Process.GetCurrentProcess();
            return current.ProcessName.IndexOf(hostName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Forces the current layer to a layer named "Default" (creates it if missing).
        /// </summary>
        /// <remarks>
        /// "Default" is treated as a convention here.
        /// </remarks>
        public static void ForceSetCurrentLayerToDefault()
        {
            int defaultIdx = GetOrCreateLayer("Default", Color.Black);
            rhinoDoc.Layers.SetCurrentLayerIndex(defaultIdx, false);
        }

        /// <summary>
        /// Moves an object to the destination layer by modifying its attributes.
        /// </summary>
        /// <param name="rhObj">Target Rhino object.</param>
        /// <param name="destinationLayer">Layer to move into.</param>
        /// <param name="hard">
        /// If true, ensure the object is unlocked and visible before modifying attributes.
        /// </param>
        public static void MoveObjectToLayer(RhinoObject rhObj, Layer destinationLayer, bool hard = true)
        {
            var attr = rhObj.Attributes.Duplicate();
            attr.LayerIndex = destinationLayer.Index;

            if (hard)
            {
                rhinoDoc.Objects.Unlock(rhObj, true);
                rhinoDoc.Objects.Show(rhObj, true);
            }

            rhinoDoc.Objects.ModifyAttributes(rhObj, attr, true);
        }

        //public static void OnLayerTableEvent(object sender, LayerTableEventArgs e)
        //{
        //    if (e.EventType == LayerTableEventType.Modified)
        //    {
        //        var currentLayer = RhinoDoc.ActiveDoc.Layers.CurrentLayer;
        //        if (currentLayer != null && currentLayer.Index == e.LayerIndex)
        //        {
        //            rhinoDoc.Layers.SetCurrentLayerIndex(0, true);
        //        }
        //    }
        //}
    }
}
