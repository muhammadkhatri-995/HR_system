using HR_system.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace HR_system.Controllers
{
    // Any Controller that inherits from THIS instead of plain "Controller"
    // automatically gets these two convenience methods. This avoids
    // repeating "TempData[NotificationHelper.SuccessKey] = ..." everywhere.
    public class BaseController : Controller
    {
        protected void NotifySuccess(string message)
        {
            TempData[NotificationHelper.SuccessKey] = message;
        }

        protected void NotifyError(string message)
        {
            TempData[NotificationHelper.ErrorKey] = message;
        }
    }
}