﻿using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuantSA.Shared.Dates;
using QuantSA.Shared.Primitives;
using QuantSA.TestUtils;
using QuantSA.Valuation.Models.Rates;

namespace QuantSA.Valuation.Test
{
    [TestClass]
    public class IRSwapEPETest
    {
        [TestMethod]
        public void TestCoordinatorEPESwap()
        {
            // Make the swap
            var rate = 0.07;
            var payFixed = true;
            double notional = 1000000;
            var startDate = new Date(2016, 9, 17);
            var tenor = Tenor.FromYears(5);
            var swap = TestHelpers.CreateZARSwap(rate, payFixed, notional, startDate, tenor, TestHelpers.Jibar3M);

            // Set up the model
            var valueDate = new Date(2016, 9, 17);
            var a = 0.05;
            var vol = 0.005;
            var flatCurveRate = 0.07;
            var hullWiteSim = new HullWhite1F(TestHelpers.ZAR, a, vol, flatCurveRate, flatCurveRate);
            hullWiteSim.AddForecast(TestHelpers.Jibar3M);
            var coordinator = new Coordinator(hullWiteSim, new List<Simulator>(), 5000);

            var date = valueDate;
            var endDate = valueDate.AddTenor(tenor);
            var fwdValueDates = new List<Date>();
            while (date < endDate)
            {
                fwdValueDates.Add(date);
                date = date.AddTenor(Tenor.FromDays(10));
            }

            var epe = coordinator.EPE(new IProduct[] {swap}, valueDate, fwdValueDates.ToArray());
            //Debug.WriteToFile(@"c:\dev\temp\epe_rate08_vol005.csv", epe);

            Assert.AreEqual(2392, epe[0], 100.0);
            Assert.AreEqual(6560, epe[90], 100.0);
            Assert.AreEqual(712, epe[182], 30);
        }
    }
}