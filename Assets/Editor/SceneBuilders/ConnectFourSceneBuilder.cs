using TwoUp.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.EditorTools
{
    /// <summary>Authors ConnectFour.unity: 7x6 board, column tap zones, game-over/rematch panel.</summary>
    public static class ConnectFourSceneBuilder
    {
        private static readonly Vector2 Center = UiKit.Center;

        public static void BuildConnectFourScene()
        {
            const int columns = ConnectFourController.Columns;
            const int rows = ConnectFourController.Rows;
            const float cell = 130f, spacing = 6f, pad = 10f;
            var boardSize = new Vector2(
                columns * cell + (columns - 1) * spacing + 2 * pad,
                rows * cell + (rows - 1) * spacing + 2 * pad);

            var scene = UiKit.NewScene();
            var screen = UiKit.CreateCanvasWithScreen("Screen_Game");

            var turn = UiKit.CreateText(screen.transform, "TurnText", "Waiting for game state...", 60, Color.white);
            UiKit.Place(turn.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(950, 90));

            var youAre = UiKit.CreateText(screen.transform, "YouAreText", "", 42, Color.white);
            UiKit.Place(youAre.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -230), new Vector2(950, 60));

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

            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
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

            // Game-over / rematch panel (inactive by default; dim layer blocks board input).
            var gameOver = UiKit.CreateUIObject("Panel_GameOver", screen.transform);
            UiKit.StretchFull(gameOver);
            var dim = gameOver.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            var popup = UiKit.CreatePanel(gameOver.transform, "Popup", UiKit.PanelBg);
            UiKit.Place(popup, Center, Center, Vector2.zero, new Vector2(860, 640));

            var result = UiKit.CreateText(popup.transform, "ResultText", "", 72, Color.white);
            UiKit.Place(result.gameObject, Center, Center, new Vector2(0, 190), new Vector2(700, 100));

            var rematchStatus = UiKit.CreateText(popup.transform, "RematchStatusText", "", 38, new Color(0.8f, 0.84f, 0.92f));
            UiKit.Place(rematchStatus.gameObject, Center, Center, new Vector2(0, 100), new Vector2(700, 60));

            var rematch = UiKit.CreateButton(popup.transform, "RematchButton", "Rematch", new Vector2(560, 120), UiKit.ButtonBg);
            UiKit.Place(rematch.gameObject, Center, Center, new Vector2(0, -30), new Vector2(560, 120));

            var back = UiKit.CreateButton(popup.transform, "BackButton", "Back to Home", new Vector2(560, 120), UiKit.ButtonMuted);
            UiKit.Place(back.gameObject, Center, Center, new Vector2(0, -180), new Vector2(560, 120));

            gameOver.SetActive(false);

            var controller = screen.AddComponent<ConnectFourController>();
            UiKit.SetRef(controller, "turnText", turn);
            UiKit.SetRef(controller, "youAreText", youAre);
            UiKit.SetArray(controller, "cells", cellImages);
            UiKit.SetArray(controller, "columnButtons", columnButtons);
            UiKit.SetRef(controller, "gameOverPanel", gameOver);
            UiKit.SetRef(controller, "resultText", result);
            UiKit.SetRef(controller, "rematchStatusText", rematchStatus);
            UiKit.SetRef(controller, "rematchButton", rematch);
            UiKit.SetRef(controller, "backButton", back);

            UiKit.SaveScene(scene, "ConnectFour");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/ConnectFour.unity");
        }
    }
}
