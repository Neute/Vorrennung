using System;
using System.Collections.Generic;

namespace Vorrennung.Daniel
{
	public class ValueAssistant
	{
		
		public static double findGoodSilenceThreshold (List<double> volumeDistribution)
		{
			int width = 20, partit = 10;
			double slopeT = -0.0001, vT = 0.2;
			bool firstHeap = false;

			double[] av = new double[volumeDistribution.Count/partit];
			for (int i = 0; i < av.Length; i++)
				av [i] = 0;

			for (int i = 0; i < volumeDistribution.Count; i++)
				for (int j = -(width / 2); j < (width / 2); j++)
					if ((i/partit) + j >= 0 && (i/partit) + j < av.Length)
						av [(i/partit) + j] += volumeDistribution [i];


			double slope = 0, oSlope = 0, curvature = 0;

			for (int i = 0; i < av.Length; i++) {
				av [i] /= width*partit;
				if (i > 1) {
					oSlope = slope;
					slope = av [i] - av [i - 1];
					curvature = slope - oSlope;
					if (av [i] > vT)
						firstHeap = true;
					if (firstHeap && slope > slopeT && curvature > 0 && av [i] < vT)
						return ((double)i / av.Length);
				}
			}
			return 1;
		}

	}
}

