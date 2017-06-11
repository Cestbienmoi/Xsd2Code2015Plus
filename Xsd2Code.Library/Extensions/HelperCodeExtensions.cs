using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xsd2Code.Library.Extensions
{
    public static class HelperCodeExtensions
    {
        public static string GetFieldNameFromProperty(this CodeMemberProperty prop)
        {
            var propReturnStatment = prop.GetStatements[0] as CodeMethodReturnStatement;
            if (propReturnStatment != null)
            {
                var field = propReturnStatment.Expression as CodeFieldReferenceExpression;
                if (field != null)
                {
                    return field.FieldName;
                }
            }
            return null;
        }

        public static IEnumerable<T> GetMemberFiltred<T>(this CodeTypeDeclaration type) where T : class
        {
            foreach (var member in type.Members)
            {
                var field = member as T;
                if (field != null)
                    yield return field;
            }
        }

        public static CodeMemberField GetField(this CodeTypeDeclaration type, string fieldName)
        {
            return type.GetMemberFiltred<CodeMemberField>().FirstOrDefault(el => el.Name == fieldName);
        }

        public static bool CheckFieldInCtor(this CodeConstructor ctor, string fieldName)
        {
            foreach (var statement in ctor.Statements)
            {
                var codeAssignStatement = statement as CodeAssignStatement;
                if (codeAssignStatement == null) continue;
                var code = codeAssignStatement.Left as CodeFieldReferenceExpression;
                if (code != null)
                {
                    if (code.FieldName == fieldName)
                        return true;
                }
            }
            return false;
        }
    }
}
