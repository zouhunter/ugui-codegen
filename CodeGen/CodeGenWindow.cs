using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEditor;
using Rotorz.ReorderableList;
using System;
using Object = UnityEngine.Object;
using System.Reflection;
public class CodeGenWindow : EditorWindow
{
    [MenuItem("Window/CodeGen")]
    private static void Open()
    {
        window = GetWindow<CodeGenWindow>();
    }
    static CodeGenWindow window;
    private SerializedProperty script;
    private SerializedObject serializedObj;
    private List<Graphic> graphics = new List<Graphic>();
    private List<Selectable> selectables = new List<Selectable>();
    private GameObject parent;
    private GameObject target;
    private List<MonoBehaviour> scripts;
    private bool[] scriptgroup;
    private List<MonoBehaviour> _selectedScript;
    private List<MonoBehaviour> Selected
    {
        get
        {
            if (_selectedScript == null)
            {
                _selectedScript = new List<MonoBehaviour>();
            }
            else
            {
                _selectedScript.Clear();
            }
            if (scripts != null && scriptgroup != null && scripts.Count == scriptgroup.Length)
            {
                for (int i = 0; i < scriptgroup.Length; i++)
                {
                    if (scriptgroup[i])
                    {
                        _selectedScript.Add(scripts[i]);
                    }
                }
            }
            return _selectedScript;
        }
    }

    private string imgFormat = "\t[SerializeField] private Image m_{0}; \n";
    private string rawimgFormat = "\t[SerializeField] private RawImage m_{0}; \n";
    private string txtFormat = "\t[SerializeField] private Text m_{0}; \n";

    private string btnFormat = "\t[SerializeField] private Button m_{0}; \n";
    private string togFormat = "\t[SerializeField] private Toggle m_{0}; \n";
    private string inptFormat = "\t[SerializeField] private InputField m_{0}; \n";
    private string slidFormat = "\t[SerializeField] private Slider m_{0}; \n";

    private string onClickFormat = "\t\tm_{0}.onClick.AddListener(On{1}Clicked); \n";
    private string onValueChangeFormat = "\t\tm_{0}.onValueChanged.AddListener(On{1}ValueChanged); \n";

    private string btnFuncFormat = "\tprivate void On{0}Clicked()\n\t{\n\t}\n";
    private string togFuncFormat = "\tprivate void On{0}ValueChanged(bool arg)\n\t{\n\t}\n";
    private string inputFuncFormat = "\tprivate void On{0}ValueChanged(string arg)\n\t{\n\t\t\n\t}\n";
    private string sliderFuncFormat = "\tprivate void On{0}ValueChanged(float arg)\n\t{\n\t\t\n\t}\n";

    private Vector2 scrollPos;
    private void OnEnable()
    {
        serializedObj = new SerializedObject(this);
        script = serializedObj.FindProperty("m_Script");
    }
    void OnGUI()
    {
        serializedObj.Update();
        if (window == null) window = GetWindow<CodeGenWindow>();
        EditorGUILayout.PropertyField(script);
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            using (var v = new EditorGUILayout.VerticalScope(GUILayout.Width(window.position.width * 0.3f)))
            {
                GUI.backgroundColor = Color.green;
                var rect = v.rect;
                rect.height = window.position.height - EditorGUIUtility.singleLineHeight;
                GUI.Box(rect, "");
                GUI.backgroundColor = Color.white;
                DrawAutoImport();
                DrawGraphis();
                DrawSelectables();
            }
            using (var v = new EditorGUILayout.VerticalScope(GUILayout.Width(window.position.width * 0.7f)))
            {
                DrawObjectItem();
                DrawToolButtons();
            }
        }
        serializedObj.ApplyModifiedProperties();
    }

    private void DrawAutoImport()
    {
        using (var h = new EditorGUILayout.HorizontalScope())
        {
            var rect = h.rect;
            rect.width /= 3f;
            rect.height = EditorGUIUtility.singleLineHeight;
            parent = EditorGUI.ObjectField(rect, parent, typeof(GameObject), true) as GameObject;
            rect.x += rect.width;
            rect.width /= 2f;
            if (GUI.Button(rect, "btn"))
            {
                if (parent == null) return;
                Recursive(parent.transform, (tran) =>
                {
                    Button obj = tran.GetComponent<Button>();
                    if (obj != null && !selectables.Contains(obj))
                    {
                        selectables.Add(obj);
                    }
                });

            }
            rect.x += rect.width;
            if (GUI.Button(rect, "tog"))
            {
                if (parent == null) return;
                Recursive(parent.transform, (tran) =>
                {
                    Toggle obj = tran.GetComponent<Toggle>();
                    if (obj != null && !selectables.Contains(obj))
                    {
                        selectables.Add(obj);
                    }
                });
            }
            rect.x += rect.width;
            if (GUI.Button(rect, "ipt"))
            {
                if (parent == null) return;
                Recursive(parent.transform, (tran) =>
                {
                    InputField obj = tran.GetComponent<InputField>();
                    if (obj != null && !selectables.Contains(obj))
                    {
                        selectables.Add(obj);
                    }
                });
            }
            rect.x += rect.width;
            if (GUI.Button(rect, "sid"))
            {
                if (parent == null) return;
                Recursive(parent.transform, (tran) =>
                {
                    Slider obj = tran.GetComponent<Slider>();
                    if (obj != null && !selectables.Contains(obj))
                    {
                        selectables.Add(obj);
                    }
                });
            }
        }
        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }

    private void DrawSelectables()
    {
        ReorderableListGUI.Title("交互控件");
        ReorderableListGUI.ListField<Selectable>(selectables, SelectableDrawer);
    }


    private void DrawGraphis()
    {
        ReorderableListGUI.Title("显示控件");
        ReorderableListGUI.ListField<Graphic>(graphics, GrapDrawer);
    }

    private Graphic GrapDrawer(Rect position, Graphic item)
    {
        item = (Graphic)EditorGUI.ObjectField(position, item, typeof(Graphic), true);
        return item;
    }
    private Selectable SelectableDrawer(Rect position, Selectable item)
    {
        item = (Selectable)EditorGUI.ObjectField(position, item, typeof(Selectable), true);
        return item;
    }

    private void DrawObjectItem()
    {
        using (var hor = new EditorGUILayout.HorizontalScope(GUILayout.Height(EditorGUIUtility.singleLineHeight)))
        {
            EditorGUILayout.SelectableLabel("[目标对象:]");
            target = EditorGUILayout.ObjectField(target, typeof(GameObject), true) as GameObject;
            if (GUILayout.Button("加载脚本"))
            {
                if (target != null)
                {
                    scripts = new List<MonoBehaviour>();
                    var allscripts = target.GetComponents<MonoBehaviour>();
                    for (int i = 0; i < allscripts.Length; i++)
                    {
                        var namesp = allscripts[i].GetType().Namespace;
                        if (namesp == null || !namesp.Contains("UnityEngine"))
                        {
                            scripts.Add(allscripts[i]);
                        }
                    }
                    scriptgroup = new bool[scripts.Count];
                }
            }
        }
        using (var ver = new EditorGUILayout.VerticalScope(GUILayout.Height(window.position.height * 0.7f - 2 * EditorGUIUtility.singleLineHeight)))
        {
            GUI.backgroundColor = Color.yellow;
            GUI.Box(ver.rect, "");
            GUI.backgroundColor = Color.white;
            if (!(scripts == null || scriptgroup == null || scripts.Count != scriptgroup.Length))
            {

                using (var hor = new EditorGUILayout.ToggleGroupScope("选择脚本", true))
                {
                    for (int i = 0; i < scripts.Count; i++)
                    {
                        scriptgroup[i] = EditorGUILayout.ToggleLeft(scripts[i].GetType().Name, scriptgroup[i]);
                    }
                }
            }

            using (var scr = new EditorGUILayout.ScrollViewScope(scrollPos))
            {
                scrollPos = scr.scrollPosition;
                EditorGUILayout.LabelField(new GUIContent("[Text格式]:" + btnFormat));
                EditorGUILayout.LabelField(new GUIContent("[Image格式]:" + btnFormat));
                EditorGUILayout.LabelField(new GUIContent("[Button格式]:" + btnFormat));
                EditorGUILayout.LabelField(new GUIContent("[Toggle格式]:" + btnFormat));
                EditorGUILayout.LabelField(new GUIContent("[Slider格式]:" + btnFormat));
                EditorGUILayout.LabelField(new GUIContent("[InputField格式]:" + btnFormat));

                EditorGUILayout.LabelField(new GUIContent("[OnClick格式]:" + onClickFormat));
                EditorGUILayout.LabelField(new GUIContent("[OnValueChange格式]:" + onValueChangeFormat));

                EditorGUILayout.LabelField(new GUIContent("[Button事件]:" + btnFuncFormat));
                EditorGUILayout.LabelField(new GUIContent("[Toggle事件]:" + togFuncFormat));
                EditorGUILayout.LabelField(new GUIContent("[Slider事件]:" + sliderFuncFormat));
                EditorGUILayout.LabelField(new GUIContent("[InputField事件]:" + inputFuncFormat));

            }
        }

    }

    private void DrawToolButtons()
    {
        var height = window.position.height * 0.2f;

        using (var hor = new EditorGUILayout.HorizontalScope(GUILayout.Height(height)))
        {
            if (GUILayout.Button("复制代码", GUILayout.Height(height)))
            {
                TextEditor p = new TextEditor();
                p.text = GetCodeStr();
                p.OnFocus();
                p.Copy();
                Debug.Log(p.text);
            }
            if (GUILayout.Button("保存到脚本", GUILayout.Height(height)))
            {
                if (!(scripts == null || scriptgroup == null || scripts.Count != scriptgroup.Length))
                {
                    for (int i = 0; i < Selected.Count; i++)
                    {
                        MonoScript scr = MonoScript.FromMonoBehaviour(Selected[i]);
                        string newCode = scr.text.Remove(scr.text.LastIndexOf('}')) + GetCodeStr() + "}";
                        string path = AssetDatabase.GetAssetPath(scr);
                        System.IO.File.WriteAllText(System.IO.Path.GetFullPath(path), newCode, System.Text.Encoding.UTF8);
                    }
                }
                AssetDatabase.Refresh();
            }
            if (GUILayout.Button("连接到ui", GUILayout.Height(height)))
            {
                if (!(scripts == null || scriptgroup == null || scripts.Count != scriptgroup.Length))
                {
                    for (int i = 0; i < Selected.Count; i++)
                    {
                        Type type = Selected[i].GetType();

                        TraverseSelectable((sele) =>
                        {
                            type.InvokeMember("m_" + sele.name,
                                BindingFlags.SetField |
                                BindingFlags.Instance |
                                BindingFlags.NonPublic,
                                null, Selected[i], new object[] { sele }, null, null, null);
                        });
                        TraverseGraphic((grap) =>
                        {
                            type.InvokeMember("m_" + grap.name,
                               BindingFlags.SetField |
                               BindingFlags.Instance |
                               BindingFlags.NonPublic,
                               null, Selected[i], new object[] { grap }, null, null, null);
                        });
                    }

                   
                }

            }
        }
    }

    private string GetCodeStr()
    {
        string str = "";
        #region 记录全局变量
        TraverseGraphic((gra) =>
        {
            if (gra is Image)
            {
                str += string.Format(imgFormat, gra.name);
            }
            else if (gra is Text)
            {
                str += string.Format(txtFormat, gra.name);
            }
            else if (gra is RawImage)
            {
                str += string.Format(rawimgFormat, gra.name);
            }

        });
        TraverseSelectable((sele) =>
        {
            if (sele is Button)
            {
                str += string.Format(btnFormat, sele.name);
            }
            else if (sele is Toggle)
            {
                str += string.Format(togFormat, sele.name);
            }
            else if (sele is Slider)
            {
                str += string.Format(slidFormat, sele.name);
            }
            else if (sele is InputField)
            {
                str += string.Format(inptFormat, sele.name);
            }
        });
        #endregion

        #region 记录事件注册
            str += "\tprivate void Awake()\n\t{\n";
        TraverseSelectable((sele) =>
        {
            if (sele is Button)
            {
                str += string.Format(onClickFormat, sele.name, sele.name[0].ToString().ToUpper() + sele.name.Substring(1));
            }
            else if (sele is Toggle || sele is Slider || sele is InputField)
            {
                str += string.Format(onValueChangeFormat, sele.name, sele.name[0].ToString().ToUpper() + sele.name.Substring(1));
            }
        });
            str += "\t}\n";
        #endregion

        #region 记录方法
        TraverseSelectable((sele) =>
        {
            if (sele is Button)
            {
                str += btnFuncFormat.Replace("{0}", sele.name[0].ToString().ToUpper() + sele.name.Substring(1));
            }
            else if (sele is Toggle)
            {
                str += togFuncFormat.Replace("{0}", sele.name[0].ToString().ToUpper() + sele.name.Substring(1));
            }
            else if (sele is Slider)
            {
                str += sliderFuncFormat.Replace("{0}", sele.name[0].ToString().ToUpper() + sele.name.Substring(1));
            }
            else if (sele is InputField)
            {
                str += inputFuncFormat.Replace("{0}", sele.name[0].ToString().ToUpper() + sele.name.Substring(1));
            }
        });
        #endregion
        return str;
    }

    private void TraverseGraphic(UnityAction<Graphic> Get)
    {
        for (int i = 0; i < graphics.Count; i++)
        {
            Get(graphics[i]);
        }
    }
    private void TraverseSelectable(UnityAction<Selectable> Get)
    {
        for (int i = 0; i < selectables.Count; i++)
        {
            Get(selectables[i]);
        }
    }
    /// <summary>
    /// 遍历操作
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="Func"></param>
    public static void Recursive(Transform parent, UnityAction<Transform> Func)
    {
        Func(parent);
        if (parent.childCount >= 0)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Recursive(child, Func);
            }
        }
    }
}
