namespace HealthCalendar.Shared
{
    // class used to set role in User Model
    public class Roles
    {
        public static string Patient { get { return "Patient"; } }
        public static string Worker { get { return "Worker"; } }
        public static string Admin { get { return "Admin"; } }
    }
}