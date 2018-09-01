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

namespace LibNoise.Demo
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.Windows.Forms;

    using LibNoise.Builder;
    using LibNoise.Demo.Ext.Dotnet;
    using LibNoise.Filter;
    using LibNoise.Modifier;
    using LibNoise.Primitive;
    using LibNoise.Renderer;

    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();

#if DEBUG
            Text = String.Format("libnoise-dotnet {0} - demo (DEBUG)", Libnoise.Version);
#else
            Text = String.Format("libnoise-dotnet {0} - demo", Libnoise.Version);
#endif

            // Primitive
            _tbxSeed.Text = PrimitiveModule.DefaultSeed.ToString();
            _cbxPrimitive.Items.AddRange(Enum.GetNames(typeof (NoisePrimitive)));
            _cbxPrimitive.SelectedItem = Enum.GetName(typeof (NoisePrimitive), NoisePrimitive.ImprovedPerlin);

            _cbxQuality.Items.AddRange(Enum.GetNames(typeof (NoiseQuality)));
            _cbxQuality.SelectedItem = Enum.GetName(typeof (NoiseQuality), NoiseQuality.Standard);

            // Filter
            _cbxFilter.Items.AddRange(Enum.GetNames(typeof (NoiseFilter)));
            _cbxFilter.SelectedItem = Enum.GetName(typeof (NoiseFilter), NoiseFilter.SumFractal);

            _tbxFrequency.Text = FilterModule.DEFAULT_FREQUENCY.ToString();
            _tbxLacunarity.Text = FilterModule.DEFAULT_LACUNARITY.ToString();
            _tbxOffset.Text = FilterModule.DEFAULT_OFFSET.ToString();
            _tbxExponent.Text = FilterModule.DEFAULT_SPECTRAL_EXPONENT.ToString();
            _tbxGain.Text = FilterModule.DEFAULT_GAIN.ToString();
            _nstpOctave.Value = (decimal) FilterModule.DEFAULT_OCTAVE_COUNT;

            // Render
            _cbxGradient.SelectedItem = "Grayscale";
            _cbxProjection.SelectedItem = "Planar";
            _cbxSize.SelectedItem = "256 x 256";
            _chkbx.Checked = true;

            // Progress
            _prbarRenderProgression.Value = 0;
            _lblProgressPercent.Text = "";
            _prbarRenderProgression.Visible = false;
            _lblProgressPercent.Visible = false;
        }

        /// <summary>
        /// 
        /// </summary>
        protected void GenerateNoise()
        {
            EnabledInterface(false);

            // Parse input ------------------------------------------------------------------------------------
            int seed = ParseInt(_tbxSeed.Text, PrimitiveModule.DefaultSeed);
            double frequency = ParseDouble(_tbxFrequency.Text, FilterModule.DEFAULT_FREQUENCY);
            double lacunarity = ParseDouble(_tbxLacunarity.Text, FilterModule.DEFAULT_LACUNARITY);
            double gain = ParseDouble(_tbxGain.Text, FilterModule.DEFAULT_GAIN);
            double offset = ParseDouble(_tbxOffset.Text, FilterModule.DEFAULT_OFFSET);
            double exponent = ParseDouble(_tbxExponent.Text, FilterModule.DEFAULT_SPECTRAL_EXPONENT);
            var octaveCount = (int) _nstpOctave.Value;
            bool seamless = _chkbx.Checked;

            GradientColor gradient = GradientColors.Grayscale;
            NoiseQuality quality = PrimitiveModule.DefaultQuality;
            var primitive = NoisePrimitive.ImprovedPerlin;
            var filter = NoiseFilter.SumFractal;

            try
            {
                quality = (NoiseQuality) Enum.Parse(typeof (NoiseQuality), _cbxQuality.Text);
            }
            catch
            {
                MessageBox.Show(
                    String.Format("Unknown quality '{0}'", _cbxQuality.Text),
                    "Libnoise Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                    );

                EnabledInterface(true);

                return;
            }

            try
            {
                primitive = (NoisePrimitive) Enum.Parse(typeof (NoisePrimitive), _cbxPrimitive.Text);
            }
            catch
            {
                MessageBox.Show(
                    String.Format("Unknown primitive '{0}'", _cbxPrimitive.Text),
                    "Libnoise Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                    );

                EnabledInterface(true);

                return;
            }

            try
            {
                filter = (NoiseFilter) Enum.Parse(typeof (NoiseFilter), _cbxFilter.Text);
            }
            catch
            {
                MessageBox.Show(
                    String.Format("Unknown filter '{0}'", _cbxFilter.Text),
                    "Libnoise Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                    );

                EnabledInterface(true);

                return;
            }

            switch (_cbxGradient.Text)
            {
                case "Grayscale":
                    gradient = GradientColors.Grayscale;
                    break;

                case "Terrain":
                    gradient = GradientColors.Terrain;
                    break;
            }

            // Create module tree ------------------------------------------------------------------------------------

            PrimitiveModule pModule = null;

            switch (primitive)
            {
                case NoisePrimitive.Constant:
                    pModule = new Constant(offset);
                    break;

                case NoisePrimitive.Cylinders:
                    pModule = new Cylinders(offset);
                    seamless = false;
                    break;

                case NoisePrimitive.Spheres:
                    pModule = new Spheres(offset);
                    seamless = false;
                    break;

                case NoisePrimitive.BevinsGradient:
                    pModule = new BevinsGradient();
                    break;

                case NoisePrimitive.BevinsValue:
                    pModule = new BevinsValue();
                    break;

                case NoisePrimitive.ImprovedPerlin:
                    pModule = new ImprovedPerlin();
                    break;

                case NoisePrimitive.SimplexPerlin:
                    pModule = new SimplexPerlin();
                    break;
            }

            pModule.Quality = quality;
            pModule.Seed = seed;

            FilterModule fModule = null;
            ScaleBias scale = null;

            switch (filter)
            {
                case NoiseFilter.Pipe:
                    fModule = new Pipe();
                    break;

                case NoiseFilter.SumFractal:
                    fModule = new SumFractal();
                    break;

                case NoiseFilter.SinFractal:
                    fModule = new SinFractal();
                    break;

                case NoiseFilter.MultiFractal:
                    fModule = new MultiFractal();
                    // Used to show the difference with our gradient color (-1 + 1)
                    scale = new ScaleBias(fModule, 1, -0.8);
                    break;

                case NoiseFilter.Billow:
                    fModule = new Billow();
                    ((Billow) fModule).Bias = -0.2;
                    ((Billow) fModule).Scale = 2;
                    break;

                case NoiseFilter.HeterogeneousMultiFractal:
                    fModule = new HeterogeneousMultiFractal();
                    // Used to show the difference with our gradient color (-1 + 1)
                    scale = new ScaleBias(fModule, -1, 2);
                    break;

                case NoiseFilter.HybridMultiFractal:
                    fModule = new HybridMultiFractal();
                    // Used to show the difference with our gradient color (-1 + 1)
                    scale = new ScaleBias(fModule, 0.7, -2);
                    break;

                case NoiseFilter.RidgedMultiFractal:
                    fModule = new RidgedMultiFractal();
                    // Used to show the difference with our gradient color (-1 + 1)
                    scale = new ScaleBias(fModule, 0.9, -1.25);
                    break;

                case NoiseFilter.Voronoi:
                    fModule = new Voronoi();
                    break;
            }

            fModule.Frequency = frequency;
            fModule.Lacunarity = lacunarity;
            fModule.OctaveCount = octaveCount;
            fModule.Offset = offset;
            fModule.Offset = offset;
            fModule.Gain = gain;
            fModule.Primitive3D = (IModule3D) pModule;

            IModule3D finalModule;

            if (scale == null)
                finalModule = (IModule3D) fModule;
            else
                finalModule = scale;

            NoiseMapBuilder projection;

            switch (_cbxProjection.Text)
            {
                case "Spherical":
                    projection = new NoiseMapBuilderSphere();
                    ((NoiseMapBuilderSphere) projection).SetBounds(-90, 90, -180, 180); // degrees
                    break;

                case "Cylindrical":
                    projection = new NoiseMapBuilderCylinder();
                    ((NoiseMapBuilderCylinder) projection).SetBounds(-180, 180, -10, 10);
                    break;

                case "Planar":
                default:
                    double bound = 2;
                    projection = new NoiseMapBuilderPlane(bound, bound*2, bound, bound*2, seamless);
                    //projection = new NoiseMapBuilderPlane(-bound, bound, -bound, bound, seamless);
                    //projection = new NoiseMapBuilderPlane(0, bound, 0, bound, seamless);
                    break;
            }

            int width = 0;
            int height = 0;

            switch (_cbxSize.Text)
            {
                case "256 x 256":
                    width = 256;
                    height = 256;
                    break;

                case "512 x 512":
                    width = 512;
                    height = 512;
                    break;

                case "1024 x 1024":
                    width = 1024;
                    height = 1024;
                    break;

                case "256 x 128":
                    width = 256;
                    height = 128;
                    break;

                case "512 x 256":
                    width = 512;
                    height = 256;
                    break;

                case "1024 x 512":
                    width = 1024;
                    height = 512;
                    break;

                case "2048 x 1024":
                    width = 2048;
                    height = 1024;
                    break;
                default:

                case "128 x 128":
                    width = 128;
                    height = 128;
                    break;
            }

            // ------------------------------------------------------------------------------------------------
            // 0 - Initializing
            _prbarRenderProgression.Visible = true;
            _lblProgressPercent.Visible = true;
            _prbarRenderProgression.Value = 0;
            ;
            _lblProgressPercent.Text = "";

            _lblLog.Text = String.Format("Create a {0} image with a {1} projection\n", _cbxSize.Text,
                _cbxProjection.Text);

            var watchDog = new Stopwatch();
            TimeSpan ts;
            double elaspedTime = 0;

            //
            // ------------------------------------------------------------------------------------------------
            // 1 - Build the noise map
            watchDog.Reset();

            _prbarRenderProgression.Value = 0;
            _lblLog.Text += "Building noise map ... ";

            var noiseMap = new NoiseMap();

            /* 
			// ShapeFilter test
			Bitmap bmpShape = new Bitmap("smileyShape.bmp");
			BitmapAdaptater bmShapeAdaptater = new BitmapAdaptater(bmpShape);

			ShapeFilter shapeFilter = new ShapeFilter();
			shapeFilter.Shape = bmShapeAdaptater;

			projection.Filter = shapeFilter;
			*/

            projection.SetSize(width, height);
            projection.SourceModule = finalModule;
            projection.NoiseMap = noiseMap;
            projection.CallBack = delegate(int line)
            {
                line++;

                watchDog.Stop();

                //Process message
                Application.DoEvents();

                _prbarRenderProgression.Value = (line*100/height);
                _lblProgressPercent.Text = String.Format("{0} % - {1} line(s)", _prbarRenderProgression.Value, line);

                watchDog.Start();
            };

            watchDog.Start();
            projection.Build();
            watchDog.Stop();

            ts = watchDog.Elapsed;
            elaspedTime += ts.TotalMilliseconds;

            _lblLog.Text += String.Format("{0:00}:{1:00} {2:00},{3:0000}\n",
                ts.Hours, ts.Minutes,
                ts.Seconds, ts.Milliseconds*10
                );

            // ------------------------------------------------------------------------------------------------
            // 2 - Render image
            // Create a renderer, BitmapAdaptater create a System.Drawing.Bitmap on the fly
            watchDog.Reset();
            _prbarRenderProgression.Value = 0;
            _lblLog.Text += "Rendering image ... ";

            var renderer = new ImageRenderer();
            renderer.NoiseMap = noiseMap;
            renderer.Gradient = gradient;
            renderer.LightBrightness = 2;
            renderer.LightContrast = 8;
            //renderer.LightEnabled = true;

            // Libnoise image struct strategy
            //Graphics.Tools.Noise.Renderer.Image image = new Graphics.Tools.Noise.Renderer.Image();
            //renderer.Image = image;

            // Dotnet Bitmap Strategy
            var bmpAdaptater = new BitmapAdaptater(width, height);
            renderer.Image = bmpAdaptater;

            renderer.CallBack = delegate(int line)
            {
                line++;

                watchDog.Stop();

                //Process message
                Application.DoEvents();

                _prbarRenderProgression.Value = (line*100/height);
                _lblProgressPercent.Text = String.Format("{0} % - {1} line(s)", _prbarRenderProgression.Value, line);

                watchDog.Start();
            };

            // Render the texture.
            watchDog.Start();
            renderer.Render();
            watchDog.Stop();

            ts = watchDog.Elapsed;
            elaspedTime += ts.TotalMilliseconds;

            _lblLog.Text += String.Format("{0:00}:{1:00} {2:00},{3:0000}\n",
                ts.Hours, ts.Minutes,
                ts.Seconds, ts.Milliseconds*10
                );

            //----------------------------------------
            // Normalmap rendering test
            //
            /*
			BitmapAdaptater nmapAdaptater = new BitmapAdaptater(width, height);
			NormalMapRenderer nmap = new NormalMapRenderer();
			nmap.Image = nmapAdaptater;
			nmap.BumpHeight = 30.0;
			nmap.NoiseMap = noiseMap;
			nmap.Render();
			nmapAdaptater.Bitmap.Save("normalMap.png", ImageFormat.Png);
			*/
            //----------------------------------------

            /*
			Heightmap8 heightmap8 = new Heightmap8();
			Heightmap8Renderer heightmapRenderer = new Heightmap8Renderer();
			heightmapRenderer.Heightmap = heightmap8;
			*/

            /*
			Heightmap16 heightmap16 = new Heightmap16();
			Heightmap16Renderer heightmapRenderer = new Heightmap16Renderer();
			heightmapRenderer.Heightmap = heightmap16;
			*/

            /*
			Heightmap32 heightmap32 = new Heightmap32();
			Heightmap32Renderer heightmapRenderer = new Heightmap32Renderer();
			heightmapRenderer.Heightmap = heightmap32;
			*/

            /*
			heightmapRenderer.NoiseMap = noiseMap;
			heightmapRenderer.ExactFit();
			heightmapRenderer.Render();
			*/

            /*
			Heightmap16RawWriter rawWriter = new Heightmap16RawWriter();
			rawWriter.Heightmap = heightmap16;
			rawWriter.Filename = "heightmap16.raw";
			rawWriter.WriteFile();
			*/

            // ------------------------------------------------------------------------------------------------
            // 3 - Painting

            // Save the file
            //bmpAdaptater.Bitmap.Save("rendered.png",ImageFormat.Png);
            _imageRendered.Width = width;
            _imageRendered.Height = height;

            //_imageRendered.Image = _bitmap;
            _imageRendered.Image = bmpAdaptater.Bitmap;

            if (_imageRendered.Width > _panImageViewport.Width)
                _imageRendered.Left = 0;
            else
                _imageRendered.Left = (_panImageViewport.Width - _imageRendered.Width)/2;

            if (_imageRendered.Height > _panImageViewport.Height)
                _imageRendered.Top = 0;
            else
                _imageRendered.Top = (_panImageViewport.Height - _imageRendered.Height)/2;

            if (_imageRendered.Width > _panImageViewport.Width || _imageRendered.Height > _panImageViewport.Height)
            {
                _imageRendered.Anchor = (AnchorStyles.Left | AnchorStyles.Top);
                _panImageViewport.AutoScroll = true;
            }
            else
                _panImageViewport.AutoScroll = false;

            // ----------------------------------------------------------------

            ts = TimeSpan.FromMilliseconds(elaspedTime);

            // Format and display the TimeSpan value.
            _lblLog.Text += String.Format("Duration : {0:00}:{1:00} {2:00},{3:0000}\n",
                ts.Hours, ts.Minutes,
                ts.Seconds, ts.Milliseconds*10
                );

            EnabledInterface(true);

            _prbarRenderProgression.Value = 0;
            _lblProgressPercent.Text = "";
            _prbarRenderProgression.Visible = false;
            _lblProgressPercent.Visible = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enabled"></param>
        protected void EnabledInterface(bool enabled)
        {
            _cbxGradient.Enabled = enabled;
            _cbxPrimitive.Enabled = enabled;
            _cbxFilter.Enabled = enabled;
            _cbxProjection.Enabled = enabled;
            _cbxQuality.Enabled = enabled;
            _cbxSize.Enabled = enabled;
            _tbxFrequency.Enabled = enabled;
            _tbxLacunarity.Enabled = enabled;
            _tbxOffset.Enabled = enabled;
            _tbxSeed.Enabled = enabled;
            _tbxGain.Enabled = enabled;
            _tbxOffset.Enabled = enabled;
            _tbxExponent.Enabled = enabled;
            _nstpOctave.Enabled = enabled;
            _btnStart.Enabled = enabled;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected int ParseInt(string value, int defaultValue)
        {
            try
            {
                return Int32.Parse(
                    value,
                    NumberStyles.Number,
                    new CultureInfo("en-US", false).NumberFormat
                    );
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected double ParseDouble(string value, double defaultValue)
        {
            value = value.Replace(',', '.');

            try
            {
                return Double.Parse(
                    value,
                    NumberStyles.Number,
                    new CultureInfo("en-US", false).NumberFormat
                    );
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="minRange"></param>
        /// <param name="maxRange"></param>
        protected void TextFilterNumeric_keyPress(TextBox sender, KeyPressEventArgs e, int minRange, int maxRange)
        {
            String allowed = "0123456789";
            int value;

            try
            {
                if (allowed.IndexOf(e.KeyChar) >= 0)
                {
                    int selStart = sender.SelectionStart;
                    string before = sender.Text.Substring(0, selStart);
                    string after = sender.Text.Substring(before.Length);

                    value = Convert.ToInt32(string.Concat(before, e.KeyChar, after));

                    if (value >= minRange && value <= maxRange)
                    {
                        sender.Text = value.ToString();
                        sender.SelectionStart = before.Length + 1;
                    }

                    e.Handled = true;
                }
                else
                {
                    // Lets the default back space
                    if (e.KeyChar != (char) Keys.Back)
                        e.Handled = true;
                }
            }
            catch
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="minRange"></param>
        /// <param name="maxRange"></param>
        protected void TextFilterNumeric_keyPress(TextBox sender, KeyPressEventArgs e, double minRange, double maxRange)
        {
            String allowed = "0123456789.,";
            double value;

            try
            {
                if (allowed.IndexOf(e.KeyChar) >= 0)
                {
                    if (e.KeyChar == ',')
                        e.KeyChar = '.';

                    int selStart = sender.SelectionStart;
                    string before = sender.Text.Substring(0, selStart);
                    string after = sender.Text.Substring(before.Length);
                    string newText = string.Concat(before, e.KeyChar, after);

                    value = Double.Parse(
                        newText,
                        NumberStyles.Number,
                        new CultureInfo("en-US", false).NumberFormat
                        );

                    if (value >= minRange && value <= maxRange)
                    {
                        sender.Text = newText;
                        sender.SelectionStart = before.Length + 1;
                    }

                    e.Handled = true;
                }
                else
                {
                    // Lets the default back space
                    if (e.KeyChar != (char) Keys.Back)
                        e.Handled = true;
                }
            }
            catch
            {
                e.Handled = true;
            }
        }

        private void _tbxSeed_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextFilterNumeric_keyPress((TextBox) sender, e, 0, int.MaxValue);
        }

        private void _tbxFrequency_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextFilterNumeric_keyPress((TextBox) sender, e, 0, 10.0);
        }

        private void _tbxLacunarity_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextFilterNumeric_keyPress((TextBox) sender, e, 0, 10.0);
        }

        private void _tbxGain_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextFilterNumeric_keyPress((TextBox) sender, e, 0, 10.0);
        }

        private void _tbxOffset_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextFilterNumeric_keyPress((TextBox) sender, e, 0, 10.0);
        }

        private void _tbxExponent_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextFilterNumeric_keyPress((TextBox) sender, e, 0, 10.0);
        }

        private void _btnStart_Click(object sender, EventArgs e)
        {
            GenerateNoise();
        }
    }
}
