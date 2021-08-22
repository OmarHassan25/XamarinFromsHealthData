using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Android.Content.PM;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Fitness;
using Android.Gms.Fitness.Data;
using Android.Gms.Fitness.Request;
using Android.Gms.Fitness.Result;
using Java.Util.Concurrent;
using TestHealthData.Droid;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Dependency(typeof(HealthDatImpl))]
namespace TestHealthData.Droid
{
    public class HealthDatImpl : IHealthData
    {
        private int REQUEST_OAUTH_REQUEST_CODE = 0x1001;
        private Action<bool> permissionGranted;


        public HealthDatImpl()
        {
            MessagingCenter.Subscribe<MainActivity, bool>(this, "Permission", (sender, arg) =>
            {
                permissionGranted(arg);
            });
        }


        private bool HasFitPermission()
        {
            IGoogleSignInOptionsExtension fitnessOptions = GetFitnessSignInOptions();
            return GoogleSignIn.HasPermissions(GoogleSignIn.GetLastSignedInAccount(MainActivity.activity), fitnessOptions);
        }


        private void RequestFitnessPermission()
        {
            var signInOptionsExtension = GetFitnessSignInOptions();

            GoogleSignIn.RequestPermissions(
                    MainActivity.activity,
                    REQUEST_OAUTH_REQUEST_CODE,
                    GoogleSignIn.GetAccountForExtension(MainActivity.activity, signInOptionsExtension),
                    signInOptionsExtension);
        }

        private IGoogleSignInOptionsExtension GetFitnessSignInOptions()
        {
            IGoogleSignInOptionsExtension fitnessOptions = FitnessOptions.InvokeBuilder()
                .AddDataType(DataType.TypeHeartRateBpm, FitnessOptions.AccessRead)
                .AddDataType(DataType.TypeActivitySegment, FitnessOptions.AccessRead)
                .AddDataType(DataType.AggregateActivitySummary, FitnessOptions.AccessRead)
                .AddDataType(DataType.TypeSpeed, FitnessOptions.AccessRead)
                .Build();

            return fitnessOptions;

            //.AddDataType(DataType.AggregateSpeedSummary, FitnessOptions.AccessRead)
            //.AddDataType(DataType.AggregateHeartRateSummary, FitnessOptions.AccessRead)
            //.AddDataType(DataType.TypeHeartPoints, FitnessOptions.AccessRead)
            //.AddDataType(DataType.AggregateHeartPoints, FitnessOptions.AccessRead)
        }


        public bool IsDataHealthAvailable()
        {
            string PACKAGE_NAME = "com.google.android.apps.fitness";

            try
            {
                var pkgInfo = MainActivity.activity.ApplicationContext.PackageManager.GetPackageInfo(PACKAGE_NAME, 0);
                var Activities = pkgInfo.Activities;
                return true;
            }
            catch (PackageManager.NameNotFoundException)
            {
                return false;
            }
        }

        public Task<bool> ShouldRequestAuthurizationPermissionAsync()
        {
            bool hasFitPermission = HasFitPermission();
            return Task.FromResult(!hasFitPermission);
        }

        public void GetTodayStepCounts(Action<HealthDataItem, object> stepCount)
        {
            FitnessClass.
                  GetHistoryClient(MainActivity.activity,
                  GoogleSignIn.GetLastSignedInAccount(MainActivity.activity))
                                  .ReadDailyTotal(DataType.AggregateStepCountDelta)
                                  .AddOnSuccessListener(new OnSuccessListener((response) =>
                                  {
                                      DataSet dataSet = (DataSet)response;

                                      foreach (DataPoint dp in dataSet.DataPoints)
                                      {
                                          stepCount(new HealthDataItem()
                                          {
                                              DataType = HealthDataType.StepCount,
                                              Value = dp.GetValue(dp.DataType.Fields[0]).ToString(),
                                              LastUpdated = DateTimeUtils.ConvertToLocalDate(dp.GetEndTime(TimeUnit.Milliseconds)),
                                              MeasureUnit = MeasureUnit.Step
                                          }, null);
                                      }
                                  }))
                                  .AddOnFailureListener(new OnFailureListener((err) =>
                                  {
                                      stepCount(null, err.ToString());
                                  }));
        }

        public void RequestAuthorizationAsync(Action<bool> action)
        {
            this.permissionGranted = action;
            RequestFitnessPermission();
        }

        public void GetTodayWalkingDistance(Action<HealthDataItem, object> walkingDistance)
        {
            FitnessClass.GetHistoryClient(MainActivity.activity, GoogleSignIn.GetLastSignedInAccount(MainActivity.activity))
                .ReadDailyTotal(DataType.AggregateDistanceDelta)
                 .AddOnSuccessListener(new OnSuccessListener((response) =>
                 {
                     List<FiledData> distance = FormatResult(response);
                     FiledData lastData = distance[distance.Count - 1];

                     walkingDistance(new HealthDataItem()
                     {
                         MeasureUnit = MeasureUnit.Meter,
                         Value = lastData.FieldValue.ToString(),
                         LastUpdated = lastData.EndDate,
                         DataType = HealthDataType.WalkingDistance
                     }, null);

                 }))
                  .AddOnFailureListener(new OnFailureListener((error) =>
                  {
                      walkingDistance(null, error.ToString());
                  }));
        }

        public void GetTodayCalories(Action<HealthDataItem, object> calories)
        {
            FitnessClass.
                 GetHistoryClient(MainActivity.activity,
                 GoogleSignIn.GetLastSignedInAccount(MainActivity.activity))
                                 .ReadDailyTotal(DataType.AggregateCaloriesExpended)
                                 .AddOnSuccessListener(new OnSuccessListener((response) =>
                                 {
                                     DataSet dataSet = (DataSet)response;

                                     foreach (DataPoint dp in dataSet.DataPoints)
                                     {
                                         calories(new HealthDataItem()
                                         {
                                             DataType = HealthDataType.BurnedCalories,
                                             Value = dp.GetValue(dp.DataType.Fields[0]).AsFloat().ToString(),
                                             LastUpdated = DateTimeUtils.ConvertToLocalDate(dp.GetEndTime(TimeUnit.Milliseconds)),
                                             MeasureUnit = MeasureUnit.Calorie
                                         }, null);

                                     }
                                 }))
                                 .AddOnFailureListener(new OnFailureListener((err) =>
                                 {
                                     calories(null, err.ToString());
                                 }));

        }

        public void GetLastHeartRate(Action<HealthDataItem, object> heartRate)
        {
            var endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
            var startTime = endTime.Subtract(TimeSpan.FromDays(7));

            DataReadRequest readRequest = new DataReadRequest.Builder()
                 .Read(DataType.TypeHeartRateBpm)
                  .SetTimeRange(DateTimeUtils.DateTimeToMilliSeconds(startTime), DateTimeUtils.DateTimeToMilliSeconds(endTime), TimeUnit.Milliseconds)
                 .BucketByTime(365, TimeUnit.Days)
                 .Build();


            FitnessClass.
                 GetHistoryClient(MainActivity.activity,
                 GoogleSignIn.GetLastSignedInAccount(MainActivity.activity))
                 .ReadData(readRequest)
                                 .AddOnSuccessListener(new OnSuccessListener((response) =>
                                 {
                                     DataReadResponse data = (DataReadResponse)response;
                                     List<FiledData> heartRatePoints = FormatResult(response);
                                     heartRatePoints.Sort((p1, p2) => p1.EndDate.CompareTo(p2.EndDate) * -1);
                                     FiledData lastHeartData = heartRatePoints[0];
                                     heartRate(new HealthDataItem()
                                     {
                                         MeasureUnit = MeasureUnit.Bpm,
                                         Value = lastHeartData.FieldValue.ToString(),
                                         LastUpdated = lastHeartData.EndDate,
                                         DataType = HealthDataType.HeartRate
                                     }, null);

                                 }))
                                 .AddOnFailureListener(new OnFailureListener((err) =>
                                 {
                                     heartRate(null, err.ToString());
                                 }));
        }

        public void GetTodatMoveMinutes(Action<HealthDataItem, object> moveMinutes)
        {
            FitnessClass.GetHistoryClient(MainActivity.activity, GoogleSignIn.GetLastSignedInAccount(MainActivity.activity))
              .ReadDailyTotal(DataType.TypeMoveMinutes)
               .AddOnSuccessListener(new OnSuccessListener((response) =>
               {
                   DataSet dataSet = (DataSet)response;
                   foreach (DataPoint dp in dataSet.DataPoints)
                   {
                       var result = dp.GetValue(dp.DataType.Fields[0]).AsInt();

                       moveMinutes(new HealthDataItem()
                       {
                           DataType = HealthDataType.MoveTimeInMinutes,
                           Value = result.ToString(),
                           LastUpdated = DateTimeUtils.ConvertToLocalDate(dp.GetEndTime(TimeUnit.Milliseconds)),
                           MeasureUnit = MeasureUnit.Minute
                       }, null);
                   }
               }))
               .AddOnFailureListener(new OnFailureListener((err) =>
               {
                   moveMinutes(null, err.ToString());
               }));
        }


        public void GetTodayStepLength(Action<HealthDataItem, object> stepLenth)
        {

        }

        public void GetTodatWalkingSpeed(Action<HealthDataItem, object> walkingSpeed)
        {
            FitnessClass.
               GetHistoryClient(MainActivity.activity,
               GoogleSignIn.GetLastSignedInAccount(MainActivity.activity))
                               .ReadDailyTotal(DataType.TypeSpeed)
                               .AddOnSuccessListener(new OnSuccessListener((response) =>
                               {
                                   DataSet dataSet = (DataSet)response;

                                   foreach (DataPoint dp in dataSet.DataPoints)
                                   {
                                       walkingSpeed(new HealthDataItem()
                                       {
                                           DataType = HealthDataType.WalkingSpeed,
                                           Value = (dp.GetValue(dp.DataType.Fields[0]).AsFloat() * 3.6).ToString(),
                                           LastUpdated = DateTimeUtils.ConvertToLocalDate(dp.GetEndTime(TimeUnit.Milliseconds)),
                                           MeasureUnit = MeasureUnit.KMH
                                       }, null);
                                   }

                               }))
                               .AddOnFailureListener(new OnFailureListener((err) =>
                               {
                                   walkingSpeed(null, err.ToString());
                               }));
        }

        public void ReadActivity()
        {

            FitnessClass.
                 GetHistoryClient(MainActivity.activity,
                 GoogleSignIn.GetLastSignedInAccount(MainActivity.activity))
                                 .ReadDailyTotal(DataType.TypeActivitySegment)
                                 .AddOnSuccessListener(new OnSuccessListener((response) =>
                                 {
                                     DataSet dataSet = (DataSet)response;


                                     foreach (DataPoint dp in dataSet.DataPoints)
                                     {
                                         foreach (Field f in dp.DataType.Fields)
                                         {
                                             try
                                             {
                                                 Console.WriteLine("&&&&&&&&&&&Field Activity << " + dp.GetValue(f).AsActivity() + "\n");
                                             }
                                             catch (Exception e)
                                             {

                                             }

                                             try
                                             {
                                                 Console.WriteLine("&&&&&&&&&&&Field String<< " + dp.GetValue(f).AsString() + "\n");
                                             }
                                             catch (Exception e)
                                             {

                                             }

                                             try
                                             {
                                                 Console.WriteLine("&&&&&&&&&&&Field Float<< " + dp.GetValue(f).AsFloat() + "\n");
                                             }
                                             catch (Exception e)
                                             {

                                             }

                                             try
                                             {
                                                 Console.WriteLine("&&&&&&&&&&&Field Int<< " + dp.GetValue(f).AsInt() + "\n");
                                             }
                                             catch (Exception e)
                                             {

                                             }

                                             Console.WriteLine("ـــــــــــــــــــــــــــ\n");

                                         }


                                         //result.Append(dp.GetValue(dp.DataType.Fields[0]).ToString());
                                     }

                                 }))
                                 .AddOnFailureListener(new OnFailureListener((err) =>
                                 {
                                     //calories(null, err.ToString());
                                 }));
        }

        private List<FiledData> FormatResult(object heathData)
        {
            if (heathData is DataReadResponse)
            {
                DataReadResponse data = (DataReadResponse)heathData;

                var result = new List<FiledData>();

                if (data.Buckets.Count > 0)
                {

                    foreach (Bucket bucket in data.Buckets)
                    {
                        var dataSets = bucket.DataSets;
                        foreach (DataSet dataSet in dataSets)
                        {
                            result.AddRange(FormatDataSet(dataSet));
                        }
                    }
                }
                else if (data.DataSets.Count > 0)
                {
                    foreach (DataSet dataSet in data.DataSets)
                    {
                        result.AddRange(FormatDataSet(dataSet));
                    }
                }

                return result;
            }

            else if (heathData is DataSet)
            {
                DataSet data = (DataSet)heathData;
                return FormatDataSet(data);
            }

            return null;
        }

        private List<FiledData> FormatDataSet(DataSet dataSet)
        {

            var result = new List<FiledData>();

            foreach (DataPoint dp in dataSet.DataPoints)
            {
                DateTime sDT = DateTimeUtils.ConvertToLocalDate(dp.GetStartTime(TimeUnit.Milliseconds));
                DateTime eDT = DateTimeUtils.ConvertToLocalDate(dp.GetEndTime(TimeUnit.Milliseconds));
                result.Add(new FiledData
                {
                    StartDtae = sDT,
                    EndDate = eDT,
                    FieldName = dp.DataType.Fields[0].Name,
                    FieldValue = decimal.Parse(dp.GetValue(dp.DataType.Fields[0]).ToString())
                });
            }

            return result;
        }

    }
}

//DataSource ESTIMATED_STEP_DELTAS = new DataSource.Builder()
//    .SetDataType(DataType.TypeStepCountDelta)
//    .SetType(DataSource.TypeDerived)
//    .SetStreamName("estimated_steps")
//    .SetAppPackageName("com.google.android.gms")
//    .Build();

//private void ToDayCalories()
//{
//    FitnessClass.
//          GetHistoryClient(MainActivity.activity,
//          GoogleSignIn.GetLastSignedInAccount(MainActivity.activity))
//                          .ReadDailyTotal(DataType.AggregateCaloriesExpended)
//                          .AddOnSuccessListener(new OnSuccessListener2((response) =>
//                          {
//                              Console.WriteLine("Today AggregateCaloriesExpended:\n{0}", FormatResult(response));
//                              Console.WriteLine("-----------------------------------\n");
//                          }))
//                          .AddOnFailureListener(new OnFailureListener2((err) =>
//                           {
//                               Console.WriteLine("");
//                           }));
//}

//private void GetHeartRate()
//{
//    FitnessClass.
//           GetHistoryClient(MainActivity.activity,
//           GoogleSignIn.GetLastSignedInAccount(MainActivity.activity))
//                           .ReadDailyTotal(DataType.TypeHeartRateBpm)
//                           .AddOnSuccessListener(new OnSuccessListener2((response) =>
//                           {
//                               Console.WriteLine("Today TypeHeartRateBpm:\n{0}", FormatResult(response));
//                               Console.WriteLine("-----------------------------------\n");
//                           }))
//                           .AddOnFailureListener(new OnFailureListener2((err) =>
//                           {
//                               Console.Write("");
//                           }));
//}

//private void ToDaySteps()
//{
//    FitnessClass.
//           GetHistoryClient(MainActivity.activity,
//           GoogleSignIn.GetLastSignedInAccount(MainActivity.activity))
//                           .ReadDailyTotal(DataType.AggregateStepCountDelta)
//                           .AddOnSuccessListener(new OnSuccessListener2((response) =>
//                           {
//                               Console.WriteLine("Today ToDaySteps:\n{0}", FormatResult(response));
//                               Console.WriteLine("-----------------------------------\n");
//                           }))
//                           .AddOnFailureListener(new OnFailureListener2((err) =>
//                           {
//                           }));
//}

//private void ReadTypeDistanceDelta()
//{
//    FitnessClass.GetHistoryClient(MainActivity.activity, GoogleSignIn.GetLastSignedInAccount(MainActivity.activity))
//        .ReadDailyTotal(DataType.TypeDistanceDelta)
//         .AddOnSuccessListener(new OnSuccessListener2((response) =>
//         {
//             Console.WriteLine("Today Distance:\n{0}", FormatResult(response));
//             Console.WriteLine("-----------------------------------\n");
//         }))
//          .AddOnFailureListener(new OnFailureListener2((error) =>
//          {
//          }));
//}


//        var endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

//        var startTime = endTime.Subtract(TimeSpan.FromDays(30));

//        DataReadRequest readRequest = new DataReadRequest.Builder()
//.Aggregate(DataType.TypeActivitySegment, DataType.AggregateActivitySummary)
//.BucketBySession(1, TimeUnit.Minutes)
//                .SetTimeRange(DateTimeUtils.DateTimeToMilliSeconds(startTime), DateTimeUtils.DateTimeToMilliSeconds(endTime), TimeUnit.Milliseconds)
//.Build();

//        FitnessClass.GetHistoryClient(MainActivity.activity, GoogleSignIn.GetLastSignedInAccount(MainActivity.activity))
//.ReadData(readRequest).AddOnSuccessListener(new OnSuccessListener((response) =>
//             {
//                 var res = FormatResult(response);
//                 Console.WriteLine(res);
//             }))
//             .AddOnFailureListener(new OnFailureListener((err) =>
//             {
//                 walkingSpeed(null, err.ToString());
//             }));


//private void ReadHistoricStepCount()
//{
//    if (!HasFitPermission())
//    {
//        RequestFitnessPermission();
//        return;
//    }

//    FitnessClass.GetHistoryClient(MainActivity.activity,
//        GoogleSignIn.GetLastSignedInAccount(MainActivity.activity))
//            .ReadData(QueryFitnessData())
//            .AddOnSuccessListener(new OnSuccessListener2((response) =>
//            {
//                Console.WriteLine("Today History Step Counts:\n{0}", FormatResult(response));
//                Console.WriteLine("-----------------------------------\n");
//            }))
//            .AddOnFailureListener(new OnFailureListener2((err) =>
//            {

//            }));
//}

//public DataReadRequest QueryFitnessData()
//{
//    // [START build_read_data_request]
//    // Setting a start and end date using a range of 1 week working backwards using today's
//    // start of the day (midnight). This ensures that the buckets are in line with the days.

//    var endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

//    var startTime = endTime.Subtract(TimeSpan.FromDays(1));

//    return new DataReadRequest.Builder()
//            // The data request can specify multiple data types to return, effectively
//            // combining multiple data queries into one call.
//            // In this example, it's very unlikely that the request is for several hundred
//            // datapoints each consisting of a few steps and a timestamp.  The more likely
//            // scenario is wanting to see how many steps were walked per day, for 7 days.
//            .Aggregate(ESTIMATED_STEP_DELTAS, DataType.AggregateStepCountDelta)
//            // Analogous to a "Group By" in SQL, defines how data should be aggregated.
//            // bucketByTime allows for a time span, whereas bucketBySession would allow
//            // bucketing by "sessions", which would need to be defined in code.
//            .BucketByTime(1, TimeUnit.Days)
//            .SetTimeRange(DateTimeUtils.DateTimeToMilliSeconds(startTime), DateTimeUtils.DateTimeToMilliSeconds(endTime), TimeUnit.Milliseconds)
//            .Build();
//}


class FiledData
{
    public DateTime StartDtae { get; set; }
    public DateTime EndDate { get; set; }
    public string FieldName { get; set; }
    public decimal FieldValue { get; set; }
}