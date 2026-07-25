using System;
using System.Collections.Generic;
using Mahjong.Presenter;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Mahjong.View
{
    /// <summary>
    /// GamePresenter.DisplayText を購読して画面全体に表示し、
    /// GamePresenter.Human が公開する選択肢をボタンとして表示する最小View
    /// シーンに何も配置しなくても、再生するだけで自分自身とGamePresenterを生成する
    /// </summary>
    public sealed class InGameView : MonoBehaviour
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 背景パネルの色（読みやすさのための半透明の黒）
        /// </summary>
        private static readonly Color BackgroundColor = new(0f, 0f, 0f, 0.85f);
        /// <summary>
        /// 表示文字の色
        /// </summary>
        private static readonly Color TextColor = Color.white;
        /// <summary>
        /// ボタンの背景色
        /// </summary>
        private static readonly Color ButtonColor = new(0.25f, 0.45f, 0.25f, 0.95f);
        /// <summary>
        /// 表示文字のサイズ
        /// </summary>
        private const int FONT_SIZE = 20;
        /// <summary>
        /// ボタン文字のサイズ
        /// </summary>
        private const int BUTTON_FONT_SIZE = 16;
        /// <summary>
        /// ボタンパネルの高さ
        /// </summary>
        private const float BUTTON_PANEL_HEIGHT = 64f;
        /// <summary>
        /// Canvasの基準解像度
        /// </summary>
        private static readonly Vector2 ReferenceResolution = new(1280f, 720f);
        /// <summary>
        /// テキスト表示パネルが占める画面下端からの割合
        /// 残りの下部領域はP0の3D手牌表示に充てる
        /// </summary>
        private const float TEXT_PANEL_BOTTOM = 0.35f;


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 表示内容を購読する対象
        /// </summary>
        private GamePresenter _presenter;
        /// <summary>
        /// 表示用テキスト
        /// </summary>
        private Text _displayText;
        /// <summary>
        /// 選択肢ボタンを並べるパネル
        /// </summary>
        private RectTransform _buttonPanel;
        /// <summary>
        /// 現在表示中のボタン（次の選択肢に差し替える際にまとめて破棄する）
        /// </summary>
        private readonly List<GameObject> _activeButtons = new();


        // ========================================
        // 起動
        // ========================================
        /// <summary>
        /// シーン読み込み後、対局進行役と表示役をまとめて生成する
        /// シーンファイルを直接編集せずに済ませるため、実行時にすべてコードから組み立てる
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var root = new GameObject("MahjongGameRoot");
            root.AddComponent<GamePresenter>();
            root.AddComponent<InGameView>();
            root.AddComponent<HandTileFieldView>();

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
            _presenter = GetComponent<GamePresenter>();
            BuildUi();
        }

        private void Start()
        {
            _presenter.DisplayText.Subscribe(text => _displayText.text = text).AddTo(this);
            _presenter.Human.IsPendingRiichiChoice.Subscribe(OnRiichiChoicePending).AddTo(this);
            _presenter.Human.PendingDiscardChoices.Subscribe(OnDiscardChoicesPending).AddTo(this);
            _presenter.Human.PendingCallChoices.Subscribe(OnCallChoicesPending).AddTo(this);
        }


        // ========================================
        // プライベートメソッド（選択肢の表示）
        // ========================================
        /// <summary>
        /// リーチ宣言の確認ボタンを表示する
        /// </summary>
        private void OnRiichiChoicePending(bool isPending)
        {
            ClearButtons();

            if (!isPending)
            {
                return;
            }

            CreateButton("リーチする", () => _presenter.Human.SubmitRiichi(true));
            CreateButton("リーチしない", () => _presenter.Human.SubmitRiichi(false));
        }
        /// <summary>
        /// 打牌の候補ボタンを表示する
        /// </summary>
        private void OnDiscardChoicesPending(IReadOnlyList<DiscardChoice> choices)
        {
            ClearButtons();

            if (choices == null)
            {
                return;
            }

            foreach (var choice in choices)
            {
                CreateButton(choice.Label, () => _presenter.Human.SubmitDiscard(choice));
            }
        }
        /// <summary>
        /// 宣言（ポン・チー・カン・スルー）の候補ボタンを表示する
        /// </summary>
        private void OnCallChoicesPending(IReadOnlyList<CallChoice> choices)
        {
            ClearButtons();

            if (choices == null)
            {
                return;
            }

            foreach (var choice in choices)
            {
                CreateButton(choice.Label, () => _presenter.Human.SubmitCall(choice));
            }
        }


        // ========================================
        // プライベートメソッド（UI構築）
        // ========================================
        /// <summary>
        /// Canvas・背景パネル・全画面テキスト・ボタンパネルをコードのみで組み立てる
        /// TextMeshPro はフォントアセットのInspector設定が必要になるため、
        /// 組み込みフォントだけで完結するレガシーuGUI Textを使う
        /// </summary>
        private void BuildUi()
        {
            var canvasGameObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGameObject.transform.SetParent(transform, false);

            var canvas = canvasGameObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGameObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;

            var backgroundGameObject = new GameObject("Background", typeof(Image));
            backgroundGameObject.transform.SetParent(canvasGameObject.transform, false);
            var background = backgroundGameObject.GetComponent<Image>();
            background.color = BackgroundColor;
            StretchToTopArea(background.rectTransform);

            var textGameObject = new GameObject("DisplayText", typeof(Text));
            textGameObject.transform.SetParent(canvasGameObject.transform, false);
            _displayText = textGameObject.GetComponent<Text>();
            _displayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _displayText.fontSize = FONT_SIZE;
            _displayText.color = TextColor;
            _displayText.alignment = TextAnchor.UpperLeft;
            _displayText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _displayText.verticalOverflow = VerticalWrapMode.Overflow;

            var rect = _displayText.rectTransform;
            StretchToTopArea(rect);
            rect.offsetMin += new Vector2(16f, 16f);
            rect.offsetMax -= new Vector2(16f, 16f);

            BuildButtonPanel(canvasGameObject.transform);
        }
        /// <summary>
        /// 画面下部に選択肢ボタンを並べるパネルを組み立てる
        /// </summary>
        private void BuildButtonPanel(Transform canvasTransform)
        {
            var panelGameObject = new GameObject("ButtonPanel", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            panelGameObject.transform.SetParent(canvasTransform, false);

            _buttonPanel = panelGameObject.GetComponent<RectTransform>();
            _buttonPanel.anchorMin = new Vector2(0f, 0f);
            _buttonPanel.anchorMax = new Vector2(1f, 0f);
            _buttonPanel.pivot = new Vector2(0.5f, 0f);
            _buttonPanel.sizeDelta = new Vector2(0f, BUTTON_PANEL_HEIGHT);
            _buttonPanel.anchoredPosition = new Vector2(0f, 8f);

            var layout = panelGameObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;
        }
        /// <summary>
        /// 選択肢ボタンを1つ作る
        /// </summary>
        private void CreateButton(string label, Action onClick)
        {
            var buttonGameObject = new GameObject($"Button_{label}", typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGameObject.transform.SetParent(_buttonPanel, false);

            var image = buttonGameObject.GetComponent<Image>();
            image.color = ButtonColor;

            var layoutElement = buttonGameObject.GetComponent<LayoutElement>();
            layoutElement.minWidth = 90f;
            layoutElement.minHeight = BUTTON_PANEL_HEIGHT - 16f;

            var button = buttonGameObject.GetComponent<Button>();
            button.onClick.AddListener(() => onClick());

            var textGameObject = new GameObject("Text", typeof(Text));
            textGameObject.transform.SetParent(buttonGameObject.transform, false);
            var text = textGameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = BUTTON_FONT_SIZE;
            text.color = TextColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
            StretchToParent(text.rectTransform);

            _activeButtons.Add(buttonGameObject);
        }
        /// <summary>
        /// 表示中のボタンをすべて破棄する
        /// </summary>
        private void ClearButtons()
        {
            foreach (var button in _activeButtons)
            {
                Destroy(button);
            }

            _activeButtons.Clear();
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
        /// <summary>
        /// RectTransformを画面上部（TEXT_PANEL_BOTTOM〜100%）に広げる
        /// 残した下部領域は HandTileFieldView がP0の3D手牌を表示するのに使う
        /// </summary>
        private static void StretchToTopArea(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, TEXT_PANEL_BOTTOM);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
