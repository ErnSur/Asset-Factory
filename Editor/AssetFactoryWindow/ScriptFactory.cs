using UnityEngine;
using System;
using System.Text;
using UnityEditor;
using System.CodeDom;
using Microsoft.CSharp;
using System.IO;
using System.CodeDom.Compiler;

namespace QuickEye.Scaffolding
{
    [Serializable]
    [SerializeField]
    public class ScriptTemplate { public TextAsset template; public string path; }

    public class AssetFactorySettings : ScriptableObject
    {
        [SerializeField]
        private ScriptTemplate[] _templates;
    }
    //[InitializeOnLoad]
    public static class ScriptFactory
    {
        static ScriptFactory()
        {
            var type = new CodeTypeDeclaration("MonoBe");
            type.Attributes = MemberAttributes.Public;
            var field = new CodeMemberField(typeof(int),"_hp");
            var refer = new CodeTypeReference(typeof(SerializeField));
            
            var attribute = new CodeAttributeDeclaration();
            
            field.CustomAttributes.Add(attribute);
            var field2 = new CodeMemberField(typeof(int),"_mana");
            type.Members.Add(field);
            var provider = new CSharpCodeProvider();
            var sw = new StringWriter();
            var options = new CodeGeneratorOptions();

            provider.GenerateCodeFromMember(field, sw, options);
            provider.GenerateCodeFromMember(field2, sw, options);
            Debug.Log($"Field: {sw}");

            
        }

        public class ScriptContent
        {
            public string[] usingNamespaces;
            public string @namespace;
            public string typeName;
            public CodeMemberField[] fields;
            public CodeMemberMethod[] methods;
        }

        public static string CreateScriptText(ScriptContent content, string template)
        {
            template = AddNamespaces(content, template);

            var @namespace = content.@namespace;

            template = template.Replace("#NAMESPACE#", @namespace);

            template = template.Replace("#SCRIPTNAME#", content.typeName);
            template = AddFields(content, template);

            //template = template.Replace("#METHODS#", content.methods);

            template = template.Replace("#NOTRIM#", "");

            return template;
        }

        private static string AddFields(ScriptContent content, string template)
        {
            var fieldsIndex = template.IndexOf("#FIELDS#");

            var endOfLineIndex = template.LastIndexOf('\n', fieldsIndex);
            var indent = new string(' ', fieldsIndex - endOfLineIndex - 1);

            var sb = new StringBuilder();
            foreach (var field in content.fields)
            {
                sb.Append($"\n{field};");
            }
                //template = template.

            //var indentedFields = content.fields.Replace("\n", Environment.NewLine + indent);
            //template = template.Replace("#FIELDS#", indentedFields);
            return template;
        }

        private static string AddNamespaces(ScriptContent content, string template)
        {
            var usingsEndIndex = template.IndexOf("#USINGSEND#");

            foreach (var namescp in content.usingNamespaces)
            {
                if (string.IsNullOrEmpty(namescp)) continue;
                if (template.LastIndexOf(namescp, usingsEndIndex) == -1)
                {
                    var newLine = $"\nusing {namescp};";
                    template = template.Insert(usingsEndIndex, newLine);
                    usingsEndIndex += newLine.Length;
                }
            }
            template = template.Replace("#USINGSEND#", string.Empty);
            return template;
        }
    }
}