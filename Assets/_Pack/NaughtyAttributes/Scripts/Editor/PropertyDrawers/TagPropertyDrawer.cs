using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NaughtyAttributes.Editor
{
    [CustomPropertyDrawer(typeof(TagAttribute))]
    public class TagPropertyDrawer : PropertyDrawerBase
    {
        protected override float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
        {
            return (property.propertyType == SerializedPropertyType.String)
                ? GetPropertyHeight(property)
                : GetPropertyHeight(property) + GetHelpBoxHeight();
        }

        protected override void OnGUI_Internal(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            if (property.propertyType == SerializedPropertyType.String)
            {

                List<string> tagList = new List<string>();
                tagList.Add("(None)");
                tagList.Add("Untagged");
                tagList.AddRange(UnityEditorInternal.InternalEditorUtility.tags);

                string propertyString = property.stringValue;
                int index = 0;


                for (int i = 1; i < tagList.Count; i++)
                {
                    if (tagList[i].Equals(propertyString, System.StringComparison.Ordinal))
                    {
                        index = i;
                        break;
                    }
                }


                int newIndex = EditorGUI.Popup(rect, label.text, index, tagList.ToArray());


                string newValue = newIndex > 0 ? tagList[newIndex] : string.Empty;

                if (!property.stringValue.Equals(newValue, System.StringComparison.Ordinal))
                {
                    property.stringValue = newValue;
                }
            }
            else
            {
                string message = string.Format("{0} supports only string fields", typeof(TagAttribute).Name);
                DrawDefaultPropertyAndHelpBox(rect, property, message, MessageType.Warning);
            }

            EditorGUI.EndProperty();
        }
    }
}
