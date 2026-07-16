using TwoUp.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.EditorTools
{
    /// <summary>
    /// Authors Battleship.unity: Panel_Placement (tray-tap-rotate, active by default) and
    /// Panel_Firing (three BattleshipGridView grids, inactive until the server reports BS_FIRING).
    /// </summary>
    public static class BattleshipSceneBuilder
    {
        private const float CellSize = 64f;
        private const int GridDim = 10;
        private static readonly float GridPixelSize = CellSize * GridDim;
        private static readonly Vector2 Center = UiKit.Center;

        private static readonly string[] ShipTrayNames = { "Btn_Ship5", "Btn_Ship4", "Btn_Ship3a", "Btn_Ship3b", "Btn_Ship2" };
        private static readonly string[] ShipTrayLabels = { "5", "4", "3", "3", "2" };

        [MenuItem("2UP/Build Scenes/Battleship")]
        public static void Build()
        {
            var scene = UiKit.NewScene();
            var screen = UiKit.CreateCanvasWithScreen("Screen_Battleship");

            var placementPanel = BuildPlacementPanel(screen.transform, out var myFleetGrid, out var trayButtons,
                out var rotateButton, out var rotateButtonText, out var randomButton, out var lockButton, out var hintText);

            var firingPanel = BuildFiringPanel(screen.transform, out var targetGrid, out var myBoardGrid,
                out var turnBannerText, out var fireButton, out var shotResultText);
            firingPanel.SetActive(false);

            UiKit.BuildEmoteWheel(screen.transform);
            UiKit.BuildConnectionBadge(screen.transform);

            var toast = UiKit.CreateText(screen.transform, "Text_Toast", "", 40, Color.white);
            UiKit.Place(toast.gameObject, Center, Center, Vector2.zero, new Vector2(800, 100));
            toast.gameObject.SetActive(false);

            var controller = screen.AddComponent<BattleshipController>();
            UiKit.SetRef(controller, "placementPanel", placementPanel);
            UiKit.SetRef(controller, "firingPanel", firingPanel);
            UiKit.SetRef(controller, "myFleetGrid", myFleetGrid);
            UiKit.SetArray(controller, "shipTrayButtons", trayButtons);
            UiKit.SetRef(controller, "rotateButton", rotateButton);
            UiKit.SetRef(controller, "rotateButtonText", rotateButtonText);
            UiKit.SetRef(controller, "randomButton", randomButton);
            UiKit.SetRef(controller, "lockPlacementButton", lockButton);
            UiKit.SetRef(controller, "placementHintText", hintText);
            UiKit.SetRef(controller, "targetGrid", targetGrid);
            UiKit.SetRef(controller, "myBoardGrid", myBoardGrid);
            UiKit.SetRef(controller, "turnBannerText", turnBannerText);
            UiKit.SetRef(controller, "fireButton", fireButton);
            UiKit.SetRef(controller, "shotResultText", shotResultText);
            UiKit.SetRef(controller, "toastText", toast);

            UiKit.SaveScene(scene, "Battleship");
            UiKit.AddSceneToBuildSettings("Assets/Scenes/Battleship.unity");
        }

        private static GameObject BuildPlacementPanel(Transform screenRoot, out BattleshipGridView myFleetGrid,
            out Button[] trayButtons, out Button rotateButton, out TMPro.TMP_Text rotateButtonText,
            out Button randomButton, out Button lockButton, out TMPro.TMP_Text hintText)
        {
            var panel = UiKit.CreatePanel(screenRoot, "Panel_Placement", UiKit.ScreenBg);
            UiKit.StretchFull(panel);
            panel.GetComponent<Image>().raycastTarget = false;

            hintText = UiKit.CreateText(panel.transform, "Text_PlacementHint", "Place your ships", 44, Color.white);
            UiKit.Place(hintText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(900, 70));

            myFleetGrid = BuildGrid(panel.transform, "Grid_MyFleet", Center, Center, new Vector2(0, 250));

            var tray = UiKit.CreateUIObject("Tray_Ships", panel.transform);
            UiKit.Place(tray, Center, new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(1000, 130));
            var hlg = tray.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            trayButtons = new Button[ShipTrayNames.Length];
            for (int i = 0; i < ShipTrayNames.Length; i++)
                trayButtons[i] = UiKit.CreateButton(tray.transform, ShipTrayNames[i], ShipTrayLabels[i], new Vector2(160, 130), UiKit.ButtonBg);

            rotateButton = UiKit.CreateButton(panel.transform, "Btn_Rotate", "Rotate: Horizontal", new Vector2(300, 100), UiKit.ButtonMuted);
            UiKit.Place(rotateButton.gameObject, Center, new Vector2(0.5f, 1f), new Vector2(-160, -240), new Vector2(300, 100));
            rotateButtonText = rotateButton.GetComponentInChildren<TMPro.TMP_Text>();

            randomButton = UiKit.CreateButton(panel.transform, "Btn_Random", "Random", new Vector2(300, 100), UiKit.ButtonMuted);
            UiKit.Place(randomButton.gameObject, Center, new Vector2(0.5f, 1f), new Vector2(160, -240), new Vector2(300, 100));

            lockButton = UiKit.CreateButton(panel.transform, "Btn_LockPlacement", "Lock Placement", new Vector2(500, 110), UiKit.ButtonBg);
            UiKit.Place(lockButton.gameObject, Center, new Vector2(0.5f, 1f), new Vector2(0, -370), new Vector2(500, 110));

            return panel;
        }

        private static GameObject BuildFiringPanel(Transform screenRoot, out BattleshipGridView targetGrid,
            out BattleshipGridView myBoardGrid, out TMPro.TMP_Text turnBannerText, out Button fireButton,
            out TMPro.TMP_Text shotResultText)
        {
            var panel = UiKit.CreatePanel(screenRoot, "Panel_Firing", UiKit.ScreenBg);
            UiKit.StretchFull(panel);
            panel.GetComponent<Image>().raycastTarget = false;

            turnBannerText = UiKit.CreateText(panel.transform, "Text_TurnBanner", "YOUR TURN", 48, Color.white);
            UiKit.Place(turnBannerText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(900, 80));

            targetGrid = BuildGrid(panel.transform, "Grid_Target", Center, Center, new Vector2(0, 150));

            myBoardGrid = BuildGrid(panel.transform, "Grid_MyBoard", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40, -40));
            var myBoardRect = (RectTransform)myBoardGrid.transform;
            myBoardRect.localScale = new Vector3(0.5f, 0.5f, 1f);

            shotResultText = UiKit.CreateText(panel.transform, "Text_ShotResult", "", 40, Color.white);
            UiKit.Place(shotResultText.gameObject, Center, new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(900, 80));

            fireButton = UiKit.CreateButton(panel.transform, "Btn_Fire", "Fire", new Vector2(400, 110), UiKit.ButtonBg);
            UiKit.Place(fireButton.gameObject, Center, new Vector2(0.5f, 1f), new Vector2(0, -290), new Vector2(400, 110));
            fireButton.interactable = false;

            return panel;
        }

        /// <summary>10x10 grid of Image+Button cells (row-major), wired onto a new BattleshipGridView.</summary>
        private static BattleshipGridView BuildGrid(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos)
        {
            var root = UiKit.CreatePanel(parent, name, UiKit.BoardBg);
            UiKit.Place(root, anchor, pivot, anchoredPos, new Vector2(GridPixelSize, GridPixelSize));

            var grid = root.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(CellSize, CellSize);
            grid.spacing = Vector2.zero;
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GridDim;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;

            var cellImages = new Image[GridDim * GridDim];
            var cellButtons = new Button[GridDim * GridDim];
            for (int r = 0; r < GridDim; r++)
            {
                for (int c = 0; c < GridDim; c++)
                {
                    int index = r * GridDim + c;
                    var go = UiKit.CreateUIObject($"Cell_r{r}c{c}", root.transform);
                    var img = go.AddComponent<Image>();
                    img.color = UiKit.CellEmpty;
                    img.raycastTarget = true;
                    var btn = go.AddComponent<Button>();
                    btn.transition = Selectable.Transition.None;
                    btn.targetGraphic = img;
                    cellImages[index] = img;
                    cellButtons[index] = btn;
                }
            }

            var view = root.AddComponent<BattleshipGridView>();
            UiKit.SetArray(view, "cells", cellImages);
            UiKit.SetArray(view, "cellButtons", cellButtons);
            return view;
        }
    }
}
