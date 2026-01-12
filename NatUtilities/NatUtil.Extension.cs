using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NatBase
{
    public static partial class NatUtil
    {
        /// <summary>
        /// Determines whether the U or V direction of a BrepFace is aligned with world Z.
        /// </summary>
        /// <param name="face">Target BrepFace.</param>
        /// <param name="tolerance">
        /// Cosine tolerance against Z-axis (default ~0.001). Values near 1 mean vertical.
        /// </param>
        /// <returns>
        /// 0: U+Z, 1: U−Z, 2: V+Z, 3: V−Z, -1: no vertical direction detected or failure.
        /// </returns>
        public static int GetVerticalDirection(this BrepFace face, double tolerance = 1e-3)
        {
            if (face == null)
                return -1;

            Interval uDomain = face.Domain(0);
            Interval vDomain = face.Domain(1);

            double u = (uDomain.T0 + uDomain.T1) / 2.0;
            double v = (vDomain.T0 + vDomain.T1) / 2.0;

            if (!face.FrameAt(u, v, out Plane frame))
                return -1;

            Vector3d uDir = frame.XAxis;
            Vector3d vDir = frame.YAxis;
            Vector3d zAxis = Vector3d.ZAxis;

            double uDotZ = uDir * zAxis;
            double vDotZ = vDir * zAxis;

            if (Math.Abs(uDotZ) > 1.0 - tolerance && Math.Abs(uDotZ) > Math.Abs(vDotZ))
                return uDotZ > 0 ? 0 : 1;

            if (Math.Abs(vDotZ) > 1.0 - tolerance && Math.Abs(vDotZ) > Math.Abs(uDotZ))
                return vDotZ > 0 ? 2 : 3;

            return -1;
        }

        /// <summary>
        /// Finds the BrepFace whose closest point to a given point is minimal.
        /// </summary>
        /// <param name="brep">Source Brep.</param>
        /// <param name="point">Query point.</param>
        /// <returns>Closest BrepFace, or null if none.</returns>
        public static BrepFace FindClosestBrepFace(this Brep brep, Point3d point)
        {
            BrepFace closestFace = null;
            double minDist = double.MaxValue;

            foreach (BrepFace face in brep.Faces)
            {
                face.ClosestPoint(point, out double u, out double v);
                Point3d facePt = face.PointAt(u, v);
                double dist = facePt.DistanceTo(point);

                if (dist < minDist)
                {
                    minDist = dist;
                    closestFace = face;
                }
            }
            return closestFace;
        }

        /// <summary>
        /// Tests whether a surface lies on the XY plane (normal aligned with +Z).
        /// </summary>
        /// <param name="srf">Target surface.</param>
        /// <param name="strict">
        /// If true, test center + 4 corners; otherwise only center.
        /// </param>
        /// <returns>True if surface normal(s) align with Z within tolerance.</returns>
        public static bool IsSurfaceOnXYPlane(this Surface srf, bool strict = false)
        {
            List<(double, double)> testPts = new List<(double, double)>()
            {
                srf.NormalizedToUV(0.5, 0.5),
                srf.NormalizedToUV(0,0),
                srf.NormalizedToUV(1,0),
                srf.NormalizedToUV(0,1),
                srf.NormalizedToUV(1,1),
            };

            if (strict)
                return testPts.All(pt =>
                    VecRadAngleInHalfPi(srf.NormalAt(pt.Item1, pt.Item2), Vector3d.ZAxis) < abstol);
            else
                return VecRadAngleInHalfPi(
                    srf.NormalAt(testPts[0].Item1, testPts[0].Item2), Vector3d.ZAxis) < abstol;
        }

        /// <summary>
        /// Evaluates a surface point using normalized UV coordinates [0–1].
        /// </summary>
        public static Point3d PointAtNormalizedUV(this Surface srf, double uNorm, double vNorm)
        {
            Point2d uv = srf.NormalizedToUV(new Point2d(uNorm, vNorm));
            return srf.PointAt(uv[0], uv[1]);
        }

        /// <summary>
        /// Evaluates a surface normal using normalized UV coordinates [0–1].
        /// </summary>
        public static Vector3d NormalAtNormalizedUV(this Surface srf, double uNorm, double vNorm)
        {
            Point2d uv = srf.NormalizedToUV(new Point2d(uNorm, vNorm));
            return srf.NormalAt(uv[0], uv[1]);
        }

        /// <summary>
        /// Converts normalized UV coordinates [0–1] to surface UV domain values.
        /// </summary>
        public static (double, double) NormalizedToUV(this Surface srf, double uNorm, double vNorm)
        {
            Point2d normalized = srf.NormalizedToUV(new Point2d(uNorm, vNorm));
            return (normalized[0], normalized[1]);
        }

        /// <summary>
        /// Converts normalized UV coordinates [0–1] to surface UV domain values.
        /// </summary>
        public static Point2d NormalizedToUV(this Surface srf, Point2d uv)
        {
            if (srf == null)
                throw new ArgumentNullException(nameof(srf));

            Interval domU = srf.Domain(0);
            Interval domV = srf.Domain(1);

            double u = domU.Min + uv[0] * (domU.Max - domU.Min);
            double v = domV.Min + uv[1] * (domV.Max - domV.Min);

            return new Point2d(u, v);
        }

        /// <summary>
        /// Converts surface UV domain values to normalized [0–1] coordinates.
        /// </summary>
        public static (double, double) UVToNormalized(this Surface srf, double u, double v)
        {
            Point2d uv = srf.UVToNormalized(new Point2d(u, v));
            return (uv[0], uv[1]);
        }

        /// <summary>
        /// Converts surface UV domain values to normalized [0–1] coordinates.
        /// </summary>
        public static Point2d UVToNormalized(this Surface srf, Point2d normalized)
        {
            if (srf == null)
                throw new ArgumentNullException(nameof(srf));

            Interval domU = srf.Domain(0);
            Interval domV = srf.Domain(1);

            double uNorm = (normalized[0] - domU.Min) / (domU.Max - domU.Min);
            double vNorm = (normalized[1] - domV.Min) / (domV.Max - domV.Min);

            return new Point2d(uNorm, vNorm);
        }

        /// <summary>
        /// Safely casts common GeometryBase types to Brep.
        /// </summary>
        /// <param name="gBase">GeometryBase instance.</param>
        /// <returns>Brep if convertible; otherwise null.</returns>
        public static Brep CastGeometryToBrep(GeometryBase gBase)
        {
            if (gBase is Extrusion extru)
                return extru.ToBrep();
            else if (gBase is Brep brep)
                return brep;
            else
                return null;
        }
    }
}
