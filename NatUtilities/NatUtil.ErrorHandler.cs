using Eto.Forms;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace NatBase
{
    public static partial class NatUtil
    {
        /// <summary>
        /// This is a placeholder messagebox that utilizes eto.forms to create message box.
        /// </summary>
        /// <param name="msg"></param>
        public static void ErrorHandler_LEGACY(string msg)
        {
            Debug.WriteLine("");
        }
        /// <summary>
        /// Reports an error/warning, optionally annotating geometry in the Rhino document.
        /// </summary>
        /// <param name="msg">Message to log/show.</param>
        /// <param name="errorType">Message box type / severity.</param>
        /// <param name="throwError">If true, throws after reporting.</param>
        /// <param name="skipLog">If true, skips writing to the log.</param>
        public static void ErrorHandler(string msg, MessageBoxType errorType = MessageBoxType.Warning, bool throwError = true, bool skipLog = false)
        {
            OnErrorHandling(msg, errorType, throwError, (IEnumerable<GeometryBase>)null, skipLog);
        }
        /// <summary>
        /// Reports an error/warning, optionally annotating geometry in the Rhino document.
        /// </summary>
        /// <param name="msg">Message to log/show.</param>
        /// <param name="basePts">Points to mark in the document for debugging.</param>
        /// <param name="errorType">Message box type / severity.</param>
        /// <param name="throwError">If true, throws after reporting.</param>
        /// <param name="skipLog">If true, skips writing to the log.</param>
        public static void ErrorHandler(string msg, IEnumerable<Point3d> basePts, MessageBoxType errorType = MessageBoxType.Error, bool throwError = true, bool skipLog = false)
        {
            List<Curve> errorCircles = basePts.Select(pt => (Curve)(new Circle(pt, circleRadius * UnitConversion).ToNurbsCurve())).ToList();
            string addedMsg = msg + "\nCircles are generated to locate the erroneous geometry.";
            OnErrorHandling(addedMsg, errorType, throwError, errorCircles, skipLog);
        }
        /// <summary>
        /// Reports an error/warning, optionally annotating geometry in the Rhino document.
        /// </summary>
        /// <param name="msg">Message to log/show.</param>
        /// <param name="basePt">Point to mark in the document for debugging.</param>
        /// <param name="errorType">Message box type / severity.</param>
        /// <param name="throwError">If true, throws after reporting.</param>
        /// <param name="skipLog">If true, skips writing to the log.</param>
        public static void ErrorHandler(string msg, Point3d basePt, MessageBoxType errorType = MessageBoxType.Error, bool throwError = true, bool skipLog = false)
        {
            Circle errorCircle = new Circle(basePt, circleRadius * UnitConversion);
            Curve errorCrv = errorCircle.ToNurbsCurve();
            string addedMsg = msg + "\nCircle is generated to locate the erroneous geometry.";
            OnErrorHandling(addedMsg, errorType, throwError, errorCrv, skipLog);
        }
        /// <summary>
        /// Reports an error/warning, optionally annotating geometry in the Rhino document.
        /// </summary>
        /// <param name="msg">Message to log/show.</param>
        /// <param name="curve">Curve to mark in the document for debugging.</param>
        /// <param name="errorType">Message box type / severity.</param>
        /// <param name="makePipe">If true, generates a pipe around the curve(s) for visibility.</param>
        /// <param name="throwError">If true, throws after reporting.</param>
        /// <param name="skipLog">If true, skips writing to the log.</param>
        public static void ErrorHandler(string msg, Curve curve, MessageBoxType errorType = MessageBoxType.Error, bool makePipe = true, bool throwError = true, bool skipLog = false)
        {
            ErrorHandler(msg, new[] { curve }, errorType, makePipe, throwError, skipLog);
        }
        /// <summary>
        /// Reports an error/warning, optionally annotating geometry in the Rhino document.
        /// </summary>
        /// <param name="msg">Message to log/show.</param>
        /// <param name="curves">Curves to mark in the document for debugging.</param>
        /// <param name="errorType">Message box type / severity.</param>
        /// <param name="makePipe">If true, generates a pipe around the curve(s) for visibility.</param>
        /// <param name="throwError">If true, throws after reporting.</param>
        /// <param name="skipLog">If true, skips writing to the log.</param>
        public static void ErrorHandler(string msg, IEnumerable<Curve> curves, MessageBoxType errorType = MessageBoxType.Error, bool makePipe = true, bool throwError = true, bool skipLog = false)
        {
            List<GeometryBase> errorGeoIds = new List<GeometryBase>();
            foreach (Curve crv in curves)
            {
                if (makePipe)
                {
                    Brep errorPipe = Brep.CreatePipe(crv, pipeRadius * UnitConversion, false, PipeCapMode.None, false, abstol, angtol)[0];
                    errorGeoIds.Add(errorPipe);
                }
                else
                    errorGeoIds.Add(crv);
            }
            string addedMsg = msg + "\nPipes are generated to locate the erroneous geometry.";
            OnErrorHandling(addedMsg, errorType, throwError, errorGeoIds, skipLog);
        }

        /// <summary>
        /// Core implementation for logging, annotating, and surfacing errors.
        /// </summary>
        /// <param name="msg">Message to log/show.</param>
        /// <param name="errorType">Message box type / severity.</param>
        /// <param name="throwError">If true, throws after reporting.</param>
        /// <param name="errorObj">Optional geometry to mark in the document for debugging.</param>
        /// <param name="skipLog">If true, skips writing to the log.</param>
        public static void OnErrorHandling(string msg, MessageBoxType errorType, bool throwError, GeometryBase errorObj = null, bool skipLog = false)
        {
            if (errorObj != null)
                OnErrorHandling(msg, errorType, throwError, new[] { errorObj }, skipLog);
            else
                OnErrorHandling(msg, errorType, throwError, Enumerable.Empty<GeometryBase>(), skipLog);
        }


        /// <summary>
        /// Core implementation for logging, annotating, and surfacing errors.
        /// </summary>
        /// <param name="msg">Message to log/show.</param>
        /// <param name="errorType">Message box type / severity.</param>
        /// <param name="throwError">If true, throws after reporting.</param>
        /// <param name="errorObjs">Geometry to mark in the document for debugging.</param>
        /// <param name="skipLog">If true, skips writing to the log.</param>
        private static void OnErrorHandling(string msg, MessageBoxType errorType, bool throwError, IEnumerable<GeometryBase> errorObjs, bool skipLog)
        {
            void ShowError(object sender, EventArgs e)
            {
                RhinoApp.Idle -= ShowError;
                MessageBox.Show(msg, errorType.ToString(), MessageBoxButtons.OK, errorType, MessageBoxDefaultButton.OK);
            }
            ObjectAttributes objAttrs = CreateLayerAttributes(errorLayer, Color.Red);
            foreach (var objBase in errorObjs ?? Enumerable.Empty<GeometryBase>())
            {
                if (objBase != null)
                    rhinoDoc.Objects.Add(objBase, objAttrs);
            }

            rhinoDoc.Views.Redraw();
            if (errorType == MessageBoxType.Error)
                RhinoApp.Idle += ShowError;
            else
                RhinoApp.WriteLine(msg);
            if (throwError)
                throw new InvalidOperationException(msg);
        }
        /// <summary>
        /// Validates selection input and reports an error when empty or null.
        /// </summary>
        /// <param name="rhObjs">Selected Rhino objects to validate.</param>
        public static void SelectionNullCheck(List<RhinoObject> rhObjs)
        {
            if (rhObjs == null || rhObjs.Count == 0)
                ErrorHandler("Input geometry type/amount is wrong, AI layout aborts.");
        }
        /// <summary>
        /// Validates selection input and reports an error when empty or null.
        /// </summary>
        /// <param name="rhObj">Selected Rhino object to validate.</param>
        public static void SelectionNullCheck(RhinoObject rhObj)
        {
            if (rhObj == null || rhObj.Geometry == null)
                ErrorHandler("Input geometry type/amount is wrong, AI layout aborts.");
        }
    }
}