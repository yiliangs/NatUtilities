using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NatBase
{
    public static partial class NatUtil
    {
        private static int defaultLevel = 1;
        private static int defaultOption = 3;
        private static string defaultPrompt = "Stack";

        /// <summary>
        /// Prompts the user to pick a point in the Rhino viewport.
        /// </summary>
        /// <param name="prompt">Command-line prompt shown to the user.</param>
        /// <returns>
        /// The picked <see cref="Point3d"/>, or <see cref="Point3d.Unset"/> if the operation is cancelled.
        /// </returns>
        public static Point3d UserGetPoint(string prompt)
        {
            using (GetPoint getPoint = new GetPoint())
            {
                if (getPoint.Get() != GetResult.Point)
                    return Point3d.Unset;
                return getPoint.Point();
            }
        }

        /// <summary>
        /// Prompts the user to select one or more curve objects.
        /// </summary>
        /// <param name="prompt">Command-line prompt shown to the user.</param>
        /// <returns>
        /// A list of selected curves, or an empty list if none are selected.
        /// </returns>
        public static List<Curve> UserGetCurves(string prompt)
        {
            return UserGetObjects(prompt, ObjectType.Curve)
                .ConvertAll(obj => obj.Geometry as Curve);
        }

        /// <summary>
        /// Prompts the user to select a single curve object.
        /// </summary>
        /// <param name="prompt">Command-line prompt shown to the user.</param>
        /// <param name="preselect">Whether preselected objects are allowed.</param>
        /// <returns>The selected curve.</returns>
        public static Curve UserGetCurve(string prompt, bool preselect = true)
        {
            RhinoObject rhObj = OnUserGetObjects(prompt, ObjectType.Curve, false, 1)[0];
            return rhObj.Geometry as Curve;
        }

        /// <summary>
        /// Prompts the user to select a single block instance.
        /// </summary>
        /// <param name="prompt">Command-line prompt shown to the user.</param>
        /// <param name="preselect">Whether preselected objects are allowed.</param>
        /// <returns>The selected <see cref="InstanceObject"/>.</returns>
        public static InstanceObject UserGetBlock(string prompt, bool preselect = true)
        {
            RhinoObject rhObj = OnUserGetObjects(prompt, ObjectType.InstanceReference, preselect, 1)[0];
            return rhObj as InstanceObject;
        }

        /// <summary>
        /// Prompts the user to select multiple block instances.
        /// </summary>
        /// <param name="prompt">Command-line prompt shown to the user.</param>
        /// <param name="ot">Object type filter.</param>
        /// <param name="preselect">Whether preselected objects are allowed.</param>
        /// <param name="maxCount">Maximum number of selectable objects (0 = unlimited).</param>
        /// <returns>A list of selected <see cref="InstanceObject"/>s.</returns>
        public static List<InstanceObject> UserGetBlocks
            (string prompt, ObjectType ot = ObjectType.InstanceReference, bool preselect = true, int maxCount = 0)
        {
            List<RhinoObject> rhObjs = OnUserGetObjects(prompt, ot, preselect, maxCount);
            return rhObjs.ConvertAll(ro => (InstanceObject)ro);
        }

        /// <summary>
        /// Prompts the user to select a single Brep object.
        /// </summary>
        /// <param name="prompt">Command-line prompt shown to the user.</param>
        /// <param name="preselect">Whether preselected objects are allowed.</param>
        /// <returns>The selected <see cref="Brep"/>.</returns>
        public static Brep UserGetBrep(string prompt, bool preselect = true)
        {
            RhinoObject rhObj = OnUserGetObjects(prompt, ObjectType.Brep, false, 1)[0];
            return rhObj.Geometry as Brep;
        }

        /// <summary>
        /// Prompts the user to select a single Rhino object.
        /// </summary>
        /// <param name="prompt">Command-line prompt shown to the user.</param>
        /// <param name="ot">Object type filter.</param>
        /// <param name="preselect">Whether preselected objects are allowed.</param>
        /// <returns>The selected <see cref="RhinoObject"/>.</returns>
        public static RhinoObject UserGetObject
            (string prompt, ObjectType ot = ObjectType.AnyObject, bool preselect = true)
        {
            List<RhinoObject> rhObjs = OnUserGetObjects(prompt, ot, preselect, 1);
            return rhObjs?[0];
        }

        /// <summary>
        /// Prompts the user to select multiple Rhino objects.
        /// </summary>
        /// <param name="prompt">Command-line prompt shown to the user.</param>
        /// <param name="ot">Object type filter.</param>
        /// <param name="preselect">Whether preselected objects are allowed.</param>
        /// <param name="maxCount">Maximum number of selectable objects (0 = unlimited).</param>
        /// <returns>A list of selected <see cref="RhinoObject"/>s.</returns>
        public static List<RhinoObject> UserGetObjects
            (string prompt, ObjectType ot = ObjectType.AnyObject, bool preselect = true, int maxCount = 0)
        {
            List<RhinoObject> rhObjs = OnUserGetObjects(prompt, ot, preselect, maxCount);
            return rhObjs;
        }

        /// <summary>
        /// Core helper that performs Rhino object selection with common settings.
        /// </summary>
        /// <param name="prompt">Command-line prompt shown to the user.</param>
        /// <param name="ot">Object type filter.</param>
        /// <param name="preselect">Whether preselected objects are allowed.</param>
        /// <param name="maxCount">Maximum number of selectable objects (0 = unlimited).</param>
        /// <returns>
        /// A list of selected <see cref="RhinoObject"/>s, or null if selection fails or is cancelled.
        /// </returns>
        private static List<RhinoObject> OnUserGetObjects
            (string prompt, ObjectType ot, bool preselect = true, int maxCount = 0)
        {
            using (GetObject getObjectAction = new GetObject())
            {
                getObjectAction.GroupSelect = true;
                getObjectAction.SubObjectSelect = false;
                getObjectAction.SetCommandPrompt(prompt);
                getObjectAction.GeometryFilter = ot;
                getObjectAction.EnablePreSelect(preselect, true);
                if (getObjectAction.GetMultiple(1, maxCount) != GetResult.Object) return null;
                List<RhinoObject> rhObjs = getObjectAction.Objects().Select(obj => obj.Object()).ToList();
                rhinoDoc.Objects.UnselectAll();
                rhinoDoc.Views.Redraw();
                return rhObjs;
            }
        }

        /// <summary>
        /// Prompts the user to choose a patterning option.
        /// </summary>
        /// <param name="callerId">Caller identifier used to control prompt behavior.</param>
        /// <returns>The selected option index.</returns>
        public static int UserGetPatternOption(int callerId)
        {
            using (GetOption getOptionAction = new GetOption())
            {
                int rando = getOptionAction.AddOption("Random");
                int shift = getOptionAction.AddOption("Stagger");
                int stack = getOptionAction.AddOption("Stack");
                getOptionAction.SetCommandPrompt(callerId == 0 ? $"Select the patterning you want:<{defaultPrompt}>" : "");
                if (getOptionAction.Get() == GetResult.Option)
                {
                    defaultPrompt = getOptionAction.Option().LocalName;
                    defaultOption = getOptionAction.Option().Index;
                    return getOptionAction.Option().Index;
                }
                else
                    return defaultOption;
            }
        }

        /// <summary>
        /// Prompts the user to input a building level index.
        /// </summary>
        /// <returns>
        /// The entered level value, or 0 if the operation is cancelled.
        /// </returns>
        public static int UserGetLevel()
        {
            using (GetInteger getIntAction = new GetInteger())
            {
                string prompt = "Set the level of the building:";
                int intValue = 0;
                while (intValue < 1)
                {
                    getIntAction.SetCommandPrompt(prompt);
                    getIntAction.SetDefaultInteger(defaultLevel);
                    if (getIntAction.Result() == GetResult.Cancel)
                        return 0;
                    if (getIntAction.Get() == GetResult.Number)
                        intValue = getIntAction.Number();
                    prompt = "Invalid input! Please type in an integer larger than 0.";
                }
                defaultLevel = intValue;
                return intValue;
            }
        }
    }
}
