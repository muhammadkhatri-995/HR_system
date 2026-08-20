namespace HR_system.Helpers
{
    // A tiny helper so every Controller sets notifications the SAME way,
    // instead of everyone inventing their own TempData key names
    // (some using "Success", others "SuccessMessage", etc. — that
    // inconsistency is exactly what caused us to only show messages
    // in a couple of places before this module).
    public static class NotificationHelper
    {
        public const string SuccessKey = "Notification_Success";
        public const string ErrorKey = "Notification_Error";
        public const string InfoKey = "Notification_Info";
    }
}