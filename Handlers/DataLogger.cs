﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bachelor_Project
{
    public static class DataLogger
    {
        // TODO

        public static void SaveLogData(string fileName, string data)
        {
            fileName += $"_{DateTime.Now.Day}_{DateTime.Now.Month}_{DateTime.Now.Year}.txt";

            if(!File.Exists(fileName))
            {
                File.Create(fileName).Dispose();
            }

            using (StreamWriter file = new StreamWriter(fileName, append: true))
            {
                file.WriteLine($"[{DateTime.UtcNow} | {(DateTime.Now.Millisecond < 100 ? " " : "")}{(DateTime.Now.Millisecond < 10 ? " " : "")}{DateTime.Now.Millisecond} ms] : ( {data} )");
            }
        }
    }
}
