/***
A new algorithm for spatial clustering of the line segments 
as a tool for lineament extraction.

Ondrej Kaas, 2016
Faculty of Applied Sciences,University of West Bohemia, Pilsen, Czech Republic     
======================================================= */
#define RUN_PARALLEL
using System;
using System.Collections.Generic;

namespace Zcu.Graphics.Clustering
{
    /// <summary>
    /// Computes clustering as a facility location.
    /// </summary>
    public class FacilityLocation
    {
        /// <summary>
        /// Array of vertices (clients to service).
        /// </summary>
        private Vertex[] vertices = null;

        /// <summary>
        /// Bounding box of vertices.
        /// </summary>
        private BoundingBox boundingBox;

        /// <summary>
        /// List of open facilities.
        /// </summary>
        private List<Facility> facilities = null;

        /// <summary>
        /// Facility cost multiplier.
        /// </summary>
        private double facCostMult = 1;

        /// <summary>
        /// Facility cost.
        /// </summary>
        private double facilityCost;

        /// <summary>
        /// Number of iterations to do when computing the clustering.
        /// Value of zero defaults to 0.1 N iterations.
        /// </summary>
        private int iterations = 0;

        /// <summary>
        /// List of indices of vertices that should be reassigned.
        /// </summary>
        private List<int> vertsToReassign;

        /// <summary>
        /// List of facilities that should be closed.
        /// </summary>
        private List<Facility> facilsToClose;

#if RUN_PARALLEL
        /// <summary>
        /// Worker threads for parallel gain computation.
        /// </summary>
        private GainWorker[] workers;

        /// <summary>
        /// The number of the worker threads.
        /// </summary>
        private int threadCount;
#endif





        /// <summary>
        /// Random number generator.
        /// </summary>
        private static Random rnd;

        /// <summary>
        /// Static constructor. Initializes the random number generator.
        /// </summary>
        static FacilityLocation()
        {
#if DEBUG
            rnd = new Random(1234);
#else
            // CHANGE
            rnd = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
#endif
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        public FacilityLocation()
        {
#if RUN_PARALLEL
            // determine the number of processors
            threadCount = Environment.ProcessorCount;
            // prepare workers
            workers = new GainWorker[threadCount];
#endif
        }

        public List<Facility> Facilities
        {
            get { return facilities; }
        }

        /// <summary>
        /// Gets or sets the facility cost multiplier.
        /// </summary>
        public double FacilityCostMultiplier
        {
            get { return facCostMult; }
            set { facCostMult = value; }
        }

        /// <summary>
        /// Gets or sets the number of iterations.
        /// Zero value means the default 0.1 N iterations for computing a new clustering
        /// and 0.01 N iterations for updating an existing clustering.
        /// </summary>
        public int Iterations
        {
            get { return iterations; }
            set { iterations = value; }
        }

        /// <summary>
        /// Gets the number of clusters.
        /// </summary>
        public int NumberOfClusters
        {
            get { return facilities == null ? 0 : facilities.Count; }
        }

        /// <summary>
        /// Gets the cost for opening all the facilities that are currently in the solution.
        /// </summary>
        public double CostForFacilities
        {
            get { return facCostMult * facilities.Count; }
        }

        /// <summary>
        /// Gets the cost for connecting all the points to their facilities.
        /// </summary>
        public double CostForConnections
        {
            get
            {
                double cost = 0;

                // sum up all the distances
                for (int i = 0; i < vertices.Length; i++)
                    cost += vertices[i].WeightedDistToFac;

                return cost;
            }
        }

        /// <summary>
        /// Gets indices of vertices belonging to a specified cluster.
        /// </summary>
        /// <param name="facilityIndex">Index of the facility (cluster).</param>
        /// <returns>Returns an array of vertex indices.</returns>
        public int[] GetClusterVertices(int facilityIndex)
        {
            // facilityIndex points into the vertices array!

            //if (facilities == null || facilityIndex < 0 || facilityIndex >= facilities.Count)
            //	// no such facility
            //	throw new ApplicationException("No such facility.");
            // will throw IndexOutOfBoundsException

            // get the facility
            Facility f = vertices[facilityIndex].Facility;
            // copy vertex indices
            int[] result = new int[f.VertexIndices.Count];
            f.VertexIndices.CopyTo(result, 0);
            return result;
        }

        /// <summary>
        /// Gets indices of all facilities (cluster centres).
        /// </summary>
        /// <returns>Returns an array of facility indices.</returns>
        public int[] GetAllFacilities()
        {
            // returned indices point into the vertices array!

            int count;
            // count the number of facilities
            if (facilities == null)
                count = 0;
            else
                count = facilities.Count;

            // prepare array for output
            int[] output = new int[count];

            // loop through all facilities
            for (int i = 0; i < count; i++)
                // store the vertex index
                output[i] = facilities[i].VertexIndex;

            return output;
        }

        /// <summary>
        /// Gets weighted radii of all clusters.
        /// </summary>
        /// <returns>Returns an array of cluster radii.</returns>
        public double[] GetAllClusterRadii()
        {
            int count;
            // count the number of facilities
            if (facilities == null)
                count = 0;
            else
                count = facilities.Count;

            // prepare array for output
            double[] output = new double[count];

            // loop through all facilities
            for (int i = 0; i < count; i++)
            {
                double radius = 0;

                // check all cluster members
                foreach (int vIndex in facilities[i].VertexIndices)
                {
                    if (vertices[vIndex].WeightedDistToFac > radius)
                        radius = vertices[vIndex].WeightedDistToFac;
                }

                // store the cluster radius
                output[i] = radius;
            }

            return output;
        }

        /// <summary>
        /// Updates vertex coordinates without destroying the current clustering.
        /// The clustering is then quickly recomputed to reflect actual vertex positions.
        /// </summary>
        /// <param name="newVertices">Array of updated vertices.</param>
        /// <returns>Returns the length of computation in milliseconds.</returns>
        public int UpdateVertices(Vertex[] newVertices)
        {
            // check that the number of vertices did not change
            if (this.vertices.Length != newVertices.Length)
                throw new ApplicationException("Update of vertices not possible. The number of vertices has changed.");

            // check that some clustering has been computed before
            if (facilities == null)
                // no, compute a new clustering
                return ComputeClustering(newVertices);

            // update vertex coordinates
            this.vertices = newVertices;

            // update the bounding box
            boundingBox.Initialize(this.vertices[0].Dimension);
            for (int i = 0; i < this.vertices.Length; i++)
                boundingBox.AddVertex(this.vertices[i]);

            // update cluster assignments
            foreach (Facility f in facilities)
            {
                // get the vertex where this facility is
                Vertex facilityVertex = this.vertices[f.VertexIndex];

                // update assignments and distances to facility
                foreach (int index in f.VertexIndices)
                {
                    // assign vertex to this facility
                    this.vertices[index].IsFacility = false;
                    //this.vertices[index].Facility = f;
                    // compute the distance to this facility
                    //this.vertices[index].WeightedDistToFac = this.vertices[index].Metric(facilityVertex);
                    this.vertices[index].AssignToFacility(f);
                }

                // set the facility vertex to be a facility
                this.vertices[f.VertexIndex].IsFacility = true;
            }

            // recompute the clustering

            // start measuring time
            int start = Environment.TickCount;

            // compute the facility cost
            this.facilityCost = this.boundingBox.GetWeightedDiagonal() * facCostMult;

            // compute the number of iterations
            int iter;
            if (iterations <= 0)
                iter = vertices.Length / 100;
            else
                iter = iterations;

            //PerformIterations(iter);

            // end measuring time
            int end = Environment.TickCount;

            return end - start;
        }

        /// <summary>
        /// Finds the data bounding box (respecting the coordinate weights) and computes the clustering.
        /// </summary>
        /// <param name="vertices">The array of vertices to cluster.</param>
        /// <returns>Returns the length of the computation in milliseconds.</returns>
        public int ComputeClustering(Vertex[] vertices)
        {
            // prepare an empty bounding box
            BoundingBox bbox = new BoundingBox();
            bbox.Initialize(vertices[0].Dimension);

            // compute the bounding box of the input vetices
            for (int i = 0; i < vertices.Length; i++)
                bbox.AddVertex(vertices[i]);

            // continue with the clustering
            return ComputeClustering(vertices, bbox);
        }

        /// <summary>
        /// Computes the clustering.
        /// </summary>
        /// <param name="vertices">The array of vertices to cluster.</param>
        /// <param name="boundingBox">The bounding box of the vertices.</param>
        /// <returns>Returns the duration of the computation in milliseconds.</returns>
        public int ComputeClustering(Vertex[] vertices, BoundingBox boundingBox)
        {
            // store the vertices
            this.vertices = vertices;
            this.boundingBox = boundingBox;

            // start measuring time
            int start = Environment.TickCount;

            // compute the facility cost
            this.facilityCost = this.boundingBox.GetWeightedDiagonal() * facCostMult;

            // generate initial solution
            GenerateInitialSolution();

            // prepare lists for reassignments
            vertsToReassign = new List<int>();
            facilsToClose = new List<Facility>();

            // compute the number of iterations
            int iter;
            if (iterations <= 0)
                iter = vertices.Length / 10;
            else
                iter = iterations;

            // compute only if there is some reasonable facility cost
            //if (facilityCost > 0)
            //    PerformIterations(iter);

            // end measuring time
            int end = Environment.TickCount;

            return end - start;
        }

        private void PerformIterations(int itersToDo)
        {
            // perform iterations
            for (int i = 0; i < itersToDo; i++)
            {
                // get random vertex
                int vertexIndex = rnd.Next(vertices.Length);

                if (Gain(vertexIndex) > 0)
                {
                    Reassign(vertexIndex);
                }
            }
        }

        /// <summary>
        /// Computes the clustering.
        /// </summary>
        /// <param name="vertices">The array of vertices to cluster.</param>
        /// <param name="boundingBox">The bounding box of the vertices.</param>
        /// <returns>Returns the duration of the computation in milliseconds.</returns>
        public int ComputeClusteringVector(Vertex[] vertices, BoundingBox boundingBox)
        {
            // store the vertices
            this.vertices = vertices;
            this.boundingBox = boundingBox;

            // start measuring time
            int start = Environment.TickCount;

            // compute the facility cost
            this.facilityCost = this.boundingBox.GetWeightedDiagonalVector() * facCostMult;

            // generate initial solution
            GenerateInitialSolution();

            // prepare lists for reassignments
            //vertsToReassign = new List<int>();
            //facilsToClose = new List<Facility>();

            // compute the number of iterations
            //int iter;
            //if (iterations <= 0)
            //    iter = vertices.Length / 10;
            //else
            //    iter = iterations;

            // compute only if there is some reasonable facility cost
            //if (facilityCost > 0)
            //    PerformIterations(iter);

            // end measuring time
            int end = Environment.TickCount;

            return end - start;
        }

        /// <summary>
        /// Finds the cluster containing the given vertex.
        /// </summary>
        /// <param name="vertexIndex">Index of the vertex to find.</param>
        /// <returns>Returns index of the facility where the vertex belongs.</returns>
        public int FindClusterContainingVertex(int vertexIndex)
        {
            // returns index pointing into the vertices array!

            //if (vertexIndex < 0 || vertexIndex >= vertices.Length)
            //	// no such vertex exists
            //	throw new ApplicationException("No such vertex exists.");
            // will throw IndexOutOfBoundsException

            // find the vertex, get the facility, and get its index
            return vertices[vertexIndex].Facility.VertexIndex;
        }

        /// <summary>
        /// Finds clusters containing given sample vertices.
        /// </summary>
        /// <param name="samples">Indices of sample vertices.</param>
        /// <param name="indexOfFirstCentre">Output. Index of the first cluster centre
        /// (after the vertices from clusters). Also can be treated as the number
        /// of vertices from clusters.</param>
        /// <returns>Returns all the vertices from found clusters
        /// plus centres of the remaining clusters.</returns>
        public int[] GetVerticesSubset(int[] samples, out int indexOfFirstCentre)
        {
            // all returned indices point into the vertices array!

            // reset all markers
            foreach (Facility f in facilities)
                f.Marked = false;

            // number of marked facilities
            int facilitiesMarked = 0;
            // total number of vertices in all marked facilities
            int verticesToOutput = 0;

            // mark all clusters containing sample points
            for (int i = 0; i < samples.Length; i++)
            {
                // get the facility
                Facility f = vertices[samples[i]].Facility;

                // is the facility already marked?
                if (!f.Marked)
                {
                    // no, mark it
                    f.Marked = true;
                    // one more marked
                    facilitiesMarked++;
                    // add number of points to the total sum
                    verticesToOutput += f.VertexIndices.Count;
                }
            }

            // prepare output array
            int[] output = new int[verticesToOutput + (facilities.Count - facilitiesMarked)];

            // index into the first part (vertices from clusters)
            int indexVertices = 0;
            // index into the second part (cluster centres)
            int indexCentres = verticesToOutput;

            // for marked facilities, copy all the vertices
            // for unmarked facilities, copy just the centre
            foreach (Facility f in facilities)
            {
                // determine whether the facility is marked or not
                if (f.Marked)
                {
                    // copy all the vertices from the cluster
                    foreach (int vertexIndex in f.VertexIndices)
                    {
                        output[indexVertices] = vertexIndex;
                        indexVertices++;
                    }
                }
                else
                {
                    // store the cluster centre
                    output[indexCentres] = f.VertexIndex;
                    indexCentres++;
                }
            }

            // return results
            indexOfFirstCentre = verticesToOutput;
            return output;
        }

        /// <summary>
        /// Generates an array of indices from 0 to length in random order.
        /// </summary>
        /// <returns>Returns array of indices in random order.</returns>
        private int[] GenShufflingArray()
        {
            // number of indices to generate
            int length = vertices.Length;

            // initialize sorted array
            int[] shuffle = new int[length];
            for (int i = 0; i < length; i++)
                shuffle[i] = i;

            // count the number of forced facilities (they have infinite weight)
            int forcedFacilCount = 0;
            while (forcedFacilCount < vertices.Length && vertices[forcedFacilCount].VertexWeight > double.MaxValue)
                forcedFacilCount++;

            // shuffle array
            //for (int i = length - 1; i > 0; i--)
            for (int i = length - 1; i > forcedFacilCount; i--)
            {
                // generate random index from 0 to i (inclusive)
                //int index = rnd.Next(i + 1);
                int index = forcedFacilCount + rnd.Next(i + 1 - forcedFacilCount);

                // swap array cells
                int tmp = shuffle[index];
                shuffle[index] = shuffle[i];
                shuffle[i] = tmp;
            }

            return shuffle;
        }

        /// <summary>
        /// Finds the closest facility for given point.
        /// </summary>
        /// <param name="vertex">Point for which to search closest facility.</param>
        /// <param name="distance">Output parameter. Distance to closest facility.</param>
        /// <returns>Returns the closest facility.</returns>
        private Facility GetClosestFacility(Vertex vertex, out double dist)
        {
            double minDist = double.PositiveInfinity;
            Facility closestFacil = null;

            // inspect all facilities
            foreach (Facility facil in facilities)
            {
                double currDist = vertex.Metric(vertices[facil.VertexIndex]);

                // compute distance to current facility
                // LINE
                if (currDist < minDist)
                {
                    dist = currDist;
                    return facil;
                }
            }

            // we didnt find some 
            dist = minDist;
            return closestFacil;
        }

        //private Facility GetClosestFacility(Vertex vertex, out double distance)
        //{
        //    // initialize infinite distance
        //    double minDist = double.PositiveInfinity;
        //    Facility closestFacil = null;

        //    // inspect all facilities
        //    foreach (Facility facil in facilities)
        //    {
        //        // compute distance to current facility
        //        double currDist = vertex.WeightedDistance(vertices[facil.VertexIndex]);

        //        // look for minimum
        //        if (currDist < minDist)
        //        {
        //            // we found a shorter distance
        //            minDist = currDist;
        //            closestFacil = facil;
        //        }
        //    }

        //    // return minimum distance and the closest facility
        //    distance = minDist;
        //    return closestFacil;
        //}

        /// <summary>
        /// Generates initial solution for the incremental algorithm.
        /// </summary>
        private void GenerateInitialSolution()
        {
            // reset list of facilities
            facilities = new List<Facility>();

            // special case - facilities are for free
            if (facilityCost <= 0)
            {
                facilities.Capacity = vertices.Length;

                // every point will be a facility
                // no need to shuffle
                for (int i = 0; i < vertices.Length; i++)
                {
                    // create a new facility
                    Facility newFacil = new Facility(i);
                    facilities.Add(newFacil);

                    // mark vertex as facility
                    vertices[i].IsFacility = true;
                    //vertices[i].Facility = newFacil;
                    //vertices[i].WeightedDistToFac = 0;
                    vertices[i].AssignToFacility(newFacil);

                    // assign the vertex to the facility
                    newFacil.AddVertex(i, 0);
                }

                // end of special case
                return;
            }

            // prepare shuffling array for points
            int[] shuffle = GenShufflingArray();
            int vertexIndex = shuffle[0];

            // first point (in random order) will always become a facility
            Facility newFac = new Facility(vertexIndex);
            facilities.Add(newFac);

            // mark first point as facility
            vertices[vertexIndex].IsFacility = true;
            //vertices[shuffle[0]].Facility = newFac;
            //vertices[shuffle[0]].WeightedDistToFac = 0;
            vertices[vertexIndex].AssignToFacility(newFac);

            // assign the vertex to the facility
            newFac.AddVertex(vertexIndex, 0);

            // process remaining points
            for (int i = 1; i < vertices.Length; i++)
            {
                // get current vertex index
                vertexIndex = shuffle[i];

                double dist;
                // compute distance to closest facility
                Facility foundFac = GetClosestFacility(vertices[vertexIndex], out dist);

                // decide whether to open a new facility
                // or assign current point to some existing facility
                if ( foundFac == null )
                //if (rnd.NextDouble() <= dist / facilityCost)
                {
                    // create a new facility
                    newFac = new Facility(vertexIndex);
                    facilities.Add(newFac);

                    // mark vertex as facility
                    vertices[vertexIndex].IsFacility = true;
                    //vertices[vertexIndex].Facility = newFac;
                    //vertices[vertexIndex].WeightedDistToFac = 0;
                    vertices[vertexIndex].AssignToFacility(newFac);

                    // add the vertex to the facility
                    newFac.AddVertex(vertexIndex, 0);
                }
                else
                {
                    // assign point to closest existing facility
                    //vertices[vertexIndex].Facility = found;
                    vertices[vertexIndex].IsFacility = false;
                    //vertices[vertexIndex].WeightedDistToFac = dist;
                    vertices[vertexIndex].AssignToFacility(foundFac);

                    // update point list in the existing facility
                    foundFac.AddVertex(vertexIndex, dist);
                }
            }
        }

        /// <summary>
        /// Computes gain for given vertex.
        /// </summary>
        /// <param name="vertex">Vertex for which to compute gain.</param>
        /// <returns>Returns the gain for the given vertex.</returns>
        private double Gain(int vertexIndex)
        {
            // get vertex for which to compute gain
            Vertex vertex = vertices[vertexIndex];

            double gain;
            // initialize gain
            if (vertex.IsFacility)
                // facility already open
                gain = 0;
            else
                // facility would be opened
                gain = -facilityCost;

            // prepare list for indices of vertices that should be re-assigned
            vertsToReassign.Clear();

            // look at all vertices
            // and determine whether new facility would be better
            // include also the vertex for which we compute gain
            // because we will benefit from re-assigning it from current facility to itself
            for (int i = 0; i < vertices.Length; i++)
            {
                // get vertex which we are going to inspect
                Vertex v = vertices[i];

                // skip all facilities
                //if (v.isFacility)
                /* following for-cycle should go here
                 * no - the cycle must be performed after this is done
                 * may be yes if we have a list of verts assigned to facility,
                 * but it would be unefficient
                 */
                //	continue;
                // THE ABOVE IS PROBABLY MISTAKE

                // compute distance to possible new facility
                // LINE
                double distToPossibleFacility = v.Metric(vertex);
                // compute difference
                double distGain = v.WeightedDistToFac - distToPossibleFacility;

                // compare distances
                if (distGain > 0)
                {
                    // the new possible facility would be better
                    // mark current vertex for re-assignment
                    vertsToReassign.Add(i);

                    // add difference (possible improvement) to gain
                    gain += distGain;
                }
                else
                {
                    // current facility is better
                    // but add difference (possible disimprovement) to facility accumulator
                    // in the accumulator we store how much would it cost to close this facility
                    // and re-assign it's vertices to other facilities
                    v.Facility.AddToAccum(distGain);	// distance gain is negative here!
                }
            }

            facilsToClose.Clear();
            // look at all facilities
            // and determine whether it would be beneficial to close it and reassign points
            // also close "empty" facilities
            // empty facilities not possible, each facility is also a vertex
            foreach (Facility f in facilities)
            {
                // determine whether it would be beneficial to close this facility
                // accumulator == (negative value) cost for re-assigning points
                // facilityCost == cost spared by closing facility
                // if f.Accumulator + facilityCost > 0 we will benefit from it
                if (f.Accumulator + facilityCost > 0
                    && f.VertexIndex != vertexIndex)	// we cannot close facility beeing examined
                {
                    // mark facility for closure
                    // points for re-assignment are stored in facility
                    facilsToClose.Add(f);

                    // add the benefit to gain
                    // f.Accumulator is negative!
                    gain += f.Accumulator + facilityCost;
                }

                // reset facility accumulator for future use
                f.ResetAccum();
            }

            // DEBUG OUTPUT
            //Console.WriteLine("{0} {1}", vertexIndex, gain);

            return gain;
        }

        private void Reassign(int vertexIndex)
        {
            // facility where to reassign
            Facility facilToAssign;

            // create the new facility
            if (vertices[vertexIndex].IsFacility)
            {
                // facility already exists
                facilToAssign = vertices[vertexIndex].Facility;
            }
            else
            {
                // mark the vertex as a facility
                vertices[vertexIndex].IsFacility = true;

                // create a new facility
                facilToAssign = new Facility(vertexIndex);
                facilities.Add(facilToAssign);
                // vertex will be assigned to the facility automatically
                // as all others in the following foreach
            }

            // get the new facility
            Vertex newFacility = vertices[vertexIndex];

            // perform re-assignments
            foreach (int index in vertsToReassign)
            {
                // remove from current facility
                Facility facilRemoveFrom = vertices[index].Facility;
                facilRemoveFrom.RemoveVertex(index, vertices);

                // set new facility
                //vertices[index].Facility = facilToAssign;
                // update distance
                // may be precomputed from the evaluation phase
                //vertices[index].WeightedDistToFac = vertices[index].WeightedDistance(newFacility);
                double dist = vertices[index].WeightedDistance(newFacility);
                vertices[index].AssignToFacility(facilToAssign);

                // assign to new facility
                facilToAssign.AddVertex(index, dist);
            }

            //#if DEBUG
            //            string msg1;
            //            if ( ! CheckAssignments(out msg1))
            //                // some vertex could be assigned to a facility that is going to be closed
            //                // that is not an error
            //                throw new Exception(msg1);
            //#endif

            // perform facility closures
            foreach (Facility f in facilsToClose)
            {
                // vertex is no longer a facility
                vertices[f.VertexIndex].IsFacility = false;

                // reassign all it's points to the newly created facility (may be precomputed)
                // facility vertex itself will be re-assigned too
                foreach (int index in f.VertexIndices)
                {
                    // removing vertex from current facility not neccessary
                    // we will close the facility anyway
                    // removing even not possible because we iterate over the list

                    // set new facility
                    //vertices[index].Facility = facilToAssign;
                    // update distance
                    //vertices[index].WeightedDistToFac = vertices[index].WeightedDistance(newFacility);
                    double dist = vertices[index].WeightedDistance(newFacility);
                    vertices[index].AssignToFacility(facilToAssign);

                    // assign to new facility
                    facilToAssign.AddVertex(index, dist);
                }

                //  remove facility from list
                facilities.Remove(f);
            }

//#if DEBUG
//            string msg2;
//            if (!CheckAssignments(out msg2))
//                throw new Exception(msg2);
//#endif
        }

#if RUN_PARALLEL
        /// <summary>
        /// Computes the gain of the given vertex.
        /// The gain is computed using parallel threads.
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex for which to compute the gain.</param>
        /// <returns>Returns the gain for the given vertex.</returns>
        private double ParallelGain(int vertexIndex)
        {
            double gain;
            // initialize gain
            if (vertices[vertexIndex].IsFacility)
                // facility already open
                gain = 0;
            else
                // facility would be opened
                gain = -facilityCost;

            // prepare lists
            vertsToReassign.Clear();
            facilsToClose.Clear();

            // divide work
            int chunk = facilities.Count / threadCount;

            // set up and run workers
            int j;
            for (j = 0; j < threadCount - 1; j++)
            {
                // set up the job
                workers[j].StartIndex = j * chunk;
                workers[j].EndIndexPlusOne = (j + 1) * chunk;

                // run the worker
                System.Threading.ThreadPool.QueueUserWorkItem(workers[j].ComputeGain, vertexIndex);
            }
            // the last worker
            // set up the job
            workers[j].StartIndex = j * chunk;
            workers[j].EndIndexPlusOne = facilities.Count;
            // run the worker
            System.Threading.ThreadPool.QueueUserWorkItem(workers[j].ComputeGain, vertexIndex);

            // wait for workers and collect results
            for (int i = 0; i < threadCount; i++)
            {
                // wait for the thread to finish
                workers[i].DoneEvent.WaitOne();

                // collect the results
                gain += workers[i].PartialGain;
                vertsToReassign.AddRange(workers[i].VertsToReassign);
                facilsToClose.AddRange(workers[i].FacilsToClose);
            }

            return gain;
        }
#endif

        /// <summary>
        /// Debug method. Checks mutual vertex and facility assignments.
        /// </summary>
        /// <param name="message">The output error message.</param>
        /// <returns>Returns true if everything is OK, otherwise returns false.</returns>
        private bool CheckAssignments(out string message)
        {
            // check that vertices that are facilities are assigned to a facility with the same vertex index
            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].IsFacility
                    && vertices[i].Facility.VertexIndex != i)
                {
                    message = string.Format("Vertex {0} is a facility but is assigned to the facility with vertex index {1}.",
                        i, vertices[i].Facility.VertexIndex);
                    return false;
                }
            }

            // check that facilities contain its facility vertex
            foreach (Facility f in facilities)
            {
                if (!f.VertexIndices.Contains(f.VertexIndex))
                {
                    message = string.Format("Facility with vertex index {0} doesn't contain the vertex.", f.VertexIndex);
                    return false;
                }
            }

            // if we got here, it's OK
            message = "Check OK.";
            return true;
        }

    }
}
