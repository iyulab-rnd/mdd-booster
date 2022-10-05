namespace MDDBooster
{
    public static class Utils
    {
        internal static bool IsInterfaceName(string name)
        {
            return name.StartsWith("I") && Char.IsUpper(name[1]);
        }
    }
}