﻿// This file is part of libnoise-dotnet.
//
// libnoise-dotnet is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// libnoise-dotnet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with libnoise-dotnet.  If not, see <http://www.gnu.org/licenses/>.
// 
// From the original Jason Bevins's Libnoise (http://libnoise.sourceforge.net)

namespace LibNoise.Primitive
{
    using System;

    /// <summary>
    /// Noise module that outputs concentric spheres.
    ///
    /// This noise module outputs concentric spheres centered on the origin
    /// like the concentric rings of an onion.
    ///
    /// The first sphere has a radius of 1.0.  Each subsequent sphere has a
    /// radius that is 1.0 unit larger than the previous sphere.
    ///
    /// The output value from this noise module is determined by the distance
    /// between the input value and the the nearest spherical surface.  The
    /// input values that are located on a spherical surface are given the
    /// output value 1.0 and the input values that are equidistant from two
    /// spherical surfaces are given the output value -1.0.
    ///
    /// An application can change the frequency of the concentric spheres.
    /// Increasing the frequency reduces the distances between spheres.
    ///
    /// This noise module, modified with some low-frequency, low-power
    /// turbulence, is useful for generating agate-like textures.
    /// 
    /// </summary>
    public class Spheres : PrimitiveModule, IModule3D
    {
        #region Constants

        /// <summary>
        /// Frequency of the concentric spheres.
        /// </summary>
        public const double DEFAULT_FREQUENCY = 1.0;

        #endregion

        #region Fields

        /// <summary>
        /// Frequency of the concentric cylinders.
        /// </summary>
        protected double _frequency = DEFAULT_FREQUENCY;

        #endregion

        #region Accessors

        /// <summary>
        /// Gets or sets the frequency
        /// </summary>
        public double Frequency
        {
            get { return _frequency; }
            set { _frequency = value; }
        }

        #endregion

        #region Ctor/Dtor

        /// <summary>
        /// Create new Spheres generator with default values
        /// </summary>
        public Spheres()
            : this(DEFAULT_FREQUENCY)
        {
        }


        /// <summary>
        /// Create a new Spheres generator with given values
        /// </summary>
        /// <param name="frequency"></param>
        public Spheres(double frequency)
        {
            _frequency = frequency;
        }

        #endregion

        #region IModule3D Members

        /// <summary>
        /// Generates an output value given the coordinates of the specified input value.
        /// </summary>
        /// <param name="x">The input coordinate on the x-axis.</param>
        /// <param name="y">The input coordinate on the y-axis.</param>
        /// <param name="z">The input coordinate on the z-axis.</param>
        /// <returns>The resulting output value.</returns>
        public double GetValue(double x, double y, double z)
        {
            x *= _frequency;
            y *= _frequency;
            z *= _frequency;

            var distFromCenter = Math.Sqrt(x*x + y*y + z*z);
            double distFromSmallerSphere = distFromCenter - Math.Floor(distFromCenter);
            double distFromLargerSphere = 1.0 - distFromSmallerSphere;
            double nearestDist = Math.Min(distFromSmallerSphere, distFromLargerSphere);
            return 1.0 - (nearestDist*4.0); // Puts it in the -1.0 to +1.0 range.
        }

        #endregion
    }
}
