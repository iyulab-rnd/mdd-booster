using System.Reflection;

namespace MDDBooster
{
    internal class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            args = new string[] { @"D:/data/Plands/Plands.Core/data/data.sb" };
#endif

            if (args.Length == 0)
            {
                var versionString = Assembly.GetEntryAssembly()?
                                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                        .InformationalVersion
                                        .ToString();

                Console.WriteLine($"MDD Booster v{versionString}");
                Console.WriteLine("-------------");
                Console.WriteLine("\nUsage:");
                Console.WriteLine("mdd <file-path>");
                return;
            }

            var filePath = args.ElementAt(0);

#if DEBUG
            Run(filePath);
#else
            try
            {
                Run(filePath);
            }
            catch (Exception e)
            {
                ShowBot(e.Message, false);
            }
#endif
        }

        private static void Run(string filePath)
        {
            ShowBot($"{filePath} working...", true);

            var runner = new Runner(filePath);
            runner.Run();

            ShowBot($"done.", false);
        }

        static void ShowBot(string message, bool withIamge)
        {
            if (withIamge)
            {
                string bot = $"\n        {message}";
                bot += @"
    __________________
                      \
                       \
                          ....
                          ....'
                           ....
                        ..........
                    .............'..'..
                 ................'..'.....
               .......'..........'..'..'....
              ........'..........'..'..'.....
             .'....'..'..........'..'.......'.
             .'..................'...   ......
             .  ......'.........         .....
             .    _            __        ......
            ..    #            ##        ......
           ....       .                 .......
           ......  .......          ............
            ................  ......................
            ........................'................
           ......................'..'......    .......
        .........................'..'.....       .......
     ........    ..'.............'..'....      ..........
   ..'..'...      ...............'.......      ..........
  ...'......     ...... ..........  ......         .......
 ...........   .......              ........        ......
.......        '...'.'.              '.'.'.'         ....
.......       .....'..               ..'.....
   ..       ..........               ..'........
          ............               ..............
         .............               '..............
        ...........'..              .'.'............
       ...............              .'.'.............
      .............'..               ..'..'...........
      ...............                 .'..............
       .........                        ..............
        .....
";
                Console.WriteLine(bot);
            }
            else
            {
                string bot = $"";
                bot += $@"
                        /
                       /
    __________________/
        {message}";
                Console.WriteLine(bot);
            }
        }

    }
}