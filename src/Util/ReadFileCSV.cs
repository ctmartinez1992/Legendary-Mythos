using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LM.Util {
    public static class ReadFileCSV {
        public static List<string> ReadFileCSVOneColumn(string filePath) {
            if(File.Exists(filePath)) {
                List<string> list = new List<string>();

                using(var reader = new StreamReader(filePath)) {
                    while(!reader.EndOfStream) {
                        list.Add(reader.ReadLine());
                    }
                }

                return list;
            }
            else {
                return null;
            }
        }
    }
}