using TwoUp.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.EditorTools
{
    /// <summary>Authors ConnectFour.unity: 7x6 board, column tap zones, turn timer ring, emote wheel.</summary>
    public static class ConnectFourSceneBuilder
    {
        private static readonly Vector2 Center = UiKit.Center;

        [MenuItem("2UP/Build Scenes/ConnectFour")]
        public static void Build()
        {
            const int columns = ConnectFourController.Columns;
            const int rows = ConnectFourController.Rows;
            const float cell = 130f, spacing = 6f, pad = 10f;
            var boardSize = new Vector2(
                columns * cell + (columns - 1) * spacing + 2 * pad,
                rows * cell + (rows - 1) * spacing + 2 * pad);

            var scene = UiKit.NewScene();
            var screen = UiKit.CreateCanvasWithScreen("Screen_Game");
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var turn = UiKit.CreateText(screen.transform, "TurnText", "Waiting for game state...", 60, Color.white);
            UiKit.Place(turn.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(950, 90));

            var youAre = UiKit.CreateText(screen.transform, "YouAreText", "", 42, Color.white);
            UiKit.Place(youAre.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -230), new Vector2(950, 60));

            // Turn timer ring, next to TurnText (top-right corner so it never overlaps the centered label).
            var ringGo = UiKit.CreateUIObject("Ring_TurnTimer", screen.transform);
            var ring = ringGo.AddComponent<Image>();
            ring.sprite = knob;
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillAmount = 1f;
            ring.color = Color.white;
            ring.raycastTarget = false;
            UiKit.Place(ringGo, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30, -120), new Vector2(110, 110));

            // Board: 7x6 grid of disc Images, display order top-left → bottom-right.
            var board = UiKit.CreatePanel(screen.transform, "Board", UiKit.BoardBg);
            UiKit.Place(board, Center, Center, new Vector2(0, 40), boardSize);
            var grid = board.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cell, cell);
            grid.spacing = new Vector2(spacing, spacing);
            grid.padding = new RectOffset((int)pad, (int)pad, (int)pad, (int)pad);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;

            var cellImages = new Image[rows * columns];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    var go = UiKit.CreateUIObject($"Cell_r{r}c{c}", board.transform);
                    var img = go.AddComponent<Image>();
                    img.sprite = knob;
                    img.color = UiKit.CellEmpty;
                    img.raycastTarget = false;
                    cellImages[r * columns + c] = img;
                }
            }

            // Invisible full-height tap zones over each column (sibling of Board, same rect).
            var columnsRoot = UiKit.CreateUIObject("Columns", screen.transform);
            UiKit.Place(columnsRoot, Center, Center, new Vector2(0, 40), boardSize);
            var hlg = columnsRoot.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset((int)pad, (int)pad, (int)pad, (int)pad);
            hlg.spacing = spacing;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            var columnButtons = new Button[columns];
            for (int c = 0; c < columns; c++)
            {
                var go = UiKit.CreateUIObject($"ColumnButton_{c}", columnsRoot.transform);
                var img = go.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0f); // invisible but raycastable tap zone
                img.raycastTarget = true;
                var btn = go.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = img;
                columnButtons[c] = btn;
            }

            UiKit.BuildEmoteWheel(screen.transform);
            UiKit.BuildConnectionBadge(screen.transform);

            var toast = UiKit.CreateText(screen.transform, "Text_Toast", "", 40, Color.white);
            UiKit.Place(toast.gameObject, Center, Center, Vector2.zero, new Vector2(800, 100));
            toast.gameObject.SetActive(false);

            var controller = screen.AddComponent<ConnectFourController>();
            UiKit.SetRef(controller, "turnText", turn);
            UiKit.SetRef(controller, "youAreText", youAre);
            UiKit.SetArray(controller, "cells", cellImages);
            UiKit.SetArray(controller, "columnButtons", columnButtons);
            UiKit.SetRef(controller, "turnTimerRing", ring);
            UiKit.SetRef(controller, "toastText", toast);

            UiKit.SaveScene(scene, "ConnectFour");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/ConnectFour.unity");
        }
    }
}
