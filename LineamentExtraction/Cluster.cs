/***
A new algorithm for spatial clustering of the line segments 
as a tool for lineament extraction.

Ondrej Kaas, 2016
Faculty of Applied Sciences,University of West Bohemia, Pilsen, Czech Republic     
======================================================= */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LineamentExtraction.Load;
using Zcu.Graphics.Clustering;

namespace LineamentExtraction
{
    /// <summary>
    /// Main class for clustering. Parameters are given by static class Setup
    /// </summary>
    static public class Cluster
    {
        public static bool Clust()
        {
            BoundingBox Bound;
            List<Facility> Facility;

            // read data
            Vertex[] Points = LoadLine.Load( Setup.InputPath, out Bound );

            // setup dimension of points
            Setup.Dim = Points[0].Dimension;

            FacilityLocation fac = new FacilityLocation();

            // setup clustering properties
            Vertex.CoordWeights = Setup.ValueDim;
            Vertex.CoordBorder = Setup.Border;
            Vertex.BorderMin = Setup.BorderMin;
            Vertex.BorderPercent = Setup.BorderPercent;


            // setup metric
            VertexExtension.SetLineMetric();

            // start clustering
            fac.ComputeClustering(Points, Bound);

            // get index of facilities
            int[] indexCentre = fac.GetAllFacilities();

            // create array for facilities
            Vertex[] centre = new Vertex[indexCentre.Length];

            // insert facilities vertices into array
            for (int f = 0; f < centre.Length; f++)
            {
                centre[f] = Points[indexCentre[f]];
            }

            Facility = fac.Facilities;
            Analysis.SaveSite(ref Points, ref Facility, Setup.OutPath, Setup.SizeFilter);

            return false;
        }
    }
}
