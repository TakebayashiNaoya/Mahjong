using System;
using System.Linq;
using Mahjong.Presenter;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Mahjong.View
{
    /// <summary>
    /// AppFlowPresenter.CurrentScreen を購読し、タイトル・モード選択・設定・ゲーム終了の各画面を切り替えて表示する
    /// 対局中（InGame）は AppFlowPresenter.ActiveGame を購読し、InGameView 等の対局用Viewを動的に取り付ける
    /// シーンに何も配置しなくても、再生するだけで自分自身とAppFlowPresenterを生成する（InGameViewの旧Bootstrapの後継）
    /// </summary>
    public sealed class AppFlowView : MonoBehaviour
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 背景パネルの色（読みやすさのための半透明の黒）
        /// </summary>
        private static readonly Color BackgroundColor = new(0.0f, 0.0f, 0.0f, 0.85f);
        /// <summary>
        /// 内容パネルの背景色
        /// </summary>
        private static readonly Color PanelColor = new(0.12f, 0.12f, 0.14f, 0.95f);
        /// <summary>
        /// ボタンの背景色
        /// </summary>
        private static readonly Color ButtonColor = new(0.25f, 0.45f, 0.25f, 0.95f);
        /// <summary>
        /// 表示文字の色
        /// </summary>
        private static readonly Color TextColor = Color.white;
        /// <summary>
        /// 見出し文字のサイズ
        /// </summary>
        private const int HEADER_FONT_SIZE = 32;
        /// <summary>
        /// 本文・選択肢ボタンの文字サイズ
        /// </summary>
        private const int BODY_FONT_SIZE = 18;
        /// <summary>
        /// 内容パネルの幅
        /// </summary>
        private const float PANEL_WIDTH = 480.0f;
        /// <summary>
        /// 選択肢ボタンの高さ
        /// </summary>
        private const float OPTION_BUTTON_HEIGHT = 44.0f;
        /// <summary>
        /// Canvasの基準解像度
        /// </summary>
        private static readonly Vector2 ReferenceResolution = new(1280.0f, 720.0f);


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 画面遷移を購読する対象
        /// </summary>
        private AppFlowPresenter _flow;
        /// <summary>
        /// 画面全体を覆う半透明の背景（対局中は3D表示を隠さないよう非表示にする）
        /// </summary>
        private GameObject _background;
        /// <summary>
        /// タイトル画面のパネル
        /// </summary>
        private GameObject _titlePanel;
        /// <summary>
        /// モード選択画面のパネル
        /// </summary>
        private GameObject _modeSelectPanel;
        /// <summary>
        /// 設定画面のパネル本体（内容は選択が変わるたびに作り直す）
        /// </summary>
        private GameObject _settingsPanel;
        /// <summary>
        /// 設定画面の内容を積み上げるコンテナ
        /// </summary>
        private Transform _settingsContent;
        /// <summary>
        /// ゲーム終了画面のパネル本体（内容は結果が変わるたびに作り直す）
        /// </summary>
        private GameObject _gameOverPanel;
        /// <summary>
        /// ゲーム終了画面の内容を積み上げるコンテナ
        /// </summary>
        private Transform _gameOverContent;


        // ========================================
        // 起動
        // ========================================
        /// <summary>
        /// シーン読み込み後、画面遷移役と表示役をまとめて生成する
        /// シーンファイルを直接編集せずに済ませるため、実行時にすべてコードから組み立てる
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var root = new GameObject("MahjongAppRoot");
            root.AddComponent<AppFlowPresenter>();
            root.AddComponent<AppFlowView>();

            EnsureEventSystemExists();
        }
        /// <summary>
        /// ボタンのクリックを受け付けるために EventSystem が必要なため、無ければ生成する
        /// このプロジェクトは Active Input Handling が新Input Systemのみのため、
        /// 旧来の StandaloneInputModule ではなく InputSystemUIInputModule を使う
        /// </summary>
        private static void EnsureEventSystemExists()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }


        // ========================================
        // Unityライフサイクル
        // ========================================
        private void Awake()
        {
            _flow = GetComponent<AppFlowPresenter>();
            BuildUi();
        }

        private void Start()
        {
            _flow.CurrentScreen.Subscribe(OnScreenChanged).AddTo(this);
            _flow.ActiveGame.Subscribe(OnActiveGameChanged).AddTo(this);
            _flow.FinalSummary.Subscribe(_ => RefreshGameOverPanel()).AddTo(this);
            _flow.SelectedPlayerCount.Subscribe(_ => RefreshSettingsPanel()).AddTo(this);
            _flow.SettingsDifficulty.Subscribe(_ => RefreshSettingsPanel()).AddTo(this);
            _flow.SettingsUseRedDora.Subscribe(_ => RefreshSettingsPanel()).AddTo(this);
            _flow.SettingsUseKitaNuki.Subscribe(_ => RefreshSettingsPanel()).AddTo(this);
        }


        // ========================================
        // プライベートメソッド（画面切り替え）
        // ========================================
        /// <summary>
        /// 現在の画面に応じて4枚のパネルの表示・非表示を排他的に切り替える
        /// </summary>
        private void OnScreenChanged(AppScreen screen)
        {
            _background.SetActive(screen != AppScreen.InGame);
            _titlePanel.SetActive(screen == AppScreen.Title);
            _modeSelectPanel.SetActive(screen == AppScreen.ModeSelect);
            _settingsPanel.SetActive(screen == AppScreen.Settings);
            _gameOverPanel.SetActive(screen == AppScreen.GameOver);
        }
        /// <summary>
        /// 対局用の GamePresenter が生成・破棄されたタイミングで、対局用Viewを取り付ける
        /// 破棄時（null になったとき）は GameObject ごと AppFlowPresenter 側で破棄されるため、ここでは何もしない
        /// </summary>
        private static void OnActiveGameChanged(GamePresenter gamePresenter)
        {
            if (gamePresenter == null)
            {
                return;
            }

            gamePresenter.gameObject.AddComponent<InGameView>();
            gamePresenter.gameObject.AddComponent<TableFieldView>();
            gamePresenter.gameObject.AddComponent<RoundResultOverlayView>();
        }


        // ========================================
        // プライベートメソッド（設定画面の組み立て）
        // ========================================
        /// <summary>
        /// 設定画面の内容（CPU強度・赤ドラ・北抜き・確定ボタン）を作り直す
        /// 選択中の項目が視覚的に分かるよう、選択肢ボタンのラベルに印を付けて毎回作り直す
        /// </summary>
        private void RefreshSettingsPanel()
        {
            if (_settingsContent == null)
            {
                return;
            }

            ClearChildren(_settingsContent);

            CreateText(_settingsContent, "設定", HEADER_FONT_SIZE, TextAnchor.MiddleCenter);

            CreateSettingsRow(
                "CPU強度",
                ("弱", () => _flow.SetSettingsDifficulty(CpuDifficultyChoice.Easy), _flow.SettingsDifficulty.Value == CpuDifficultyChoice.Easy),
                ("普通", () => _flow.SetSettingsDifficulty(CpuDifficultyChoice.Normal), _flow.SettingsDifficulty.Value == CpuDifficultyChoice.Normal));

            CreateSettingsRow(
                "赤ドラ",
                ("あり", () => _flow.SetSettingsUseRedDora(true), _flow.SettingsUseRedDora.Value),
                ("なし", () => _flow.SetSettingsUseRedDora(false), !_flow.SettingsUseRedDora.Value));

            if (_flow.SelectedPlayerCount.Value == 3)
            {
                CreateSettingsRow(
                    "北抜き",
                    ("あり", () => _flow.SetSettingsUseKitaNuki(true), _flow.SettingsUseKitaNuki.Value),
                    ("なし", () => _flow.SetSettingsUseKitaNuki(false), !_flow.SettingsUseKitaNuki.Value));
            }

            CreateButton(_settingsContent, "対局開始", () => _flow.ConfirmSettings());
        }
        /// <summary>
        /// 設定1項目分（ラベル＋選択肢2つ）の行を組み立てる
        /// </summary>
        private void CreateSettingsRow(
            string label, (string Label, Action OnClick, bool IsSelected) optionA, (string Label, Action OnClick, bool IsSelected) optionB)
        {
            CreateText(_settingsContent, label, BODY_FONT_SIZE, TextAnchor.MiddleCenter);

            var rowGameObject = new GameObject($"Row_{label}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGameObject.transform.SetParent(_settingsContent, false);

            var layout = rowGameObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8.0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            CreateButton(rowGameObject.transform, FormatOptionLabel(optionA.Label, optionA.IsSelected), optionA.OnClick);
            CreateButton(rowGameObject.transform, FormatOptionLabel(optionB.Label, optionB.IsSelected), optionB.OnClick);
        }
        /// <summary>
        /// 選択中の選択肢が分かるよう、ラベルの先頭に印を付ける
        /// </summary>
        private static string FormatOptionLabel(string label, bool isSelected)
        {
            return (isSelected ? "● " : "○ ") + label;
        }


        // ========================================
        // プライベートメソッド（ゲーム終了画面の組み立て）
        // ========================================
        /// <summary>
        /// ゲーム終了画面の内容（見出し・最終順位・タイトルへ戻るボタン）を作り直す
        /// </summary>
        private void RefreshGameOverPanel()
        {
            if (_gameOverContent == null)
            {
                return;
            }

            ClearChildren(_gameOverContent);

            CreateText(_gameOverContent, "ゲーム終了", HEADER_FONT_SIZE, TextAnchor.MiddleCenter);

            var summary = _flow.FinalSummary.Value;

            if (summary != null)
            {
                foreach (var standing in summary.Standings.OrderBy(s => s.Rank))
                {
                    var humanMark = standing.IsHuman ? "（あなた）" : string.Empty;
                    CreateText(
                        _gameOverContent,
                        $"{standing.Rank}位　P{standing.PlayerIndex}{humanMark}　{standing.Score}点",
                        BODY_FONT_SIZE, TextAnchor.MiddleCenter);
                }
            }

            CreateButton(_gameOverContent, "タイトルへ戻る", () => _flow.AcknowledgeGameOver());
        }


        // ========================================
        // プライベートメソッド（UI構築）
        // ========================================
        /// <summary>
        /// Canvas・背景パネル・4画面分のパネルをコードのみで組み立てる
        /// TextMeshPro はフォントアセットのInspector設定が必要になるため、組み込みフォントだけで完結するレガシーuGUI Textを使う
        /// </summary>
        private void BuildUi()
        {
            var canvasGameObject = new GameObject("AppFlowCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGameObject.transform.SetParent(transform, false);

            var canvas = canvasGameObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGameObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;

            _background = new GameObject("Background", typeof(Image));
            _background.transform.SetParent(canvasGameObject.transform, false);
            _background.GetComponent<Image>().color = BackgroundColor;
            StretchToParent(_background.GetComponent<RectTransform>());

            _titlePanel = BuildTitlePanel(canvasGameObject.transform);
            _modeSelectPanel = BuildModeSelectPanel(canvasGameObject.transform);
            _settingsPanel = BuildEmptyContentPanel(canvasGameObject.transform, "SettingsPanel", out _settingsContent);
            _gameOverPanel = BuildEmptyContentPanel(canvasGameObject.transform, "GameOverPanel", out _gameOverContent);

            _titlePanel.SetActive(false);
            _modeSelectPanel.SetActive(false);
            _settingsPanel.SetActive(false);
            _gameOverPanel.SetActive(false);
        }
        /// <summary>
        /// タイトル画面のパネル（見出し＋スタートボタン）を組み立てる
        /// </summary>
        private GameObject BuildTitlePanel(Transform canvasTransform)
        {
            var panel = CreateContentPanel(canvasTransform, "TitlePanel", out var content);
            CreateText(content, "麻雀", HEADER_FONT_SIZE, TextAnchor.MiddleCenter);
            CreateButton(content, "スタート", () => _flow.SubmitTitleStart());
            return panel;
        }
        /// <summary>
        /// モード選択画面のパネル（見出し＋4つの選択肢ボタン）を組み立てる
        /// </summary>
        private GameObject BuildModeSelectPanel(Transform canvasTransform)
        {
            var panel = CreateContentPanel(canvasTransform, "ModeSelectPanel", out var content);
            CreateText(content, "モード選択", HEADER_FONT_SIZE, TextAnchor.MiddleCenter);

            CreateButton(content, "四人麻雀・東風戦", () => _flow.SubmitModeSelection(4, GameLengthChoice.EastOnly));
            CreateButton(content, "四人麻雀・半荘戦", () => _flow.SubmitModeSelection(4, GameLengthChoice.HalfGame));
            CreateButton(content, "三人麻雀・東風戦", () => _flow.SubmitModeSelection(3, GameLengthChoice.EastOnly));
            CreateButton(content, "三人麻雀・半荘戦", () => _flow.SubmitModeSelection(3, GameLengthChoice.HalfGame));

            return panel;
        }
        /// <summary>
        /// 内容が空のパネル（設定画面・ゲーム終了画面用）を組み立てる
        /// 内容は選択・結果が変わるたびに Refresh 系メソッドで作り直す
        /// </summary>
        private GameObject BuildEmptyContentPanel(Transform canvasTransform, string name, out Transform content)
        {
            return CreateContentPanel(canvasTransform, name, out content);
        }
        /// <summary>
        /// 中央に配置される、縦積みの内容パネルを1つ組み立てる
        /// </summary>
        private static GameObject CreateContentPanel(Transform canvasTransform, string name, out Transform content)
        {
            var panelGameObject = new GameObject(name, typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panelGameObject.transform.SetParent(canvasTransform, false);

            panelGameObject.GetComponent<Image>().color = PanelColor;

            var panelRect = panelGameObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(PANEL_WIDTH, 0.0f);

            var layout = panelGameObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 16.0f;
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = panelGameObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            content = panelGameObject.transform;
            return panelGameObject;
        }
        /// <summary>
        /// テキストを1つ作る
        /// </summary>
        private static void CreateText(Transform parent, string text, int fontSize, TextAnchor alignment)
        {
            var textGameObject = new GameObject("Text", typeof(Text));
            textGameObject.transform.SetParent(parent, false);

            var textComponent = textGameObject.GetComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = fontSize;
            textComponent.color = TextColor;
            textComponent.alignment = alignment;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            textComponent.text = text;
        }
        /// <summary>
        /// ボタンを1つ作る
        /// </summary>
        private static void CreateButton(Transform parent, string label, Action onClick)
        {
            var buttonGameObject = new GameObject(
                $"Button_{label}", typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGameObject.transform.SetParent(parent, false);

            buttonGameObject.GetComponent<Image>().color = ButtonColor;

            var layoutElement = buttonGameObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = OPTION_BUTTON_HEIGHT;
            layoutElement.flexibleWidth = 1.0f;

            var button = buttonGameObject.GetComponent<Button>();
            button.onClick.AddListener(() => onClick());

            var textGameObject = new GameObject("Text", typeof(Text));
            textGameObject.transform.SetParent(buttonGameObject.transform, false);
            var text = textGameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = BODY_FONT_SIZE;
            text.color = TextColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
            StretchToParent(text.rectTransform);
        }
        /// <summary>
        /// 指定したTransformの子をすべて破棄する
        /// </summary>
        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
        /// <summary>
        /// RectTransformを親いっぱいに広げる
        /// </summary>
        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
