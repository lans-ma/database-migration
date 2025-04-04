namespace Kata.Db.Console;

using System.Runtime.CompilerServices;
using System;

public static class PsgModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }
}