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
	/// Worker thread for computing the gain.
	/// </summary>
	class GainWorker
	{
		/// <summary>
		/// The array of vertices.
		/// </summary>
		private Vertex[] vertices;

		/// <summary>
		/// The list of facilities.
		/// </summary>
		private List<Facility> facilities;

		/// <summary>
		/// The facility cost.
		/// </summary>
		private double facilityCost;

		/// <summary>
		/// Index of the first facility to inspect.
		/// </summary>
		private int startIndex;

		/// <summary>
		/// Index of the next facility after the last one to inspect.
		/// The computation will stop BEFORE this index.
		/// </summary>
		private int endIndexPlusOne;

		/// <summary>
		/// The partial gain computed by this worker.
		/// </summary>
		private double gain;

		/// <summary>
		/// The list of vertices to reassign.
		/// </summary>
		private List<int> vertsToReassign;

		/// <summary>
		/// The list of facilities to close.
		/// </summary>
		private List<Facility> facilsToClose;

		/// <summary>
		/// Event to signal that we are done with the computation.
		/// </summary>
		private System.Threading.AutoResetEvent doneEvent;





		/// <summary>
		/// Constructor.
		/// </summary>
		public GainWorker(Vertex[] vertices, List<Facility> facilities, double facilityCost)
		{
			// store the clustering information
			this.vertices = vertices;
			this.facilities = facilities;
			this.facilityCost = facilityCost;

			// initialize lists
			vertsToReassign = new List<int>();
			facilsToClose = new List<Facility>();

			// set up done event
			doneEvent = new System.Threading.AutoResetEvent(false);
		}

		/// <summary>
		/// Gets the event signaling the end of the computation.
		/// </summary>
		public System.Threading.AutoResetEvent DoneEvent
		{
			get { return doneEvent; }
		}

		/// <summary>
		/// Gets or sets the index of the first facility to inspect.
		/// </summary>
		public int StartIndex
		{
			get { return startIndex; }
			set { startIndex = value; }
		}

		/// <summary>
		/// Gets or sets the index of the next facility after the last one to inspect.
		/// The computation will stop BEFORE this index.
		/// </summary>
		public int EndIndexPlusOne
		{
			get { return endIndexPlusOne; }
			set { endIndexPlusOne = value; }
		}

		/// <summary>
		/// Gets the partial gain computed by this worker.
		/// </summary>
		public double PartialGain
		{
			get { return gain; }
		}

		/// <summary>
		/// Gets the list of vertices that should be reassigned.
		/// </summary>
		public List<int> VertsToReassign
		{
			get { return vertsToReassign; }
		}

		/// <summary>
		/// Gets the list of facilities that should be closed.
		/// </summary>
		public List<Facility> FacilsToClose
		{
			get { return facilsToClose; }
		}

		/// <summary>
		/// The method to compute the part of the gain.
		/// </summary>
		/// <param name="threadParameter">Index of the vertex for which to compute the gain.</param>
		public void ComputeGain(Object threadParameter)
		{
			// initialize
			gain = 0;
			vertsToReassign.Clear();
			facilsToClose.Clear();

			// get the facility candidate
			int candidateIndex = (int)threadParameter;
			// get the new facility candidate vertex
			Vertex candidate = vertices[candidateIndex];

			// inspect our part of the facilities
			for (int i = startIndex; i < endIndexPlusOne; i++)
			{
				// get the facility
				Facility f = facilities[i];

				// the non-weighted distance of the candidate to the facility
				double dist = candidate.NonWeightedDistance(vertices[f.VertexIndex]);

				// the vertex with the max distance can be at the closer side of the cluster --> 0.5*dist
				if (dist <= 2 * f.MaxNonWeightedDistance)
					// near enough; inspect all facility vertices
					CheckFacilityVertices(f, candidateIndex);
				else if (f.VertexIndex != candidateIndex)	// cannot close the candidate
					// too far; check possible closure
					CheckFacilityClosure(f, candidateIndex);
			}

			// done
			doneEvent.Set();
		}

		/// <summary>
		/// Checks whether some of the facility vertices could be reassigned to the new facility candidate.
		/// </summary>
		/// <param name="fac">The facility whose vertices to inspect.</param>
		/// <param name="candidateIndex">Index of the new facility candidate.</param>
		private void CheckFacilityVertices(Facility fac, int candidateIndex)
		{
			// reset the accumulator
			fac.ResetAccum();

			// get the new facility candidate vertex
			Vertex candidate = vertices[candidateIndex];

			// check all the facility vertices
			foreach (int vertexIndex in fac.VertexIndices)
			{
				// compute the weighted distance to the new facility candidate
				double distToCandidate = vertices[vertexIndex].WeightedDistance(candidate);

				// compute the difference
				double distGain = vertices[vertexIndex].WeightedDistToFac - distToCandidate;

				if (distGain > 0)
				{
					// the vertex is worth reassigning
					vertsToReassign.Add(vertexIndex);
					// update the gain
					gain += distGain;
				}
				else
					// better to keep the vertex where it is
					fac.AddToAccum(distGain);
			}

			// would it be beneficial to close the facility?
			if (fac.Accumulator + facilityCost > 0
				&& fac.VertexIndex != candidateIndex)	// cannot close the candidate
			{
				// mark the facility for closure
				facilsToClose.Add(fac);
				// update the gain
				gain += fac.Accumulator + facilityCost;
			}
		}

		/// <summary>
		/// Checks whether the facility could be closed.
		/// </summary>
		/// <param name="fac">The facility to check.</param>
		/// <param name="candidateIndex">Index of the new facility candidate.</param>
		private void CheckFacilityClosure(Facility fac, int candidateIndex)
		{
			// reset the accumulator
			fac.ResetAccum();

			// get the new facility candidate vertex
			Vertex candidate = vertices[candidateIndex];

			// check all the facility vertices
			foreach (int vertexIndex in fac.VertexIndices)
			{
				// compute the weighted distance to the new facility candidate
				double distToCandidate = vertices[vertexIndex].WeightedDistance(candidate);

				// compute the difference
				// should be always negative
				double distGain = vertices[vertexIndex].WeightedDistToFac - distToCandidate;

				// update the accumulator
				fac.AddToAccum(distGain);

				if (fac.Accumulator + facilityCost <= 0)
					// too bad, no chance the closure would be beneficial
					return;
			}

			// if we got here, it's OK to close the facility
			// (fac.Accumulator + facilityCost > 0)
			
			// mark the facility for closure
			facilsToClose.Add(fac);
			// update the gain
			gain += fac.Accumulator + facilityCost;
		}
	}
}
