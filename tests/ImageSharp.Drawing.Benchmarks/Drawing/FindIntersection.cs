// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace SixLabors.ImageSharp.Drawing.Benchmarks.Drawing
{
    public class FindIntersection
    {
        [Benchmark(Baseline = true)]
        public Vector2 FindIntersectionClass()
        {
            var tl = new Vector2(0, 0);
            var br = new Vector2(2500, 2000);

            var tr = new Vector2(2500, 0);
            var bl = new Vector2(0, 2000);

            var tlbr = new Segment(tl, br);
            var trbl = new Segment(tr, bl);

            return InternalPath.FindIntersection(in tlbr, in trbl);
        }

        [Benchmark]
        public Vector2 FindIntersectionStruct()
        {
            var tl = new Vector2(0, 0);
            var br = new Vector2(2500, 2000);

            var tr = new Vector2(2500, 0);
            var bl = new Vector2(0, 2000);

            var tlbr = new Segment(tl, br);
            var trbl = new Segment(tr, bl);

            return tlbr.FindIntersection(in trbl);
        }
    }

    public readonly struct Segment
    {
        private static readonly Vector2 MaxVector = new Vector2(float.MaxValue);
        private const float Epsilon = 0.003f;
        private const float Epsilon2 = 0.2f;
        public readonly Vector2 Start;
        public readonly Vector2 End;
        public readonly Vector2 Min;
        public readonly Vector2 Max;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Segment(Vector2 start, Vector2 end)
        {
            this.Start = start;
            this.End = end;

            this.Min = Vector2.Min(start, end);
            this.Max = Vector2.Max(start, end);
        }

        public Vector2 Vector() => this.End - this.Start;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 FindIntersection(in Segment target)
        {
            Vector2 v1 = this.End - this.Start;
            Vector2 v2 = target.End - target.Start;
            Vector2 v3 = this.Start - target.Start;

            Vector2 c1 = CrossProduct(v1, v2);
            Vector2 c2 = CrossProduct(v2, v3);

            if (NearEqual(c1, Vector2.Zero))
            {
                // DirectX Maths does a separate near-equal check
                // if (NearEqual(C2,  Vector2.Zero))
                // returning two separate error results
                // Coincident : Infinity
                // Parallel : NaN
                return MaxVector;
            }

            // Intersection point = start + v1 * (c2 / c1)
            Vector2 point = this.Start + (v1 * (c2 / c1));

            // TODO: Why are intersections falling out of bounds?
            // DirectX Maths does not need to do this.
            if (IsOnSegments(in this, in target, ref point))
            {
                return point;
            }

            return MaxVector;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 FindIntersection2(in Segment target)
        {
            Vector2 line1Start = this.Start;
            Vector2 line1End = this.End;
            Vector2 line2Start = target.Start;
            Vector2 line2End = target.End;

            // Use double precision for the intermediate calculations, because single precision calculations
            // easily gets over the Epsilon2 threshold for bitmap sizes larger than about 1500.
            // This is still symptom fighting though, and probably the intersection finding algorithm
            // should be looked over in the future (making the segments fat using epsilons doesn't truely fix the
            // robustness problem).
            // Future potential improvement: the precision problem will be reduced if the center of the bitmap is used as origin (0, 0),
            // this will keep coordinates smaller and relatively precision will be larger.
            double x1, y1, x2, y2, x3, y3, x4, y4;
            x1 = line1Start.X;
            y1 = line1Start.Y;
            x2 = line1End.X;
            y2 = line1End.Y;

            x3 = line2Start.X;
            y3 = line2Start.Y;
            x4 = line2End.X;
            y4 = line2End.Y;

            double x12 = x1 - x2;
            double y12 = y1 - y2;
            double x34 = x3 - x4;
            double y34 = y3 - y4;

            double det = (x12 * y34) - (y12 * x34);
            if (det > -Epsilon && det < Epsilon)
            {
                return MaxVector;
            }

            double u = (x1 * y2) - (x2 * y1);
            double v = (x3 * y4) - (x4 * y3);
            double x = ((x34 * u) - (x12 * v)) * 1F / det;
            double y = ((y34 * u) - (y12 * v)) * 1F / det;

            var point = new Vector2((float)x, (float)y);

            if (IsOnSegments(in this, in target, ref point))
            {
                return point;
            }

            return MaxVector;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOnSegments(in Segment seg1, in Segment seg2, ref Vector2 q)
        {
            float t = q.X - Epsilon2;
            if (t > seg1.Max.X || t > seg2.Max.X)
            {
                return false;
            }

            t = q.X + Epsilon2;
            if (t < seg1.Min.X || t < seg2.Min.X)
            {
                return false;
            }

            t = q.Y - Epsilon2;
            if (t > seg1.Max.Y || t > seg2.Max.Y)
            {
                return false;
            }

            t = q.Y + Epsilon2;
            if (t < seg1.Min.Y || t < seg2.Min.Y)
            {
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        private static Vector2 CrossProduct(Vector2 v1, Vector2 v2)
        {
            return new Vector2((v1.X * v2.Y) - (v1.Y * v2.X), (v1.X * v2.Y) - (v1.Y * v2.X));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NearEqual(Vector2 v1, Vector2 v2)
        {
            const float epsilon = 1.192092896e-7F;
            var dxy = Vector2.Abs(v1 - v2);
            return (dxy.X <= epsilon) && (dxy.Y <= epsilon);
        }
    }

    public class InternalPath
    {
        private static readonly Vector2 MaxVector = new Vector2(float.MaxValue);
        private const float Epsilon = 0.003f;
        private const float Epsilon2 = 0.2f;

        public static Vector2 FindIntersection(in Segment source, in Segment target)
        {
            Vector2 line1Start = source.Start;
            Vector2 line1End = source.End;
            Vector2 line2Start = target.Start;
            Vector2 line2End = target.End;

            // Use double precision for the intermediate calculations, because single precision calculations
            // easily gets over the Epsilon2 threshold for bitmap sizes larger than about 1500.
            // This is still symptom fighting though, and probably the intersection finding algorithm
            // should be looked over in the future (making the segments fat using epsilons doesn't truely fix the
            // robustness problem).
            // Future potential improvement: the precision problem will be reduced if the center of the bitmap is used as origin (0, 0),
            // this will keep coordinates smaller and relatively precision will be larger.
            double x1, y1, x2, y2, x3, y3, x4, y4;
            x1 = line1Start.X;
            y1 = line1Start.Y;
            x2 = line1End.X;
            y2 = line1End.Y;

            x3 = line2Start.X;
            y3 = line2Start.Y;
            x4 = line2End.X;
            y4 = line2End.Y;

            double x12 = x1 - x2;
            double y12 = y1 - y2;
            double x34 = x3 - x4;
            double y34 = y3 - y4;

            double det = (x12 * y34) - (y12 * x34);
            if (det > -Epsilon && det < Epsilon)
            {
                return MaxVector;
            }

            double u = (x1 * y2) - (x2 * y1);
            double v = (x3 * y4) - (x4 * y3);
            double x = ((x34 * u) - (x12 * v)) * 1F / det;
            double y = ((y34 * u) - (y12 * v)) * 1F / det;

            var point = new Vector2((float)x, (float)y);

            if (IsOnSegments(source, target, point))
            {
                return point;
            }

            return MaxVector;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOnSegments(in Segment seg1, in Segment seg2, Vector2 q)
        {
            float t = q.X - Epsilon2;
            if (t > seg1.Max.X || t > seg2.Max.X)
            {
                return false;
            }

            t = q.X + Epsilon2;
            if (t < seg1.Min.X || t < seg2.Min.X)
            {
                return false;
            }

            t = q.Y - Epsilon2;
            if (t > seg1.Max.Y || t > seg2.Max.Y)
            {
                return false;
            }

            t = q.Y + Epsilon2;
            if (t < seg1.Min.Y || t < seg2.Min.Y)
            {
                return false;
            }

            return true;
        }
    }

    // BenchmarkDotNet=v0.12.0, OS=Windows 10.0.19041
    // Intel Core i7-8650U CPU 1.90GHz(Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
    // .NET Core SDK = 3.1.301
    //
    // [Host]     : .NET Core 3.1.5 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.27001), X64 RyuJIT
    // DefaultJob : .NET Core 3.1.5 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.27001), X64 RyuJIT
    //
    // |                 Method |     Mean |    Error |   StdDev | Ratio | RatioSD |
    // |----------------------- |---------:|---------:|---------:|------:|--------:|
    // |  FindIntersectionClass | 24.50 ns | 0.414 ns | 0.387 ns |  1.00 |    0.00 |
    // | FindIntersectionStruct | 18.44 ns | 0.217 ns | 0.192 ns |  0.75 |    0.02 |
}
