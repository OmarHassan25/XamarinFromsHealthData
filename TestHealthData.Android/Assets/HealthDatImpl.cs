using System;
using System.Threading.Tasks;
using Foundation;
using HealthKit;
using TestHealthData.iOS;

[assembly: Xamarin.Forms.Dependency(typeof(HealthDatImpl))]
namespace TestHealthData.iOS
{
    public class HealthDatImpl : IHealthData
    {
        private readonly HKHealthStore HealthKitStore = new HKHealthStore();
        readonly NSSet<HKObjectType> DataTypesToRead = new NSSet<HKObjectType>(new HKObjectType[] {

            HKQuantityType.Create(
                   HKQuantityTypeIdentifier.DistanceWalkingRunning),
                    HKQuantityType.Create(HKQuantityTypeIdentifier.DietaryEnergyConsumed),
                    HKQuantityType.Create(HKQuantityTypeIdentifier.ActiveEnergyBurned),
                    HKQuantityType.Create(HKQuantityTypeIdentifier.AppleMoveTime),
                    HKQuantityType.Create(HKQuantityTypeIdentifier.StepCount),
                    HKQuantityType.Create(HKQuantityTypeIdentifier.WalkingStepLength),
                    HKQuantityType.Create(HKQuantityTypeIdentifier.WalkingSpeed),
                    HKQuantityType.Create(HKQuantityTypeIdentifier.BasalEnergyBurned),
                    HKQuantityType.Create(HKQuantityTypeIdentifier.RestingHeartRate),
                    HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRate)
      });


        NSSet DataTypesToWrite
        {
            get
            {
                return NSSet.MakeNSObjectSet(new HKObjectType[] {

                });
            }
        }


        public HealthDatImpl()
        {

        }

        public bool IsDataHealthAvailable()
        {
            return HKHealthStore.IsHealthDataAvailable;
        }


        public async Task<bool> ShouldRequestAuthurizationPermissionAsync()
        {
            var access = await HealthKitStore.GetRequestStatusForAuthorizationToShareAsync(new NSSet<HKSampleType>(), DataTypesToRead);

            return access != HKAuthorizationRequestStatus.Unnecessary;
        }

        void FetchAccumlativeData(HealthDataItem resultHealthDataItem,DateTime startDate, DateTime endDate, HKQuantityTypeIdentifier sampleType, HKUnit unit, Action<HealthDataItem, object> completionHandler)
        {
            var predicate = HKQuery.GetPredicateForSamples((NSDate)startDate, (NSDate)endDate, HKQueryOptions.StrictStartDate);
            var quantityType = HKQuantityType.Create(sampleType);

            var query = new HKStatisticsQuery(quantityType, predicate, HKStatisticsOptions.CumulativeSum,
                            (HKStatisticsQuery resultQuery, HKStatistics result, NSError error) =>
                            {

                                if (error != null && completionHandler != null)
                                {
                                    resultHealthDataItem.LastUpdated = DateTime.Now;
                                    resultHealthDataItem.Value = error == null? "-" : error.ToString();
                                    resultHealthDataItem.HasNoData = true;
                                    completionHandler(resultHealthDataItem, error);
                                }

                                if (result != null)
                                {
                                    resultHealthDataItem.LastUpdated = (DateTime)result.EndDate;
                                    var total = result.SumQuantity();
                                    if (total == null)
                                        total = HKQuantity.FromQuantity(unit, 0.0);

                                    resultHealthDataItem.Value = total.GetDoubleValue(unit).ToString();
                                    completionHandler?.Invoke(resultHealthDataItem, error);
                                }
                            });

            HealthKitStore.ExecuteQuery(query);
        }

        void FetchSamplingData(DateTime startDate, DateTime endDate, HKQuantityTypeIdentifier type, Action<HKSample[], NSError> completionHandler)
        {
            var predicate = HKQuery.GetPredicateForSamples((NSDate)startDate, (NSDate)endDate, HKQueryOptions.StrictStartDate);

            var sort = new NSSortDescriptor[] {
                                new  NSSortDescriptor("HKSampleSortIdentifierStartDate",false)

                //new  NSSortDescriptor(key: HKSampleSortIdentifierStartDate, ascending: false)
                };

            var sampleType = HKQuantityType.Create(type);

            var query = new HKSampleQuery(sampleType, predicate, 1, null, (HKSampleQuery resultQuery, HKSample[] results, NSError error) =>
            {
                if (error != null && completionHandler != null)
                    completionHandler(null, error);

                completionHandler?.Invoke(results, error);
            });

            HealthKitStore.ExecuteQuery(query);
        }

        public async void RequestAuthorizationAsync(Action<bool> permissionGranted)
        {
            var success = await HealthKitStore.RequestAuthorizationToShareAsync(DataTypesToWrite, DataTypesToRead);

            if (!success.Item1)
            {
                permissionGranted(false);
            }

            else
                permissionGranted(true);
        }

        public void GetTodayStepCounts(Action<HealthDataItem, object> stepsCount)
        {
            var startDate = DateTime.Now.Date;
            var endDate = startDate.AddDays(1);

            FetchAccumlativeData(new HealthDataItem { MeasureUnit = MeasureUnit.Step, DataType  = HealthDataType.StepCount}, startDate, endDate, HKQuantityTypeIdentifier.StepCount, HKUnit.Count, stepsCount);
        }

        public void GetTodayStepLength(Action<HealthDataItem, object> stepLenth)
        {
            var startDate = DateTime.Now.Date;
            var endDate = startDate.AddDays(1);

            FetchSamplingData(startDate, endDate, HKQuantityTypeIdentifier.WalkingStepLength, (results, error) =>
             {
                 var resultHealthDataItem = new HealthDataItem
                 {
                     MeasureUnit = MeasureUnit.Meter,
                     DataType = HealthDataType.StepLenght,
                     LastUpdated = DateTime.Now
             };


                 if (error != null && stepLenth != null)
                 {
                     resultHealthDataItem.Value = error == null ? "-" : error.ToString();
                     resultHealthDataItem.HasNoData = true;
                     stepLenth(resultHealthDataItem, error);
                 }

                 if (results != null && results.Length > 0)
                 {
                      var lastest = results[results.Length-1];

                     //for (int i = 0; i < results.Length; i++)
                     //{
                         HKQuantitySample currData = (HKQuantitySample)lastest;
                         var len = currData.Quantity.GetDoubleValue(HKUnit.Meter);
                     //Console.WriteLine("currData.QuantityType<< {0}", currData.QuantityType.ToString());
                     //Console.WriteLine("currData.StartDate<< {0}", currData.StartDate.ToString());
                     //Console.WriteLine("currData.EndDate<< {0}", currData.EndDate.ToString());
                     //if (currData.Device != null)
                     //    Console.WriteLine("currData.Device<< {0}", currData.Device.ToString());

                     resultHealthDataItem.LastUpdated = (DateTime)currData.EndDate;
                     resultHealthDataItem.Value = len.ToString();
                         stepLenth.Invoke(resultHealthDataItem, error);
                     //}
                 }
                 else
                 {
                     resultHealthDataItem.HasNoData = true;
                     stepLenth?.Invoke(resultHealthDataItem, error);
                 }
             });
        }

        public void GetTodayWalkingDistance(Action<HealthDataItem, object> walkingDistance)
        {
            var startDate = DateTime.Now.Date;
            var endDate = startDate.AddDays(1);
            FetchAccumlativeData(new HealthDataItem { MeasureUnit = MeasureUnit.Meter, DataType = HealthDataType.WalkingDistance }, startDate, endDate, HKQuantityTypeIdentifier.DistanceWalkingRunning, HKUnit.Meter, walkingDistance);
        }

        public void GetTodayCalories(Action<HealthDataItem, object> calories)
        {
            var startDate = DateTime.Now.Date;
            var endDate = startDate.AddDays(1);
            FetchAccumlativeData(new HealthDataItem { MeasureUnit = MeasureUnit.Calorie, DataType = HealthDataType.BurnedCalories }, startDate, endDate, HKQuantityTypeIdentifier.ActiveEnergyBurned, HKUnit.Calorie, calories);
        }

        public void GetLastHeartRate(Action<HealthDataItem, object> heartRate)
        {
            var startDate = DateTime.Now.AddDays(-10);
            var endDate = DateTime.Now.Date.AddDays(1);

            var sampleType = HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRate);

            FetchSamplingData(startDate, endDate, HKQuantityTypeIdentifier.HeartRate, (HKSample[] results, NSError error) =>
             {
                 var resultHealthDataItem = new HealthDataItem
                 {
                     MeasureUnit = MeasureUnit.Bpm,
                     DataType = HealthDataType.HeartRate,
                     LastUpdated = DateTime.Now
                 };


                 if (error != null && heartRate != null)
                 {
                     resultHealthDataItem.Value = error == null ? "-" : error.ToString();
                     resultHealthDataItem.HasNoData = true;
                     heartRate(resultHealthDataItem, error);
                 }


                 if (results != null && results.Length > 0)
                 {
                     var unit = HKUnit.Count.UnitDividedBy(HKUnit.Minute);
                     
                     var last = results[results.Length - 1];
                    //for (int i = 0; i < results.Length; i++)
                    //{
                    HKQuantitySample currData = (HKQuantitySample)last;
                     var hr = currData.Quantity.GetDoubleValue(unit);
                     //Console.WriteLine("currData.QuantityType<< {0}", currData.QuantityType.ToString());
                     //Console.WriteLine("currData.StartDate<< {0}", currData.StartDate.ToString());
                     //Console.WriteLine("currData.EndDate<< {0}", currData.EndDate.ToString());
                     resultHealthDataItem.LastUpdated = (DateTime)currData.EndDate;
                     //if (currData.Device != null)
                     //    Console.WriteLine("currData.Device<< {0}", currData.Device.ToString());
                     resultHealthDataItem.Value = hr.ToString();
                     heartRate.Invoke(resultHealthDataItem, error);
                    //}
                }
                 else
                 {
                     resultHealthDataItem.HasNoData = true;
                     heartRate?.Invoke(resultHealthDataItem, error);
                 }
             });
        }

        public void GetTodatMoveMinutes(Action<HealthDataItem, object> moveTime)
        {
            var startDate = DateTime.Now.Date;
            var endDate = startDate.AddDays(1);
            FetchAccumlativeData(new HealthDataItem { MeasureUnit = MeasureUnit.Minute, DataType = HealthDataType.MoveTimeInMinutes },startDate, endDate, HKQuantityTypeIdentifier.AppleMoveTime, HKUnit.Minute, moveTime);
        }

        public void GetTodatWalkingSpeed(Action<HealthDataItem, object> speed)
        {
            var startDate = DateTime.Now.AddDays(-10);
            var endDate = DateTime.Now.Date.AddDays(1);

            FetchSamplingData(startDate, endDate, HKQuantityTypeIdentifier.WalkingSpeed, (HKSample[] results, NSError error) =>
            {
                if (results != null)
                {
                    var unit = HKUnit.Meter.UnitDividedBy(HKUnit.Second);
                    double totalSpeed = 0;
                    int count = 0;

                    var resultHealthDataItem = new HealthDataItem
                    {
                        MeasureUnit = MeasureUnit.KMH,
                        DataType = HealthDataType.WalkingSpeed
                    };

                    for (int i = 0; i < results.Length; i++)
                    {
                        HKQuantitySample currData = (HKQuantitySample)results[i];
                        var speedItem = currData.Quantity.GetDoubleValue(HKUnit.Meter.UnitDividedBy(HKUnit.Second));

                        if (speedItem > 0)
                        {
                            var s = speedItem * 3.6;
                            totalSpeed += s;
                            //Console.WriteLine("currData.QuantityType << {0}", currData.QuantityType.ToString());
                            //Console.WriteLine("currData.StartDate<< {0}", currData.StartDate.ToString());
                            //Console.WriteLine("currData.EndDate<< {0}", currData.EndDate.ToString());
                            //Console.WriteLine("currData.Device<< {0}", currData.Device.ToString());
                            resultHealthDataItem.LastUpdated = (DateTime)currData.EndDate;
                            count++;
                        }

                    }
                    resultHealthDataItem.Value = (totalSpeed / count).ToString();
                    speed.Invoke(resultHealthDataItem, error);
                }
            });
        }

    }
}

//void FetchMostRecentDataDistanceWalkingRunning(Action<string, NSError> completionHandler)
//{
//    var calendar = NSCalendar.CurrentCalendar;
//    var startDate = DateTime.Now.Date;
//    var endDate = startDate.AddDays(1);

//    var sampleType = HKQuantityType.Create(HKQuantityTypeIdentifier.DistanceWalkingRunning);
//    var predicate = HKQuery.GetPredicateForSamples((NSDate)startDate, (NSDate)endDate, HKQueryOptions.StrictStartDate);

//    var query = new HKStatisticsQuery(sampleType, predicate, HKStatisticsOptions.CumulativeSum,
//                    (HKStatisticsQuery resultQuery, HKStatistics results, NSError error) =>
//                    {

//                        if (error != null && completionHandler != null)
//                            completionHandler("0.0f", error);

//                        if (results != null)
//                        {
//                            var totalSteps = results.SumQuantity();
//                            if (totalSteps == null)
//                                totalSteps = HKQuantity.FromQuantity(HKUnit.Meter, 0.0);

//                            completionHandler?.Invoke(totalSteps.GetDoubleValue(HKUnit.Meter).ToString(), error);

//                        }

//                    });

//    HealthKitStore.ExecuteQuery(query);
//}

//void FetchMostRecentDataAppleStandTime(Action<double, NSError> completionHandler)
//{
//    var calendar = NSCalendar.CurrentCalendar;
//    var startDate = DateTime.Now.Date;
//    var endDate = startDate.AddDays(1);

//    var sampleType = HKQuantityType.Create(HKQuantityTypeIdentifier.AppleStandTime);
//    var predicate = HKQuery.GetPredicateForSamples((NSDate)startDate, (NSDate)endDate, HKQueryOptions.StrictStartDate);

//    var query = new HKStatisticsQuery(sampleType, predicate, HKStatisticsOptions.CumulativeSum,
//                    (HKStatisticsQuery resultQuery, HKStatistics results, NSError error) =>
//                    {

//                        if (error != null && completionHandler != null)
//                            completionHandler(0.0f, error);

//                        if (results != null)
//                        {
//                            var totalSteps = results.SumQuantity();
//                            if (totalSteps == null)
//                                totalSteps = HKQuantity.FromQuantity(HKUnit.Minute, 0.0);

//                            completionHandler?.Invoke(totalSteps.GetDoubleValue(HKUnit.Minute), error);

//                        }

//                    });

//    HealthKitStore.ExecuteQuery(query);
//}

//void FetchMostRecentDataWalkingStepLength(Action<double, NSError> completionHandler)
//{
//    var calendar = NSCalendar.CurrentCalendar;
//    var startDate = DateTime.Now.Date;
//    var endDate = startDate.AddDays(1);

//    var sampleType = HKQuantityType.Create(HKQuantityTypeIdentifier.WalkingStepLength);
//    var predicate = HKQuery.GetPredicateForSamples((NSDate)startDate, (NSDate)endDate, HKQueryOptions.StrictStartDate);

//    var query = new HKSampleQuery(sampleType, predicate, HKSampleQuery.NoLimit, null, (HKSampleQuery resultQuery, HKSample[] results, NSError error) =>
//    {

//        if (error != null && completionHandler != null)
//            completionHandler(0.0f, error);

//        if (results != null)
//        {
//            for (int i = 0; i < results.Length; i++)
//            {
//                HKQuantitySample currData = (HKQuantitySample)results[i];
//                var totalSteps = currData.Quantity.GetDoubleValue(HKUnit.Meter);
//                Console.WriteLine("currData.QuantityType<< {0}", currData.QuantityType.ToString());
//                Console.WriteLine("currData.StartDate<< {0}", currData.StartDate.ToString());
//                Console.WriteLine("currData.EndDate<< {0}", currData.EndDate.ToString());
//                Console.WriteLine("currData.Device<< {0}", currData.Device.ToString());
//                completionHandler?.Invoke(totalSteps, error);
//            }
//        }

//    });

//    HealthKitStore.ExecuteQuery(query);
//}

//void FetchMostRecentDataWalkingSpeed(Action<double, NSError> completionHandler)
//{
//    var calendar = NSCalendar.CurrentCalendar;
//    var startDate = DateTime.Now.Date;
//    var endDate = startDate.AddDays(1);

//    var sampleType = HKQuantityType.Create(HKQuantityTypeIdentifier.WalkingSpeed);
//    var predicate = HKQuery.GetPredicateForSamples((NSDate)startDate, (NSDate)endDate, HKQueryOptions.StrictStartDate);

//    var query = new HKSampleQuery(sampleType, predicate, HKSampleQuery.NoLimit, null, (HKSampleQuery resultQuery, HKSample[] results, NSError error) =>
//    {

//        if (error != null && completionHandler != null)
//            completionHandler(0.0f, error);

//        if (results != null)
//        {
//            for (int i = 0; i < results.Length; i++)
//            {
//                HKQuantitySample currData = (HKQuantitySample)results[i];
//                var totalSteps = currData.Quantity.GetDoubleValue(HKUnit.Meter.UnitDividedBy(HKUnit.Second));
//                Console.WriteLine("currData.QuantityType<< {0}", currData.QuantityType.ToString());
//                Console.WriteLine("currData.StartDate<< {0}", currData.StartDate.ToString());
//                Console.WriteLine("currData.EndDate<< {0}", currData.EndDate.ToString());
//                Console.WriteLine("currData.Device<< {0}", currData.Device.ToString());
//                completionHandler?.Invoke(totalSteps, error);
//            }
//        }

//    });

//    HealthKitStore.ExecuteQuery(query);
//}

//void FetchBasalEnergyBurned(Action<string, NSError> completionHandler)
//{
//    var startDate = DateTime.Now.Date;
//    var endDate = startDate.AddDays(1);

//    var sampleType = HKQuantityType.Create(HKQuantityTypeIdentifier.BasalEnergyBurned);
//    var predicate = HKQuery.GetPredicateForSamples((NSDate)startDate, (NSDate)endDate, HKQueryOptions.StrictStartDate);

//    var query = new HKStatisticsQuery(sampleType, predicate, HKStatisticsOptions.CumulativeSum,
//                    (HKStatisticsQuery resultQuery, HKStatistics results, NSError error) =>
//                    {

//                        if (error != null && completionHandler != null)
//                            completionHandler(error.ToString(), error);

//                        if (results != null)
//                        {
//                            var totalSteps = results.SumQuantity();
//                            if (totalSteps == null)
//                                totalSteps = HKQuantity.FromQuantity(HKUnit.Calorie, 0.0);

//                            completionHandler?.Invoke(totalSteps.GetDoubleValue(HKUnit.Calorie).ToString(), error);

//                        }

//                    });

//    HealthKitStore.ExecuteQuery(query);
//}

//void FetchRestingHeartRate(Action<string, NSError> completionHandler)
//{
//    //var calendar = NSCalendar.CurrentCalendar;
//    //var startDate = DateTime.Now.Date;
//    var endDate = DateTime.Now.Date.AddDays(1);
//    DateTime startDate = DateTime.Now.AddDays(-10);


//    var sampleType = HKQuantityType.Create(HKQuantityTypeIdentifier.HeartRate);
//    var bpm = HKUnit.Count.UnitDividedBy(HKUnit.Minute);

//    var predicate = HKQuery.GetPredicateForSamples((NSDate)startDate, (NSDate)endDate, HKQueryOptions.StrictStartDate);

//    var query = new HKSampleQuery(sampleType, predicate, HKSampleQuery.NoLimit, null, (HKSampleQuery resultQuery, HKSample[] results, NSError error) =>
//    {

//        if (error != null && completionHandler != null)
//            completionHandler("0.0f", error);

//        if (results != null && results.Length > 0)
//        {
//            for (int i = 0; i < results.Length; i++)
//            {
//                HKQuantitySample currData = (HKQuantitySample)results[i];
//                var heartRate = currData.Quantity.GetDoubleValue(bpm);
//                Console.WriteLine("currData.QuantityType<< {0}", currData.QuantityType.ToString());
//                Console.WriteLine("currData.StartDate<< {0}", currData.StartDate.ToString());
//                Console.WriteLine("currData.EndDate<< {0}", currData.EndDate.ToString());
//                if (currData.Device != null)
//                    Console.WriteLine("currData.Device<< {0}", currData.Device.ToString());
//                completionHandler?.Invoke(heartRate.ToString(), error);
//            }

//        }
//        else
//            completionHandler?.Invoke("-", error);
//    });

//    HealthKitStore.ExecuteQuery(query);
//}