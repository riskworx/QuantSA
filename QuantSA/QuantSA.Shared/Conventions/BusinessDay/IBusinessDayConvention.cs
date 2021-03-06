﻿using QuantSA.Shared.Dates;

namespace QuantSA.Shared.Conventions.BusinessDay
{
    /// <summary>
    /// A business day convention adjusts a date according to a rule and with a provided calendar.
    /// </summary>
    public interface IBusinessDayConvention
    {
        /// <summary>
        /// Adjusts the specified date using the provided calendar.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="calendar">The calendar.</param>
        /// <returns></returns>
        Date Adjust(Date date, Calendar calendar);
    }
}