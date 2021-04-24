// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SixLabors.ImageSharp.Drawing.Tests
{
    public class PolygonTests
    {
        public static TheoryData<TestPoint[], TestPoint, bool> PointInPolygonTheoryData =
            new TheoryData<TestPoint[], TestPoint, bool>
            {
                {
                    new TestPoint[] { new PointF(10, 10), new PointF(10, 100), new PointF(100, 100), new PointF(100, 10) },

                    // loc
                    new PointF(10, 10), // test
                    true
                }, // corner is inside
                {
                    new TestPoint[] { new PointF(10, 10), new PointF(10, 100), new PointF(100, 100), new PointF(100, 10) },

                    // loc
                    new PointF(10, 11), // test
                    true
                }, // on line
                {
                    new TestPoint[] { new PointF(10, 10), new PointF(10, 100), new PointF(100, 100), new PointF(100, 10) },

                    // loc
                    new PointF(9, 9), // test
                    false
                }, // corner is inside
            };

        [Theory]
        [MemberData(nameof(PointInPolygonTheoryData))]
        public void PointInPolygon(TestPoint[] controlPoints, TestPoint point, bool isInside)
        {
            var shape = new Polygon(new LinearLineSegment(controlPoints.Select(x => (PointF)x).ToArray()));
            Assert.Equal(isInside, shape.Contains(point));
        }

        public static TheoryData<TestPoint[], TestPoint, float> DistanceTheoryData =
           new TheoryData<TestPoint[], TestPoint, float>
           {
                {
                    new TestPoint[] { new PointF(10, 10), new PointF(10, 100), new PointF(100, 100), new PointF(100, 10) },
                    new PointF(10, 10),
                    0
                },
                {
                   new TestPoint[] { new PointF(10, 10), new PointF(10, 100), new PointF(100, 100), new PointF(100, 10) },
                   new PointF(10, 11), 0
                },
                {
                   new TestPoint[] { new PointF(10, 10), new PointF(10, 100), new PointF(100, 100), new PointF(100, 10) },
                   new PointF(11, 11), -1
                },
                {
                   new TestPoint[] { new PointF(10, 10), new PointF(10, 100), new PointF(100, 100), new PointF(100, 10) },
                   new PointF(9, 10), 1
                },
           };

        [Fact]
        public void AsSimpleLinearPath()
        {
            var poly = new Polygon(new LinearLineSegment(new PointF(0, 0), new PointF(0, 10), new PointF(5, 5)));
            IReadOnlyList<PointF> paths = poly.Flatten().First().Points.ToArray();
            Assert.Equal(3, paths.Count);
            Assert.Equal(new PointF(0, 0), paths[0]);
            Assert.Equal(new PointF(0, 10), paths[1]);
            Assert.Equal(new PointF(5, 5), paths[2]);
        }

        [Fact]
        public void FindIntersectionsBuffer()
        {
            var poly = new Polygon(new LinearLineSegment(new PointF(0, 0), new PointF(0, 10), new PointF(10, 10), new PointF(10, 0)));

            IEnumerable<PointF> intersections = poly.FindIntersections(new PointF(5, -5), new PointF(5, 15));

            Assert.Equal(2, intersections.Count());
            Assert.Contains(new PointF(5, 10), intersections);
            Assert.Contains(new PointF(5, 0), intersections);
        }

        [Fact]
        public void FindIntersectionsCollection()
        {
            var poly = new Polygon(new LinearLineSegment(new PointF(0, 0), new PointF(0, 10), new PointF(10, 10), new PointF(10, 0)));

            PointF[] buffer = poly.FindIntersections(new PointF(5, -5), new PointF(5, 15)).ToArray();
            Assert.Equal(2, buffer.Length);
            Assert.Contains(new PointF(5, 10), buffer);
            Assert.Contains(new PointF(5, 0), buffer);
        }

        [Fact]
        public void ReturnsWrapperOfSelfASOwnPath_SingleSegment()
        {
            var poly = new Polygon(new LinearLineSegment(new PointF(0, 0), new PointF(0, 10), new PointF(5, 5)));
            ISimplePath[] paths = poly.Flatten().ToArray();
            Assert.Single(paths);
            Assert.Equal(poly, paths[0]);
        }

        [Fact]
        public void ReturnsWrapperOfSelfASOwnPath_MultiSegment()
        {
            var poly = new Polygon(new LinearLineSegment(new PointF(0, 0), new PointF(0, 10)), new LinearLineSegment(new PointF(2, 5), new PointF(5, 5)));
            ISimplePath[] paths = poly.Flatten().ToArray();
            Assert.Single(paths);
            Assert.Equal(poly, paths[0]);
        }

        [Fact]
        public void Bounds()
        {
            var poly = new Polygon(new LinearLineSegment(new PointF(0, 0), new PointF(0, 10), new PointF(5, 5)));
            RectangleF bounds = poly.Bounds;
            Assert.Equal(0, bounds.Left);
            Assert.Equal(0, bounds.Top);
            Assert.Equal(5, bounds.Right);
            Assert.Equal(10, bounds.Bottom);
        }

        [Fact]
        public void MaxIntersections()
        {
            var poly = new Polygon(new LinearLineSegment(new PointF(0, 0), new PointF(0, 10)));

            // with linear polygons its the number of points the segments have
            Assert.Equal(2, poly.MaxIntersections);
        }

        [Fact]
        public void FindBothIntersections()
        {
            var poly = new Polygon(new LinearLineSegment(
                            new PointF(10, 10),
                            new PointF(200, 150),
                            new PointF(50, 300)));
            IEnumerable<PointF> intersections = poly.FindIntersections(new PointF(float.MinValue, 55), new PointF(float.MaxValue, 55));
            Assert.Equal(2, intersections.Count());
        }

        [Fact]
        public void HandleClippingInnerCorner()
        {
            var simplePath = new Polygon(new LinearLineSegment(
                             new PointF(10, 10),
                             new PointF(200, 150),
                             new PointF(50, 300)));

            var hole1 = new Polygon(new LinearLineSegment(
                            new PointF(37, 85),
                            new PointF(130, 40),
                            new PointF(65, 137)));

            ComplexPolygon poly = simplePath.Clip(hole1);

            int count = poly.CountIntersections(new PointF(float.MinValue, 137), new PointF(float.MaxValue, 137));

            // returns an even number of points
            Assert.Equal(4, count);
        }

        [Fact]
        public void CrossingCorner()
        {
            var simplePath = new Polygon(new LinearLineSegment(
                             new PointF(10, 10),
                             new PointF(200, 150),
                             new PointF(50, 300)));

            int count = simplePath.CountIntersections(new PointF(float.MinValue, 150), new PointF(float.MaxValue, 150));

            // returns an even number of points
            Assert.Equal(2, count);
        }

        [Fact]
        public void ClippingEdgefromInside()
        {
            ComplexPolygon simplePath = new RectangularPolygon(10, 10, 100, 100)
                .Clip(new RectangularPolygon(20, 0, 20, 20));

            int count = simplePath.CountIntersections(new PointF(float.MinValue, 20), new PointF(float.MaxValue, 20));

            // returns an even number of points
            Assert.Equal(4, count);
        }

        [Fact]
        public void ClippingEdgeFromOutside()
        {
            var simplePath = new Polygon(new LinearLineSegment(
                             new PointF(10, 10),
                             new PointF(100, 10),
                             new PointF(50, 300)));

            int count = simplePath.CountIntersections(new PointF(float.MinValue, 10), new PointF(float.MaxValue, 10));

            // returns an even number of points
            Assert.Equal(0, count % 2);
        }

        [Fact]
        public void HandleClippingOutterCorner()
        {
            var simplePath = new Polygon(new LinearLineSegment(
                             new PointF(10, 10),
                             new PointF(200, 150),
                             new PointF(50, 300)));

            var hole1 = new Polygon(new LinearLineSegment(
                            new PointF(37, 85),
                            new PointF(130, 40),
                            new PointF(65, 137)));

            ComplexPolygon poly = simplePath.Clip(hole1);

            int count = poly.CountIntersections(new PointF(float.MinValue, 300), new PointF(float.MaxValue, 300));

            // returns an even number of points
            Assert.Equal(2, count);
        }

        [Fact]
        public void MissingIntersection()
        {
            var simplePath = new Polygon(new LinearLineSegment(
                             new PointF(10, 10),
                             new PointF(200, 150),
                             new PointF(50, 300)));

            var hole1 = new Polygon(new LinearLineSegment(
                            new PointF(37, 85),
                            new PointF(130, 40),
                            new PointF(65, 137)));

            ComplexPolygon poly = simplePath.Clip(hole1);

            int count = poly.CountIntersections(new PointF(float.MinValue, 85), new PointF(float.MaxValue, 85));

            // returns an even number of points
            Assert.Equal(4, count);
        }

        [Theory]
        [InlineData(243)]
        [InlineData(341)]
        [InlineData(199)]
        public void BezierPolygonReturning2Points(int y)
        {
            // missing bands in test from ImageSharp
            PointF[] simplePath = new[]
            {
                        new PointF(10, 400),
                        new PointF(30, 10),
                        new PointF(240, 30),
                        new PointF(300, 400)
            };

            var poly = new Polygon(new CubicBezierLineSegment(simplePath));

            int count = poly.CountIntersections(new PointF(float.MinValue, y), new PointF(float.MaxValue, y));

            Assert.Equal(2, count);
        }
    }
}
