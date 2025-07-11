﻿using System;
using System.Collections.Generic;
using Accord.Math;
using Accord.Math.Random;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantSA.Core.CurvesAndSurfaces;
using QuantSA.Core.MarketData;
using QuantSA.Core.Primitives;
using QuantSA.Shared.Dates;
using QuantSA.Shared.Debug;
using QuantSA.Shared.MarketData;
using QuantSA.TestUtils;
using QuantSA.Valuation.Models.Rates;

namespace QuantSA.Valuation.Test
{
    [TestClass]
    public class SpeedTest
    {
        private Product[] GetListOfSwaps()
        {
            var N = 50000;
            var anchorDate = new Date(2016, 11, 21);
            var rate = 0.08;
            var payFixed = true;
            double notional = 1000000;

            var swapDist = new[,]
            {
                {0.171}, {0.148}, {0.101}, {0.094}, {0.108}, {0.056}, {0.041}, {0.049}, {0.047}, {0.056}, {0.013},
                {0.013}, {0.010}, {0.011}, {0.011}, {0.004}, {0.003}, {0.005}, {0.007}, {0.006}, {0.004}, {0.004},
                {0.007}, {0.005}, {0.006}, {0.006}, {0.003}, {0.003}, {0.002}, {0.005}
            };
            IRandomNumberGenerator<double> generator1 = new ZigguratUniformGenerator(0, 1);
            IRandomNumberGenerator<double> generator365 = new ZigguratUniformGenerator(1, 365);
            var cumSum = swapDist.CumulativeSum(1);

            var allSwaps = new Product[N];
            for (var swapNum = 0; swapNum < N; swapNum++)
            {
                var x = generator1.Generate();
                var years = 0;
                while (years < cumSum.GetLength(0) && x > cumSum[years, 0]) years++;
                var days = (int) Math.Round(generator365.Generate());
                var endDate = anchorDate.AddTenor(new Tenor(days, 0, 0, years));
                var startDate = endDate.AddTenor(Tenor.FromYears(-years - 1));
                allSwaps[swapNum] = TestHelpers.CreateZARSwap(rate, payFixed, notional, startDate, Tenor.FromYears(years + 1), TestHelpers.Jibar3M);
            }

            return allSwaps;
        }


        [Ignore]
        [TestMethod]
        public void TestManySwaps()
        {
            Debug.StartTimer();
            var allSwaps = GetListOfSwaps();

            Debug.WriteLine("Create swaps took: " + Debug.ElapsedTime());

            // Set up the model
            var valueDate = new Date(2016, 11, 21);
            Date[] datesLong =
            {
                new Date(2016, 11, 21), new Date(2018, 11, 21), new Date(2020, 11, 21), new Date(2022, 11, 21),
                new Date(2024, 11, 21), new Date(2047, 11, 21)
            };
            double[] ratesLong = {0.07, 0.071, 0.072, 0.073, 0.074, 0.08};
            IDiscountingSource discountCurve = new DatesAndRates(TestHelpers.ZAR, valueDate, datesLong, ratesLong);
            IFloatingRateSource forecastCurve = new ForecastCurveFromDiscount(discountCurve, TestHelpers.Jibar3M,
                new FloatingRateFixingCurve1Rate(valueDate, 0.07, TestHelpers.Jibar3M));
            var curveSim = new DeterministicCurves(discountCurve);
            curveSim.AddRateForecast(forecastCurve);
            var coordinator = new Coordinator(curveSim, new List<Simulator>(), 1);

            // Run the valuation
            Debug.StartTimer();
            var value = coordinator.Value(allSwaps, valueDate);
            Debug.WriteLine("Value took: " + Debug.ElapsedTime());
        }
    }
}