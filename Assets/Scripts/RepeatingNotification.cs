using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

public class RepeatingNotification : MonoBehaviour
{
    private const string NotificationPrefKey = "NotificationsEnabled";

    void Start()
    {
        if (PlayerPrefs.GetInt(NotificationPrefKey, 1) == 1)
        {
            ScheduleRepeatingNotification();
        }
    }

    public void EnableNotifications()
    {
        PlayerPrefs.SetInt(NotificationPrefKey, 1);
        PlayerPrefs.Save();
        CancelNotifications(); // Cancel previous to avoid duplicates
        ScheduleRepeatingNotification();
        Debug.Log("Notifications enabled.");
    }

    public void DisableNotifications()
    {
        PlayerPrefs.SetInt(NotificationPrefKey, 0);
        PlayerPrefs.Save();
        CancelNotifications();
        Debug.Log("Notifications disabled.");
    }

    private void ScheduleRepeatingNotification()
    {
#if UNITY_ANDROID
        var channel = new AndroidNotificationChannel()
        {
            Id = "repeat_channel",
            Name = "Repeat Channel",
            Importance = Importance.Default,
            Description = "Repeating notifications",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);

        var notification = new AndroidNotification()
        {
            Title = "Reminder!",
            Text = "It's been a minute. Come back!",
            FireTime = System.DateTime.Now.AddMinutes(1),
            RepeatInterval = System.TimeSpan.FromMinutes(1)
        };

        AndroidNotificationCenter.SendNotification(notification, "repeat_channel");

#elif UNITY_IOS
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new System.TimeSpan(0, 1, 0),
            Repeats = true
        };

        var notification = new iOSNotification()
        {
            Identifier = "repeating_notification",
            Title = "Reminder!",
            Body = "It's been a minute. Come back!",
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            Trigger = timeTrigger
        };

        iOSNotificationCenter.ScheduleNotification(notification);
#endif
    }

    private void CancelNotifications()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllNotifications();
#elif UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
        iOSNotificationCenter.RemoveAllDeliveredNotifications();
#endif
    }
}
