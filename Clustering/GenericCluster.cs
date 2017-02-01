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
	/// Generic cluster. Contains cluster centre index and a list of indices of the cluster members.
	/// </summary>
	public class GenericCluster
	{
		/// <summary>
		/// Index of the cluster centre.
		/// </summary>
		protected readonly int centreIndex;

		/// <summary>
		/// List of indices of the cluster members.
		/// </summary>
		protected List<int> memberIndices;





		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="centreIndex">Index of the cluster centre.
		/// The centre is not automatically added to the list of members.</param>
		public GenericCluster(int centreIndex)
		{
			// store centre index
			this.centreIndex = centreIndex;

			// prepare list of members
			// the centre is not automatically added
			// in some cases it will be added along with other members so do not add it now
			memberIndices = new List<int>();
		}

		/// <summary>
		/// Gets the index of the centre.
		/// </summary>
		public int CentreIndex
		{
			get { return centreIndex; }
		}

		/// <summary>
		/// Gets a read-only list of indices of the cluster members.
		/// </summary>
		public IList<int> MemberIndices
		{
			get { return memberIndices.AsReadOnly(); }
		}

		/// <summary>
		/// Adds a vertex to the cluster.
		/// </summary>
		/// <param name="vertexIndex">Index of the vertex to add.</param>
		public void AddVertex(int vertexIndex)
		{
			memberIndices.Add(vertexIndex);
		}
	}
}
