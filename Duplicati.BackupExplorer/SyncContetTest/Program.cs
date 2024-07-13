namespace SyncContetTest
{
    internal class Program
    {
        static async Task AsyncSub()
        {
            Console.WriteLine("AsyncSub");
            var delayTask = Task.Delay(5000);
            Console.WriteLine("AsycSub finished");
        }

        static void GetVal()
        {
            int i;
            i = 5;
            return;
        }

        static double DoCalc()
        {
            double g = 0;
            for (int i = 0; i < 1000000000; ++i)
            {
                double d = Math.Sqrt(i);
                g += d;
            }
            return g;
        }

        static int RunSync()
        {
            Console.WriteLine("In RunSsync start");

            var g = DoCalc();
            Thread.Sleep(2000);
            //await Task.Delay(5000);
            Console.WriteLine("In RunSsync");
            return (int)g;
        }


        static async Task<string> RunAsync()
        {
            Console.WriteLine("In RunAsync start");
            DoCalc();
            //Thread.Sleep(2000);
            //await AsyncSub();
            await Task.Delay(2000).ConfigureAwait(true);
            Console.WriteLine("In RunAsync");
            return "as";
        }

        static string RunAsync2()
        {
            Console.WriteLine("In RunAsync start");
            Thread.Sleep(2000);
            //await Task.Delay(5000);
            Console.WriteLine("In RunAsync");
            return "{}";
        }


        static void Main()
        {
            var t = RunAsync();

            //var t = new Task(RunSync);
            t.Wait();
            Console.WriteLine(t.Result);
            

            //var b = RunSync();
            //b.Wait();

            var a = RunAsync();
            a.Wait();
            /*
            var a = async () =>
            {
                Console.WriteLine("1");

                await RunAsync();
                Console.WriteLine("2");
                return 5;
            };
            */
            Console.WriteLine("Waitin for a");
            a.Wait();

            Action g;

            var f = Task.Factory.StartNew(RunAsync2);
            f.Start();
            var task = RunAsync();

            task.Start();
            task.Wait();

            Console.WriteLine("In Approach1");
        }
    }
}
