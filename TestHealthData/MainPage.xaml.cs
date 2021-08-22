using System;
using Xamarin.Forms;

namespace TestHealthData
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Init();
        }

        private async void Init()
        {
            bool hasHealthData = DependencyService.Get<IHealthData>().IsDataHealthAvailable();
            if (hasHealthData)
            {
                bool shouldRequestPermission = await DependencyService.Get<IHealthData>().ShouldRequestAuthurizationPermissionAsync();

                if (!shouldRequestPermission)
                {
                    AuthButton.IsVisible = true;
                    FetchData();
                }
                else
                    AuthButton.IsVisible = true;
            }
            else
            {
                //AuthButton.IsVisible = true;
            }
        }

        void Button_Clicked(System.Object sender, System.EventArgs e)
        {
            DependencyService.Get<IHealthData>().RequestAuthorizationAsync((res) =>
            {
                FetchData();
            });
        }

        private async void check()
        {
            bool shouldRequestPermission = await DependencyService.Get<IHealthData>().ShouldRequestAuthurizationPermissionAsync();

            if (!shouldRequestPermission)
            {
                FetchData();
            }

            else
            {
                DependencyService.Get<IHealthData>().RequestAuthorizationAsync((res) =>
                {
                    FetchData();
                });
            }
        }

        private void FetchData()
        {
            DependencyService.Get<IHealthData>().GetTodayStepLength((res, err) => Device.BeginInvokeOnMainThread(() =>
            {
                StepLength.Text = res.ToString();
            }));


            DependencyService.Get<IHealthData>().GetTodayStepCounts((res, err) => Device.BeginInvokeOnMainThread(() =>
            {
                StepsCount.Text = res.ToString();
            }));

            DependencyService.Get<IHealthData>().GetTodayWalkingDistance((res, err) =>
             Device.BeginInvokeOnMainThread(() =>
             {
                 WalkingDistance.Text = res.ToString();
             }));

            DependencyService.Get<IHealthData>().GetTodayCalories((res, err) => Device.BeginInvokeOnMainThread(() =>
            {
                Calories.Text = res.ToString();
            }));

            DependencyService.Get<IHealthData>().GetLastHeartRate((res, err) => Device.BeginInvokeOnMainThread(() =>
            {
                HeartRate.Text = res.ToString();
            }));

            DependencyService.Get<IHealthData>().GetTodatMoveMinutes((res, err) =>
             Device.BeginInvokeOnMainThread(() =>
             {
                 MoveMinutes.Text = res.ToString();
             }));

            DependencyService.Get<IHealthData>().GetTodatWalkingSpeed((res, err) =>
            Device.BeginInvokeOnMainThread(() =>
            {
                WalkingSpeed.Text = res.ToString();
            }));

        }
    }
}
