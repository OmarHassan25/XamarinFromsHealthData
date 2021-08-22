using System;
namespace TestHealthData
{
    public class DateTimeUtils
    {
        private static DateTime StartingDateTime { get { return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); } }

        public static long DateTimeToMilliSeconds(DateTime dt)
        {
            return (long)dt.ToUniversalTime().Subtract(StartingDateTime).TotalMilliseconds;
        }

        public static DateTime ConvertToLocalDate(long timeInMilliseconds)
        {
            double timeInTicks = double.Parse(timeInMilliseconds.ToString());
            TimeSpan dateTimeSpan = TimeSpan.FromMilliseconds(timeInTicks);
            DateTime dateAfterEpoch = StartingDateTime + dateTimeSpan;
            DateTime dateInLocalTimeFormat = dateAfterEpoch.ToLocalTime();
            return dateInLocalTimeFormat;
        }
    }
}
