namespace ItemRequests
{
    public static class ExtensionsString
    {
        public static string GenderString(this ThingEntry entry)
        {
            return entry.gender.ToString().ToLower();
        }
    }
}