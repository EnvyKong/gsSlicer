﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using g3;

namespace gs
{
	public class LayersDetector
	{
		public PathSet Paths;

		// layers with fewer points than this are filtered out
		public int MinLayerCount = 4;
		public int RoundLayerToPrecision = 3;

		public Dictionary<double, int> LayersCounts;
		public List<double> LayerZ;

		public double EstimatedLayerHeight;

		public LayersDetector(PathSet paths)
		{
			Paths = paths;
			Compute();
		}



		public int Layers {
			get { return LayerZ.Count; }
		}

		public double GetLayerZ(int iLayer) {
			return LayerZ[iLayer];
		}

		public Interval1d GetLayerZInterval(int iLayer) {
			if (Layers == 0)
				return Interval1d.Zero;

			iLayer = MathUtil.Clamp(iLayer, 0, Layers - 1);

			double low = LayerZ[iLayer] - EstimatedLayerHeight * 0.45f;
			double high = LayerZ[iLayer] + EstimatedLayerHeight * 0.45f;

			//double low = (iLayer <= 0) ? LayerZ[iLayer] :
			//	(LayerZ[iLayer] + LayerZ[iLayer - 1]) * 0.5;
			//double high = (iLayer == Layers-1) ? LayerZ[iLayer] :
				//(LayerZ[iLayer] + LayerZ[iLayer + 1]) * 0.5;
			
			return new Interval1d(low, high);
		}

        public int GetLayerIndex(double fZ)
        {
            int i = 0;
            while (i < LayerZ.Count && LayerZ[i] < fZ)
                i++;
            return MathUtil.Clamp(i-1, 0, Layers - 1);
        }


		public void Compute() 
		{
			LayersCounts = new Dictionary<double, int>();

			Action<IPath> processPathF = (path) => {
				if ( path.HasFinitePositions ) {
					foreach (Vector3d v in path.AllPositionsItr())
						accumulate(v);
				}
			};
			Action<IPathSet> processPathsF = null;
			processPathsF = (paths) => {
				foreach (IPath path in paths) {
					if (path is IPathSet)
						processPathsF(path as IPathSet);
					else
						processPathF(path);
				}
			};

			processPathsF(Paths);

			List<double> erase = new List<double>();
			foreach ( var v in LayersCounts ) {
				if (v.Value < MinLayerCount)
					erase.Add(v.Key);
			}
			foreach (var e in erase)
				LayersCounts.Remove(e);

			LayerZ = new List<double>(LayersCounts.Keys);
			LayerZ.Sort();

			// estimate layer height
			Dictionary<double, int> LayerHeights = new Dictionary<double, int>();
			for (int i = 0; i < LayerZ.Count - 1; ++i ) {
				double dz = Math.Round(LayerZ[i + 1] - LayerZ[i], 3);
				if (LayerHeights.ContainsKey(dz) == false)
					LayerHeights[dz] = 0;
				LayerHeights[dz] = LayerHeights[dz] + 1;
			}
			double best_height = 0; int max_count = 0;
			foreach ( var pair in LayerHeights ) {
				if ( pair.Value > max_count ) {
					max_count = pair.Value;
					best_height = pair.Key;
				}
			}
			EstimatedLayerHeight = best_height;

		}


		void accumulate(Vector3d v) {
			if (v.z == GCodeUtil.UnspecifiedValue)
				return;
			double z = Math.Round(v.z, RoundLayerToPrecision);
			int count = 0;
			if ( LayersCounts.TryGetValue(z, out count) ) {
				count++;
				LayersCounts[z] = count;
			} else {
				LayersCounts.Add(z, 1);
			}
		}
	}
}