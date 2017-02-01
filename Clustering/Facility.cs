/***
A new algorithm for spatial clustering of the line segments 
as a tool for lineament extraction.

Ondrej Kaas, 2016
Faculty of Applied Sciences,University of West Bohemia, Pilsen, Czech Republic     
======================================================= */
using System;
using System.Collections.Generic;

namespace Zcu.Graphics.Clustering
{
	/// <summary>
	/// Facility is a cluster centre.
	/// Each facility is also a vertex.
	/// </summary>
	public class Facility
	{
		/// <summary>
		/// List of indices of vertices assigned to this facility.
		/// </summary>
		private List<int> vertices;

		/// <summary>
		/// Index of corresponding vertex.
		/// This index is not to local <code>vertices</code> array.
		/// It is index to some external array outside this class.
		/// </summary>
		/// <remarks>Each facility is also a vertex.</remarks>
		private readonly int vertexIndex;

		/// <summary>
		/// Accumulator for cost of closing this facility.
		/// That is the cost for reassigning all vertices somewhere else.
		/// </summary>
		private double accumulator;

		/// <summary>
		/// Determines whether this facility has been marked.
		/// Could be used for anything. This is for identification
		/// of clusters containing some sample points.
		/// </summary>
		private bool marked = false;

		/// <summary>
		/// The maximal non-weighted distance of any vertex assigned to this facility.
		/// </summary>
		private double maxNonWeightedDistance;

		/// <summary>
		/// Index of the vertex having the maximal distance from this facility.
		/// </summary>
		private int maxDistVertexIndex;

        /// <summary>
        /// Index v poli shluku
        /// </summary>
        public int FacilityIndex;

       // public double[] ;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="vertex">Index of corresponding vertex.</param>
		public Facility(int vertexIndex)
		{
			this.vertexIndex = vertexIndex;

			vertices = new List<int>();
		}

		/// <summary>
		/// Gets the vertex index.
		/// </summary>
		public int VertexIndex
		{
			get { return vertexIndex; }
		}

		/// <summary>
		/// Gets or sets whether this facility is marked.
		/// </summary>
		public bool Marked
		{
			get { return marked; }
			set { marked = value; }
		}

		/// <summary>
		/// Gets the maximal non-weighted distance of all assigned vertices.
		/// </summary>
		public double MaxNonWeightedDistance
		{
			get { return maxNonWeightedDistance; }
		}

		/// <summary>
		/// Adds given vertex to facility.
		/// </summary>
		/// <param name="vertexToAdd">Index of vertex to add.</param>
		/// <param name="nonWeightedDistance">The non-weighted distance of the vertex being added.</param>
		public void AddVertex(int vertexToAdd, double nonWeightedDistance)
		{
			// add the vertex
			vertices.Add(vertexToAdd);

			// check the distance
			if (nonWeightedDistance > maxNonWeightedDistance)
			{
				// the vertex is farther than the current maximum
				maxNonWeightedDistance = nonWeightedDistance;
				maxDistVertexIndex = vertexToAdd;
			}
		}

		/// <summary>
		/// Removes given vertex from facility.
		/// </summary>
		/// <param name="vertexToRemove">Index of vertex to remove.</param>
		/// <param name="allVertices">Array of all the vertices
		/// (necessary for updating the maximal distance).</param>
		public void RemoveVertex(int vertexToRemove, Vertex[] allVertices)
		{
			// remove the vertex
			vertices.Remove(vertexToRemove);

			// is the vertex with the maximal distance being removed?
			if (vertexToRemove == maxDistVertexIndex)
				// yes, must find the new maximum
				FindNewMaximum(allVertices);
		}

        /// <summary>
        /// Odstrani vsechny body od daneho centra
        /// </summary>
        /// <param name="vertices"></param>
        public void RemoveVertex( List<int> ver )
        {
            for (int i = 0; i < ver.Count; i++)
            {
                foreach( int vi in vertices )
                {
                    if( i == vi )
                    {
                        vertices.Remove( vi );
                    }
                }
            }
        }

        /// <summary>
        /// Removes given vertex from facility.
        /// </summary>
        /// <param name="vertexToRemove">Index of vertex to remove.</param>
        public void RemoveVertex(int vertexToRemove )
        {
            // remove the vertex
            vertices.Remove(vertexToRemove);
        }

		/// <summary>
		/// Finds the new maximal weighted distance.
		/// </summary>
		/// <param name="allVertices">The array of all the vertices involved in the clustering.</param>
		private void FindNewMaximum(Vertex[] allVertices)
		{
			maxNonWeightedDistance = 0;

			// find the new maximum
			foreach (int i in vertices)	// looking at THIS.vertices
			{
				// get the distance
				double dist = allVertices[i].NonWeightedDistToFac;

				// check the distance
				if (dist > maxNonWeightedDistance)
				{
					// new maximum found
					maxNonWeightedDistance = dist;
					maxDistVertexIndex = i;
				}
			}
		}

		/// <summary>
		/// Property to get accumulator value.
		/// </summary>
		public double Accumulator
		{
			get { return accumulator; }
		}

		/// <summary>
		/// Adds a value to accumulator.
		/// </summary>
		/// <param name="value">Value to add.</param>
		public void AddToAccum(double value)
		{
			accumulator += value;
		}

		/// <summary>
		/// Resets the accumulator.
		/// </summary>
		public void ResetAccum()
		{
			accumulator = 0;
		}

		/// <summary>
		/// Gets a read-only list of vertices assigned to this facility.
		/// </summary>
		public List<int> VertexIndices
		{
			get { return vertices; }
		}
	}
}
