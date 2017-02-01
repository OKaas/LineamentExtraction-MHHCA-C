/***
A new algorithm for spatial clustering of the line segments 
as a tool for lineament extraction.

Ondrej Kaas, 2016
Faculty of Applied Sciences,University of West Bohemia, Pilsen, Czech Republic     
======================================================= */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Zcu.Graphics.Clustering;

namespace LineamentExtraction
{
    /// <summary>
    /// Analytics class. Calculate maximal and minimal distance in particular clusters and their clients
    /// </summary>
    public struct Statistic
    {
        /// <summary>
        /// Maximal distance
        /// </summary>
        public double MaxDistance;

        /// <summary>
        /// Index of vertices which has maximal distance
        /// </summary>
        public int IndexMaxDist;

        /// <summary>
        /// Maximal distance from its facility
        /// </summary>
        public double[] Max;

        /// <summary>
        /// Minimal distance from its facility
        /// </summary>
        public double[] Min;

        /// <summary>
        /// Indexes of maximal vertices
        /// </summary>
        public int[] IndexMax;

        public int Dimension;

        public double[] CentreCoord;

        public int Clients;

        /// <summary>
        /// Does vertice some facility?
        /// </summary>
        public bool HasFacility;

        public Statistic( int dim )
        {
            Max = new double[dim];
            for (int f = 0; f < dim; ++f )
            {
                Max[f] = double.NegativeInfinity;
            }

            Min = new double[dim];
            for (int f = 0; f < dim; ++f)
            {
                Min[f] = double.PositiveInfinity;
            }

            // initialization
            IndexMax = Enumerable.Repeat(-1, dim).ToArray();
            IndexMaxDist = -1;

            CentreCoord = new double[ dim ];
            Clients = -1;

            Dimension = dim;
            MaxDistance = 0;

            HasFacility = false;
        }

        public void Add( )
        { 
        
        }
    }

    public static class Analysis
    {
        /// <summary>
        /// Shift to investigated dimensions
        /// </summary>
        public static int DimOffset = 1;

        public static int CoordX = 0;
        public static int CoordY = 1;

        /// <summary>
        /// Calculate maximal and minimal values of particular vertices
        /// </summary>
        /// <param name="v">list of vertices</param>
        /// <param name="f">list of facilities</param>
        /// <returns></returns>
        public static Statistic GetGeoStatistic(ref Vertex[] v, ref List<Facility> f)
        {
            Statistic ret = new Statistic( Setup.Dim - DimOffset );

            foreach( Facility fac in f )
            {
                Vertex centre = v[fac.VertexIndex];
                List<int> clients = fac.VertexIndices;

                foreach( int c in clients )
                {
                    double d = VertexExtension.DistanceBetween(centre, v[c], CoordX, CoordY);

                    if( d > ret.MaxDistance )
                    {
                        ret.MaxDistance = d;

                        ret.IndexMaxDist = c;
                    }

                    for (int i = 0; i < Setup.Dim - DimOffset; ++i)
                    {
                        int of = i + DimOffset;

                        if (ret.Max[i] < v[c][of])
                        {
                            ret.Max[i] = v[c][of];

                            ret.IndexMax[i] = c;
                        }

                        if (ret.Min[i] > v[c][of])
                        {
                            ret.Min[i] = v[c][of];

                            ret.IndexMax[i] = c;
                        }
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Return statistics about vertex
        /// </summary>
        /// <param name="v">vertex</param>
        /// <returns></returns>
        public static Statistic GetStatistic( Vertex v )
        {
            Statistic ret = new Statistic(Setup.Dim - DimOffset);

            Facility fac = v.Facility;

            if ( fac == null )
            {
                ret.HasFacility = false;
            }
            else
            {
                ret.HasFacility = true;

                Vertex centre = Program.Points[fac.VertexIndex];
                List<int> clients = fac.VertexIndices;

                ret.CentreCoord = centre.coords;
                ret.Clients = clients.Count;

                foreach (int c in clients)
                {
                    Vertex cl = Program.Points[c];
                    double d = VertexExtension.DistanceBetween(centre, cl, CoordX, CoordY);

                    if (d > ret.MaxDistance)
                    {
                        ret.MaxDistance = d;

                        ret.IndexMaxDist = c;
                    }

                    for (int i = 0; i < Setup.Dim - DimOffset; ++i)
                    {
                        int of = i + DimOffset;

                        if (ret.Max[i] < cl[of])
                        {
                            ret.Max[i] = cl[of];
                            ret.IndexMax[i] = c;
                        }

                        if (ret.Min[i] > cl[of])
                        {
                            ret.Min[i] = cl[of];

                            ret.IndexMax[i] = c;
                        }
                    }
                } 
            }

            return ret;
        }

        /// <summary>
        /// Save clustering solution. Write into output file "output" only clusters wich contains at least >= "filter" points
        /// </summary>
        /// <param name="vertex">list of vertices</param>
        /// <param name="facility">list of facilities</param>
        /// <param name="output">output file</param>
        /// <param name="filter">number of points in cluster</param>
        public static void SaveSite(ref Vertex[] vertex, ref List<Facility> facility, string output, uint filter)
        {
            using (StreamWriter writer = new StreamWriter(output))
            {
                writer.Write("C;");

                foreach( string i in Setup.NameDim )
                {
                    writer.Write("{0};", i);
                }

                writer.Write("\n");

                for (int f = 0; f < facility.Count; ++f)
                {
                    Vertex centre;
                    List<int> clients = facility[f].VertexIndices;

                    if ( filter == 0 || clients.Count >= filter)
                    {
                        // we use geomathematics coord system 
                        if (Setup.GeomaticCoordSystem)
                            centre = Program.ChangeSystem(vertex[facility[f].VertexIndex]);
                        else
                            centre = vertex[facility[f].VertexIndex];

                        writer.Write("*;", clients.Count);

                        foreach (double i in centre.coords)
                        {
                            writer.Write("{0};", i.ToString("F2"));
                        }

                        writer.Write("\n");

                        foreach (int ver in clients)
                        {
                            if (!vertex[ver].IsFacility)
                            {
                                writer.Write("{0};", centre.coords[0]);

                                Vertex client;

                                if (Setup.GeomaticCoordSystem)
                                    client = Program.ChangeSystem(vertex[ver]);
                                else
                                    client = vertex[ver];

                                foreach (double i in client.coords)
                                {
                                    writer.Write("{0};", i.ToString("F2"));
                                }

                                writer.Write("\n");
                            }
                        }
                    }
                }
            }
        }
    }
}
