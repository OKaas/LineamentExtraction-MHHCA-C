/***
A new algorithm for spatial clustering of the line segments 
as a tool for lineament extraction.

Ondrej Kaas, 2016
Faculty of Applied Sciences,University of West Bohemia, Pilsen, Czech Republic     
======================================================= */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Zcu.Graphics.Clustering;

namespace LineamentExtraction
{
    public static class Program
    {
        public static Vertex[] Points;

        public static Vertex ChangeSystem(Vertex v)
        {
            uint sX = 1;
            uint sY = 2;
            uint eX = 3;
            uint eY = 4;

            Vertex ret = new Vertex(v.coords);

            double[] coord = ret.coords;

            double tmp = coord[sX];
            coord[sX] = -coord[sY];
            coord[sY] = -tmp;

            tmp = coord[eX];
            coord[eX] = -coord[eY];
            coord[eY] = -tmp;

            return new Vertex(coord);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main( string[] args )
        {
            System.Globalization.CultureInfo invarCult
              = System.Globalization.CultureInfo.InvariantCulture;

            try
            {
                Setup.InputPath = args[0];

                Setup.Border = new double[] { double.Parse(args[1], invarCult),
                                          double.Parse(args[2], invarCult),
                                          double.Parse(args[3], invarCult) };

                Setup.OutPath = args[4];

                Setup.GeomaticCoordSystem = true;

                if (Cluster.Clust())
                {
                    Console.WriteLine("Result saved in file {0} :", Setup.OutPath);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Args:\n GIS.exe 0<input file path> <border X> <border Y> <border azimuth> <output file path>");
            }
        }
    }
}
