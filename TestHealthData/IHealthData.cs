using System;
using System.Threading.Tasks;

namespace TestHealthData
{
    public interface IHealthData
    {
        bool IsDataHealthAvailable();

        void RequestAuthorizationAsync(Action<bool> permissionGranted);
        Task<bool> ShouldRequestAuthurizationPermissionAsync();

        void GetTodayStepCounts(Action<HealthDataItem, object> stepsCount);
        void GetTodayWalkingDistance(Action<HealthDataItem, object> walkingDistance);
        void GetTodayCalories(Action<HealthDataItem, object> calories);
        void GetLastHeartRate(Action<HealthDataItem, object> calories);
        void GetTodatMoveMinutes(Action<HealthDataItem, object> calories);
        void GetTodatWalkingSpeed(Action<HealthDataItem, object> calories);
        void GetTodayStepLength(Action<HealthDataItem, object> stepLenth);
    }
}