using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VoxelAR.AR;
using VoxelAR.Voxel;

namespace VoxelAR.UI
{
    /// <summary>
    /// Builds and drives the whole build UI in code: instruction banner, toast, blocking message,
    /// bottom bar (palette, Build/Break, Undo, Menu) and the menu panel (save/load/clear/re-place).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public const string MsgFindSurface = "Move your phone slowly to find a table or floor";
        public const string MsgTapToPlace = "Tap to place your build area";
        public const string MsgBuild = "Tap to build · Pinch to scale · Twist to rotate";
        public const string MsgBreak = "Tap a block to break it";

        public static UIManager Instance { get; private set; }

        /// <summary>Hooks the menu calls back into (owned by AppManager).</summary>
        public interface IActions
        {
            void SaveSlot(int slot);
            void LoadSlot(int slot);
            void ClearBuild();
            void Replace();
        }

        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color ButtonColor = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        private static readonly Color BuildColor = new Color(0.20f, 0.55f, 0.25f, 0.95f);
        private static readonly Color BreakColor = new Color(0.70f, 0.20f, 0.20f, 0.95f);

        private Font font;
        private RectTransform safeRoot;

        private GameObject banner;
        private Text bannerText;
        private GameObject toast;
        private Text toastText;
        private float toastTimer;
        private GameObject blocker;
        private Text blockerText;
        private Button blockerButton;
        private Text blockerButtonText;
        private UnityAction blockerAction;

        private GameObject bottomBar;
        private Outline[] swatchOutlines;
        private Button modeButton;
        private Text modeText;
        private Button undoButton;
        private GameObject menu;

        private WorldPlacement placement;
        private BlockInteractor interactor;
        private EditHistory history;
        private IActions actions;
        private string statusOverride;

        public bool IsMenuOpen => menu != null && menu.activeSelf;
        public string CurrentInstruction => bannerText != null && banner.activeSelf ? bannerText.text : null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (placement != null)
            {
                placement.Placed -= OnPlaced;
                placement.Removed -= OnRemoved;
                placement.PlaneStatusChanged -= OnPlaneStatus;
            }
            if (interactor != null)
            {
                interactor.ModeChanged -= OnModeChanged;
                interactor.SelectedBlockChanged -= OnBlockChanged;
            }
            if (history != null) history.Changed -= RefreshUndo;
        }

        public void Init(WorldPlacement worldPlacement, BlockInteractor blockInteractor, EditHistory editHistory, IActions menuActions)
        {
            placement = worldPlacement;
            interactor = blockInteractor;
            history = editHistory;
            actions = menuActions;

            BuildHierarchy();

            placement.Placed += OnPlaced;
            placement.Removed += OnRemoved;
            placement.PlaneStatusChanged += OnPlaneStatus;
            interactor.ModeChanged += OnModeChanged;
            interactor.SelectedBlockChanged += OnBlockChanged;
            history.Changed += RefreshUndo;

            OnModeChanged(interactor.Mode);
            OnBlockChanged(interactor.SelectedBlock);
            RefreshUndo();
            bottomBar.SetActive(placement.IsPlaced);
            RefreshInstruction();
        }

        private void Update()
        {
            if (toast != null && toast.activeSelf)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f) toast.SetActive(false);
            }
        }

        // ───────────────────────── public API ─────────────────────────

        public void SetInstruction(string message)
        {
            if (banner == null) return;
            banner.SetActive(!string.IsNullOrEmpty(message));
            bannerText.text = message ?? "";
        }

        /// <summary>Overrides the normal instruction (e.g. tracking lost). Null restores it.</summary>
        public void SetStatusOverride(string message)
        {
            statusOverride = message;
            RefreshInstruction();
        }

        public void ShowToast(string message, float duration = 2.5f)
        {
            if (toast == null) return;
            toastText.text = message;
            toast.SetActive(true);
            toast.transform.SetAsLastSibling();
            toastTimer = duration;
        }

        /// <summary>Full-screen message that blocks input. Pass a button label + action for a retry button.</summary>
        public void ShowBlockingMessage(string message, string buttonLabel = null, UnityAction onButton = null)
        {
            if (blocker == null) return;
            blockerText.text = message;
            blockerButton.onClick.RemoveListener(OnBlockerButton);
            blockerAction = onButton;
            bool hasButton = !string.IsNullOrEmpty(buttonLabel) && onButton != null;
            blockerButton.gameObject.SetActive(hasButton);
            if (hasButton)
            {
                blockerButtonText.text = buttonLabel;
                blockerButton.onClick.AddListener(OnBlockerButton);
            }
            blocker.SetActive(true);
            blocker.transform.SetAsLastSibling();
        }

        public void HideBlockingMessage()
        {
            if (blocker != null) blocker.SetActive(false);
        }

        public void SetMenuOpen(bool open)
        {
            if (menu != null) menu.SetActive(open);
        }

        private void OnBlockerButton()
        {
            HideBlockingMessage();
            blockerAction?.Invoke();
        }

        // ───────────────────────── state → view ─────────────────────────

        private void RefreshInstruction()
        {
            if (banner == null) return;
            if (!string.IsNullOrEmpty(statusOverride)) { SetInstruction(statusOverride); return; }
            if (placement.IsPlaced) SetInstruction(interactor.Mode == ToolMode.Build ? MsgBuild : MsgBreak);
            else SetInstruction(placement.IsPlaneDetected ? MsgTapToPlace : MsgFindSurface);
        }

        private void OnPlaced(Transform _)
        {
            bottomBar.SetActive(true);
            RefreshInstruction();
        }

        private void OnRemoved()
        {
            bottomBar.SetActive(false);
            SetMenuOpen(false);
            RefreshInstruction();
        }

        private void OnPlaneStatus(bool _) => RefreshInstruction();

        private void OnModeChanged(ToolMode mode)
        {
            bool build = mode == ToolMode.Build;
            modeText.text = build ? "Build" : "Break";
            modeButton.image.color = build ? BuildColor : BreakColor;
            RefreshInstruction();
        }

        private void OnBlockChanged(byte id)
        {
            for (int i = 0; i < swatchOutlines.Length; i++)
            {
                bool sel = i + 1 == id;
                swatchOutlines[i].enabled = sel;
                swatchOutlines[i].transform.localScale = Vector3.one * (sel ? 1.12f : 1f);
            }
        }

        private void RefreshUndo()
        {
            if (undoButton != null) undoButton.interactable = history.CanUndo;
        }

        // ───────────────────────── hierarchy ─────────────────────────

        private void BuildHierarchy()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            safeRoot = NewRect("SafeArea", transform);
            Stretch(safeRoot);
            safeRoot.gameObject.AddComponent<SafeAreaFitter>();

            // Instruction banner (top)
            var bannerRt = NewRect("InstructionBanner", safeRoot);
            Anchor(bannerRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            bannerRt.sizeDelta = new Vector2(-80, 150);
            bannerRt.anchoredPosition = new Vector2(0, -40);
            AddImage(bannerRt, PanelColor);
            bannerText = AddText(bannerRt, "", 46);
            banner = bannerRt.gameObject;

            // Bottom bar
            var barRt = NewRect("BottomBar", safeRoot);
            Anchor(barRt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            barRt.sizeDelta = new Vector2(0, 360);
            AddImage(barRt, PanelColor);
            bottomBar = barRt.gameObject;

            var paletteRow = NewRect("Palette", barRt);
            Anchor(paletteRow, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            paletteRow.sizeDelta = new Vector2(-40, 150);
            paletteRow.anchoredPosition = new Vector2(0, -20);
            var ph = paletteRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            ph.spacing = 16; ph.childAlignment = TextAnchor.MiddleCenter;
            ph.childControlWidth = ph.childControlHeight = false;
            ph.childForceExpandWidth = ph.childForceExpandHeight = false;

            swatchOutlines = new Outline[BlockPalette.Count];
            for (int i = 0; i < BlockPalette.Count; i++)
            {
                byte id = (byte)(i + 1);
                var sw = NewRect("Swatch " + BlockPalette.Name(id), paletteRow);
                sw.sizeDelta = new Vector2(104, 104);
                var img = AddImage(sw, BlockPalette.Color(id));
                var btn = sw.gameObject.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(() => interactor.SelectedBlock = id);
                var outline = sw.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(6, -6);
                outline.enabled = false;
                swatchOutlines[i] = outline;
            }

            var toolRow = NewRect("Tools", barRt);
            Anchor(toolRow, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            toolRow.sizeDelta = new Vector2(-40, 150);
            toolRow.anchoredPosition = new Vector2(0, 24);
            var th = toolRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            th.spacing = 24; th.childAlignment = TextAnchor.MiddleCenter;
            th.childControlWidth = th.childControlHeight = true;
            th.childForceExpandWidth = th.childForceExpandHeight = true;

            modeButton = AddButton(toolRow, "Build", () =>
                interactor.Mode = interactor.Mode == ToolMode.Build ? ToolMode.Break : ToolMode.Build, out modeText);
            undoButton = AddButton(toolRow, "Undo", () => history.Undo(), out _);
            AddButton(toolRow, "Menu", () => SetMenuOpen(!IsMenuOpen), out _);

            BuildMenu();
            BuildToast();
            BuildBlocker();
        }

        private void BuildMenu()
        {
            var rt = NewRect("Menu", safeRoot);
            Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.sizeDelta = new Vector2(760, 1380);
            AddImage(rt, new Color(0, 0, 0, 0.85f));
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(40, 40, 40, 40);
            v.spacing = 18;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = v.childForceExpandHeight = true;

            for (int s = 0; s < Save.SaveSystem.SlotCount; s++)
            {
                int slot = s;
                AddButton(rt, s == 0 ? "Save 1 (auto)" : $"Save {s + 1}", () => { actions.SaveSlot(slot); SetMenuOpen(false); }, out _);
            }
            for (int s = 0; s < Save.SaveSystem.SlotCount; s++)
            {
                int slot = s;
                AddButton(rt, $"Load {s + 1}", () => { actions.LoadSlot(slot); SetMenuOpen(false); }, out _);
            }
            AddButton(rt, "Clear build", () => { actions.ClearBuild(); SetMenuOpen(false); }, out _);
            AddButton(rt, "Re-place area", () => { SetMenuOpen(false); actions.Replace(); }, out _);
            AddButton(rt, "Close", () => SetMenuOpen(false), out _);

            menu = rt.gameObject;
            menu.SetActive(false);
        }

        private void BuildToast()
        {
            var rt = NewRect("Toast", safeRoot);
            Anchor(rt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            rt.sizeDelta = new Vector2(900, 120);
            rt.anchoredPosition = new Vector2(0, 400);
            AddImage(rt, new Color(0, 0, 0, 0.8f));
            toastText = AddText(rt, "", 42);
            toast = rt.gameObject;
            toast.SetActive(false);
        }

        private void BuildBlocker()
        {
            var rt = NewRect("BlockingMessage", transform); // full screen, not safe-area constrained
            Stretch(rt);
            AddImage(rt, new Color(0, 0, 0, 0.9f));

            var textRt = NewRect("Message", rt);
            Anchor(textRt, new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.75f), new Vector2(0.5f, 0.5f));
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;
            blockerText = AddTextOn(textRt, "", 52);

            var btnParent = NewRect("ButtonHolder", rt);
            Anchor(btnParent, new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.5f));
            btnParent.sizeDelta = new Vector2(520, 150);
            var h = btnParent.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;
            blockerButton = AddButton(btnParent, "Retry", null, out blockerButtonText);

            blocker = rt.gameObject;
            blocker.SetActive(false);
        }

        // ───────────────────────── helpers ─────────────────────────

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
        }

        private static Image AddImage(RectTransform rt, Color c)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            return img;
        }

        private Text AddText(RectTransform parent, string text, int size)
        {
            var rt = NewRect("Text", parent);
            Stretch(rt);
            rt.offsetMin = new Vector2(24, 8);
            rt.offsetMax = new Vector2(-24, -8);
            return AddTextOn(rt, text, size);
        }

        private Text AddTextOn(RectTransform rt, string text, int size)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private Button AddButton(RectTransform parent, string label, UnityAction onClick, out Text labelText)
        {
            var rt = NewRect("Button " + label, parent);
            rt.sizeDelta = new Vector2(300, 130);
            var img = AddImage(rt, ButtonColor);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);
            labelText = AddText(rt, label, 44);
            return btn;
        }
    }
}
