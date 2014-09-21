namespace NEventStore.Persistence.GetEventStore
{
    using System;

    internal static class Guard
    {
        public static void Against<TException>(bool condition, params object[] args)
            where TException : Exception
        {
            if (false == condition)
                return;

            var exception = (Exception) Activator.CreateInstance(typeof (TException), args);

            throw exception;
        }

        public static void Against(bool condition, string message)
        {
            Against<InvalidOperationException>(condition, message);
        }

        public static void AgainstNull(object arg, string paramName)
        {
            Against<ArgumentNullException>(arg == null, paramName);
        }
    }
}