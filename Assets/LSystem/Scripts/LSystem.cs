using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace LSystem
{
    public class LSystem
    {
        private readonly float default_dist;
        private readonly short default_angle;

        private readonly uint default_cross_sections;
        private readonly uint default_cross_section_divisions;

        private readonly Vector3 initial_direction;

        /// <summary>
        /// An example for the output of the L-System.
        /// </summary>
        public static readonly List<Tuple<Vector3, Vector3>> EXAMPLE = new() { new(Vector3.zero, new(0f, 10f, 0f)), new(new(0f, 10f, 0f), new(0f, 20f, 0f)), new(new(0f, 20f, 0f), new(-10f, 20f, 0f)), new(new(0f, 20f, 0f), new(10f, 20f, 0f)), new(new(0f, 20f, 0f), new(0f, 30f, 0f)), new(new(0f, 30f, 0f), new(-5f, 38.66f, 0f)), new(new(0f, 30f, 0f), new(5f, 38.66f, 0f)) };
        /// <summary>
        /// terminal symbols of the l-system
        /// </summary>
        public static readonly List<char> TERMINALS = new() { 'F', '+', '-', '&', '^', '\\', '/', '|', '*', '[', ']', 'S' };

        public List<Tuple<Vector3, Vector3>> segments;
        public List<Tuple<int, char>> fromRule;

        /// <summary>
        /// Initializes an L-System.
        /// </summary>
        /// <param name="d">Default forward distance of the turtle.</param>
        /// <param name="a">Default angle of the turtle</param>
        /// <param name="cs">Default number of cross sections for the drawing.</param>
        /// <param name="csd">Default number of cross section divisions for the drawing.</param>
        /// <param name="init_dir">Initial direction of the turtle.</param>
        // public LSystem(float d, short a, uint cs, uint csd, INITIAL_DIRECTION init_dir)
        // {
        //     default_dist = d;
        //     default_angle = a;
        //     default_cross_sections = cs;
        //     default_cross_section_divisions = csd;
        //     initial_direction = init_dir switch
        //     {
        //         INITIAL_DIRECTION.UP => Vector3.up,
        //         INITIAL_DIRECTION.DOWN => Vector3.down,
        //         INITIAL_DIRECTION.LEFT => Vector3.left,
        //         INITIAL_DIRECTION.RIGHT => Vector3.right,
        //         INITIAL_DIRECTION.FORWARD => Vector3.forward,
        //         INITIAL_DIRECTION.BACK => Vector3.back,
        //         INITIAL_DIRECTION.DIAGONAL => Vector3.one,
        //         _ => Vector3.down,
        //     };
        // }

        public LSystem(float d, short a, uint cs, uint csd, INITIAL_DIRECTION init_dir, string start, uint iterations, Dictionary<char, string> rules, bool translatePointsOfList, bool printResult = false)
        {
            default_dist = d;
            default_angle = a;
            default_cross_sections = cs;
            default_cross_section_divisions = csd;
            initial_direction = init_dir switch
            {
                INITIAL_DIRECTION.UP => Vector3.up,
                INITIAL_DIRECTION.DOWN => Vector3.down,
                INITIAL_DIRECTION.LEFT => Vector3.left,
                INITIAL_DIRECTION.RIGHT => Vector3.right,
                INITIAL_DIRECTION.FORWARD => Vector3.forward,
                INITIAL_DIRECTION.BACK => Vector3.back,
                INITIAL_DIRECTION.DIAGONAL => Vector3.one,
                _ => Vector3.down,
            };
            fromRule = new();
            segments = Evaluate(start, iterations, rules, translatePointsOfList, printResult);
            // fromRule = new List<Tuple<int, char>>();
        }

        private Tuple<char, float[]> ParseArguments(ref string str, ref int index, char symbol, float default_value)
        {
            Tuple<char, float[]> result;
            if (index < str.Length - 1 && str[index + 1] == '(')
            {
                // search index of ')'
                var end = index + str[index..].IndexOf(')');
                var tmp = str[(index + 2)..end];
                var d = float.Parse(tmp);
                result = new(symbol, new float[] { d });
                index = end + 1;
            }
            else
            {
                result = new(symbol, new float[] { default_value });
                index++;
            }
            return result;
        }

        // TODO: Houdini Syntax; in grammatik variable länge erlauben
        // https://www.sidefx.com/docs/houdini/nodes/sop/lsystem.html
        private List<Tuple<char, float[]>> Tokenize(string str)
        {
            List<Tuple<char, float[]>> tokens = new();
            for (int i = 0; i < str.Length;)
            {
                switch (str[i])
                {
                    case 'F':
                        tokens.Add(ParseArguments(ref str, ref i, str[i], default_dist));
                        break;
                    case '+':
                        tokens.Add(ParseArguments(ref str, ref i, str[i], default_angle));
                        break;
                    case '-':
                        tokens.Add(ParseArguments(ref str, ref i, str[i], default_angle));
                        break;
                    case '&':
                        tokens.Add(ParseArguments(ref str, ref i, str[i], default_angle));
                        break;
                    case '^':
                        tokens.Add(ParseArguments(ref str, ref i, str[i], default_angle));
                        break;
                    case '\\':
                        tokens.Add(ParseArguments(ref str, ref i, str[i], default_angle));
                        break;
                    case '/':
                        tokens.Add(ParseArguments(ref str, ref i, str[i], default_angle));
                        break;
                    default:
                        tokens.Add(new(str[i], new float[0]));
                        i++;
                        break;
                }
            }
            return tokens;
        }

        private double ConvertToRadians(double angle) => (Math.PI / 180) * angle;

        private Vector3 CalculateEnd2D(int rot, Vector3 start, int dist)
        {
            var x = start.x + dist * (float)Math.Cos(ConvertToRadians(rot));
            var y = start.y + dist * (float)Math.Sin(ConvertToRadians(rot));
            return new Vector3(x, y, 0);
        }

        private Vector3 CalculateEnd3D(Vector3 direction, Vector3 start, int dist) => start + dist * direction;

        private List<Tuple<Vector3, Vector3>> Turtle3D(List<Tuple<char, float[]>> tokens)
        {
            Vector3 scarlet_rot = initial_direction;
            Vector3 current_pos = Vector3.zero;
            List<Tuple<Vector3, Vector3>> tuples = new();
            Stack<Tuple<Vector3, Vector3>> st = new();
            foreach (var e in tokens)
            {
                switch (e.Item1)
                {
                    case 'F':
                        // var end = CalculateEnd3D(scarlet_rot, current, e.Item2[0]);
                        var endpoint = current_pos + e.Item2[0] * scarlet_rot; // new endpoint
                        tuples.Add(new(current_pos, endpoint));
                        current_pos = endpoint; // last endpoint = current position 
                        break;
                    case '+': // turn right
                        scarlet_rot = Quaternion.Euler(0, 0, e.Item2[0]) * scarlet_rot;
                        break;
                    case '-': // turn left
                        scarlet_rot = Quaternion.Euler(0, 0, -e.Item2[0]) * scarlet_rot;
                        break;
                    case '&': // pitch up
                        scarlet_rot = Quaternion.Euler(e.Item2[0], 0, 0) * scarlet_rot;
                        break;
                    case '^': // pitch down
                        scarlet_rot = Quaternion.Euler(-e.Item2[0], 0, 0) * scarlet_rot;
                        break;
                    case '\\': // roll clockwise
                        scarlet_rot = Quaternion.Euler(0, e.Item2[0], 0) * scarlet_rot;
                        break;
                    case '/': // roll counter-clockwise
                        scarlet_rot = Quaternion.Euler(0, -e.Item2[0], 0) * scarlet_rot;
                        break;
                    case '|': // turn 180 deg
                        scarlet_rot = Quaternion.Euler(0, 0, 180) * scarlet_rot;
                        break;
                    case '*': // roll 180 deg
                        scarlet_rot = Quaternion.Euler(0, 180, 0) * scarlet_rot;
                        break;
                    case '[':
                        st.Push(new(scarlet_rot, current_pos));
                        break;
                    case ']':
                        var tmp = st.Pop();
                        scarlet_rot = tmp.Item1;
                        current_pos = tmp.Item2;
                        break;
                    default:
                        break;
                }
            }
            return tuples;
        }

        private string Parse(string s, uint it, Dictionary<char, string> rules)
        {   
            string a = "";
            string w = s;
            for (int i = 0; i < it; i++)
            {
                string temp = "";
                string at = "";
                int j = 0;
                foreach (var c in w)
                {
                    if (rules.ContainsKey(c))
                    {
                        // NT
                        var replacement = rules[c];
                        temp += replacement;
                        //foreach (var n in replacement) if (n == 'F') fromRule.Add(new(i, c));
                        foreach(var n in replacement) at += c.ToString();
                    }
                    else
                    {
                        // Terminal
                        temp += c;
                        //if (i == 0 && c == 'F') fromRule.Add(new(0, 'S')); // for F's in the start string
                        if(i == 0) at += "S";
                        else at += a[j];
                    }
                    j += 1;
                }
                w = temp;
                a = at;
            }
            for(int i = 0; i<w.Length; i++){
                if(w[i] == 'F') fromRule.Add(new Tuple<int,char>(0,a[i]));
            }
            return w;
        }

        private List<Tuple<Vector3, Vector3>> TranslatePoints(List<Tuple<Vector3, Vector3>> l)
        {
            var miny = 0f;
            foreach (var t in l)
            {
                var m = Math.Min(t.Item1.y, t.Item2.y);
                if (miny > m) miny = m;
            }
            Vector3 translation = new(0, Math.Abs(miny), 0);
            List<Tuple<Vector3, Vector3>> newlist = new();
            foreach (var t in l)
            {
                newlist.Add(new(t.Item1 + translation, t.Item2 + translation));
            }
            return newlist;
        }

        /// <summary>
        /// Constructs the result string from the supplied start string and rules for the specified number of iterations and
        /// returns a list of <start,end> 3D-points that the turtle has drawn.
        /// </summary>
        /// <param name="start">The initial string that will be expanded.</param>
        /// <param name="iterations">The number of iterations that the replacements will be applied for.</param>
        /// <param name="rules">The replacement rules.</param>
        /// <param name="printResult">If set to true, will print the list of <start,end> points to the console. Default: false</param>
        /// <returns>The list of <start,end> 3D-points the turtle has drawn.</returns>
        public List<Tuple<Vector3, Vector3>> Evaluate(string start, uint iterations, Dictionary<char, string> rules, bool translatePointsOfList, bool printResult = false)
        {
            var expanded = Parse(start, iterations, rules);
            if (printResult) Debug.Log(expanded);
            // List<char> non_terminals = new(rules.Keys);
            var tokens = Tokenize(expanded);
            PrintList(tokens);
            var res = Turtle3D(tokens);
            if (translatePointsOfList) res = TranslatePoints(res);
            if (printResult) PrintList(res);
            return res;
        }

        /// <summary>
        /// Prints a list of tuples of 3D Vectors.
        /// </summary>
        /// <param name="l">The list that shall be printed.</param>
        public static void PrintList<T, T2>(List<Tuple<T, T2>> l)
        {
            string o = "[  ";
            foreach (var e in l)
            {
                o += e.ToString() + "  ";
            }
            Debug.Log(o + "]");
        }
    }
}