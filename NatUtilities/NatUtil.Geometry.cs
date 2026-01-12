using Eto.Forms;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace NatBase
{
    public static partial class NatUtil
    {
        private static byte[] osbyte = Encoding.UTF8.GetBytes($"{oscard}");
        private static byte[] ubyte = Encoding.UTF8.GetBytes($"{ucard}");
        
        // geometries that proven to be problematic
        private static readonly string[] CrptGeomFlags = new string[] { "Wm1sa1pXeHBidz09", "YnJva2VuIGdlbw==" };
        private static byte[] geoType = osbyte ?? Guid.Empty.ToByteArray();
        private static byte[] geoHash = ubyte ?? Guid.Empty.ToByteArray();


        /// <summary>
        /// Creates a new block definition in the active document with a unique name ("Block 01", "Block 02", ...).
        /// </summary>
        /// <param name="geometries">Geometry to include in the block definition.</param>
        /// <returns>The created <see cref="InstanceDefinition"/>, or null on failure.</returns>
        public static InstanceDefinition CreateNewBlockDefinition(IEnumerable<GeometryBase> geometries)
        {
            if (rhinoDoc == null || geometries == null)
                return null;

            var geoList = geometries.ToList();
            if (geoList.Count == 0)
                return null;

            int index = 1;
            string name;
            do
            {
                name = $"Block {index:00}";
                index++;
            } while (rhinoDoc.InstanceDefinitions.Find(name) != null);

            var idefIndex = rhinoDoc.InstanceDefinitions.Add(name, string.Empty, Point3d.Origin, geoList);
            if (idefIndex < 0)
                return null;

            return rhinoDoc.InstanceDefinitions[idefIndex];
        }
        // MUST make sure both the rectangle and cutting line are orthogonal and in the same plane!
        public static Curve[] SplitRectangleOrthogonal(Curve rectangle, Line cuttingLine)
        {
            var results = new Curve[2];

            BoundingBox bbox = rectangle.GetBoundingBox(true);

            Vector3d delta = cuttingLine.To - cuttingLine.From;
            bool isVerticalCut = Math.Abs(delta.X) < 1e-6;
            bool isHorizontalCut = Math.Abs(delta.Y) < 1e-6;

            BoundingBox box1, box2;

            if (isVerticalCut)
            {
                double cutX = cuttingLine.From.X;

                if (cutX <= bbox.Min.X || cutX >= bbox.Max.X) 
                    return results;

                box1 = new BoundingBox(bbox.Min.X, bbox.Min.Y, 0, cutX, bbox.Max.Y, 0);
                box2 = new BoundingBox(cutX, bbox.Min.Y, 0, bbox.Max.X, bbox.Max.Y, 0);
            }
            else if (isHorizontalCut)
            {
                double cutY = cuttingLine.From.Y;

                if (cutY <= bbox.Min.Y || cutY >= bbox.Max.Y) 
                    return results;

                box1 = new BoundingBox(bbox.Min.X, bbox.Min.Y, 0, bbox.Max.X, cutY, 0);
                box2 = new BoundingBox(bbox.Min.X, cutY, 0, bbox.Max.X, bbox.Max.Y, 0);
            }
            else
            {
                return results;
            }
            results[0] = new Rectangle3d(Plane.WorldXY, box1.Min, box1.Max).ToNurbsCurve();
            results[1] = new Rectangle3d(Plane.WorldXY, box2.Min, box2.Max).ToNurbsCurve();

            return results;
        }

        /// <summary>
        /// Checks whether the insertion point aligns to a specified bounding box face (min/max of an axis).
        /// </summary>
        public static bool CheckInsertionPointAlignments(InstanceDefinition insDef, Bound bound)
        {
            List<GeometryBase> gBase = insDef.GetObjects().Select(insObj => insObj.Geometry).ToList();
            BoundingBox bbox = GetUnionBoundingBox(gBase);
            return OnCheckInsertionPointAlignment(bbox, bound);
        }

        /// <summary>
        /// Checks whether the insertion point aligns to all specified bounds.
        /// </summary>
        public static bool CheckInsertionPointAlignments(InstanceDefinition insDef, IEnumerable<Bound> bounds)
        {
            List<GeometryBase> gBase = insDef.GetObjects().Select(insObj => insObj.Geometry).ToList();
            BoundingBox bbox = GetUnionBoundingBox(gBase);
            return bounds.All(b => OnCheckInsertionPointAlignment(bbox, b));
        }

        /// <summary>
        /// Performs bound-specific alignment test against a bounding box.
        /// </summary>
        private static bool OnCheckInsertionPointAlignment(BoundingBox bbox, Bound bound)
        {
            switch (bound)
            {
                case Bound.Xmin:
                    return CheckValueCloseness(bbox.Min.X);
                case Bound.Xmax:
                    return CheckValueCloseness(bbox.Max.X);
                case Bound.Ymin:
                    return CheckValueCloseness(bbox.Min.Y);
                case Bound.Ymax:
                    return CheckValueCloseness(bbox.Max.Y);
                case Bound.Zmin:
                    return CheckValueCloseness(bbox.Min.Z);
                case Bound.Zmax:
                    return CheckValueCloseness(bbox.Max.Z);
                default:
                    return true;
            }
        }

        /// <summary>
        /// Generates a random RGB color.
        /// </summary>
        public static Color RandomColor()
        {
            return Color.FromArgb(Random.Next(256), Random.Next(256), Random.Next(256));
        }

        /// <summary>
        /// Draws a vector visualization as a line with an arrowhead at the end.
        /// </summary>
        /// <param name="anchor">Line start point.</param>
        /// <param name="vector">Direction vector.</param>
        /// <param name="length">Length multiplier (in model units, scaled by UnitConversion).</param>
        /// <param name="color">Object color.</param>
        public static void VisualizeVector(Point3d anchor, Vector3d vector, double length = 1, Color color = default)
        {
            Line line = new Line(anchor, vector, length * UnitConversion);
            ObjectAttributes oa = new ObjectAttributes()
            { ObjectColor = color, ColorSource = ObjectColorSource.ColorFromObject };
            oa.ObjectDecoration = ObjectDecoration.EndArrowhead;
            rhinoDoc.Objects.AddLine(line, oa);
        }

        /// <summary>
        /// Checks whether a value is close to zero using the global absolute tolerance.
        /// </summary>
        private static bool CheckValueCloseness(double value)
        {
            return Math.Abs(value) < abstol;
        }

        /// <summary>
        /// Sorts points by the major axis of spread: X if X-range &gt; Y-range, otherwise Y.
        /// </summary>
        public static List<Point3d> SortPointsByMajorOrient(List<Point3d> points)
        {
            var xs = points.Select(p => p.X);
            double xRange = xs.Max() - xs.Min();
            var ys = points.Select(p => p.Y);
            double yRange = ys.Max() - ys.Min();
            if (xRange > yRange) return points.OrderBy(p => p.X).ToList();
            else return points.OrderBy(p => p.Y).ToList();
        }

        /// <summary>
        /// Sorts objects by the major axis of the associated point spread.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="objects">Objects to sort.</param>
        /// <param name="points">Reference points (must match objects count).</param>
        public static List<T> SortObjectsByMajorOrient<T>(List<T> objects, List<Point3d> points)
        {
            if (objects.Count != points.Count)
                throw new ArgumentException("counts don't match!");

            var pairs = points.Zip(objects, (pt, obj) => (pt, obj)).ToList();
            var xs = points.Select(p => p.X);
            double xRange = xs.Max() - xs.Min();
            var ys = points.Select(p => p.Y);
            double yRange = ys.Max() - ys.Min();

            if (xRange > yRange)
                pairs = pairs.OrderBy(p => p.pt.X).ToList();
            else
                pairs = pairs.OrderBy(p => p.pt.Y).ToList();

            return pairs.Select(pa => pa.obj).ToList();
        }

        /// <summary>
        /// Collapses a point list to its centroid (average X/Y/Z).
        /// </summary>
        public static Point3d CollapsePointList(List<Point3d> points)
        {
            double centerX = points.Average(point => point.X);
            double centerY = points.Average(point => point.Y);
            double centerZ = points.Average(point => point.Z);
            return new Point3d(centerX, centerY, centerZ);
        }

        /// <summary>
        /// Sorts points by projection along a direction vector.
        /// </summary>
        public static void SortPointsAlongVector(List<Point3d> pts, Vector3d vec)
        {
            vec.Unitize();
            pts.Sort((pt1, pt2) => (new Vector3d(pt1) * vec).CompareTo(new Vector3d(pt2) * vec));
        }

        /// <summary>
        /// Returns the scalar projection of a point position vector onto a direction.
        /// </summary>
        public static double GetProjectedHeight(Point3d pt, Vector3d alongVec)
        {
            alongVec.Unitize();
            Vector3d distVec = new Vector3d(pt);
            return distVec * alongVec;
        }
        /// <summary>
        /// Calculates the shortest distance from the line segment defined by two points to its projection perpendicular
        /// to a specified direction vector.
        /// </summary>
        /// <param name="a">The starting point of the line segment.</param>
        /// <param name="b">The ending point of the line segment.</param>
        /// <param name="v">The direction vector to which the perpendicular projection is computed. Must be non-zero.</param>
        /// <returns>The length of the component of the segment from <paramref name="a"/> to <paramref name="b"/> that is
        /// perpendicular to <paramref name="v"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="v"/> is a zero vector.</exception>
        public static double ProjectionDistancePerpToVector(Point3d a, Point3d b, Vector3d v)
        {
            if (!v.Unitize())
                throw new ArgumentException("Direction vector must be non-zero.", nameof(v));

            Vector3d d = b - a;
            Vector3d perp = d - (d * v) * v;
            return perp.Length;
        }
        /// <summary>
        /// Calculates the absolute distance that the vector from point <paramref name="a"/> to point <paramref
        /// name="b"/> projects onto the specified direction vector.
        /// </summary>
        /// <param name="a">The starting point of the segment to project.</param>
        /// <param name="b">The ending point of the segment to project.</param>
        /// <param name="v">The direction vector onto which the segment is projected. Must be non-zero.</param>
        /// <returns>The absolute value of the scalar projection of the vector from <paramref name="a"/> to <paramref name="b"/>
        /// onto <paramref name="v"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="v"/> is the zero vector.</exception>
        public static double ProjectionDistanceAlongVector(Point3d a, Point3d b, Vector3d v)
        {
            if (!v.Unitize())
                throw new ArgumentException("Direction vector must be non-zero.", nameof(v));

            Vector3d d = b - a;
            return Math.Abs(d * v);
        }

        /// <summary>
        /// Returns a perpendicular vector rotated +90° around Z, made unsigned (direction canonicalized).
        /// </summary>
        /// <param name="vec">Input vector (modified).</param>
        /// <param name="unitize">If true, unitizes before returning.</param>
        public static Vector3d GetUnsignedUnitPerpVec(Vector3d vec, bool unitize = false)
        {
            vec.Rotate(Math.PI / 2, Vector3d.ZAxis);
            vec = UnsignVector(vec);
            if (unitize) vec.Unitize();
            return vec;
        }

        /// <summary>
        /// Checks whether the vector from the curve closest point to test point is perpendicular to the curve tangent.
        /// </summary>
        public static bool IsPointPerpToCrvClosest(Point3d testPt, Curve crv, out Point3d closestPt)
        {
            crv.ClosestPoint(testPt, out double closestT);
            closestPt = crv.PointAt(closestT);
            Vector3d pullVec = testPt - closestPt;
            return Vector3d.VectorAngle(pullVec, crv.TangentAt(closestT)) - Math.PI / 2 < angtol;
        }

        /// <summary>
        /// Canonicalizes a vector direction so it points to the upper half-plane (Y&gt;0) or +X when Y==0.
        /// </summary>
        public static Vector3d UnsignVector(Vector3d vec)
        {
            if (vec.Y < 0 || (vec.Y == 0 && vec.X < 0)) vec = -vec;
            return vec;
        }

        /// <summary>
        /// Canonicalizes and unitizes a vector.
        /// </summary>
        public static Vector3d GetUnsignedUnitVec(Vector3d vec)
        {
            vec = UnsignVector(vec);
            vec.Unitize();
            return vec;
        }

        /// <summary>
        /// Creates a WorldXY rectangle given a center point, width (Y) and length (X).
        /// </summary>
        public static Rectangle3d DrawRectFromCenter(Point3d center, double width, double length)
        {
            Plane plane = Plane.WorldXY;
            plane.Origin = center - new Vector3d(length / 2, width / 2, 0);
            return new Rectangle3d(plane, length, width);
        }

        /// <summary>
        /// Closest-point distance in XY only (ignores Z).
        /// </summary>
        public static double Closest2dDistanceToCurve(Curve crv, Point3d pt, out double t)
        {
            crv.ClosestPoint(pt, out t);
            Point3d closest = crv.PointAt(t);
            return new Point2d(pt.X, pt.Y).DistanceTo(new Point2d(closest.X, closest.Y));
        }

        /// <summary>
        /// Closest-point distance in 3D.
        /// </summary>
        public static double Closest3dDistanceToCurve(Curve crv, Point3d pt, out double t)
        {
            crv.ClosestPoint(pt, out t);
            Point3d closest = crv.PointAt(t);
            return closest.DistanceTo(pt);
        }

        /// <summary>
        /// Validates a Brep as valid and solid.
        /// </summary>
        public static bool CheckBrepValidity(Brep brep, out string msg)
        {
            if (!brep.IsValid)
            {
                msg = "Input massing is not valid!";
                return false;
            }
            if (!brep.IsSolid)
            {
                msg = "Input massing is not solid!";
                return false;
            }
            msg = string.Empty;
            return true;
        }

        /// <summary>
        /// Obsolete wrapper that shows UI on failure.
        /// </summary>
        public static void CheckPlanarCurveValidity_OBSOLETE(Curve crv)
        {
            if (!CheckPlanarCurveValidity(crv, out string msg))
            {
                string addmsg = msg + "\nPlease fix before continue.";
                ErrorHandler(addmsg, crv, MessageBoxType.Error);
            }
        }

        /// <summary>
        /// Checks closure (attempts to close), planarity, and validity for a curve.
        /// </summary>
        /// <remarks>
        /// Note: This method calls <see cref="Curve.MakeClosed(double)"/> which may modify the input curve.
        /// </remarks>
        /// <returns>True if the curve passes all checks; otherwise false.</returns>
        public static bool CheckPlanarCurveValidity(Curve crv, out string msg)
        {
            if (!crv.MakeClosed(localtol))
            {
                msg = "Input curve is not closed!";
                return false;
            }
            if (!crv.IsPlanar(abstol))
            {
                msg = "Input curve is not planar!";
                return false;
            }
            if (!crv.IsValid)
            {
                msg = "Input curve is not valid!";
                return false;
            }
            msg = string.Empty;
            return true;
        }

        /// <summary>
        /// Calibrates a closed curve to a consistent seam, CCW orientation (around +Z), and domain [0,1000].
        /// </summary>
        public static void CalibrateCurve(ref Curve crv)
        {
            Curve[] segCurves = crv.DuplicateSegments();
            List<Point3d> ctrlPts = segCurves.Select(sc => sc.PointAtStart).ToList();
            List<Point3d> sortedCtrlPts = ctrlPts.OrderBy(p => p.X + p.Y + p.Z).ToList();
            crv.ClosestPoint(sortedCtrlPts[0], out double t);
            crv.ChangeClosedCurveSeam(t);
            if (crv.ClosedCurveOrientation(Vector3d.ZAxis) == CurveOrientation.Clockwise)
                crv.Reverse();
            crv.Domain = new Interval(0, 1000);
        }

        /// <summary>
        /// Cleans up a curve by removing short segments, combining arcs, and simplifying.
        /// </summary>
        public static void RefineCurve(ref Curve crv)
        {
            crv.RemoveShortSegments(abstol);
            CombineArcsIfPossible(ref crv);
            crv = crv.Simplify(CurveSimplifyOptions.All, abstol, angtol) ?? crv;
        }

        /// <summary>
        /// Attempts to merge neighboring arc segments into longer arcs when continuity is detected.
        /// </summary>
        public static void CombineArcsIfPossible(ref Curve curve)
        {
            List<Curve> segments = curve.DuplicateSegments().ToList();
            if (segments.Count < 2) return;

            for (int i = 0; i < segments.Count; i++)
            {
                Curve seg1 = segments[i];
                Curve seg2 = segments[(i + 1) % segments.Count];
                if (seg1.TryGetArc(out Arc arc1, localtol) && seg2.TryGetArc(out Arc arc2, localtol))
                {
                    if (AreNeighboringArcsContinuous(arc1, arc2))
                    {
                        Curve combinedArc = CombineContinuousArcs(arc1, arc2).ToNurbsCurve();
                        segments[i] = combinedArc;
                        segments.Remove(seg2);
                    }
                }
            }

            Curve[] joinedCrvs = Curve.JoinCurves(segments, localtol);
            if (joinedCrvs.Length == 1)
                curve = joinedCrvs[0];
            else
                ErrorHandler("Fix the tiny gap/segment marked in red circle before proceeding!", joinedCrvs[1].PointAtStart);
        }

        /// <summary>
        /// Continuity test for neighboring arcs (currently: same center within tolerance).
        /// </summary>
        private static bool AreNeighboringArcsContinuous(Arc arc1, Arc arc2)
        {
            return arc1.Center.DistanceTo(arc2.Center) < localtol;
        }

        /// <summary>
        /// Combines two continuous arcs into a single arc with summed angle (must be ≤ 2π).
        /// </summary>
        private static Arc CombineContinuousArcs(Arc arc1, Arc arc2)
        {
            double totalAngle = arc1.Angle + arc2.Angle;
            if (totalAngle <= 2 * Math.PI)
            {
                return new Arc(arc1.Plane, arc1.Center, arc1.Radius, totalAngle);
            }
            throw new InvalidOperationException("Sum of Arc angles beyond 2*Pi");
        }

        /// <summary>
        /// Computes a union bounding box from RhinoObjects, optionally excluding invisible objects.
        /// </summary>
        public static BoundingBox GetUnionBoundingBox(IEnumerable<RhinoObject> objects, bool includeInvisible = true)
        {
            return GetUnionBoundingBox(objects?
                .Where(o => includeInvisible || o.Visible)
                .Select(o => o?.Geometry));
        }

        /// <summary>
        /// Computes a union bounding box from object ids in the active document.
        /// </summary>
        public static BoundingBox GetUnionBoundingBox(IEnumerable<Guid> objectIds)
        {
            return GetUnionBoundingBox(objectIds?.Select(id => rhinoDoc.Objects.Find(id)?.Geometry));
        }

        /// <summary>
        /// Computes a union bounding box from a set of bounding boxes.
        /// </summary>
        public static BoundingBox GetUnionBoundingBox(IEnumerable<BoundingBox> bbox)
        {
            if (bbox == null || bbox.Count() == 0)
                return BoundingBox.Unset;
            if (bbox.Count() == 1)
                return bbox.First();

            BoundingBox unionBbox = bbox.First();
            for (int i = 1; i < bbox.Count(); i++)
            {
                unionBbox = BoundingBox.Union(unionBbox, bbox.ElementAt(i));
            }
            return unionBbox;
        }

        /// <summary>
        /// Computes a union bounding box from a set of geometries (World Coordinate System bbox).
        /// </summary>
        public static BoundingBox GetUnionBoundingBox(IEnumerable<GeometryBase> geometries)
        {
            if (geometries == null)
                return BoundingBox.Unset;

            BoundingBox unionBbox = BoundingBox.Empty;

            foreach (var geom in geometries)
            {
                if (geom == null)
                    continue;

                BoundingBox bbox = geom.GetBoundingBox(true);
                if (bbox.IsValid)
                    unionBbox = BoundingBox.Union(unionBbox, bbox);
            }

            return unionBbox.IsValid ? unionBbox : BoundingBox.Unset;
        }

        /// <summary>
        /// Returns 3D distance from a point to a curve (closest point).
        /// </summary>
        public static double PointDistanceToCurve(Point3d point, Curve crv)
        {
            crv.ClosestPoint(point, out double t);
            return crv.PointAt(t).DistanceTo(point);
        }

        /// <summary>
        /// Gets or creates a child layer under a parent layer, assigning color to the child.
        /// </summary>
        /// <returns>Child layer index, or -1 on failure.</returns>
        public static int GetOrCreateLayer(string parentLayer, string childLayer, Color color)
        {
            if (string.IsNullOrEmpty(parentLayer) || string.IsNullOrEmpty(childLayer))
                return -1;

            Layer parent = rhinoDoc.Layers.FindName(parentLayer);
            if (parent == null)
            {
                parent = new Layer { Name = parentLayer };
                int parentIndex = rhinoDoc.Layers.Add(parent);
                if (parentIndex == -1)
                {
                    ErrorHandler($"Failed to add parent layer: {parentLayer}");
                    return -1;
                }
                parent = rhinoDoc.Layers[parentIndex];
            }

            Layer child = rhinoDoc.Layers.FindName(childLayer);
            if (child != null)
                return child.Index;

            child = new Layer
            {
                Name = childLayer,
                Color = color,
                ParentLayerId = parent.Id
            };

            int childIndex = rhinoDoc.Layers.Add(child);
            if (childIndex == -1)
            {
                ErrorHandler($"Failed to add child layer: {childLayer}");
                return -1;
            }

            return childIndex;
        }

        /// <summary>
        /// Gets or creates a layer by name with a given color.
        /// </summary>
        /// <returns>Layer index (existing or created).</returns>
        public static int GetOrCreateLayer(string layerName, Color color)
        {
            Layer layer = rhinoDoc.Layers.FindName(layerName);
            if (layer == null || layer.Index < 0)
            {
                layer = new Layer { Name = layerName, Color = color };
                int layerIndex = rhinoDoc.Layers.Add(layer);
                if (layerIndex < 0)
                    ErrorHandler("Failed to add new layer to the document.");
                return layerIndex;
            }
            return layer.Index;
        }

        /// <summary>
        /// Ensures a curve is a NurbsCurve (returns original if already Nurbs).
        /// </summary>
        public static NurbsCurve ConvertCurveToNurbsCurve(Curve crv)
        {
            if (crv is NurbsCurve nc) return nc;
            return crv.ToNurbsCurve();
        }

        /// <summary>
        /// Creates a duplicate geometry suitable for morph/transform pipelines.
        /// </summary>
        public static GeometryBase MorphableCast(GeometryBase gb)
        {
            switch (gb)
            {
                case Brep brep:
                    return brep.Duplicate();
                case Curve crv:
                    return crv.ToNurbsCurve();
                case Extrusion extrusion:
                    return extrusion.ToBrep();
                default:
                    return gb;
            }
        }

        /// <summary>
        /// Obsolete massing collection behavior (deletes Brep/Extrusion objects).
        /// </summary>
        public static void MassingCollection_OBSOLETE(List<RhinoObject> rhObjs)
        {
            rhObjs.ForEach(ro => MassingCollection_OBSOLETE(ro));
        }

        /// <summary>
        /// Obsolete massing collection behavior (deletes Brep/Extrusion objects).
        /// </summary>
        public static void MassingCollection_OBSOLETE(RhinoObject rhObj)
        {
            if (rhObj.Geometry is Brep || rhObj.Geometry is Extrusion)
                rhinoDoc.Objects.Delete(rhObj.Id, true);
        }

        /// <summary>
        /// Duplicates a list of breps and translates each duplicate by a given vector.
        /// </summary>
        public static List<Brep> BrepBulkDuplicate(List<Brep> breps, Vector3d vec)
        {
            return breps.Select(brep =>
            {
                Brep dupBrep = brep.DuplicateBrep();
                dupBrep.Translate(vec);
                return dupBrep;
            }).ToList();
        }

        /// <summary>
        /// Produces planes at a sequence of contour heights (converted to accumulative), shifted by contourShift.
        /// </summary>
        public static List<Plane> GetAllPlanesFromContour(Point3d basePt, List<double> heights)
        {
            basePt.Z -= contourShift * UnitConversion;
            heights = ConvertIndependentToAccumulative(heights, true, false).ToList();
            return heights.Select(h => new Plane(new Point3d(basePt.X, basePt.Y, basePt.Z + h), Vector3d.ZAxis)).ToList();
        }

        /// <summary>
        /// Ensures a curve is planar to a reference plane (projects if not planar).
        /// </summary>
        public static Curve ReassureCurvePlanarity(Plane refPlane, Curve crv)
        {
            if (!crv.IsPlanar(abstol))
                crv = Curve.ProjectToPlane(crv, refPlane);
            return crv;
        }

        /// <summary>
        /// Creates object attributes that set object color explicitly.
        /// </summary>
        public static ObjectAttributes ColorAttributes(Color color)
        {
            return new ObjectAttributes
            {
                ObjectColor = color,
                ColorSource = ObjectColorSource.ColorFromObject
            };
        }

        /// <summary>
        /// Creates object attributes with a random object color.
        /// </summary>
        public static ObjectAttributes ColorAttributes()
        {
            Color color = Color.FromArgb(Random.Next(256), Random.Next(256), Random.Next(256));
            return ColorAttributes(color);
        }

        /// <summary>
        /// Checks whether two curves treated as line segments are parallel.
        /// </summary>
        public static bool TwoLineParallel(Curve c1, Curve c2)
        {
            Line l1 = new Line(c1.PointAtStart, c1.PointAtEnd);
            Line l2 = new Line(c2.PointAtStart, c2.PointAtEnd);
            return Vector3d.CrossProduct(l1.UnitTangent, l2.UnitTangent).IsZero;
        }

        /// <summary>
        /// Computes distance between two parallel infinite lines.
        /// </summary>
        /// <returns>True if parallel; otherwise false.</returns>
        public static bool ParallelLineDistance(Line l1, Line l2, out double distance)
        {
            if (!Vector3d.CrossProduct(l1.UnitTangent, l2.UnitTangent).IsZero)
            {
                distance = 0;
                return false;
            }
            Vector3d p2pVec = l1.From - l2.From;
            distance = Vector3d.CrossProduct(p2pVec, l1.Direction).Length / l1.Direction.Length;
            return true;
        }

        /// <summary>
        /// Projects a point onto an infinite line represented by a curve (using tangent at start).
        /// </summary>
        public static Point3d ProjectPointToLine(Curve line, Point3d pt)
        {
            Point3d lineStart = line.PointAtStart;
            Vector3d lineDir = line.TangentAtStart;
            lineDir.Unitize();
            Vector3d ptVec = pt - lineStart;
            double t = ptVec * lineDir;
            return lineStart + (lineDir * t);
        }

        /// <summary>
        /// Overload for <see cref="ParallelLineDistance(Line, Line, out double)"/> using curve endpoints.
        /// </summary>
        public static bool ParallelLineDistance(Curve c1, Curve c2, out double distance)
        {
            Line l1 = new Line(c1.PointAtStart, c1.PointAtEnd);
            Line l2 = new Line(c2.PointAtStart, c2.PointAtEnd);
            return ParallelLineDistance(l1, l2, out distance);
        }

        /// <summary>
        /// Finds the curve with minimal closest-point distance to a given point.
        /// </summary>
        public static Curve FindClosestCurve(IEnumerable<Curve> curves, Point3d point)
        {
            return curves.OrderBy(c =>
            {
                c.ClosestPoint(point, out double t);
                return point.DistanceTo(c.PointAt(t));
            }).First();
        }

        /// <summary>
        /// Duplicates curve segments and aligns each segment's direction to match the parent curve direction locally.
        /// </summary>
        public static List<Curve> ExplodeCurve(Curve crv)
        {
            List<Curve> segs = crv.DuplicateSegments().ToList();
            for (int i = 0; i < segs.Count; i++)
            {
                segs[i].NormalizedLengthParameter(0.5, out double st);
                Point3d pt = segs[i].PointAt(st);
                crv.ClosestPoint(pt, out double ct);
                if (Vector3d.VectorAngle(segs[i].TangentAt(st), crv.TangentAt(ct)) > Math.PI - angtol)
                    segs[i].Reverse();
            }
            return segs;
        }

        /// <summary>
        /// Checks whether two curves are connected by their ends within tolerance, and returns the connection type.
        /// </summary>
        /// <returns>True if connected; otherwise false.</returns>
        public static bool IsEndConnect(Curve crv0, Curve crv1, double tol, out int type)
        {
            type = -1;
            if (crv0.PointAtStart.DistanceTo(crv1.PointAtStart) < tol)
                type = 0;
            else if (crv0.PointAtStart.DistanceTo(crv1.PointAtEnd) < tol)
                type = 1;
            else if (crv0.PointAtEnd.DistanceTo(crv1.PointAtStart) < tol)
                type = 2;
            else if (crv0.PointAtEnd.DistanceTo(crv1.PointAtEnd) < tol)
                type = 3;
            else
                return false;
            return true;
        }

        /// <summary>
        /// Computes the connection angle between two end-connected curves.
        /// </summary>
        public static double CheckConnectionAngle(Curve crv0, Curve crv1)
        {
            if (!IsEndConnect(crv0, crv1, abstol, out int type))
                return double.NaN;
            switch (type)
            {
                case 0:
                    return Vector3d.VectorAngle(crv0.TangentAtStart, crv1.TangentAtStart);
                case 1:
                    return Vector3d.VectorAngle(crv0.TangentAtStart, -crv1.TangentAtEnd);
                case 2:
                    return Vector3d.VectorAngle(-crv0.TangentAtEnd, crv1.TangentAtStart);
                case 3:
                    return Vector3d.VectorAngle(-crv0.TangentAtEnd, -crv1.TangentAtEnd);
                default:
                    throw new ArgumentException("Unrecognized type.");
            }
        }

        /// <summary>
        /// Aligns the direction of the second curve to best match the first curve based on tangent similarity.
        /// </summary>
        public static void AlignCurveDirections(Curve crv0, ref Curve crv1, int count = 10)
        {
            Curve reversed = crv1.DuplicateCurve();
            reversed.Reverse();
            double intefScore0 = 0;
            double intefScore1 = 0;
            for (int i = 0; i < count; i++)
            {
                crv0.NormalizedLengthParameter((double)i / count, out double tb);
                Vector3d baseVec = crv0.TangentAt(tb);
                crv1.NormalizedLengthParameter((double)i / count, out double t0);
                Vector3d vec0 = crv1.TangentAt(t0);
                reversed.NormalizedLengthParameter((double)i / count, out double t1);
                Vector3d vec1 = reversed.TangentAt(t1);
                intefScore0 += Vector3d.VectorAngle(baseVec, vec0);
                intefScore1 += Vector3d.VectorAngle(baseVec, vec1);
            }
            crv1 = intefScore0 > intefScore1 ? reversed : crv1;
        }

        /// <summary>
        /// Determines which side the target curve lies on relative to a base curve (2D, Z assumed up).
        /// </summary>
        /// <returns>-1 right, +1 left, 0 mixed/unclear.</returns>
        public static int CurveOnSideOfBase(Curve baseCrv, Curve targetCrv, int samplingCount = 10)
        {
            double[] ts = baseCrv.DivideByCount(samplingCount + 1, false);
            int[] direcs = new int[samplingCount];
            for (int i = 0; i < samplingCount; i++)
            {
                Point3d basePt = baseCrv.PointAt(ts[i]);
                targetCrv.ClosestPoint(basePt, out double targetT);
                Vector3d targetVec = targetCrv.PointAt(targetT) - basePt;
                Vector3d tangVec = baseCrv.TangentAt(ts[i]);
                direcs[i] = Vector3d.CrossProduct(tangVec, targetVec)[2] > 0 ? 1 : -1;
            }
            if (direcs.All(d => d == direcs[0]))
                return direcs[0];
            else
                return 0;
        }

        /// <summary>
        /// Samples signed depths between two curves by emitting perpendicular rays from base to target.
        /// </summary>
        public static List<double> CollectDepths(Curve baseCrv, Curve targetCrv, int samplingCount = 10)
        {
            int side = CurveOnSideOfBase(baseCrv, targetCrv);
            if (side == 0)
                throw new ArgumentException("The target curve is (more or less) intersect with base curve.");
            double[] ts = baseCrv.DivideByCount(10, false);

            List<double> depths = new List<double>();
            foreach (double t in ts)
            {
                depths.Add(OnCollectDepth(baseCrv, targetCrv, t, side));
            }
            return depths;
        }

        /// <summary>
        /// Emits a perpendicular line from base curve at parameter t and measures distance to the target curve.
        /// </summary>
        public static double OnCollectDepth(Curve baseCrv, Curve targetCrv, double t, int side = 1)
        {
            AlignCurveDirections(baseCrv, ref targetCrv);
            Point3d point = baseCrv.PointAt(t);
            Line indefiniteLine = DirectionalEmitProjection(baseCrv, point, side);
            CurveIntersections intersections = Intersection.CurveCurve(indefiniteLine.ToNurbsCurve(), targetCrv, abstol, abstol);
            if (intersections.Count > 0)
                return intersections[0].PointB.DistanceTo(point);
            throw new ArgumentException("There should be one intersection.");
        }

        /// <summary>
        /// Creates a long line from a curve point in the left/right normal direction (in WorldXY).
        /// </summary>
        private static Line DirectionalEmitProjection(Curve crv, Point3d point, int side = 1, double distance = 0)
        {
            crv.ClosestPoint(point, out double t);
            Vector3d tang = crv.TangentAt(t);
            double ang = side * Math.PI / 2.0;
            tang.Rotate(ang, Vector3d.ZAxis);
            return new Line(point, tang, distance == 0 ? 1000 * UnitConversion : distance);
        }

        /// <summary>
        /// Polsby-Popper compactness score: (4πA) / P².
        /// </summary>
        public static double PolsbyPopperTest(Curve crv)
        {
            if (!CheckPlanarCurveValidity(crv, out string msg))
                ErrorHandler(msg, MessageBoxType.Error);
            double perimeter = crv.GetLength();
            double area = AreaMassProperties.Compute(crv).Area;
            return (4 * Math.PI * area) / (perimeter * perimeter);
        }

        /// <summary>
        /// Tests whether innie is regionally contained by outie using boolean intersection.
        /// </summary>
        public static bool CheckCurveRegionalContainment(Curve outie, Curve innie)
        {
            Curve[] interCrvs = Curve.CreateBooleanIntersection(outie, innie, abstol);
            if (interCrvs.Length != 1)
                return false;
            double interArea = AreaMassProperties.Compute(interCrvs[0]).Area;
            double innieArea = AreaMassProperties.Compute(innie).Area;
            if (Math.Abs(interArea - innieArea) < abstol)
                return true;
            return false;
        }

        /// <summary>
        /// Finds groups of duplicate points (within eps) by extracting proxy points from objects.
        /// </summary>
        public static List<List<Point3d>> DupObjectPointsFound<T>(List<T> objs, Func<T, Point3d> pointExtract, double eps = 1e-3)
        {
            List<Point3d> proxyPts = objs.ConvertAll(obj => pointExtract(obj));
            List<List<int>> indices = DupPointIndicesFind(proxyPts, eps);
            return indices.ConvertAll(idxgroup => idxgroup.ConvertAll(idx => proxyPts[idx]));
        }

        /// <summary>
        /// Finds groups of duplicate objects (within eps) by clustering extracted proxy points.
        /// </summary>
        public static List<List<T>> DupObjectsFind<T>(List<T> objs, Func<T, Point3d> pointExtract, double eps = 1e-3)
        {
            List<Point3d> proxyPts = objs.ConvertAll(obj => pointExtract(obj));
            List<List<int>> indices = DupPointIndicesFind(proxyPts, eps);
            return indices.ConvertAll(idxgroup => idxgroup.ConvertAll(idx => objs[idx]));
        }

        /// <summary>
        /// Finds groups of duplicate points (within eps).
        /// </summary>
        public static List<List<Point3d>> DupPointsFind(List<Point3d> points, double eps = 1e-3)
        {
            List<List<int>> indices = DupPointIndicesFind(points, eps);
            return indices.ConvertAll(idxgroup => idxgroup.ConvertAll(idx => points[idx]));
        }

        /// <summary>
        /// Finds index groups of points that lie within eps of each other using an RTree spatial search.
        /// </summary>
        public static List<List<int>> DupPointIndicesFind(List<Point3d> points, double eps = 1e-3)
        {
            var rtree = new RTree();
            for (int i = 0; i < points.Count; i++)
                rtree.Insert(points[i], i);

            var groups = new List<List<int>>();
            var visited = new HashSet<int>();

            for (int i = 0; i < points.Count; i++)
            {
                if (visited.Contains(i)) continue;

                var cluster = new List<int>();
                rtree.Search(new Sphere(points[i], eps), (s, a) =>
                {
                    if (!visited.Contains(a.Id))
                    {
                        visited.Add(a.Id);
                        cluster.Add(a.Id);
                    }
                });

                if (cluster.Count > 1)
                    groups.Add(cluster);
            }
            return groups;
        }

        /// <summary>
        /// Computes Euclidean distance from a point to the outside of a bounding box (0 if inside).
        /// </summary>
        public static double DistanceToBoundingBox(Point3d point, BoundingBox bb)
        {
            double xDist = point.X < bb.Min.X || point.X > bb.Max.X
                ? Math.Min(Math.Abs(bb.Max.X - point.X), Math.Abs(point.X - bb.Min.X))
                : 0;
            double yDist = point.Y < bb.Min.Y || point.Y > bb.Max.Y
                ? Math.Min(Math.Abs(bb.Max.Y - point.Y), Math.Abs(point.Y - bb.Min.Y))
                : 0;
            double zDist = point.Z < bb.Min.Z || point.Z > bb.Max.Z
                ? Math.Min(Math.Abs(bb.Max.Z - point.Z), Math.Abs(point.Z - bb.Min.Z))
                : 0;
            return Math.Sqrt(Math.Pow(xDist, 2) + Math.Pow(yDist, 2) + Math.Pow(zDist, 2));
        }
    }
}
