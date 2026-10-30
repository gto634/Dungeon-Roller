






















using UnityEngine;
using UnityEditor;

namespace Kino
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Bloom))]
    public class BloomEditor : Editor
    {
        BloomGraphDrawer _graph;

        SerializedProperty _threshold;
        SerializedProperty _softKnee;
        SerializedProperty _radius;
        SerializedProperty _intensity;
        SerializedProperty _highQuality;
        SerializedProperty _antiFlicker;

        static GUIContent _textThreshold = new GUIContent("Threshold (gamma)");

        void OnEnable()
        {
            _graph = new BloomGraphDrawer();
            _threshold = serializedObject.FindProperty("_threshold");
            _softKnee = serializedObject.FindProperty("_softKnee");
            _radius = serializedObject.FindProperty("_radius");
            _intensity = serializedObject.FindProperty("_intensity");
            _highQuality = serializedObject.FindProperty("_highQuality");
            _antiFlicker = serializedObject.FindProperty("_antiFlicker");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (!serializedObject.isEditingMultipleObjects) {
                EditorGUILayout.Space();
                _graph.Prepare((Bloom)target);
                _graph.DrawGraph();
                EditorGUILayout.Space();
            }

            EditorGUILayout.PropertyField(_threshold, _textThreshold);
            EditorGUILayout.PropertyField(_softKnee);
            EditorGUILayout.PropertyField(_intensity);
            EditorGUILayout.PropertyField(_radius);
            EditorGUILayout.PropertyField(_highQuality);
            EditorGUILayout.PropertyField(_antiFlicker);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
