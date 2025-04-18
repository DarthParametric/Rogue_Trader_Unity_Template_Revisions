using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.GridPatterns
{
    public class GridPatternsEditor : EditorWindow
    {
        //Path may change in future. OWLCAT_MODS left to make it easier to find.
        #if OWLCAT_MODS
        const string kGridPatternsPath = "Assets/Code/GameCore/Editor/GridPatterns/";
        #else
        const string kGridPatternsPath = "Assets/Code/GameCore/Editor/GridPatterns/";
        #endif
        const int kLeftOffset = 10;
        const int kTopOffset = 60;

        private Texture2D backTex;
        private Texture2D anchorTex;
        private Texture2D cellTex;

        private Rect FrameRect; // Frame rect where to draw grid and selected cells
        private Rect Bounds; // Grid rect which get in frame
        private Rect TextureCoords; // How to scale the image inside frame

        private Vector2 LeftTopPoint;
        private float Scale;

        private bool IsDragging;
        private Rect ResetButtonRect;
        private Rect ApplyButtonRect;

        SerializedProperty PropRef;
        LinkedList<Vector2Int> SelectedCells;

        public static void ShowWindow(SerializedProperty propRef)
        {
            var wnd = (GridPatternsEditor)GetWindow(typeof(GridPatternsEditor));
            wnd.SetEditingProperty(propRef);
        }

        private void OnEnable()
        {
            backTex = AssetDatabase.LoadAssetAtPath<Texture2D>(kGridPatternsPath + "Resources/GridCellSquare.png");
            anchorTex = AssetDatabase.LoadAssetAtPath<Texture2D>(kGridPatternsPath + "Resources/redSquare.png");
            cellTex = AssetDatabase.LoadAssetAtPath<Texture2D>(kGridPatternsPath + "Resources/yellowSquare.png");

            SelectedCells = new LinkedList<Vector2Int>();
            SelectedCells.AddLast(new Vector2Int(0, 0));

            ResetButtonRect = new Rect(10, 30, 150, 25);
            ApplyButtonRect = new Rect(170, 30, 50, 25);

            ResetGrid();
        }

        private void SetEditingProperty(SerializedProperty propRef)
        {
            PropRef = propRef;
            SelectedCells.Clear();
            if (PropRef != null && PropRef.arraySize > 0)
            {
                for (int i = 0; i < PropRef.arraySize; i++)
                {
                    Vector2Int c = PropRef.GetArrayElementAtIndex(i).vector2IntValue;
                    Vector2Int toAdd = new Vector2Int(c.x, -c.y);
                    if (!SelectedCells.Contains(toAdd))
                    {
                        SelectedCells.AddLast(toAdd);
                    }
                }
            }
            else
            {
                SelectedCells.AddLast(new Vector2Int(0, 0));
            }
            ResetGrid();
        }

        private void OnGUI()
        {
            Scale = EditorGUILayout.Slider("Scale", Scale, 20, 100);
            if (GUI.Button(ResetButtonRect, "Reset position and scale"))
            {
                ResetGrid();
            }
            if (GUI.Button(ApplyButtonRect, "Apply"))
            {
                Apply();
            }
            ProcessEvents();
            DrawGrid();

            FillCells();
        }

        private void Apply()
        {
            if (PropRef != null)
            {
                PropRef.ClearArray();
                int size = PropRef.arraySize;
                int i = 0;
                foreach (var c in SelectedCells)
                {
                    PropRef.InsertArrayElementAtIndex(i);
                    PropRef.GetArrayElementAtIndex(i).vector2IntValue = new Vector2Int(c.x, -c.y);
                    i++;
                }
                PropRef.serializedObject.ApplyModifiedProperties();
            }
        }

        private void ResetGrid()
        {
            Scale = 35;

            float x = kLeftOffset;
            float y = kTopOffset;
            float w = position.width - 2 * x;
            float h = position.height - y - 10;
            LeftTopPoint = new Vector2(-(w - Scale) / 2, -h + Scale * 1.5f);
        }

        private void FillCells()
        {
            Bounds = new Rect(LeftTopPoint.x, LeftTopPoint.y, FrameRect.width, FrameRect.height);
            foreach (var cell in SelectedCells)
            {
                Rect cellRect = new Rect(cell.x * Scale, cell.y * Scale, Scale, Scale);
                if (Bounds.Overlaps(cellRect))
                {
                    float x = Math.Max(Bounds.xMin, cellRect.xMin) - Bounds.xMin + kLeftOffset;
                    float y = Math.Max(Bounds.yMin, cellRect.yMin) - Bounds.yMin + kTopOffset;
                    float w = Math.Min(Bounds.xMax, cellRect.xMax) - Bounds.xMin + kLeftOffset - x;
                    float h = Math.Min(Bounds.yMax, cellRect.yMax) - Bounds.yMin + kTopOffset - y;
                    Texture2D tex = cell == SelectedCells.First.Value ? anchorTex : cellTex;
                    GUI.DrawTexture(new Rect(x, y, w, h), tex);
                }
            }
        }

        private void ProcessEvents()
        {
            var ev = Event.current;
            if (!FrameRect.Contains(ev.mousePosition))
            {
                return;
            }

            if (ev.type == EventType.MouseDown && ev.button == 1)
            {
                IsDragging = true;
            }
            else if (ev.type == EventType.MouseUp && ev.button == 1)
            {
                IsDragging = false;
            }
            else if (ev.type == EventType.MouseDrag && ev.button == 1 && IsDragging && ev.delta.sqrMagnitude > 0)
            {
                LeftTopPoint.x -= ev.delta.x;
                LeftTopPoint.y -= ev.delta.y;
                ev.Use();
            }
            else if (ev.type == EventType.ScrollWheel)
            {
                Scale -= ev.delta.y;
                ev.Use();
            }
            else if (ev.type == EventType.MouseDown && ev.button == 0)
            {
                ToggleCellAt(ev.mousePosition);
                ev.Use();
            }
        }

        private void ToggleCellAt(Vector2 position)
        {
            Vector2Int cell = GetCellAt(position);
            if (cell == SelectedCells.First.Value)
            {
                // Toggling initial cell is prohibited
                return;
            }

            var node = SelectedCells.Find(cell);
            if (node != null)
            {
                SelectedCells.Remove(node);
            }
            else
            {
                SelectedCells.AddLast(cell);
            }
        }

        private Vector2Int GetCellAt(Vector2 position)
        {
            Vector2 worldPosition = LeftTopPoint + (position - new Vector2(FrameRect.xMin, FrameRect.yMin));
            int x = (int)(worldPosition.x >= 0 ? worldPosition.x / Scale : worldPosition.x / Scale - 1);
            int y = (int)(worldPosition.y >= 0 ? worldPosition.y / Scale : worldPosition.y / Scale - 1);
            return new Vector2Int(x, y);
        }

        private void DrawGrid()
        {
            float x = kLeftOffset;
            float y = kTopOffset;
            float w = position.width - 2 * x;
            float h = position.height - y - 10;

            FrameRect = new Rect(x, y, w, h);
            TextureCoords = new Rect(LeftTopPoint.x / Scale, (LeftTopPoint.y + h) / Scale, w / Scale, -h / Scale);

            GUI.DrawTextureWithTexCoords(FrameRect, backTex, TextureCoords);
        }
    }
}
