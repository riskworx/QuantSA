﻿using MonteCarlo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantSA
{
    public class IRSwap : Product, IProvidesResultStore
    {
        // Product specs
        double payFixed; // -1 for payFixed, 1 for receive fixed
        Date[] indexDates;
        Date[] payDates;
        MarketObservable index;
        double[] spreads;
        double[] accrualFractions;
        double[] notionals;
        double fixedRate;
        Currency ccy;
        // Product state
        Date valueDate;
        double[] indexValues;
        
        /// <summary>
        /// Constructor for ZAR market standard, fixed for float 3m Jibar swap.
        /// </summary>
        /// <param name="rate">The fixed rate paid or received</param>
        /// <param name="payFixed">Is the fixed rate paid?</param>
        /// <param name="notional">Flat notional for all dates.</param>
        /// <param name="startDate">First reset date of swap</param>
        /// <param name="tenor">Tenor of swap, must be a whole number of years.</param>
        /// <returns></returns>
        public static IRSwap CreateZARSwap(double rate, bool payFixed, double notional, Date startDate, Tenor tenor)
        {
            IRSwap newSwap = new IRSwap();
            int quarters = tenor.years * 4;
            newSwap.payFixed = payFixed ? -1 : 1;
            newSwap.indexDates = new Date[quarters];
            newSwap.payDates = new Date[quarters];
            newSwap.index = FloatingIndex.JIBAR3M;
            newSwap.spreads = new double[quarters]; ;
            newSwap.accrualFractions = new double[quarters]; ;
            newSwap.notionals = new double[quarters];
            newSwap.fixedRate = rate;
            newSwap.ccy = Currency.ZAR;
            newSwap.indexValues = new double[quarters];

            Date date1 = new Date(startDate);
            Date date2;

            for (int i = 0; i<quarters; i++)
            {
                date2 = startDate.AddMonths(3 * (i+1));
                newSwap.indexDates[i] = new Date(date1);
                newSwap.payDates[i] = new Date(date2);
                newSwap.spreads[i] = 0.0;
                newSwap.accrualFractions[i] = (date2- date1)/365.0;
                newSwap.notionals[i] = notional;
                date1 = new Date(date2);
            }
            return newSwap;
        }

        /// <summary>
        /// Constructor for ZAR market standard, fixed for float 3m Jibar swap.
        /// </summary>
        /// <param name="rate">The fixed rate paid or received</param>
        /// <param name="payFixed">Is the fixed rate paid?</param>
        /// <param name="notional">Flat notional for all dates.</param>
        /// <param name="startDate">First reset date of swap</param>
        /// <param name="maturityTenor">Tenor of swap, must be a whole number of years.</param>
        /// <param name="paymentTenor">Tenor of the floating and fixed rate payments.</param>
        /// <returns></returns>
        public static IRSwap CreateZARSwapWithFreq(double rate, bool payFixed, double notional, Date startDate, Tenor maturityTenor, Tenor paymentTenor)
        {
            IRSwap newSwap = new IRSwap();
            int nPeriods;
            if (paymentTenor == Tenor.Months(3))
            {
                nPeriods = maturityTenor.years * 4;
                newSwap.index = FloatingIndex.JIBAR3M;
            }
            else if (paymentTenor == Tenor.Months(6))
            {
                nPeriods = maturityTenor.years * 2;
                newSwap.index = FloatingIndex.JIBAR6M;
            }
            else
            { throw new ArgumentException("This swap only handles paymentTenors of '3M' and '6M'"); }
                
            newSwap.payFixed = payFixed ? -1 : 1;
            newSwap.indexDates = new Date[nPeriods];
            newSwap.payDates = new Date[nPeriods];
            newSwap.spreads = new double[nPeriods]; ;
            newSwap.accrualFractions = new double[nPeriods]; ;
            newSwap.notionals = new double[nPeriods];
            newSwap.fixedRate = rate;
            newSwap.ccy = Currency.ZAR;
            newSwap.indexValues = new double[nPeriods];

            Date date1 = new Date(startDate);
            Date date2;

            for (int i = 0; i < nPeriods; i++)
            {
                date2 = startDate.AddMonths(paymentTenor.months * (i + 1));
                newSwap.indexDates[i] = new Date(date1);
                newSwap.payDates[i] = new Date(date2);
                newSwap.spreads[i] = 0.0;
                newSwap.accrualFractions[i] = (date2 - date1) / 365.0;
                newSwap.notionals[i] = notional;
                date1 = new Date(date2);
            }
            return newSwap;
        }

        /// <summary>
        /// Returns the single floating rate index underlying this swap.
        /// </summary>
        /// <returns></returns>
        public FloatingIndex GetFloatingIndex()
        {
            return (FloatingIndex)index;
        }


        /// <summary>
        /// Set the date after which all cashflows will be required.
        /// </summary>
        /// <param name="valueDate"></param>
        public override void SetValueDate(Date valueDate)
        {
            // TODO: At this point all the dates in the future can be found to save some later looping.
            this.valueDate = valueDate;
        }

        /// <summary>
        /// Not used by this product.
        /// </summary>
        public override void Reset()
        {
            // All floating rates will just be overwritten.  No need to do anything here.
        }
        
        /// <summary>
        /// A swap only needs a single floating rate index.
        /// </summary>
        /// <returns></returns>
        public override List<MarketObservable> GetRequiredIndices()
        {
            return new List<MarketObservable>() { index };
        }

        /// <summary>
        /// The floating rate fixing dates that correspond to payment dates strctly after the value date.
        /// </summary>
        /// <param name="index">Will be the same index as returned by <see cref="GetRequiredIndices"/>.</param>
        /// <returns></returns>
        public override List<Date> GetRequiredIndexDates(MarketObservable index)
        {
            List<Date> requiredDates = new List<Date>();
            for (int i = 0; i < payDates.Length; i++)
            {
                if (payDates[i] > valueDate)
                {
                    requiredDates.Add(indexDates[i]);
                }
            }
            return requiredDates;
        }

        /// <summary>
        /// Sets the values of the floating rates at all reset dates corresponding to payment dates after the value dates.
        /// </summary>
        /// <param name="index">Must only be called for the single index underlying the floating rate of the swap.</param>
        /// <param name="indexValues">An array of values the same length as the dates returned in <see cref="GetRequiredIndexDates(MarketObservable)"/>.</param>
        public override void SetIndexValues(MarketObservable index, double[] indexValues)
        {
            int indexCounter = 0;
            for (int i = 0; i < payDates.Length; i++)
            {
                if (payDates[i] > valueDate)
                {
                    this.indexValues[i] = indexValues[indexCounter];
                    indexCounter++;
                }
            }
        }

        /// <summary>
        /// The actual implementation of the contract cashflows.
        /// </summary>
        /// <returns></returns>
        public override List<Cashflow> GetCFs()
        {
            List<Cashflow> cfs = new List<Cashflow>();
            for (int i = 0; i < payDates.Length; i++)
            {
                if (payDates[i] > valueDate)
                {
                    double fixedAmount = payFixed * notionals[i] * accrualFractions[i] * fixedRate;
                    double floatingAmount = -payFixed * notionals[i] * accrualFractions[i] * (indexValues[i] + spreads[i]);
                    cfs.Add(new Cashflow(payDates[i], fixedAmount, ccy));
                    cfs.Add(new Cashflow(payDates[i], floatingAmount, ccy));
                }
            }
            return cfs;
        }

        public ResultStore GetResultStore()
        {
            ResultStore swapDetails = new ResultStore();
            swapDetails.Add("payFixed", payFixed);
            swapDetails.Add("indexDates", indexDates);
            swapDetails.Add("payDates", payDates);
            swapDetails.Add("index", index.ToString());
            swapDetails.Add("spreads", spreads);
            swapDetails.Add("accrualFractions", accrualFractions);
            swapDetails.Add("notionals", notionals);
            swapDetails.Add("fixedRate", fixedRate);
            
            return swapDetails;
        }
    }
}

