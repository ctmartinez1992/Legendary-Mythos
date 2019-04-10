using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LM.NameGenerator {
    public struct WeightedName {
        public string Name;
        public int Weight;

        public WeightedName(string name, int weight) {
            Name = name;
            Weight = weight;
        }
    }

    public class NameGenerator {
        private int seed;
        private readonly MarkovChain chain;

        private Dictionary<string, bool> originalNames = new Dictionary<string, bool>();
        private Dictionary<string, bool> generatedNames = new Dictionary<string, bool>();
        
        public IEnumerable<string> OriginalNames { get { return originalNames.Keys; } }
                
        public NameGenerator(int order, IEnumerable<string> names, int seed = 0) {
            chain = new MarkovChain(order);

            foreach(var name in names) {
                chain.Add(name, 1);
                originalNames[name] = true;
            }

            this.seed = seed;
        }

        //names - List of weighted names to base generator on.
        public NameGenerator(int order, IEnumerable<WeightedName> wnames, int seed = 0) {
            chain = new MarkovChain(order);

            foreach(var wn in wnames) {
                chain.Add(wn.Name, wn.Weight);
                originalNames[wn.Name] = true;
            }

            this.seed = seed;
        }

        //Returns a random name.
        public string Next() {
            return chain.Chain(seed++);
        }

        //Returns a random name, making sure not to generate a name that was given in the training data.
        public string NextNew() {
            string name;

            while(true) {
                name = Next();
                if(!originalNames.ContainsKey(name)) {
                    break;
                }
            }

            return name;
        }

        //Returns a random name, making sure not to generate a name that was given in the training data or that was previously returned by this function.
        public string NextUnique() {
            string name;

            while(true) {
                name = NextNew();
                if(!generatedNames.ContainsKey(name)) {
                    generatedNames[name] = true;
                    break;
                }
            }

            return name;
        }

        //Returns all generatable names of a certain length or less.
        //maxlen - Max returned name length.
        public IEnumerable<string> AllRaw(int maxLength) {
            return chain.AllRaw(maxLength);
        }
    }
}