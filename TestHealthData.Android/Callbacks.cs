using System;
using Android.Gms.Tasks;

namespace TestHealthData.Droid
{

    public class OnSuccessListener : Java.Lang.Object, IOnSuccessListener
    {
        private readonly Action<Java.Lang.Object> success;
        public OnSuccessListener(Action<Java.Lang.Object> success)
        {
            this.success = success;
        }

        public void OnSuccess(Java.Lang.Object result)
        {
            success(result);
        }
    }

    public class OnFailureListener : Java.Lang.Object, IOnFailureListener
    {
        private readonly Action<Java.Lang.Exception> failure;
        public OnFailureListener(Action<Java.Lang.Exception> failure)
        {
            this.failure = failure;
        }

        public void OnFailure(Java.Lang.Exception e)
        {
            failure(e);
        }
    }

}
