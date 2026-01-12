using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NatBase
{
    public static partial class NatUtil
    {
        /// <summary>
        /// Calculates the normalized proportion between two side lengths.
        /// </summary>
        /// <param name="side0"></param>
        /// <param name="side1"></param>
        /// <returns></returns>
        public static double CalNormalProportion(double side0, double side1)
        {
            if (side0 * side1 == 0)
                throw new ArgumentException("input cannot be 0");
            return Math.Min(Math.Abs(side0 / side1), Math.Abs(side1 / side0));
        }
        /// <summary>
        /// Construct Chains.
        /// </summary>
        /// <param name="rawItems"></param>
        /// <param name="getNext"></param>
        /// <param name="getPrev"></param>
        /// <returns></returns>
        public static List<List<T>> ConstructChains<T>(List<T> rawItems, Func<T, T> getNext, Func<T, T> getPrev)
        {
            List<List<T>> chains = new List<List<T>>();
            List<T> itemsToProcess = new List<T>(rawItems);
            while (itemsToProcess.Count > 0)
            {
                T currentItem = itemsToProcess[itemsToProcess.Count - 1];
                itemsToProcess.RemoveAt(itemsToProcess.Count - 1);
                List<T> chain = new List<T> { currentItem };

                T nextItem = getNext(currentItem);
                while (nextItem != null && itemsToProcess.Contains(nextItem))
                {
                    chain.Add(nextItem);
                    itemsToProcess.Remove(nextItem);
                    nextItem = getNext(nextItem);
                }
                T prevItem = getPrev(currentItem);
                while (prevItem != null && itemsToProcess.Contains(prevItem))
                {
                    chain.Insert(0, prevItem);
                    itemsToProcess.Remove(prevItem);
                    prevItem = getPrev(prevItem);
                }
                chains.Add(chain);
            }
            return chains;
        }
        /// <summary>
        /// Gets the digit at a given position from the right (1-based).
        /// </summary>
        /// <param name="value"></param>
        /// <param name="rightLoc"></param>
        /// <returns></returns>
        public static int NumberInDigit(long value, int rightLoc)
        {
            // fallback: 0
            return (int)(value % Math.Pow(10, rightLoc) / Math.Pow(10, rightLoc - 1));
        }
        /// <summary>
        /// Returns value + 1/value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double ReciprocalEquivalence(double value)
        {
            return value + (1 / value);
        }
        /// <summary>
        /// Gets the signed radian angle from the X axis to the given vector.
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static double GetRadAngleToXaxis(Vector3d vec)
        {
            return SignedVectorRadAngle(Vector3d.XAxis, vec);
        }
        /// <summary>
        /// Calculates the number of digits in an integer.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int CalNumOfDigit(int value)
        {
            return value == 0 ? 1 : (int)Math.Floor(Math.Log10(value)) + 1;
        }
        /// <summary>
        /// Calculates the unsigned radian angle from vector1 to vector2 in [0, 2π).
        /// </summary>
        /// <param name="vector1"></param>
        /// <param name="vector2"></param>
        /// <returns></returns>
        public static double VectorRadAngleIn2Pi(Vector3d vector1, Vector3d vector2)
        {
            double angle = Vector3d.VectorAngle(vector1, vector2);
            double crossProduct = Vector3d.CrossProduct(vector1, vector2).Z;
            if (crossProduct < 0)
                angle = 2 * Math.PI - angle;
            if (angle > Math.PI * 2 - angtol)
                angle = 0;
            return angle;
        }
        /// <summary>
        /// Calculates the signed radian angle from vector1 to vector2 using the Z-cross sign.
        /// </summary>
        /// <param name="vector1"></param>
        /// <param name="vector2"></param>
        /// <returns></returns>
        public static double SignedVectorRadAngle(Vector3d vector1, Vector3d vector2)
        {
            double angleTol = 1e-2;
            int sign = Math.Sign(Vector3d.CrossProduct(vector1, vector2).Z);
            double angle = Vector3d.VectorAngle(vector1, vector2);
            if (angle > Math.PI - angleTol) return Math.PI;
            return sign * angle;
        }
        /// <summary>
        /// Calculates an angle similarity score mirrored around π/2.
        /// </summary>
        /// <param name="vector1"></param>
        /// <param name="vector2"></param>
        /// <returns></returns>
        public static double VecRadAngleInHalfPi(Vector3d vector1, Vector3d vector2)
        {
            double angle = Vector3d.VectorAngle(vector1, vector2);
            return Math.PI / 2.0 - Math.Abs(angle - Math.PI / 2.0);
        }
        /// <summary>
        /// Computes the variance of a dataset (population or sample).
        /// </summary>
        /// <param name="data"></param>
        /// <param name="isSample"></param>
        /// <returns></returns>
        public static double Variance(List<double> data, bool isSample = false)
        {
            if (data == null || data.Count == 0)
                return 0;
            double mean = data.Average();
            double sumOfSquares = data.Sum(num => Math.Pow(num - mean, 2));
            int divisor = isSample ? data.Count - 1 : data.Count;
            return sumOfSquares / divisor;
        }
        /// <summary>
        /// Maps a ratio to the range (0, 1] by taking the reciprocal when > 1.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double RatioMapping(double value)
        {
            if (value < 0) throw new ArgumentException("value cannot small than 0.");
            return value > 1 ? 1 / value : value;
        }
        /// <summary>
        /// Maps a value from [fromMin, fromMax] to [0, 1], optionally clamping.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="fromMin"></param>
        /// <param name="fromMax"></param>
        /// <param name="clamp"></param>
        /// <returns></returns>
        public static double UnitizeMapping(double value, double fromMin, double fromMax, bool clamp = false)
        {
            double v = RangeMapping(value, fromMin, fromMax, 0, 1);
            if (clamp) return Clamp(v);
            return v;
        }
        /// <summary>
        /// Sorts key/value lists by key, preserving pairing.
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        public static void SortKeyValuePairs<T>(List<double> keys, List<T> values)
        {
            if (keys.Count != values.Count)
                throw new ArgumentException("The number of keys and the number of values are not the same.");
            List<KeyValuePair<double, T>> keyValuePairs = keys.Zip(values, (k, v) => new KeyValuePair<double, T>(k, v)).ToList();
            keyValuePairs.Sort((a, b) => a.Key.CompareTo(b.Key));
            for (int i = 0; i < keys.Count; i++)
            {
                keys[i] = keyValuePairs[i].Key;
                values[i] = keyValuePairs[i].Value;
            }
        }
        /// <summary>
        /// Groups a sorted list into levels by key differences and tolerance.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="keys"></param>
        /// <param name="tol"></param>
        /// <param name="bothDirection"></param>
        /// <returns></returns>
        public static List<List<T>> StratifySortedList<T>(List<T> values, List<double> keys, double tol, bool bothDirection = false)
        {
            return StratifyListByDouble(values, keys, tol, out _, bothDirection);
        }
        /// <summary>
        /// Groups items into levels based on adjacent key differences and tolerance.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="keys"></param>
        /// <param name="tol"></param>
        /// <param name="sortedKeys"></param>
        /// <param name="bothDirection"></param>
        /// <returns></returns>
        public static List<List<T>> StratifyListByDouble<T>(List<T> values, List<double> keys, double tol, out List<List<double>> sortedKeys, bool bothDirection = false)
        {
            if (keys.Count != values.Count)
                throw new ArgumentException("The number of values and the number of parentList items are not the same.");

            List<List<T>> leveledList = new List<List<T>>();
            sortedKeys = new List<List<double>>();
            List<T> currentLevel = new List<T>();
            List<double> currentKeys = new List<double>();
            for (int i = 1; i < keys.Count; i++)
            {
                currentLevel.Add(values[i - 1]);
                currentKeys.Add(keys[i - 1]);
                double diff = bothDirection ? Math.Abs(keys[i] - keys[i - 1]) : keys[i] - keys[i - 1];
                if (diff > tol)
                {
                    leveledList.Add(currentLevel);
                    sortedKeys.Add(currentKeys);
                    currentLevel = new List<T>();
                    currentKeys = new List<double>();
                }
            }
            currentLevel.Add(values.Last());
            leveledList.Add(currentLevel);
            currentKeys.Add(keys.Last());
            sortedKeys.Add(currentKeys);
            return leveledList;
        }
        /// <summary>
        /// Splits a list into groups based on boolean cut markers.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="keys"></param>
        /// <param name="loop"></param>
        /// <returns></returns>
        public static List<List<T>> StratifyListByBool<T>(List<T> values, List<bool> keys, bool loop = false)
        {
            if (values.Count == 0)
                return new List<List<T>>();
            else if (values.Count == 1)
                return new List<List<T>> { new List<T> { values[0] } };

            List<List<T>> leveledList = new List<List<T>>();
            List<T> currentList = new List<T>();

            for (int i = 0; i < values.Count - 1; i++)
            {
                currentList.Add(values[i]);
                if (keys[i])   // cutting satisfied
                {
                    leveledList.Add(currentList);
                    currentList = new List<T>();
                }
            }
            currentList.Add(values.Last());
            leveledList.Add(currentList);

            if (loop && !keys.Last())
            {
                leveledList[0].InsertRange(0, leveledList.Last());
                leveledList.RemoveAt(leveledList.Count - 1);
            }

            return leveledList;
        }
        /// <summary>
        /// Splits a list into groups based on boolean cut markers.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="ascendingCutReq"></param>
        /// <param name="loop"></param>
        /// <returns></returns>
        public static List<List<T>> StratifyListByBool<T>(List<T> values, Func<T, T, bool> ascendingCutReq, bool loop = false)
        {
            List<bool> keys = values.Select((v, i) => ascendingCutReq(v, values[(i + 1) % values.Count])).ToList();
            return StratifyListByBool(values, keys, loop);
        }
        /// <summary>
        /// Computes a sigmoid-like mapping.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static double Sigmoid(double x, double k = 3)
        {
            return 1 / (1 + Math.Pow(x, -k));
        }
        /// <summary>
        /// Pre-maps a value into the typical sigmoid input range.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="upperLimit"></param>
        /// <param name="lowerLimit"></param>
        /// <returns></returns>
        public static double SigmoidPrepMapping(double x, double upperLimit = 60, double lowerLimit = 0)
        {
            return RangeMapping(x, lowerLimit, upperLimit, -5, 5);
        }
        /// <summary>
        /// Range Mapping.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="fromMin"></param>
        /// <param name="fromMax"></param>
        /// <param name="toMin"></param>
        /// <param name="toMax"></param>
        /// <returns></returns>
        public static double RangeMapping(double value, double fromMin, double fromMax, double toMin, double toMax)
        {
            double fromRange = fromMax - fromMin;
            fromRange = fromRange == 0 ? 1 : fromRange;
            return toMin + (value - fromMin) * (toMax - toMin) / fromRange;
        }
        /// <summary>
        /// Converts a sequence of independent values into cumulative sums.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="ignoreZero"></param>
        /// <param name="ignoreEnd"></param>
        /// <returns></returns>
        public static IEnumerable<double> ConvertIndependentToAccumulative(IEnumerable<double> values, bool ignoreZero = false, bool ignoreEnd = false)
        {
            double sum = 0;
            List<double> accumu = values.Select(v => sum += v).ToList();
            if (ignoreEnd)
                accumu.RemoveAt(accumu.Count - 1);
            if (!ignoreZero)
                accumu.Insert(0, 0);
            return accumu;
        }
        /// <summary>
        /// Shuffles a list in-place using Fisher–Yates.
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static List<T> Shuffle<T>(List<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int k = rng.Next(i + 1);
                (list[i], list[k]) = (list[k], list[i]);
            }
            return list;
        }
        /// <summary>
        /// Right-shifts a list by a given number of positions (in-place).
        /// </summary>
        /// <param name="list"></param>
        /// <param name="positions"></param>
        /// <returns></returns>
        public static List<T> ShiftR<T>(List<T> list, int positions)
        {
            int count = list.Count;
            positions = positions % count;
            if (positions == 0)
                return list;
            List<T> temp = new List<T>(list.GetRange(count - positions, positions));
            list.RemoveRange(count - positions, positions);
            list.InsertRange(0, temp);
            return list;
        }
        /// <summary>
        /// Combines two sorted lists of doubles, merging near-duplicates within tolerance.
        /// </summary>
        /// <param name="l1"></param>
        /// <param name="l2"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static List<double> ListCombine(List<double> l1, List<double> l2, double tolerance)
        {
            List<double> combined = new List<double>(l1);
            foreach (double item in l2)
            {
                bool isWithinTolerance = combined.Any(x => Math.Abs(x - item) <= tolerance);
                if (!isWithinTolerance) combined.Add(item);
            }
            combined.Sort();
            return combined;
        }
        /// <summary>
        /// Clamps a value to a given range.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double Clamp(double value, double min = 0, double max = 1)
        {
            if (min > max) throw new ArgumentOutOfRangeException("min has to smaller than max!");
            return Math.Max(min, Math.Min(max, value));
        }
        /// <summary>
        /// Computes Euclidean distance between two equal-length tensors.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        public static double TensorDistance(double[] t1, double[] t2)
        {
            return Math.Sqrt(t1.Zip(t2, (a, b) => Math.Pow(a - b, 2)).Sum());
        }
        /// <summary>
        /// Computes a consistency score for vectors based on amplitude and angle variance.
        /// </summary>
        /// <param name="vectors"></param>
        /// <returns></returns>
        public static double VectorConsistancyScore(List<Vector3d> vectors)
        {
            List<double> amplitudes = vectors.Select(v => v.Length).ToList();
            List<double> angles = vectors.Select(v => SignedVectorRadAngle(v, Vector3d.XAxis)).ToList();
            return VectorConsistancyScore(amplitudes, angles);
        }
        /// <summary>
        /// Computes a consistency score for vectors based on amplitude and angle variance.
        /// </summary>
        /// <param name="amplitudes"></param>
        /// <param name="angles"></param>
        /// <returns></returns>
        public static double VectorConsistancyScore(List<double> amplitudes, List<double> angles)
        {
            if (amplitudes.Max() < 0.02) return 1;   // this prevents the disappearing amplitude tampering result
            double score = 1 / (Variance(angles) * 0.5 + Variance(amplitudes) * 0.5);
            return score;
        }
        /// <summary>
        /// Gets a range from a list, wrapping around the end if needed.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static List<T> GetCycledRange<T>(List<T> list, int start, int count)
        {
            if (count > list.Count) throw new ArgumentOutOfRangeException();
            List<T> firstPart = list.GetRange(start, Math.Min(count, list.Count - start));
            List<T> secondPart = list.GetRange(0, count - firstPart.Count);
            return firstPart.Concat(secondPart).ToList();
        }
        /// <summary>
        /// Gets a range from start to end indices, wrapping if end precedes start.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static List<T> GetCycledRangeFromIndices<T>(List<T> list, int start, int end)
        {
            if (start >= list.Count || end >= list.Count)
                throw new ArgumentOutOfRangeException();
            if (end >= start)
                return list.GetRange(start, end - start);
            List<T> firstPart = list.GetRange(start, list.Count - start);
            List<T> secondPart = list.GetRange(0, end);
            return firstPart.Concat(secondPart).ToList();
        }
        /// <summary>
        /// Splits the source list into two collections: a specific range of elements (treating the list as circular) 
        /// and the remaining elements that were not selected.
        /// </summary>
        /// <param name="remain"></param>
        /// <param name="cycled"></param>
        /// <returns></returns>
        public static (List<T> remain, List<T> taken) SplitCycledRange<T>(List<T> source, int startIndex, int count, out bool cycled)
        {
            cycled = false;
            int n = source.Count;
            if (count <= 0 || n == 0) return (new List<T>(source), new List<T>());

            cycled = startIndex + count > source.Count;
            var remain = new List<T>();
            var taken = new List<T>();

            for (int i = 0; i < count; i++)
            {
                taken.Add(source[(startIndex + i) % n]);
            }
            for (int i = 0; i < n; i++)
            {
                if (!taken.Contains(source[i]))
                {
                    remain.Add(source[i]);
                }
            }
            return (remain, taken);
        }
        /// <summary>
        /// Enumerates combinations (subsets) of a list.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="containItself"></param>
        /// <returns></returns>
        public static IEnumerable<List<T>> GetCombinations<T>(List<T> list, bool containItself)
        {
            int n = list.Count;
            int combinationsCount = (1 << n); // 2^n combinations
            int start = containItself ? 0 : 1;

            for (int i = 1; i < combinationsCount - start; i++) // Start from 1 to exclude the empty set
            {
                var combination = new List<T>();
                for (int j = 0; j < n; j++)
                {
                    if ((i & (1 << j)) != 0) // Check if the j-th bit is set
                    {
                        combination.Add(list[j]);
                    }
                }
                yield return combination;
            }
        }
        /// <summary>
        /// Generates all order permutations of a list.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static List<List<T>> GetOrderPermutations<T>(List<T> items)
        {
            var result = new List<List<T>>();
            void Permute(List<T> currentList, int startIndex)
            {
                if (startIndex == currentList.Count - 1)
                {
                    result.Add(new List<T>(currentList));
                    return;
                }
                for (int i = startIndex; i < currentList.Count; i++)
                {
                    Swap(currentList, startIndex, i);
                    Permute(currentList, startIndex + 1);
                    Swap(currentList, startIndex, i);
                }
            }
            void Swap(List<T> list, int index1, int index2)
            {
                var temp = list[index1];
                list[index1] = list[index2];
                list[index2] = temp;
            }
            Permute(items, 0);
            return result;
        }

        /// <summary>
        /// Selects a number of unique random items from a list.
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static List<T> RandomChoice<T>(List<T> objects, int count)
        {
            List<T> objectsCopy = new List<T>(objects);
            List<T> result = new List<T>();
            for (int i = 0; i < count; i++)
            {
                int index = Random.Next(objectsCopy.Count);
                result.Add(objectsCopy[index]);
                objectsCopy.RemoveAt(index);
            }
            return result;
        }
        /// <summary>
        /// Selects a single random item from a list.
        /// </summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        public static T RandomPick<T>(List<T> objects)
        {
            if (objects.Count == 0)
                throw new ArgumentNullException();
            List<T> objectsCopy = new List<T>(objects);
            int index = Random.Next(objectsCopy.Count);
            return objectsCopy[index];
        }
        /// <summary>
        /// Computes factorial for a non-negative integer.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static long Factorial(int x)
        {
            if (x < 0) throw new ArgumentException("Factorial is not defined for negative numbers.");
            if (x == 0 || x == 1) return 1; // Base case for 0! and 1!

            long result = 1;
            for (int i = 2; i <= x; i++)
            {
                result *= i;
            }
            return result;
        }
        /// <summary>
        /// Writes enumerable contents to Debug output.
        /// </summary>
        /// <param name="enumerables"></param>
        /// <param name="delegateFunc"></param>
        /// <param name="title"></param>
        /// <param name="inLine"></param>
        public static void PrintEnumerables<T>(IEnumerable<T> enumerables, Func<T, string> delegateFunc, string title = "", bool inLine = true)
        {
            if (title != null && title != "")
                Debug.Write($"{title}: \n");
            foreach (var item in enumerables)
            {
                if (inLine)
                    Debug.Write($"{delegateFunc(item)}, ");
                else
                    Debug.WriteLine($"{delegateFunc(item)}");
            }
            Debug.WriteLine("");
        }
        /// <summary>
        /// Writes enumerable contents to Debug output.
        /// </summary>
        /// <param name="enumerables"></param>
        /// <param name="title"></param>
        /// <param name="inLine"></param>
        public static void PrintEnumerables<T>(IEnumerable<T> enumerables, string title = "", bool inLine = true)
        {
            PrintEnumerables(enumerables, (item => item.ToString()), title, inLine);
        }
        /// <summary>
        /// Gets the subrange between two items (inclusive) based on their indices.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="item0"></param>
        /// <param name="item1"></param>
        /// <returns></returns>
        public static List<T> GetRangeInBetween<T>(List<T> list, T item0, T item1)
        {
            int idx0 = list.IndexOf(item0);
            int idx1 = list.IndexOf(item1);
            if (idx0 > idx1) throw new ArgumentException("index 0 must smaller than index 1.");
            return list.GetRange(idx0, idx1 - idx0 + 1);
        }
        /// <summary>
        /// Converts doubles into rank indices after sorting with tolerance grouping.
        /// </summary>
        /// <param name="doubles"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static List<int> ConvertDoubleListIntoSortIdx(List<double> doubles, double tol = 0.01)
        {
            var indexedValues = doubles
                .Select((value, index) => (Value: value, Index: index))
                .ToList();

            indexedValues.Sort((a, b) =>
            {
                if (Math.Abs(a.Value - b.Value) < tol) return 0;
                return a.Value.CompareTo(b.Value);
            });

            Dictionary<double, int> valueToIndex = new Dictionary<double, int>();
            int currentIndex = 0;

            foreach (var item in indexedValues)
            {
                if (!valueToIndex.ContainsKey(item.Value))
                {
                    valueToIndex[item.Value] = currentIndex++;
                }
            }
            return doubles.Select(value => valueToIndex[value]).ToList();
        }
        /// <summary>
        /// Get the valid partitions of a list through a validity check function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="validityCheck"></param>
        /// <returns></returns>
        public static List<List<List<T>>> GetValidPartitions<T>(List<T> list, Func<List<List<T>>, bool> validityCheck)
        {
            var allPartitions = GetAllPartitions(list);
            var validPartitions = new List<List<List<T>>>();

            foreach (var partition in allPartitions)
            {
                if (validityCheck(partition))
                {
                    validPartitions.Add(partition);
                }
            }

            return validPartitions;
        }
        /// <summary>
        /// Get all the partitions of a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static List<List<List<T>>> GetAllPartitions<T>(List<T> list)
        {
            var results = new List<List<List<T>>>();
            int n = list.Count;

            void PartitionHelper(int start, List<List<T>> currentPartition)
            {
                if (start == n)
                {
                    results.Add(currentPartition.Select(x => x.ToList()).ToList());
                    return;
                }

                for (int i = start; i < n; i++)
                {
                    var group = list.GetRange(start, i - start + 1);
                    currentPartition.Add(group);
                    PartitionHelper(i + 1, currentPartition);
                    currentPartition.RemoveAt(currentPartition.Count - 1);
                }
            }

            PartitionHelper(0, new List<List<T>>());
            return results;
        }
        /// <summary>
        /// Find the items that only appear once in the lists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lists"></param>
        /// <returns></returns>
        public static List<T> FindSoloPlayers<T>(List<List<T>> lists)
        {
            Dictionary<T, int> frequencies = new Dictionary<T, int>();
            foreach (var list in lists)
            {
                foreach (var item in list)
                {
                    if (item == null) continue;
                    if (!frequencies.ContainsKey(item))
                    {
                        frequencies[item] = 0;
                    }
                    frequencies[item]++;
                }
            }
            return frequencies.Where(kvp => kvp.Value == 1).Select(kvp => kvp.Key).ToList();
        }
        /// <summary>
        /// Uses a backtracking algorithm to find all valid ways to distribute items from two source packs into 
        /// capacity-constrained buckets, incorporating a marked default item and tolerance thresholds.
        /// </summary>
        /// <typeparam name="T">The type of items being packed.</typeparam>
        /// <param name="buckets">A list of maximum capacities for each available bucket.</param>
        /// <param name="pack1">The primary list of items that must be fully distributed.</param>
        /// <param name="pack2">The secondary list of items used to fill remaining bucket space.</param>
        /// <param name="tolRatio">The allowed waste ratio (0.0 to 1.0). If a bucket's remaining space 
        /// exceeds <c>capacity * tolRatio</c>, the solution is discarded.</param>
        /// <param name="markedItemSizes">Specific sizes of "marked" items to be pre-placed in buckets.</param>
        /// <param name="defaultObj">The placeholder object used for marked items.</param>
        /// <param name="getSize">A delegate function to retrieve the numerical size of an item of type <typeparamref name="T"/>.</param>
        /// <returns>
        /// A nested list structure: <c>List&lt;List&lt;List&lt;T&gt;&gt;&gt;</c>.
        /// The outer list contains all valid solutions; the middle list represents the buckets; 
        /// the inner list contains the items assigned to that specific bucket.
        /// </returns>
        public static List<List<List<T>>> FillBucketsWithMarkedItem<T>(
                List<double> buckets, List<T> pack1, List<T> pack2, double tolRatio,
                List<double> markedItemSizes, T defaultObj, Func<T, double> getSize)
        {
            var results = new List<List<List<T>>>();
            void Backtrack(List<double> currentBuckets, List<List<T>> currentBucketContents, List<T> remainingPack1, List<T> remainingPack2)
            {
                bool hasValidMove = false;
                for (int i = 0; i < currentBuckets.Count; i++)
                {
                    if ((remainingPack1.Count > 0 && currentBuckets[i] >= getSize(remainingPack1[0]) ||
                         remainingPack2.Count > 0 && currentBuckets[i] >= getSize(remainingPack2[0])))
                    {
                        hasValidMove = true;
                        break;
                    }
                }
                if (!hasValidMove)
                {
                    if (currentBuckets.Any(remain => remain > buckets[currentBuckets.IndexOf(remain)] * (1 - tolRatio))
                        || remainingPack1.Count > 0
                        || currentBucketContents.Any(c => c.Count == 0))
                        return;

                    results.Add(currentBucketContents.Select(bucket => new List<T>(bucket)).ToList());
                    return;
                }
                void GuideRemainingPack(List<T>[] packs, int idx)
                {
                    var nextItem = packs[idx][0];
                    double itemSize = getSize(nextItem);
                    List<T> pack01 = packs[0];
                    List<T> pack02 = packs[1];
                    if (idx == 0)
                        pack01 = packs[0].GetRange(1, packs[0].Count - 1);
                    else
                        pack02 = packs[1].GetRange(1, packs[1].Count - 1);

                    for (int i = 0; i < currentBuckets.Count; i++)
                    {
                        if (currentBuckets[i] >= itemSize)
                        {
                            currentBuckets[i] -= itemSize;
                            currentBucketContents[i].Add(nextItem); // Add the item to the bucket
                            Backtrack(
                                new List<double>(currentBuckets),
                                currentBucketContents.Select(bucket => new List<T>(bucket)).ToList(),
                                pack01, pack02
                            );
                            currentBucketContents[i].RemoveAt(currentBucketContents[i].Count - 1);
                            currentBuckets[i] += itemSize;
                        }
                    }
                }
                if (remainingPack1.Count > 0)
                    GuideRemainingPack(new List<T>[] { remainingPack1, remainingPack2 }, 0);
                if (remainingPack1.Count == 0 && remainingPack2.Count > 0)
                    GuideRemainingPack(new List<T>[] { remainingPack1, remainingPack2 }, 1);
            }

            for (int i = 0; i < buckets.Count; i++)
            {
                List<double> currentBuckets = new List<double>(buckets);
                var currentBucketContents = buckets.Select(_ => new List<T>()).ToList();
                if (markedItemSizes == null || markedItemSizes.Count == 0)
                {
                    Backtrack(currentBuckets, currentBucketContents, pack1, pack2);
                }
                else if (currentBuckets[i] >= markedItemSizes[i])
                {
                    currentBuckets[i] -= (markedItemSizes?[i] ?? 0);
                    currentBucketContents[i].Add(defaultObj);
                    Backtrack(currentBuckets, currentBucketContents, pack1, pack2);
                }
            }
            return results;
        }
        /// <summary>
        /// Parses a 6-digit hex RGB string into a Color.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static Color ConvertFromHex(string hex)
        {
            if (hex.Length == 6)
            {
                return Color.FromArgb(
                    255,
                    int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber),
                    int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber),
                    int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber)
                );
            }
            else throw new ArgumentException("Invalid hex.");
        }
        /// <summary>
        /// Removes every (realCharBlock + 1)th character from the input string.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="realCharBlock"></param>
        /// <returns></returns>
        internal static string StringStrip(string input, int realCharBlock)
        {
            var cleaned = new StringBuilder();
            for (int i = 0, count = 0; i < input.Length; i++)
            {
                if ((count % (realCharBlock + 1)) != realCharBlock)
                {
                    cleaned.Append(input[i]);
                }
                count++;
            }
            return cleaned.ToString();
        }
        /// <summary>
        /// Deterministically permutes characters by modular index mapping.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="multiplier"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static string JitterStringOrdered(string input, int multiplier = 3, int offset = 1)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            int n = input.Length;
            char[] jittered = new char[n];

            for (int i = 0; i < n; i++)
            {
                int newPos = (i * multiplier + offset) % n;
                jittered[newPos] = input[i];
            }
            return new string(jittered);
        }
        /// <summary>
        /// Deterministically permutes characters ensuring multiplier is coprime with length.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="multiplier"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static string JitterStringwGCD(string input, int multiplier = 3, int offset = 1)
        {
            if (string.IsNullOrEmpty(input))
                return "0";

            int n = input.Length;

            if (GCD(multiplier, n) != 1)
            {
                multiplier = Enumerable.Range(2, n - 1).First(m => GCD(m, n) == 1);
            }

            char[] jittered = new char[n];

            for (int i = 0; i < n; i++)
            {
                int newPos = (i * multiplier + offset) % n;
                jittered[newPos] = input[i];
            }

            return new string(jittered);
        }

        /// <summary>
        /// Computes the greatest common divisor (GCD) of two integers.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static int GCD(int a, int b)
        {
            while (b != 0)
            {
                int t = b;
                b = a % b;
                a = t;
            }
            return a;
        }
        /// <summary>
        /// Encrypts a string using AES-CBC and returns a Base64 payload with the IV prepended.
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string AEString(string plainText, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            return AEString(plainText, keyBytes);
        }
        /// <summary>
        /// Encrypts a string using AES-CBC and returns a Base64 payload with the IV prepended.
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string AEString(string plainText, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                aes.GenerateIV();
                byte[] iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
                using (var ms = new MemoryStream())
                {
                    ms.Write(iv, 0, iv.Length); // Prepend IV
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs, Encoding.UTF8))
                    {
                        sw.Write(plainText);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
    }
}
