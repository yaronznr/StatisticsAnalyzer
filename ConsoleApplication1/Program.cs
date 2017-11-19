using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using REngine;
using RserveCli;
using ServicesLib;
using StatisticsAnalyzerCore.Modeling;

namespace ConsoleApplication1
{
    class KrResult
    {
        public double Df { get; set; }
        public double PValue { get; set; }
        public double FScaling { get; set; }
        public List<double> FounderCoefs { get; set; } 
    }

    class Program
    {
        private static Dictionary<string, List<string>> CreateRowDictionary(IEnumerable<string> lines, int skipColumns)
        {
            var parsedLines = lines.Select(line => line.Split(',').ToArray()).ToList();
            var data = new Dictionary<string, List<string>>();
            for (int i = skipColumns; i < parsedLines[0].Length; i++)
            {
                data[parsedLines[0][i].Replace(" ", string.Empty)
                                      .Replace("-", string.Empty)
                                      .Replace("(", string.Empty)
                                      .Replace(")", string.Empty)
                                      .Replace("/", string.Empty)
                                      .Replace("^", string.Empty)
                                      .Replace("%", string.Empty)
                                      .Replace("\"", string.Empty)] =
                    parsedLines.Select(r => r[i]).Skip(1).ToList();
            }
            return data;
        }

        private static void CreateDataFrame(RConnection conn,
                                            Dictionary<string, List<string>> data,
                                            IEnumerable<string> vars, 
                                            string dfName,
                                            List<int> indexes)
        {
            var d = Sexp.MakeDataFrame();
            foreach (var variable in vars)
            {
                double val;
                if (double.TryParse(data[variable].First(v => (!string.IsNullOrEmpty(v) && v != "ND")), out val))
                {
                    string variable1 = variable;
                    var evalString = string.Format("c({0})",
                                             string.Join(",",
                                                        indexes.Select(idx => data[variable1][idx]).Select(m => (string.IsNullOrEmpty(m) || m == "ND") ? "NA" : m)));
                    d[variable] = conn.Eval(evalString);
                }
                else
                {
                    string variable1 = variable;
                    var fff = Sexp.Make(indexes.Select(idx => data[variable1][idx]));
                    d[variable] = fff;
                }
            }
            conn[dfName] = d;            
        }

        private static Dictionary<string, KrResult> RunModelScript(
            RConnection s,
            IEnumerable<string> genotypeFiles,
            MixedLinearModel nullMixedModel,
            List<int> genoIndexes,
            bool useAnova)
        {
            var res = new Dictionary<string, KrResult>();
            try
            {
                foreach (var genotypeFile in genotypeFiles)
                {
                    var genData = CreateRowDictionary(File.ReadAllLines(genotypeFile), 1);
                    CreateDataFrame(s, genData, genData.Keys, "gen", genoIndexes);

                    var model = nullMixedModel.Clone();
                    foreach (var vr in genData.Keys.Take(30))
                    {
                        model.AndFixedEffect(string.Format("gen${0}", vr));
                    }
                    s.VoidEval(string.Format("model = lmer(formula = {0}, data = lst, REML=TRUE)", model.ModelFormula));
                    //var exp = s.Eval(string.Format("try(model = lmer(formula = {0}, data = lst, REML=TRUE))", model.ModelFormula));
                    if (! useAnova)
                    {
                        var modelVarCount = model.FixedEffectVariables.Count();
                        var nullModelVarCount = nullMixedModel.FixedEffectVariables.Count();
                        var aa = s.Eval(string.Format("KRmodcomp(model,contr.sum({0})[{1}:{2},])",
                                                        modelVarCount + 2,
                                                        nullModelVarCount + 2,
                                                        modelVarCount + 1));
                        var kr = (SexpGenericList)aa.AsList[3];
                        var fileName = Path.GetFileName(genotypeFile);
                        if (fileName != null)
                            res[fileName.Split('.')[0]] =
                                new KrResult
                                {
                                    FScaling = (double)kr[4],
                                    PValue = (double)kr[3],
                                };                        
                    }
                    else
                    {
                        var aa = s.Eval("anova(model,nullmodel)");

                        var path = Path.GetDirectoryName(genotypeFile);
                        var name = Path.GetFileName(genotypeFile);
                        var invMatFile = Path.Combine(path ?? string.Empty, "..", "founder-liver", name ?? string.Empty);
                        s.VoidEval(string.Format("load('{0}')", invMatFile.Replace("\\", "/")));

                        s.VoidEval("rows = as.character(rownames( summary(model)$coefficients))");
                        s.VoidEval("pca_coefs_indices= grep(\"gen\",rows)");
                        s.VoidEval("pca_coefs = summary(model)$coefficients[ pca_coefs_indices ]");
                        s.VoidEval(string.Format("pcs = c( pca_coefs, rep( 0, {0} ) )", 8 - (int)aa[6][1]));
                        s.VoidEval("lines_coefs = pcs %*% inv_rot_mat");
                        var founderCoefs = new List<double>();
                        for (int i = 1; i <= 8; i++)
                        {
                            founderCoefs.Add((double)s.Eval(string.Format("lines_coefs[ {0} ]", i))[0]);
                        }
                        var fileName = Path.GetFileName(genotypeFile);
                        if (fileName != null)
                            res[fileName.Split('.')[0]] =
                                new KrResult
                                {
                                    FScaling = (double)aa[5][1],
                                    Df = (double)aa[6][1],
                                    PValue = (double)aa[7][1],
                                    FounderCoefs = founderCoefs
                                };


                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is SocketException) throw;
                Console.WriteLine(ex.Message);
            }

            return res;
        }

        private static void RunLmerLoop(string outputFile,
                                        List<string> chrFiles,
                                        int threadCount,
                                        MixedLinearModel nullModel,
                                        Dictionary<string, List<string>> pheno,
                                        List<int> phenoIndexes,
                                        List<int> genoIndexes,
                                        bool useAnova)
        {
            var lockWriter = new object();
            using (var writer = new StreamWriter(outputFile))
            {
                var queue = new Queue<string>(chrFiles);
                Func<string, int, RConnection, int, int> action = (fileName, port, conn, rServePid) =>
                {
                    try
                    {
                        var res = RunModelScript(conn, new List<string> { fileName }, nullModel, genoIndexes, useAnova);
                        if (res.Any())
                        {
                            lock (lockWriter)
                            {
                                var name = Path.GetFileName(fileName);
                                if (name != null)
                                    // ReSharper disable AccessToDisposedClosure
                                    writer.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}",
                                        name.Split('.')[0],
                                        res.First().Value.FScaling,
                                        res.First().Value.PValue,
                                        res.First().Value.Df,
                                        res.First().Value.FounderCoefs == null ?
                                            string.Empty :
                                            String.Join("\t", res.First().Value.FounderCoefs));
                                writer.Flush();
                                // ReSharper restore AccessToDisposedClosure
                            }
                        }
                        else
                        {
                            Console.WriteLine("No result for {0}", fileName);
                        }
                    }
                    catch (SocketException)
                    {
                        try
                        {
                            var proc = Process.GetProcessesByName("Rserve").FirstOrDefault(p => p.Id == rServePid);
                            if (proc != null)
                            {
                                proc.Kill();
                                proc.WaitForExit();
                            }
                        }
                        catch (Win32Exception) // Process was in a terminating state :(
                        {
                        }

                        // Try and restart Rserve
                        Console.WriteLine("waiting for process to terminate... ({0})", rServePid);
                        Thread.Sleep(10000);
                        Console.WriteLine("failed loading marker " + fileName);
                        rServePid = RHelper.LoadRserve(port);
                        Console.WriteLine("waiting for process to load...");
                        Thread.Sleep(10000);
                        Console.WriteLine("New Rserve process " + rServePid);
                    }

                    return rServePid;
                };

                Func<int, RConnection> createRConn = port =>
                {
                    var conn = new RConnection(port: port);
                    CreateDataFrame(conn, pheno, nullModel.AllVariables, "lst", phenoIndexes);
                    conn.VoidEval("library(lme4)");
                    conn.VoidEval("library(pbkrtest)");
                    conn.VoidEval(string.Format("nullmodel = lmer(formula = {0}, data = lst, REML=TRUE)", nullModel.ModelFormula));
                    return conn;
                };

                Action<int, int> threadAction = (port, pid) =>
                {
                    var conn = createRConn(port);
                    {
                        string file = null;
                        lock (queue)
                        {
                            if (queue.Count > 0) file = queue.Dequeue();
                        }

                        while (!string.IsNullOrEmpty(file))
                        {
                            var newPid = action(file, port, conn, pid);
                            if (newPid != pid)
                            {
                                pid = newPid;
                                conn = createRConn(port);
                                lock (queue)
                                {
                                    queue.Enqueue(file); // SNP range analysis failed
                                }
                            }
                            lock (queue)
                            {
                                file = (queue.Count > 0) ? queue.Dequeue() : null;
                            }
                        }
                        conn.Dispose();
                    }
                };

                Func<int, int, Action> createThreadAction = (port, pid) => (() => threadAction(port, pid));

                var threads = new List<Thread>();
                int rserveport = 6311;
                for (int i = 0; i < threadCount; i++)
                {
                    var pid = RHelper.LoadRserve(rserveport);
                    var th = new Thread(new ThreadStart(createThreadAction(rserveport++, pid)));
                    th.Start();
                    threads.Add(th);
                }

                foreach (var thread in threads)
                {
                    thread.Join();
                }
            }
        }

        private static void PurgeRserveProcs()
        {
            Console.WriteLine("Killing old Rserve executables");
            foreach (Process p in Process.GetProcessesByName("Rserve"))
            {
                try
                {
                    p.Kill();
                    p.WaitForExit(); // possibly with a timeout
                }
                catch (Win32Exception)
                {
                    // process was terminating or can't be terminated - deal with it
                }
                catch (InvalidOperationException)
                {
                    // process has already exited - might be able to let this one go
                }
            }
        }

        static void Main(string[] args)
        {
            ServiceContainer.EnvironmentService().IsLocal = true;
            ServicePointManager.MaxServicePoints = 10;

            if (args.Length != 6 && args.Length != 7 && args.Length != 9)
            {
                if (args.Length != 8 || args[6] != "PERM")
                {
                    Console.WriteLine("Usage: gwas.exe <SNP data directory> <covariate data file> <null model formula> <thread count> <output file> <anova|kr> [<custom filter>] [PERM <perm count>]");
                    return;                    
                }
            }

            // Kill old Rserves
            PurgeRserveProcs();

            //var chrPath = @"C:\Users\ziner_000\Desktop\thesis-diabetes\db";
            var chrPath = args[0];
            var chrFiles = Directory.GetFiles(chrPath).Where(f => f.Contains(".csv") && f.Contains("chr")).ToList();

            //chrFiles = chrFiles.Where(file => file.Contains("UNC14434690")).ToList();

            //var newPath = @"C:\Users\ziner_000\Desktop\thesis-diabetes\db\new.csv";
            var newPath = args[1];
            var data = CreateRowDictionary(File.ReadAllLines(newPath), 0);

            //var nullModel = new MixedLinearModel("AUC015 ~ Sex + (1|CC.Line)");
            var nullModel = new MixedLinearModel(args[2]);
            var useAnova = args[5] == "anova";

            var indexes = new List<int>();
            if (args.Length == 9 || args.Length == 7) // PERM and no filter args.Length == 7
            {
                var filter = args[6];
                for (int i = 0; i < data[filter.Split('=')[0]].Count; i++)
                {
                    if (data[filter.Split('=')[0]][i] == filter.Split('=')[1].Trim())
                    {
                        indexes.Add(i);                        
                    }
                }
            }
            else
            {
                for (int i = 0; i < data.First().Value.Count; i++)
                {
                    indexes.Add(i);
                }
            }

            bool perm = (args.Length == 9 && args[7] == "PERM") ||
                        (args.Length == 11);

            if (!perm)
            {
                RunLmerLoop(args[4], chrFiles, int.Parse(args[3]), nullModel, data, indexes, indexes, useAnova);
            }
            else
            {
                if (!Directory.Exists(args[4]))
                {
                    Directory.CreateDirectory(args[4]);
                }

                int permCount = int.Parse(args.Last());
                for (int i = 0; i < permCount; i++)
                {
                    var permuteIndexes = PermuteIndexes(indexes, nullModel.RandomEffectVariables.First(), data);
                    string outputFile = Path.Combine(args[4], string.Format("perm{0}", i));
                    RunLmerLoop(outputFile, chrFiles, int.Parse(args[3]), nullModel, data, indexes, permuteIndexes, useAnova);
                    PurgeRserveProcs();              
                }
            }
        }

        /// <summary>
        /// Knuth shuffle
        /// </summary>        
        public static void Shuffle<T>(T[] array)
        {
            var random = new Random();
            int n = array.Count();
            while (n > 1)
            {
                n--;
                int i = random.Next(n + 1);
                T temp = array[i];
                array[i] = array[n];
                array[n] = temp;
            }
        }

        private static List<int> PermuteIndexes(List<int> indexes, string pivot, Dictionary<string, List<string>> data)
        {
            var arr = indexes.Select(i => data[pivot][i]).ToList();
            var values = new Dictionary<string, int>();
            for(int i = 0; i < arr.Count; i++)
            {
                if (!values.ContainsKey(arr[i]))
                {
                    values[arr[i]] = i;
                }
            }

            var replacementPivot = new Dictionary<string, string>();
            var distinctPivotValues = arr.Distinct().ToArray();
            var permutedPivotValues = arr.Distinct().ToArray();
            Shuffle(permutedPivotValues);
            for(int i = 0; i < distinctPivotValues.Length; i++)
            {
                replacementPivot[distinctPivotValues[i]] = permutedPivotValues[i];
            }

            var retIndexes = new List<int>();
            for (int i = 0; i < indexes.Count; i++)
            {
                retIndexes.Add(indexes[values[replacementPivot[arr[i]]]]);
            }

            return retIndexes;
        }
    }
}
