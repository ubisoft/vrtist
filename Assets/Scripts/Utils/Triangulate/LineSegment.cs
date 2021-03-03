/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using UnityEngine;

namespace Triangulator
{
	struct LineSegment
	{
		public Vertex A;
		public Vertex B;

		public LineSegment(Vertex a, Vertex b)
		{
			A = a;
			B = b;
		}

		public float? IntersectsWithRay(Vector2 origin, Vector2 direction)
		{
			float largestDistance = Mathf.Max(A.Position.x - origin.x, B.Position.x - origin.x) * 2f;
			LineSegment raySegment = new LineSegment(new Vertex(origin, 0), new Vertex(origin + (direction * largestDistance), 0));

			Vector2? intersection = FindIntersection(this, raySegment);
			float? value = null;

			if (intersection != null)
				value = Vector2.Distance(origin, intersection.Value);

			return value;
		}

		public static Vector2? FindIntersection(LineSegment a, LineSegment b)
		{
			float x1 = a.A.Position.x;
			float y1 = a.A.Position.y;
			float x2 = a.B.Position.x;
			float y2 = a.B.Position.y;
			float x3 = b.A.Position.x;
			float y3 = b.A.Position.y;
			float x4 = b.B.Position.x;
			float y4 = b.B.Position.y;

			float denom = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);

			float uaNum = (x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3);
			float ubNum = (x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3);

			float ua = uaNum / denom;
			float ub = ubNum / denom;

			if (Mathf.Clamp(ua, 0f, 1f) != ua || Mathf.Clamp(ub, 0f, 1f) != ub)
				return null;

			return a.A.Position + (a.B.Position - a.A.Position) * ua;
		}
	}
}