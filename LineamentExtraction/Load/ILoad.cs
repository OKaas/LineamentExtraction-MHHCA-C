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


namespace LineamentExtraction.Input
{
    /// <summary>
    /// Marked interface for loading
    /// </summary>
    interface IInput
    {
        void ReadBlock();
    }
}
