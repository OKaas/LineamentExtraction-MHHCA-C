/***
A new algorithm for spatial clustering of the line segments 
as a tool for lineament extraction.

Ondrej Kaas, 2016
======================================================= */
using System;

namespace Zcu.Graphics.Clustering
{
	/// <summary>
	/// Bounding box of points in 3D.
	/// </summary>
	public struct BoundingBox
	{
		/// <summary>
		/// Bounding box corner points.
		/// </summary>
		public double[] MinCorner, MaxCorner;


		/// <summary>
		/// Constructor. Initializes bounding box to the first point inserted.
		/// </summary>
		/// <param name="x">X coordinate.</param>
		/// <param name="y">Y coordinate.</param>
		/// <param name="z">Z coordinate.</param>
		public BoundingBox(double x, double y, double z)
		{
			// initialize bounds to the first point
            MinCorner = new double[] { x, y, z };
			MaxCorner = new double[] { x, y, z };
		}

		/// <summary>
		/// Constructor. Initializes bounding box to the first point inserted.
		/// </summary>
		/// <param name="v">The first vertex.</param>
		public BoundingBox(Vertex v)
		{
            MinCorner = new double[v.Dimension];
			MaxCorner = new double[v.Dimension];

			// initialize bounds to the first point
			for (int i = 0; i < Dimension; i++)
                MinCorner[i] = MaxCorner[i] = v[i];
		}

        public BoundingBox( uint dim )
        {
            MinCorner = new double[dim];
            MaxCorner = new double[dim];
        }

		/// <summary>
		/// Initializes the bounding box to an empty box.
		/// </summary>
		public void Initialize(int dimension)
		{
            MinCorner = new double[dimension];
			MaxCorner = new double[dimension];

			// initialize bounds to extremes
			for (int i = 0; i < Dimension; i++)
			{
                MinCorner[i] = double.PositiveInfinity;
				MaxCorner[i] = double.NegativeInfinity;
			}
		}


		#region Field accessors

        public double[] GetMaxCoords()
        {
            return MaxCorner;
        }

        public double[] GetMinCoords()
        {
            return MinCorner;
        }

		/// <summary>
		/// Gets bounding box dimension, i.e., the number of coordinates.
		/// </summary>
		public int Dimension
		{
			get { return MinCorner.Length; }
		}

		/// <summary>
		/// Gets or sets the lower x bound.
		/// </summary>
		public double MinX
		{
			get { return MinCorner[0]; }
			set { MinCorner[0] = value; }
		}

		/// <summary>
		/// Gets or sets the upper x bound
		/// </summary>
		public double MaxX
		{
			get { return MaxCorner[0]; }
			set { MaxCorner[0] = value; }
		}

		/// <summary>
		/// Gets or sets the lower y bound.
		/// </summary>
		public double MinY
		{
			get { return MinCorner[1]; }
			set { MinCorner[1] = value; }
		}

		/// <summary>
		/// Gets or sets the upper y bound
		/// </summary>
		public double MaxY
		{
			get { return MaxCorner[1]; }
			set { MaxCorner[1] = value; }
		}

		/// <summary>
		/// Gets or sets the lower z bound
		/// </summary>
		public double MinZ
		{
			get { return MinCorner[2]; }
			set { MinCorner[2] = value; }
		}

		/// <summary>
		/// Gets or sets the upper z bound
		/// </summary>
		public double MaxZ
		{
			get { return MaxCorner[2]; }
			set { MaxCorner[2] = value; }
		}

		/// <summary>
		/// Gets the lower bound in the given dimension.
		/// </summary>
		/// <param name="dimension">Dimension index.</param>
		/// <returns>Returns the lower bound in the given dimension.</returns>
		public double GetMin(int dimension)
		{
            return MinCorner[dimension];
		}

		/// <summary>
		/// Sets the lower bound in the given dimension.
		/// </summary>
		/// <param name="dimension">Dimension index.</param>
		/// <param name="value">The lower bound value.</param>
		public void SetMin(int dimension, double value)
		{
            MinCorner[dimension] = value;
		}

		/// <summary>
		/// Gets the upper bound in the given dimension.
		/// </summary>
		/// <param name="dimension">Dimension index.</param>
		/// <returns>Returns the upper bound in the given dimension.</returns>
		public double GetMax(int dimension)
		{
			return MaxCorner[dimension];
		}

		/// <summary>
		/// Sets the upper bound in the given dimension.
		/// </summary>
		/// <param name="dimension">Dimension index.</param>
		/// <param name="value">The upper bound value.</param>
		public void SetMax(int dimension, double value)
		{
			MaxCorner[dimension] = value;
		}

		#endregion


		#region Size properties

		/// <summary>
		/// Gets bounding box width, i.e., size in the first (x) dimension.
		/// </summary>
		public double Width
		{
			get { return MaxCorner[0] - MinCorner[0]; }
		}

		/// <summary>
		/// Gets bounding box height, i.e., size in the second (y) dimension.
		/// </summary>
		public double Height
		{
			get { return MaxCorner[1] - MinCorner[1]; }
		}

		/// <summary>
		/// Gets bounding box depth, i.e., size in the third (z) dimension.
		/// </summary>
		public double Depth
		{
			get { return MaxCorner[2] - MinCorner[2]; }
		}

		/// <summary>
		/// Gets the box size in the specified dimension.
		/// </summary>
		/// <param name="dimension">Dimension index.</param>
		/// <returns>Returns the box size in the specified dimension.</returns>
		public double Size(int dimension)
		{
			return MaxCorner[dimension] - MinCorner[dimension];
		}

		#endregion


		/// <summary>
		/// Computes the diagonal of the bounding box.
		/// </summary>
		/// <returns>Returns the diagonal length.</returns>
		public double GetDiagonal()
		{
			double sum = 0;

			// sum up the squares of the box sizes in all dimensions
			for (int i = 0; i < Dimension; i++)
			{
				double s = Size(i);
				sum += s * s;
			}

			return Math.Sqrt(sum);
		}

		/// <summary>
		/// Computes diagonal in xy plane.
		/// </summary>
		/// <returns>Returns diagonal in xy plane.</returns>
		public double GetDiagonalXY()
		{
			double dx = Width;
			double dy = Height;

			return Math.Sqrt(dx*dx + dy*dy);
		}

		/// <summary>
		/// Computes diagonal in xyz plane.
		/// </summary>
		/// <returns>Returns diagonal in xyz plane.</returns>
		public double GetDiagonalXYZ()
		{
			double dx = Width;
			double dy = Height;
			double dz = Depth;

			return Math.Sqrt(dx*dx + dy*dy + dz*dz);
		}

		/// <summary>
		/// Computes the diagonal with respect to vertex coordinate weights.
		/// </summary>
		/// <returns>Returns the box diagonal.</returns>
		public double GetWeightedDiagonal()
		{
			// if no weights are defined
			if (Vertex.CoordWeights == null)
				return GetDiagonal();

			double sum = 0;

			// sum up the squares of the box sizes in all dimensions
			for (int i = 0; i < Dimension; i++)
			{
				// multiply the size by the weight
				double s = Size(i) * Vertex.CoordWeights[i];
				sum += s * s;
			}

			return Math.Sqrt(sum);
		}

        /// <summary>
        /// Computes the diagonal with respect to vertex coordinate weights.
        /// </summary>
        /// <returns>Returns the box diagonal.</returns>
        public double GetWeightedDiagonalVector()
        {
            // if no weights are defined
            if (Vertex.CoordWeights == null)
                return GetDiagonal();

            double sum = 0;

            // sum up the squares of the box sizes in all dimensions
            for (int i = 0; i < Dimension; i++)
            {
                // multiply the size by the weight
                double s = Size(i);
                sum += s * s;
            }

            return Math.Sqrt(sum);
        }

		/// <summary>
		/// Updates bounding box according to new point.
		/// </summary>
		/// <param name="x">X coordinate.</param>
		/// <param name="y">Y coordinate.</param>
		/// <param name="z">Z coordinate.</param>
		public void AddVertex(double x, double y, double z)
		{
			System.Diagnostics.Debug.Assert(Dimension == 3, "Dimension missmatch.");

            if (x < MinCorner[0])
                MinCorner[0] = x;
			if (x > MaxCorner[0])
				MaxCorner[0] = x;

            if (y < MinCorner[1])
                MinCorner[1] = y;
			if (y > MaxCorner[1])
				MaxCorner[1] = y;

            if (z < MinCorner[2])
                MinCorner[2] = z;
			if (z > MaxCorner[2])
				MaxCorner[2] = z;
		}

        private static bool First = true;

        /// <summary>
        /// Updates bounding box according to the new vertex.
        /// </summary>
        /// <param name="v">New vertex to enclose into bounding box.</param>
        public void AddVertex(Vertex v)
        {
            //System.Diagnostics.Debug.Assert(this.Dimension == v.Dimension, "Dimension missmatch.");

            if (First)
            {
                // go through all dimensions
                for (int i = 0; i < Dimension; i++)
                {
                    MinCorner[i] = v[i];
                    MaxCorner[i] = v[i];
                }

                First = false;
            }
            else
            {
                // go through all dimensions
                for (int i = 0; i < Dimension; i++)
                {
                    // check minimum
                    if (v[i] < MinCorner[i])
                        MinCorner[i] = v[i];

                    // check maximum
                    if (v[i] > MaxCorner[i])
                        MaxCorner[i] = v[i];
                }
            }
        }

        public void AddGeoVertex( Vertex v )
        {
            //System.Diagnostics.Debug.Assert(this.Dimension == v.Dimension, "Dimension missmatch.");

            if (First)
            {
                MinCorner[0] = -v.Y;
                MaxCorner[0] = -v.Y;

                MinCorner[1] = -v.X;
                MaxCorner[1] = -v.X;

                First = false;
            }

            if (-v.Y < MinCorner[0])
            {
                MinCorner[0] = -v.Y;
            }
            else if( -v.Y > MaxCorner[0] )
            {
                MaxCorner[0] = -v.Y;
            }

            if (-v.X < MinCorner[1])
            {
                MinCorner[1] = -v.X;
            }
            else if (-v.X > MaxCorner[1])
            {
                MaxCorner[1] = -v.X;
            }

            for (int f = 2; f < v.Dimension; ++f )
            {
                if (MinCorner[f] > v[f])
                {
                    MinCorner[f] = v[f];
                }
                else if (MaxCorner[f] < v[f])
                {
                    MaxCorner[f] = v[f];
                }
            }

        }

        public void AddVertex(double[] coord)
        {
            for (int f = 0; f < Dimension; ++f)
            {
                // minimum
                if (MinCorner[f] > coord[f])
                {
                    MinCorner[f] = coord[f];
                }

                // maximum
                if (MaxCorner[f] < coord[f])
                {
                    MaxCorner[f] = coord[f];
                }
            }
        }

		/// <summary>
		/// Adds given bounding box to this one.
		/// </summary>
		/// <param name="box">Bounding box to add.</param>
		public void AddBox(BoundingBox box)
		{
			System.Diagnostics.Debug.Assert(this.Dimension == box.Dimension, "Dimension missmatch.");

			// enlarge this bounding box to enclose also the given box
			this.AddVertex(new Vertex(box.MinCorner));
			this.AddVertex(new Vertex(box.MaxCorner));
		}

		/// <summary>
		/// Adds given bounding box to this one.
		/// Results in new bounding box.
		/// </summary>
		/// <param name="box">Bounding box to add.</param>
		/// <returns>Returns union of this and given bounding box.</returns>
		public BoundingBox GetUnion(BoundingBox box)
		{
			// prepare a new box
			BoundingBox newBox = new BoundingBox();
			newBox.Initialize(this.Dimension);

			// copy the current box
			newBox.AddBox(this);

			// enlarge to enclose also the given box
			newBox.AddBox(box);		// the dimensions will be checked here

			return newBox;
		}
	}
}
