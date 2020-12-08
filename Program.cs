using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;

namespace STL
{
    class Program
    {
        public class Options
        {
            [Value(0, MetaName = "Input", Required = true, HelpText = "Input STL File")]
            public string input { get; set; }

            [Option('m', "Scaling Method", Required = true, HelpText = "0: None | 1: Sqrt")]
            public int method { get; set; }

            [Option('d', "Iteration count", Required = false, Default = 1, HelpText = "Number of times to run scaling method on model")]
            public int iters { get; set; }

            [Option(Required = false, HelpText = "Remove all triangles with a Z vertex at 0")]
            public bool trim_base { get; set; }
        }

        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
            .WithParsed(RunApp)
            .WithNotParsed(HandleParseErrors);
        }

        static void RunApp(Options opts)
        {
            STLScaler.Method method;
            try
            {
                method = (STLScaler.Method)opts.method;
            }
            catch (System.Exception)
            {
                HandleParseErrors(new Error[0]);
                return;
            }

            new STLScaler(opts.input, method, opts.iters, opts.trim_base);
        }

        static void HandleParseErrors(IEnumerable<Error> errs) { }
    }
}
