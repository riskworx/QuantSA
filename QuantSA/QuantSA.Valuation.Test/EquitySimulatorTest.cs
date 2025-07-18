﻿using System;
using System.Collections.Generic;
using Accord.Math;
using Accord.Statistics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using QuantSA.Core.CurvesAndSurfaces;
using QuantSA.Core.Formulae;
using QuantSA.Core.MarketData;
using QuantSA.Core.Primitives;
using QuantSA.Core.Products;
using QuantSA.Core.Products.Equity;
using QuantSA.Shared.Dates;
using QuantSA.Shared.MarketData;
using QuantSA.Shared.MarketObservables;
using QuantSA.Shared.Primitives;
using QuantSA.TestUtils;
using QuantSA.Valuation.Models.Equity;

namespace QuantSA.Valuation.Test
{
    /// <summary>
    /// Sample product for test simulated indices.
    /// </summary>
    public class ProductWithDiviAndFwd : ProductWrapper
    {
        [JsonIgnore] private readonly Date _dealEndDate = new Date(2019, 9, 30);
        [JsonIgnore] private readonly Date _dealStartDate = new Date(2016, 9, 30); // The issue date of the scheme
        [JsonIgnore] private readonly Dividend _dividend = new Dividend(new Share("AAA", TestHelpers.ZAR));
        [JsonIgnore] private readonly FloatRateIndex _jibar = TestHelpers.Jibar3M;
        [JsonIgnore] private readonly double _nShares = 1;
        [JsonIgnore] private readonly Share _share = new Share("AAA", TestHelpers.ZAR);

        public ProductWithDiviAndFwd()
        {
            Init();
        }

        public override List<Cashflow> GetCFs()
        {
            double loanBalance = 40; // Opening balance on startDate
            var spread = 0.035 + 0.02; // 350bp prime Jibar spread + 200bp deal spread

            var periodStartDate = _dealStartDate;
            var periodEndDate = _dealStartDate.AddMonths(3);

            var cfs = new List<Cashflow>();
            // Generate the dividend cashflows.
            while (periodEndDate <= _dealEndDate)
            {
                var observedRate = Get(_jibar, periodStartDate);
                if (loanBalance < 1e-6)
                {
                    cfs.Add(new Cashflow(new Date(periodEndDate), 0, TestHelpers.ZAR));
                }
                else
                {
                    loanBalance *= 1 + (observedRate + spread) * 0.25;
                    var loanReduction = 0.9 * _nShares * Get(_dividend, periodEndDate);
                    if (loanReduction > loanBalance)
                        cfs.Add(new Cashflow(new Date(periodEndDate), loanBalance, TestHelpers.ZAR));
                    else
                        cfs.Add(new Cashflow(new Date(periodEndDate), loanReduction, TestHelpers.ZAR));
                    loanBalance = Math.Max(0, loanBalance - loanReduction);
                }

                periodStartDate = new Date(periodEndDate);
                periodEndDate = periodEndDate.AddMonths(3);
            }

            // Check if the loan is worth more than the shares.  If yes then there is a loss on the loan, 
            // otherwise the loan can be repaid in full.
            var finalPayment = Math.Min(loanBalance, _nShares * Get(_share, _dealEndDate));
            cfs.Add(new Cashflow(new Date(_dealEndDate), finalPayment, TestHelpers.ZAR));

            return cfs;
        }
    }

    /// <summary>
    /// Test class for <see cref="EquitySimulator"/>
    /// </summary>
    [TestClass]
    public class EquitySimulatorTest
    {
        private readonly Date _anchorDate = new Date(2016, 09, 30);

        private readonly double[,] _correlations =
        {
            {1.0, 0.4, 0.5},
            {0.4, 1.0, 0.6},
            {0.5, 0.6, 1.0}
        };

        private readonly double[] _divYields = {0.03, 0.0, 0.0};
        private readonly double[] _prices = {200, 50, 100};

        private readonly Share[] _shares =
            {new Share("ALSI", TestHelpers.ZAR), new Share("AAA", TestHelpers.ZAR), new Share("BBB", TestHelpers.ZAR)};

        private readonly double[] _vols = {0.22, 0.52, 0.4};

        private IDiscountingSource _discountCurve;
        private IFloatingRateSource[] _rateForecastCurves;

        [TestInitialize]
        public void Init()
        {
            _discountCurve = new DatesAndRates(TestHelpers.ZAR, _anchorDate,
                new[] {_anchorDate, _anchorDate.AddMonths(120)},
                new[] {0.07, 0.09});
            _rateForecastCurves = new List<IFloatingRateSource>
            {
                new ForecastCurveFromDiscount(_discountCurve, TestHelpers.Jibar3M,
                    new FloatingRateFixingCurve1Rate(_anchorDate,0.07, TestHelpers.Jibar3M))
            }.ToArray();
        }

        [TestMethod]
        public void TestEquitySimulatorDivis()
        {
            var sim = new EquitySimulator(_shares, _prices, _vols, _divYields, _correlations, _discountCurve,
                _rateForecastCurves);
            var dates = new List<Date> {_anchorDate, _anchorDate.AddMonths(6), _anchorDate.AddMonths(12)};
            var divi = new Dividend(_shares[0]);
            sim.Reset();
            sim.SetRequiredDates(divi, dates);
            sim.SetRequiredDates(_shares[0], dates);
            sim.Prepare(_anchorDate);
            sim.RunSimulation(0);
            var divs = sim.GetIndices(divi, dates);
            var shareprices = sim.GetIndices(_shares[0], dates);
            var fwdRates = sim.GetIndices(TestHelpers.Jibar3M, dates);
            Assert.AreEqual(shareprices[1] * 184.0 / 365 * 0.03, divs[2], 0.01);
            Assert.AreEqual(_rateForecastCurves[0].GetForwardRate(dates[1]), fwdRates[1], 0.0001);
        }


        [TestMethod]
        public void TestEquitySimulatorForwardAndVol()
        {
            var N = 100000;
            var sharePrices = Matrix.Zeros(N, 3);
            var sim = new EquitySimulator(_shares, _prices, _vols, _divYields, _correlations, _discountCurve,
                _rateForecastCurves);
            sim.Reset();
            var simDate = new List<Date> {_anchorDate.AddMonths(120)};
            double dt = (simDate[0] - _anchorDate) / 365.0;
            sim.SetRequiredDates(_shares[0], simDate);
            sim.Prepare(_anchorDate);
            for (var i = 0; i < N; i++)
            {
                sim.RunSimulation(i);
                sharePrices[i, 0] = sim.GetIndices(_shares[0], simDate)[0];
                sharePrices[i, 1] = sim.GetIndices(_shares[1], simDate)[0];
                sharePrices[i, 2] = sim.GetIndices(_shares[2], simDate)[0];
            }

            var mean = sharePrices.GetColumn(0).Mean();
            var refValue = _prices[0] * Math.Exp(-_divYields[0] * dt) / _discountCurve.GetDF(simDate[0]);
            Assert.AreEqual(refValue, mean, 2.0);

            var corr = sharePrices.Log().Correlation();

            Assert.AreEqual(corr[1, 0], 0.4, 0.05);
            Assert.AreEqual(corr[2, 0], 0.5, 0.05);
            Assert.AreEqual(corr[2, 1], 0.6, 0.05);
        }


        [TestMethod]
        public void TestEquitySimulatorProductWithRateForecast()
        {
            Product product = new ProductWithDiviAndFwd();
            // The model
            var sim = new EquitySimulator(_shares, _prices, _vols, _divYields, _correlations, _discountCurve,
                _rateForecastCurves);

            var coordinator = new Coordinator(sim, new List<Simulator>(), 10000);
            var value = coordinator.Value(new[] {product}, _anchorDate);
            Assert.AreEqual(31.6, value, 1.0);
        }

        [TestMethod]
        public void TestEquitySimulatorMultiAssetCall()
        {
            // The model            
            var sim = new EquitySimulator(_shares, _prices, _vols, _divYields, _correlations, _discountCurve,
                new IFloatingRateSource[0]);
            var coordinator = new Coordinator(sim, new List<Simulator>(), 10000);

            // Products
            var exerciseDate = new Date(2017, 08, 28);
            int p;
            double strike;

            // ALSI
            p = 0;
            strike = _prices[p] * 1.05;
            Product call0 = new EuropeanOption(_shares[p], strike, exerciseDate);
            var value0 = coordinator.Value(new[] {call0}, _anchorDate);
            var refValue0 = BlackEtc.BlackScholes(PutOrCall.Call, strike, (exerciseDate - _anchorDate) / 365.0,
                _prices[p],
                _vols[p], 0.07, _divYields[p]);
            Assert.AreEqual(refValue0, value0, refValue0 * 0.05);

            // AAA
            p = 1;
            strike = _prices[p] * 1.05;
            Product call1 = new EuropeanOption(_shares[p], strike, exerciseDate);
            var value1 = coordinator.Value(new[] {call1}, _anchorDate);
            var refValue1 = BlackEtc.BlackScholes(PutOrCall.Call, strike, (exerciseDate - _anchorDate) / 365.0,
                _prices[p],
                _vols[p], 0.07, _divYields[p]);
            Assert.AreEqual(refValue1, value1, refValue1 * 0.05);

            // BBB
            p = 2;
            strike = _prices[p] * 1.05;
            Product call2 = new EuropeanOption(_shares[p], strike, exerciseDate);
            var value2 = coordinator.Value(new[] {call2}, _anchorDate);
            var refValue2 = BlackEtc.BlackScholes(PutOrCall.Call, strike, (exerciseDate - _anchorDate) / 365.0,
                _prices[p],
                _vols[p], 0.07, _divYields[p]);
            Assert.AreEqual(refValue2, value2, refValue2 * 0.05);

            // All at once
            var valueAll = coordinator.Value(new[] {call0, call1, call2}, _anchorDate);
            var refTotal = refValue0 + refValue1 + refValue2;
            Assert.AreEqual(refTotal, valueAll, refTotal * 0.05);
        }
    }
}