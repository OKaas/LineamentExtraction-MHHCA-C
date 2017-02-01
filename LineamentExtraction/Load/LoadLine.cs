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

using LineamentExtraction;
using Zcu.Graphics.Clustering;

namespace LineamentExtraction.Load
{
    /// <summary>
    /// Load input line from file
    /// </summary>
    public static class LoadLine
    {
        static int sX = 1, sY = 2;
        static int eX = 3, eY = 4;

        /// <summary>
        /// Found real bounding box around set of line
        /// </summary>
        private static void RealBoundingBox( ref BoundingBox bound )
        {
            double[] max = bound.GetMaxCoords();
            double[] min = bound.GetMinCoords();

            bound.MaxX = max[sX] > max[eX] ? max[sX] : max[eX];
            bound.MinX = min[sX] < min[eX] ? min[sX] : min[eX];

            bound.MaxY = max[sY] > max[eY] ? max[sY] : max[eY];
            bound.MinY = min[sY] < min[eY] ? min[sY] : min[eY];
        }

        /// <summary>
        /// Load lines from file. Suppose geomathematics coord system.
        /// </summary>
        /// <param name="input">input file</param>
        /// <param name="bound">boudning box of investigated area</param>
        /// <returns></returns>
        public static Vertex[] Load( string input, out BoundingBox bound )
        {
            string[] Names = { "ID","A.X", "A.Y", "B.X", "B.Y", "AZ", "LEN" };
            char[] sep = { ';', '\t' };
        
            Setup.NameDim = Names;

            System.Globalization.CultureInfo invarCult
               = System.Globalization.CultureInfo.InvariantCulture;

            List<Vertex> ret = new List<Vertex>();

            bound = new BoundingBox();
            bound.Initialize( Names.Length );

            using (StreamReader reader = new StreamReader(input))
            {
                // skip header
                reader.ReadLine();

                Vertex temp;

                string line = null;
                string[] tokens = null;
                double[] coords = new double[Names.Length];

                while( (line = reader.ReadLine() ) != null )
                {
                    //++count;
                    tokens = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);

                    // id
                    coords[0] = double.Parse(tokens[0], invarCult);

                    // transform into math coord system
                    // geo X -> math -Y
                    coords[1] = -double.Parse(tokens[2], invarCult);
                    coords[2] = -double.Parse(tokens[1], invarCult);

                    // end x,y
                    coords[3] = -double.Parse(tokens[4], invarCult);
                    coords[4] = -double.Parse(tokens[3], invarCult);

                    // azimut
                    coords[5] = double.Parse( tokens[5], invarCult );

                    // length
                    coords[6] = double.Parse( tokens[6], invarCult);

                    temp = new Vertex(coords);

                    ret.Add(temp);
                    bound.AddVertex(temp);
                }
            }

            RealBoundingBox( ref bound );

            return ret.ToArray();
        }
    }
}
