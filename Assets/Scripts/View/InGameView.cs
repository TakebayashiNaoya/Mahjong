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
        /// 手牌アイコンパネルの高さ
        /// </summary>
        private const float HAND_PANEL_HEIGHT = 100f;
        /// <summary>
        /// 手牌アイコンパネルの左端の位置（画面幅に対する割合）
        /// 0だと画面左端にぴったり付くため、少し余白を空けて中央寄りにする
        /// </summary>
        private const float HAND_PANEL_LEFT_FRACTION = 0.05f;
        /// <summary>
        /// 手牌アイコンパネルの右端の位置（画面幅に対する割合）
        /// 1.0未満にすることで、右側にポン・チー・カン等のUIを置く余白を確保する
        /// </summary>
        private const float HAND_PANEL_RIGHT_FRACTION = 0.68f;
        /// <summary>
        /// 手牌アイコン1枚の大きさ（正方形の一辺）
        /// 元々のパネルの高さいっぱいに表示していたサイズ（HAND_PANEL_HEIGHT - 16）に戻す
        /// </summary>
        private const float HAND_ICON_SIZE = HAND_PANEL_HEIGHT - 16f;
        /// <summary>
        /// 手牌アイコン同士の隙間（アイコンの大きさに対する割合）
        /// </summary>
        private const float HAND_ICON_SPACING_FACTOR = 0.0f;
        /// <summary>
        /// Canvasの基準解像度
        /// </summary>
        private static readonly Vector2 ReferenceResolution = new(1280f, 720f);


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
        /// 手牌アイコンを並べるパネル
        /// </summary>
        private RectTransform _handPanel;
        /// <summary>
        /// 現在表示中のボタン（次の選択肢に差し替える際にまとめて破棄する）
        /// </summary>
        private readonly List<GameObject> _activeButtons = new();
        /// <summary>
        /// 現在表示中の手牌アイコン（手牌が更新されるたびにまとめて破棄する）
        /// </summary>
        private readonly List<GameObject> _activeHandTiles = new();


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
            root.AddComponent<DiscardFieldView>();
            root.AddComponent<OpponentHandFieldView>();

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
            _presenter.Human.PendingDiscardChoices.Subscribe(_ => RefreshHandRow()).AddTo(this);
            _presenter.Human.PendingCallChoices.Subscribe(OnCallChoicesPending).AddTo(this);
            _presenter.HumanHandTiles.Subscribe(_ => RefreshHandRow()).AddTo(this);
            _presenter.HasDrawnTile.Subscribe(_ => RefreshHandRow()).AddTo(this);
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
        // プライベートメソッド（手牌の表示）
        // ========================================
        /// <summary>
        /// 手牌アイコンの行を作り直す
        /// 打牌選択が発生中なら候補牌をクリック可能なアイコンとして、
        /// そうでなければ現在の手牌をクリック不可のアイコンとして並べる
        /// </summary>
        private void RefreshHandRow()
        {
            ClearHandTiles();

            var hasDrawnTile = _presenter.HasDrawnTile.Value;
            var pendingDiscardChoices = _presenter.Human.PendingDiscardChoices.Value;
            var offsetX = 0f;

            if (pendingDiscardChoices != null)
            {
                for (var i = 0; i < pendingDiscardChoices.Count; i++)
                {
                    var choice = pendingDiscardChoices[i];
                    var isDrawnTile = hasDrawnTile && i == pendingDiscardChoices.Count - 1;
                    offsetX = CreateHandTileIcon(choice.TileView, offsetX, isDrawnTile, () => _presenter.Human.SubmitDiscard(choice));
                }

                return;
            }

            var tiles = _presenter.HumanHandTiles.Value;

            if (tiles == null)
            {
                return;
            }

            for (var i = 0; i < tiles.Count; i++)
            {
                var isDrawnTile = hasDrawnTile && i == tiles.Count - 1;
                offsetX = CreateHandTileIcon(tiles[i], offsetX, isDrawnTile, null);
            }
        }
        /// <summary>
        /// 手牌アイコンを1つ作り、次の牌を置くべきX座標を返す
        /// HorizontalLayoutGroupに任せず座標を直接計算する理由: レイアウトグループは
        /// パネルの幅を子に配分するため、鳴きなどで枚数が変わると牌同士の間隔まで変わってしまう。
        /// 左端からの積み上げで置けば、枚数によらず間隔が一定になる
        /// </summary>
        /// <param name="tile">表示する牌</param>
        /// <param name="offsetX">この牌を置く、パネル左端からのX座標</param>
        /// <param name="isDrawnTile">ツモ牌かどうか。trueなら手前に牌1個分の隙間を空ける</param>
        /// <param name="onClick">クリック時の処理。nullの場合はクリック不可の表示のみになる</param>
        /// <returns>次の牌を置くべきX座標</returns>
        private float CreateHandTileIcon(TileView tile, float offsetX, bool isDrawnTile, Action onClick)
        {
            var tileGameObject = new GameObject("HandTile", typeof(Image));
            tileGameObject.transform.SetParent(_handPanel, false);

            var image = tileGameObject.GetComponent<Image>();
            var sprite = TileIconCache.GetSprite(tile);
            image.sprite = sprite;
            image.preserveAspect = true;

            // アイコン画像は牌の実際の縦横比（正方形ではない）のため、
            // 表示枠も同じ縦横比にして余白ができないようにする
            var aspect = sprite != null ? sprite.rect.width / sprite.rect.height : 1f;
            var tileWidth = HAND_ICON_SIZE * aspect;
            var spacing = tileWidth * HAND_ICON_SPACING_FACTOR;

            if (isDrawnTile)
            {
                offsetX += tileWidth;
            }

            // パネルの左端中央を基準に、左から順に積み上げていく
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(tileWidth, HAND_ICON_SIZE);
            rect.anchoredPosition = new Vector2(offsetX, 0f);

            if (onClick != null)
            {
                var button = tileGameObject.AddComponent<Button>();
                button.onClick.AddListener(() => onClick());
            }

            _activeHandTiles.Add(tileGameObject);
            return offsetX + tileWidth + spacing;
        }
        /// <summary>
        /// 表示中の手牌アイコンをすべて破棄する
        /// DestroyImmediateを使う理由: HumanHandTiles・HasDrawnTile・PendingDiscardChoicesの更新が
        /// 同じRefresh()呼び出し内で連続するため、RefreshHandRowが同フレーム内に複数回呼ばれることがある。
        /// Destroy（同フレーム内では消えない）だと古い牌と新しい牌が一時的に混在してレイアウトが崩れる
        /// </summary>
        private void ClearHandTiles()
        {
            foreach (var tileGameObject in _activeHandTiles)
            {
                DestroyImmediate(tileGameObject);
            }

            _activeHandTiles.Clear();
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
            StretchToParent(background.rectTransform);

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
            StretchToParent(rect);
            rect.offsetMin += new Vector2(16f, BUTTON_PANEL_HEIGHT + HAND_PANEL_HEIGHT + 16f);
            rect.offsetMax -= new Vector2(16f, 16f);

            BuildButtonPanel(canvasGameObject.transform);
            BuildHandPanel(canvasGameObject.transform);
        }
        /// <summary>
        /// 手牌パネルのすぐ上に、選択肢ボタンを並べるパネルを組み立てる
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
            _buttonPanel.anchoredPosition = new Vector2(0f, HAND_PANEL_HEIGHT + 16f);

            var layout = panelGameObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;
        }
        /// <summary>
        /// 画面最下部に、手牌アイコンを並べるパネルを組み立てる
        /// じゃん魂のように打牌は手牌アイコン自体のタップで行うため、他の選択肢ボタン（リーチ確認・鳴き選択）より
        /// 下・画面端に近い位置に置く
        /// </summary>
        private void BuildHandPanel(Transform canvasTransform)
        {
            // 牌の座標は CreateHandTileIcon が左端から直接計算するため、HorizontalLayoutGroupは付けない
            // （レイアウトグループに任せると、枚数が変わったときに牌同士の間隔まで変わってしまう）
            var panelGameObject = new GameObject("HandPanel", typeof(RectTransform));
            panelGameObject.transform.SetParent(canvasTransform, false);

            _handPanel = panelGameObject.GetComponent<RectTransform>();
            _handPanel.anchorMin = new Vector2(HAND_PANEL_LEFT_FRACTION, 0f);
            _handPanel.anchorMax = new Vector2(HAND_PANEL_RIGHT_FRACTION, 0f);
            _handPanel.pivot = new Vector2(0.5f, 0f);
            _handPanel.sizeDelta = new Vector2(0f, HAND_PANEL_HEIGHT);
            _handPanel.anchoredPosition = new Vector2(0f, 8f);
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
        /// DestroyImmediateを使う理由: 複数のReactiveProperty更新が同フレーム内で連続することがあり、
        /// Destroy（同フレーム内では消えない）だと古いボタンと新しいボタンが一時的に混在してしまうため
        /// </summary>
        private void ClearButtons()
        {
            foreach (var button in _activeButtons)
            {
                DestroyImmediate(button);
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
    }
}
