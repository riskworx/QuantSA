﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantSA;

namespace QuantSA
{
    /// <summary>
    /// An object to describe a floating rate index such as 3 Month Jibar.
    /// </summary>
    public class FloatingIndex : MarketObservable
    {
        private Currency currency;
        private string name;
        public Tenor tenor { get; private set; }        

        private FloatingIndex(Currency currency, string name, Tenor tenor)
        {
            this.currency = currency;
            this.name = name;
            this.tenor = tenor;
        }

        public override string ToString()
        {
            return currency.ToString() + ":" + name.ToUpper() + ":" + tenor.ToString();
           
        }

        #region Stored Indices
        public static FloatingIndex JIBAR3M = new FloatingIndex(Currency.ZAR, "Jibar", Tenor.Months(3));
        public static FloatingIndex JIBAR6M = new FloatingIndex(Currency.ZAR, "Jibar", Tenor.Months(6));
        public static FloatingIndex LIBOR3M = new FloatingIndex(Currency.USD, "Libor", Tenor.Months(3));
        #endregion
    }
}
