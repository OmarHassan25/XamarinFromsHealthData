using System;
namespace TestHealthData
{
    public class HealthDataItem
    {
        public HealthDataType DataType { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Value { get; set; }
        public MeasureUnit MeasureUnit { get; set; }
        public bool HasNoData { get; set; }

        public override string ToString()
        {
            if(HasNoData)
            {
                return string.Format("{0}, {1}\nLast Updated: {2}", DataType, "-", LastUpdated);
            }
            else
                return string.Format("{0}, {1} {2}\nLast Updated: {3}", DataType, Value, MeasureUnit, LastUpdated); 
        }
    }
}
