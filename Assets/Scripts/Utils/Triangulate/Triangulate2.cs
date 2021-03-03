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

using System.Collections.Generic;
using UnityEngine;

public class HalfEdge
{
	//The vertex the edge points to
	public Vertex v;

	//The face this edge is a part of
	public Triangle t;

	//The next edge
	public HalfEdge nextEdge;
	//The previous
	public HalfEdge prevEdge;
	//The edge going in the opposite direction
	public HalfEdge oppositeEdge;

	//This structure assumes we have a vertex class with a reference to a half edge going from that vertex
	//and a face (triangle) class with a reference to a half edge which is a part of this face 
	public HalfEdge(Vertex v)
	{
		this.v = v;
	}
}

public class Triangle
{
	//Corners
	public Vertex v1;
	public Vertex v2;
	public Vertex v3;

	//If we are using the half edge mesh structure, we just need one half edge
	public HalfEdge halfEdge;

	public Triangle(Vertex v1, Vertex v2, Vertex v3)
	{
		this.v1 = v1;
		this.v2 = v2;
		this.v3 = v3;
	}

	public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
	{
		this.v1 = new Vertex(v1);
		this.v2 = new Vertex(v2);
		this.v3 = new Vertex(v3);
	}

	public Triangle(HalfEdge halfEdge)
	{
		this.halfEdge = halfEdge;
	}

	//Change orientation of triangle from cw -> ccw or ccw -> cw
	public void ChangeOrientation()
	{
		Vertex temp = this.v1;

		this.v1 = this.v2;

		this.v2 = temp;
	}
}
public class Vertex
{
	public Vector3 position;

	//The outgoing halfedge (a halfedge that starts at this vertex)
	//Doesnt matter which edge we connect to it
	public HalfEdge halfEdge;

	//Which triangle is this vertex a part of?
	public Triangle triangle;

	//The previous and next vertex this vertex is attached to
	public Vertex prevVertex;
	public Vertex nextVertex;

	//Properties this vertex may have
	//Reflex is concave
	public bool isReflex;
	public bool isConvex;
	public bool isEar;

	public Vertex(Vector3 position)
	{
		this.position = position;
	}

	//Get 2d pos of this vertex
	public Vector2 GetPos2D_XY()
	{
		Vector2 pos_2d_xz = new Vector2(position.x, position.y);

		return pos_2d_xz;
	}
}

public static class Triangulator2
{
	public static int ClampListIndex(int index, int listSize)
	{
		index = ((index % listSize) + listSize) % listSize;

		return index;
	}

	//This assumes that we have a polygon and now we want to triangulate it
	//The points on the polygon should be ordered counter-clockwise
	//This alorithm is called ear clipping and it's O(n*n) Another common algorithm is dividing it into trapezoids and it's O(n log n)
	//One can maybe do it in O(n) time but no such version is known
	//Assumes we have at least 3 points
	public static List<Triangle> TriangulateConcavePolygon(List<Vector3> points)
	{
		//The list with triangles the method returns
		List<Triangle> triangles = new List<Triangle>();

		//If we just have three points, then we dont have to do all calculations
		if (points.Count == 3)
		{
			triangles.Add(new Triangle(points[0], points[1], points[2]));

			return triangles;
		}



		//Step 1. Store the vertices in a list and we also need to know the next and prev vertex
		List<Vertex> vertices = new List<Vertex>();

		for (int i = 0; i < points.Count; i++)
		{
			vertices.Add(new Vertex(points[i]));
		}

		//Find the next and previous vertex
		for (int i = 0; i < vertices.Count; i++)
		{
			int nextPos = ClampListIndex(i + 1, vertices.Count);

			int prevPos = ClampListIndex(i - 1, vertices.Count);

			vertices[i].prevVertex = vertices[prevPos];

			vertices[i].nextVertex = vertices[nextPos];
		}



		//Step 2. Find the reflex (concave) and convex vertices, and ear vertices
		for (int i = 0; i < vertices.Count; i++)
		{
			CheckIfReflexOrConvex(vertices[i]);
		}

		//Have to find the ears after we have found if the vertex is reflex or convex
		List<Vertex> earVertices = new List<Vertex>();

		for (int i = 0; i < vertices.Count; i++)
		{
			IsVertexEar(vertices[i], vertices, earVertices);
		}



		//Step 3. Triangulate!
		while (true)
		{
			//This means we have just one triangle left
			if (vertices.Count == 3)
			{
				//The final triangle
				triangles.Add(new Triangle(vertices[0], vertices[0].prevVertex, vertices[0].nextVertex));

				break;
			}

			//Make a triangle of the first ear
			Vertex earVertex = earVertices[0];

			Vertex earVertexPrev = earVertex.prevVertex;
			Vertex earVertexNext = earVertex.nextVertex;

			Triangle newTriangle = new Triangle(earVertex, earVertexPrev, earVertexNext);

			triangles.Add(newTriangle);

			//Remove the vertex from the lists
			earVertices.Remove(earVertex);

			vertices.Remove(earVertex);

			//Update the previous vertex and next vertex
			earVertexPrev.nextVertex = earVertexNext;
			earVertexNext.prevVertex = earVertexPrev;

			//...see if we have found a new ear by investigating the two vertices that was part of the ear
			CheckIfReflexOrConvex(earVertexPrev);
			CheckIfReflexOrConvex(earVertexNext);

			earVertices.Remove(earVertexPrev);
			earVertices.Remove(earVertexNext);

			IsVertexEar(earVertexPrev, vertices, earVertices);
			IsVertexEar(earVertexNext, vertices, earVertices);
		}

		//Debug.Log(triangles.Count);

		return triangles;
	}

	public static bool IsTriangleOrientedClockwise(Vector2 p1, Vector2 p2, Vector2 p3)
	{
		bool isClockWise = true;

		float determinant = p1.x * p2.y + p3.x * p1.y + p2.x * p3.y - p1.x * p3.y - p3.x * p2.y - p2.x * p1.y;

		if (determinant > 0f)
		{
			isClockWise = false;
		}

		return isClockWise;
	}
	//Check if a vertex if reflex or convex, and add to appropriate list
	private static void CheckIfReflexOrConvex(Vertex v)
	{
		v.isReflex = false;
		v.isConvex = false;

		//This is a reflex vertex if its triangle is oriented clockwise
		Vector3 a = v.prevVertex.position;
		Vector3 b = v.position;
		Vector3 c = v.nextVertex.position;
		Vector3 p = Vector3.Cross((b - a), (c - a));
		if(p.z > 0)
		{
			v.isReflex = true;
		}
		else
		{
			v.isConvex = true;
		}
		/*
		Vector2 a = v.prevVertex.GetPos2D_XY();
		Vector2 b = v.GetPos2D_XY();
		Vector2 c = v.nextVertex.GetPos2D_XY();

		if (IsTriangleOrientedClockwise(a, b, c))
		{
			v.isReflex = true;
		}
		else
		{
			v.isConvex = true;
		}
		*/
	}


	public static bool IsPointInTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p)
	{
		bool isWithinTriangle = false;

		//Based on Barycentric coordinates
		float denominator = ((p2.y - p3.y) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.y - p3.y));

		float a = ((p2.y - p3.y) * (p.x - p3.x) + (p3.x - p2.x) * (p.y - p3.y)) / denominator;
		float b = ((p3.y - p1.y) * (p.x - p3.x) + (p1.x - p3.x) * (p.y - p3.y)) / denominator;
		float c = 1 - a - b;

		//The point is within the triangle or on the border if 0 <= a <= 1 and 0 <= b <= 1 and 0 <= c <= 1
		//if (a >= 0f && a <= 1f && b >= 0f && b <= 1f && c >= 0f && c <= 1f)
		//{
		//    isWithinTriangle = true;
		//}

		//The point is within the triangle
		if (a > 0f && a < 1f && b > 0f && b < 1f && c > 0f && c < 1f)
		{
			isWithinTriangle = true;
		}

		return isWithinTriangle;
	}

	//Check if a vertex is an ear
	private static void IsVertexEar(Vertex v, List<Vertex> vertices, List<Vertex> earVertices)
	{
		//A reflex vertex cant be an ear!
		if (v.isReflex)
		{
			return;
		}

		//This triangle to check point in triangle
		Vector2 a = v.prevVertex.GetPos2D_XY();
		Vector2 b = v.GetPos2D_XY();
		Vector2 c = v.nextVertex.GetPos2D_XY();

		bool hasPointInside = false;

		for (int i = 0; i < vertices.Count; i++)
		{
			//We only need to check if a reflex vertex is inside of the triangle
			if (vertices[i].isReflex)
			{
				Vector2 p = vertices[i].GetPos2D_XY();

				//This means inside and not on the hull
				if (IsPointInTriangle(a, b, c, p))
				{
					hasPointInside = true;

					break;
				}
			}
		}

		if (!hasPointInside)
		{
			earVertices.Add(v);
		}
	}
}