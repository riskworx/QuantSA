﻿using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantSA.Core.CurvesAndSurfaces;
using QuantSA.Core.MarketData;
using QuantSA.Core.Primitives;
using QuantSA.Core.Products.Rates;
using QuantSA.Shared.Dates;
using QuantSA.Shared.MarketData;
using QuantSA.TestUtils;
using QuantSA.Valuation.Models.Rates;

namespace QuantSA.Valuation.Test
{
    /// <summary>
    /// Tests some FX products.
    /// </summary>
    [TestClass]
    public class FXProductTest
    {
        [TestMethod]
        public void TestFixedLegsZARUSD()
        {
            Date[] cfDates = {new Date(2016, 12, 23), new Date(2017, 03, 23)};

            var legZAR = new FixedLeg(TestHelpers.ZAR, cfDates, new double[] {-16000000, -16000000}, new[] {0.07, 0.07},
                new[] {0.25, 0.25});
            var legUSD = new FixedLeg(TestHelpers.USD, cfDates, new double[] {1000000, 1000000}, new[] {0.01, 0.01},
                new[] {0.25, 0.25});

            // Set up the model
            var valueDate = new Date(2016, 9, 23);
            Date[] dates = {new Date(2016, 9, 23), new Date(2026, 9, 23)};
            double[] rates = {0.0725, 0.0725};
            double[] basisRates = {0.0735, 0.0735};
            double[] usdRates = {0.01, 0.012};
            IDiscountingSource discountCurve = new DatesAndRates(TestHelpers.ZAR, valueDate, dates, rates);
            IDiscountingSource zarBasis = new DatesAndRates(TestHelpers.ZAR, valueDate, dates, basisRates);
            IDiscountingSource usdCurve = new DatesAndRates(TestHelpers.USD, valueDate, dates, usdRates);
            IFloatingRateSource forecastCurve = new ForecastCurve(valueDate, TestHelpers.Jibar3M, dates, rates);
            IFXSource fxSource = new FXForecastCurve(TestHelpers.USDZAR, 13.66, usdCurve, zarBasis);
            var curveSim = new DeterministicCurves(discountCurve);
            curveSim.AddRateForecast(forecastCurve);
            curveSim.AddFXForecast(fxSource);
            var coordinator = new Coordinator(curveSim, new List<Simulator>(), 1);

            // Run the valuation
            var value = coordinator.Value(new Product[] {legZAR, legUSD}, valueDate);
            var refValue = -477027.31; // See GeneralSwapTest.xlsx
            Assert.AreEqual(refValue, value, 0.01);
        }
    }
}