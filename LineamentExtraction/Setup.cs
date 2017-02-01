/***
A new algorithm for spatial clustering of the line segments 
as a tool for lineament extraction.

Ondrej Kaas, 2016
Faculty of Applied Sciences, University of West Bohemia, Pilsen, Czech Republic     
======================================================= */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace LineamentExtraction
{
    /// <summary>
    /// Clustering parameters setup
    /// </summary>
    public static class Setup
    {
        public static string InputPath { get; set; }

        public static string OutPath { get; set; }

        public static uint SizeFilter { get; set; }

        public static int Dim { get; set; }
        public static string[] NameDim { get; set; }
        public static double[] ValueDim { get; set; }
        public static double ClusterSize { get; set; }
        public static string Note { get; set; }
        public static bool Random { get; set; }
        public static bool GeomaticCoordSystem {get ; set; }

        public static double BorderMin { get; set; }
        public static double BorderPercent { get; set; }

        public static double[] Border { get; set; }

    }
}
