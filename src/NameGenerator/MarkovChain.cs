using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LM.MyRNG;
using LM.MyRNG.Generators;

namespace LM.NameGenerator {
    internal class MarkovChain {
        private readonly int order;

        private readonly Dictionary<string, Dictionary<char, int>> items = new Dictionary<string, Dictionary<char, int>>();
        private readonly Dictionary<string, int> terminals = new Dictionary<string, int>();

        //order - Indicates the desired order of the MarkovChain. It's the depth of its internal state.
        //A generator with an order of 1 will choose items based on the previous item;
        //A generator with an order of 2 will choose items based on the previous 2 items, and so on.
        //Choosing 0 has the effect that every state is equivalent to the starting state, so items will be chosen based on their total frequency.
        public MarkovChain(int order) {
            if(order < 0) {
                throw new ArgumentOutOfRangeException("order");
            }

            this.order = order;
        }

        //Adds the items to the generator with the weight specified.
        //items - The items to add to the generator.
        //weight - The weight at which to add the items.
        public void Add(string items, int weight) {
            int startIndex = 0;
            int length = 0;
            string previous = "";

            foreach(char item in items) {
                string key = previous;

                this.Add(key, item, weight);

                length++;
                if(length > order) {
                    length--;
                    startIndex++;
                }

                previous = SafeSubstring(items, startIndex, length);
            }

            string terminalKey = previous;
            terminals[terminalKey] = terminals.ContainsKey(terminalKey) ? weight + terminals[terminalKey] : weight;
        }

        private void Add(string state, char next, int weight) {
            Dictionary<char, int> weights;
            if(!items.TryGetValue(state, out weights)) {
                weights = new Dictionary<char, int>();
                items.Add(state, weights);
            }

            int oldWeight;
            weights.TryGetValue(next, out oldWeight);
            weights[next] = oldWeight + weight;
        }

        //Randomly runs through the chain.
        public string Chain() {
            return this.Chain("", new House(true));
        }

        //previous - The items preceding the first item in the chain.
        public string Chain(string previous) {
            return this.Chain(previous, new House(true));
        }
        
        public string Chain(int seed) {
            return this.Chain("", new House(true));
        }
        
        public string Chain(string previous, int seed) {
            return this.Chain(previous, new House(true));
        }
        
        public string Chain(RNG rng) {
            return this.Chain("", rng);
        }

        //previous - The items preceding the first item in the chain.
        public string Chain(string previous, RNG rng) {
            StringBuilder result = new StringBuilder();
            Queue<char> state = new Queue<char>(previous);

            while(true) {
                while(state.Count > order) {
                    state.Dequeue();
                }

                var key = new string(state.ToArray());

                Dictionary<char, int> weights;
                if(!items.TryGetValue(key, out weights)) {
                    return result.ToString();
                }

                int terminalWeight;
                terminals.TryGetValue(key, out terminalWeight);

                var total = weights.Sum(w => w.Value);
                var value = rng.GetInt32(total + terminalWeight) + 1;

                if(value > total) {
                    return result.ToString();
                }

                var currentWeight = 0;
                foreach(var nextItem in weights) {
                    currentWeight += nextItem.Value;
                    if(currentWeight >= value) {
                        result.Append(nextItem.Key);
                        state.Enqueue(nextItem.Key);
                        break;
                    }
                }
            }
        }

        internal IEnumerable<string> AllRaw(int maxlen) {
            return AllRaw("", maxlen);
        }

        internal IEnumerable<string> AllRaw(string prefix, int maxlen) {
            if(prefix.Length > order) {
                throw new ArgumentException(string.Format("prefix should be fewer than {0} chars long", order));
            }

            var queue = new Queue<string>();
            queue.Enqueue(prefix);

            while(queue.Count > 0) {
                var current = queue.Dequeue();
                if(current.Length > maxlen) {
                    continue;
                }
                var suffix = GetLast(current, order);

                //First, see if it's a possible terminal state and if so, return it.
                int terminalWeight;
                terminals.TryGetValue(suffix, out terminalWeight);
                if(terminalWeight > 0) {
                    yield return current;
                }

                //Next, enqueue all the possible extensions.
                Dictionary<char, int> weights;
                if(!items.TryGetValue(suffix, out weights)) {
                    continue;
                }

                foreach(var kvp in weights) {
                    var nextChar = kvp.Key;
                    var weight = kvp.Value;
                    if(weight == 0) {
                        continue;
                    }

                    queue.Enqueue(current + nextChar);
                }
            }
        }
        
        private static string SafeSubstring(string text, int start, int length) {
            return (text.Length <= start) ? ("") : ((text.Length - start <= length) ? (text.Substring(start)) : (text.Substring(start, length)));
        }

        private string GetLast(string prefix, int length) {
            if(prefix.Length <= length) {
                return prefix;
            }

            return prefix.Substring(prefix.Length - length);
        }
    }
}