using Rhino;
using System;
using System.IO;
using System.Reflection;

namespace NatBase
{
    public static partial class NatUtil
    {
        // magic numbers
        public static readonly double MinBlockWidth = 0.02;
        private static readonly double pipeRadius = 0.8;
        private static readonly double circleRadius = 1;
        public static readonly double contourShift = 0.02;
        public static readonly int radianRounding = 5;
        public static readonly int doubleRounding = 4;
        public static readonly double planDistTol = 0.5;
        public static readonly double areaTol = 0.1;
        public static readonly double multiTol = 1e-2;
        public static Random Random = new Random();
        public static UnitSystem UnitSys => rhinoDoc.ModelUnitSystem;
        public static RhinoDoc rhinoDoc => RhinoDoc.ActiveDoc;
        public static double UnitConversion => RhinoMath.UnitScale(UnitSystem.Meters, UnitSys);
        public static double abstol => 1e-3 * UnitConversion;
        public static double blocktol => abstol * 2;
        public static double angtol => rhinoDoc.ModelAngleToleranceRadians;
        public static double localtol => abstol * 10;
    }
}
