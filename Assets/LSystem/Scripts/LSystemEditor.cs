using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using MarchingCubesProject;
using UnityEngine;

namespace LSystem
{
    public enum INITIAL_DIRECTION { UP, DOWN, LEFT, RIGHT, FORWARD, BACK, DIAGONAL };
    public class LSystemEditor : MonoBehaviour
    {
        [Header("Default Settings")]
        [Tooltip("Default forward distance")]
        public float m_Distance = 1f;
        [Tooltip("Default turning angle")]
        public short m_Angle = 90;

        [Tooltip("Initial direction the turtle faces")]
        public INITIAL_DIRECTION m_InitialDirection = INITIAL_DIRECTION.DOWN;

        [Tooltip("Default segment thickness")]
        public float m_Thickness = .1f;

        [Tooltip("Default number of cross sections")]
        public uint m_CrossSections = 4;
        [Tooltip("Default number of cross section divisions")]
        public uint m_CrossSectionDivisions = 2;

        [Header("L-System")]
        [Tooltip("Translate points to above ground")]
        public bool m_TranslatePoints = true;
        [Tooltip("Initial string to evaluate")]
        public string m_StartString = "FABFCD";//"FF[+F][-F]F(42)";
        [Tooltip("Number of iterations to apply the replacement rules")]
        public uint m_Iterations = 1;
        [Tooltip("Replacement rules")]
        public string[] m_Rules = { "A=[+F(1.5)]", "B=[-F(1.5)]", "C=[+(30)F(1.5)C]", "D=[-(30)F(1.5)D]" };

        private Dictionary<char, string> ParseRuleInput(string[] rules)
        {
            var nt = new Dictionary<char, string>();
            foreach (var r in rules)
            {
                //check syntax
                if (r.Length < 3) throw new ArgumentException("Rule is in a wrong format");
                if (!r.Contains('=')) throw new ArgumentException("Rule needs to include an =");
                // search index of '='
                var end = r.IndexOf('=');
                var non_terminal = r[..end];
                //TODO not necessarily: non terminals may only be one character
                if (non_terminal.Length > 1) throw new ArgumentException("Non Terminals may only be exactly one character.");
                var replacement = r[(end + 1)..];
                // check if non_terminal is already contained or a terminal has the same symbol
                if (nt.ContainsKey(non_terminal[0]) || LSystem.TERMINALS.Contains(non_terminal[0])) throw new ArgumentException("Cannot add a rule with a symbol that is already in use: " + non_terminal[0]);
                else nt.Add(non_terminal[0], replacement);
            }
            return nt;
        }

        // Start is called before the first frame update
        // void Start()
        // {
        //     return;
        //     LSystem l = new(m_Distance, m_Angle, m_CrossSections, m_CrossSectionDivisions, m_InitialDirection);
        //     // var s = l.Parse("F", 1, NT);
        //     // Debug.Log(s);
        //     // var t = l.Tokenize(m_StartString);
        //     // var v = l.Turtle3D(t);
        //     // LSystem.PrintList(v);
        //     var rules = ParseRuleInput(m_Rules);
        //     Debug.Log(string.Join(Environment.NewLine, rules.Select(kvp => kvp.Key + ": " + kvp.Value.ToString())));
        //     var output = l.Evaluate(m_StartString, m_Iterations, rules, m_TranslatePoints, true);

        //     Segment[] segments = new Segment[output.Count];
        //     for (int i = 0; i < output.Count; i++)
        //     {
        //         segments[i] = new Segment(output[i].Item1, output[i].Item2, m_Thickness);
        //         Debug.Log(segments[i].startPoint + ", " + segments[i].endPoint);
        //     }


        //     // Vector3 startPoint = new Vector3(-1, -3, 0);
        //     // Vector3 endPoint = new Vector3(14, 0, 1.5f);
        //     // Segment[] segments = new Segment[] { new Segment(startPoint, endPoint, 2) };
        //     Metaball m = Metaball.BuildFromSegments(segments);


        //     //generate mesh from the metaball
        //     MeshGenerator meshGen = GetComponent<MeshGenerator>();
        //     //attributes such as the size and grid resolution can be set via component or through meshGen.gridResolution
        //     // meshGen.size = 100;
        //     // meshGen.gridResolution = 64;

        //     meshGen.Generate(m);
        // }

        // public List<Tuple<Vector3, Vector3>> Evaluate()
        // {
        //     LSystem l = new(m_Distance, m_Angle, m_CrossSections, m_CrossSectionDivisions, m_InitialDirection);
        //     var rules = ParseRuleInput(m_Rules);
        //     return l.Evaluate(m_StartString, m_Iterations, rules, true);
        // }

        public LSystem BuildLSystem()
        {
            var rules = ParseRuleInput(m_Rules);
            LSystem l = new(m_Distance, m_Angle, m_CrossSections, m_CrossSectionDivisions, m_InitialDirection, m_StartString, m_Iterations, rules, true);
            return l;
        }

        // // Update is called once per frame
        // void Update()
        // {

        // }
    }
}