
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;
using Xamarin.Forms;

namespace TestHealthData.Droid
{


    [Activity(Label = "TestHealthData", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
       public static MainActivity activity;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            LoadApplication(new App());
            activity = this;

        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            // base.OnActivityResult(requestCode, resultCode, data);

            if (resultCode == Result.Ok)
            {
                MessagingCenter.Send(this, "Permission", true);

                //if (requestCode == REQUEST_OAUTH_REQUEST_CODE)
                //{
                //    Log.i(TAG, "Fitness permission granted");
                //    subscribeStepCount();
                //    readStepCountDelta(); // Read today's data
                //    readHistoricStepCount(); // Read last weeks data
                //}
            }
            else
            {
                MessagingCenter.Send(this, "Permission", false);
            }
        }
    }
}
