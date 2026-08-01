using System;
using System.Collections.Generic;
using Mahjong.Presenter;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Mahjong.View
{
    /// <summary>
    /// GamePresenter.DisplayText を購読して画面全体に表示し、
    /// GamePresenter.Human が公開する選択肢をボタンとして表示する最小View
    /// AppFlowView が対局用GameObjectに取り付けることで生成される（自分自身では起動しない）
    /// </summary>
    public sealed class InGameView : MonoBehaviour
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 背景パネルの色（読みやすさのための半透明の黒）
        /// </summary>
        private static readonly Color BackgroundColor = new(0.0f, 0.0f, 0.0f, 0.85f);
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
        /// カオス麻雀の取得元選択では候補が20件を超えることがあるため、3行ぶんの高さを確保する
        /// </summary>
        private const float BUTTON_PANEL_HEIGHT = 152.0f;
        /// <summary>
        /// 選択肢ボタン1つの大きさ
        /// </summary>
        private static readonly Vector2 ButtonCellSize = new(112.0f, 40.0f);
        /// <summary>
        /// 選択肢ボタン同士の間隔
        /// </summary>
        private static readonly Vector2 ButtonSpacing = new(8.0f, 6.0f);
        /// <summary>
        /// 手牌アイコンパネルの高さ
        /// </summary>
        private const float HAND_PANEL_HEIGHT = 100.0f;
        /// <summary>
        /// 手牌アイコンパネルの左端の位置（画面幅に対する割合）
        /// 手牌はこのパネルの左端から右へ並べるため、この値が手牌全体の左右位置になる
        /// 0だと画面左端にぴったり付くため、アイコン2枚分ほど余白を空けて中央寄りにする
        /// </summary>
        private const float HAND_PANEL_LEFT_FRACTION = 0.1f;
        /// <summary>
        /// 手牌アイコン1枚の高さ
        /// 枚数によって大きさが変わらないよう、パネル幅には合わせず常にこの大きさで表示する
        /// 幅は牌の縦横比（正方形ではない）から都度算出する
        /// </summary>
        private const float HAND_ICON_HEIGHT = HAND_PANEL_HEIGHT - 16.0f;
        /// <summary>
        /// 門前牌の右端からツモ牌までに空ける隙間（アイコンの幅に対する割合）
        /// </summary>
        private const float DRAWN_TILE_GAP_FACTOR = 1.0f;
        /// <summary>
        /// Canvasの基準解像度
        /// </summary>
        private static readonly Vector2 ReferenceResolution = new(1280.0f, 720.0f);


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
        /// 次のLateUpdateで手牌アイコンを作り直すかどうか
        /// </summary>
        private bool _isHandRowDirty;
        /// <summary>
        /// 次のLateUpdateでボタンを作り直すかどうか
        /// </summary>
        private bool _isButtonRowDirty;
        /// <summary>
        /// 現在表示中のボタン（次の選択肢に差し替える際にまとめて破棄する）
        /// </summary>
        private readonly List<GameObject> _activeButtons = new();
        /// <summary>
        /// 現在表示中の手牌アイコン（手牌が更新されるたびにまとめて破棄する）
        /// </summary>
        private readonly List<GameObject> _activeHandTiles = new();


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

            // 購読時点では作り直しの予約だけを行い、実際の生成・破棄はLateUpdateにまとめる
            _presenter.Human.IsPendingRiichiChoice.Subscribe(_ => _isButtonRowDirty = true).AddTo(this);
            _presenter.Human.PendingCallChoices.Subscribe(_ => _isButtonRowDirty = true).AddTo(this);
            _presenter.Human.PendingChaosDrawChoices.Subscribe(_ => _isButtonRowDirty = true).AddTo(this);
            _presenter.Human.PendingDiscardChoices.Subscribe(_ => _isHandRowDirty = true).AddTo(this);
            _presenter.HumanHand.Subscribe(_ => _isHandRowDirty = true).AddTo(this);
        }

        /// <summary>
        /// 予約された作り直しをまとめて実行する
        /// LateUpdateで行う理由は2つ:
        /// ・Presenterは1手の間に複数のReactivePropertyを続けて更新するため、都度作り直すと同フレーム内で
        /// 　何度も生成・破棄が走り、更新途中の食い違った状態を一瞬描画してしまう
        /// ・uGUIのクリックはUpdateで配送され、その中でPresenterの更新まで同期的に走るため、
        /// 　その場で破棄するとクリックされたボタン自身をイベント配送中に消すことになる
        /// </summary>
        private void LateUpdate()
        {
            if (_isButtonRowDirty)
            {
                _isButtonRowDirty = false;
                RefreshButtonRow();
            }

            if (_isHandRowDirty)
            {
                _isHandRowDirty = false;
                RefreshHandRow();
            }
        }


        // ========================================
        // プライベートメソッド（選択肢の表示）
        // ========================================
        /// <summary>
        /// 選択肢ボタンを、現在の待ち状態から作り直す
        /// 取得元選択・リーチ確認・鳴き選択が同時に発生することはないため、見つかった順に1種類だけ表示する
        /// </summary>
        private void RefreshButtonRow()
        {
            ClearButtons();

            var chaosDrawChoices = _presenter.Human.PendingChaosDrawChoices.Value;

            if (chaosDrawChoices != null)
            {
                foreach (var choice in chaosDrawChoices)
                {
                    CreateButton(choice.Label, () => _presenter.Human.SubmitChaosDraw(choice));
                }

                return;
            }

            if (_presenter.Human.IsPendingRiichiChoice.Value)
            {
                CreateButton("リーチする", () => _presenter.Human.SubmitRiichi(true));
                CreateButton("リーチしない", () => _presenter.Human.SubmitRiichi(false));
                return;
            }

            var callChoices = _presenter.Human.PendingCallChoices.Value;

            if (callChoices == null)
            {
                return;
            }

            foreach (var choice in callChoices)
            {
                CreateButton(choice.Label, () => _presenter.Human.SubmitCall(choice));
            }
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
            LayoutHandTiles(BuildHandTileEntries());
        }
        /// <summary>
        /// 表示する手牌アイコンの内容を組み立てる
        /// </summary>
        private IReadOnlyList<HandTileEntry> BuildHandTileEntries()
        {
            var pendingDiscardChoices = _presenter.Human.PendingDiscardChoices.Value;

            if (pendingDiscardChoices != null)
            {
                var entries = new List<HandTileEntry>(pendingDiscardChoices.Count);

                foreach (var choice in pendingDiscardChoices)
                {
                    entries.Add(new HandTileEntry(choice.TileView, choice.IsDrawnTile, () => _presenter.Human.SubmitDiscard(choice)));
                }

                return entries;
            }

            var hand = _presenter.HumanHand.Value;

            if (hand == null)
            {
                return Array.Empty<HandTileEntry>();
            }

            var handEntries = new List<HandTileEntry>(hand.Tiles.Count);

            for (var i = 0; i < hand.Tiles.Count; i++)
            {
                handEntries.Add(new HandTileEntry(hand.Tiles[i], i == hand.DrawnTileIndex, null));
            }

            return handEntries;
        }
        /// <summary>
        /// 手牌アイコンをパネルの左端から順に並べる
        /// HorizontalLayoutGroupに任せず座標を直接計算する理由: レイアウトグループは
        /// パネルの幅を子に配分するため、鳴きなどで枚数が変わると牌同士の間隔まで変わってしまう。
        /// 左端からの積み上げで置けば、枚数によらず間隔が一定になる
        /// </summary>
        /// <param name="entries">左から並べる順のアイコンの内容</param>
        private void LayoutHandTiles(IReadOnlyList<HandTileEntry> entries)
        {
            var offsetX = 0.0f;

            foreach (var entry in entries)
            {
                var sprite = TileIconCache.GetSprite(entry.Tile);

                // アイコン画像は牌の実際の縦横比（正方形ではない）のため、
                // 表示枠も同じ縦横比にして余白ができないようにする
                var aspect = sprite != null ? sprite.rect.width / sprite.rect.height : 1.0f;
                var iconWidth = HAND_ICON_HEIGHT * aspect;

                if (entry.IsDrawnTile)
                {
                    offsetX += iconWidth * DRAWN_TILE_GAP_FACTOR;
                }

                CreateHandTileIcon(sprite, new Vector2(iconWidth, HAND_ICON_HEIGHT), offsetX, entry.OnClick);
                offsetX += iconWidth;
            }
        }
        /// <summary>
        /// 手牌アイコンを1つ作る
        /// </summary>
        /// <param name="sprite">表示する牌のアイコン</param>
        /// <param name="size">アイコンの表示サイズ</param>
        /// <param name="offsetX">パネル左端からのX座標</param>
        /// <param name="onClick">クリック時の処理。nullの場合はクリック不可の表示のみになる</param>
        private void CreateHandTileIcon(Sprite sprite, Vector2 size, float offsetX, Action onClick)
        {
            var tileGameObject = new GameObject("HandTile", typeof(Image));
            tileGameObject.transform.SetParent(_handPanel, false);

            var image = tileGameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;

            // パネルの左端中央を基準に、左から順に積み上げていく
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.0f, 0.5f);
            rect.anchorMax = new Vector2(0.0f, 0.5f);
            rect.pivot = new Vector2(0.0f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(offsetX, 0.0f);

            if (onClick != null)
            {
                var button = tileGameObject.AddComponent<Button>();
                button.onClick.AddListener(() => onClick());
            }

            _activeHandTiles.Add(tileGameObject);
        }
        /// <summary>
        /// 表示中の手牌アイコンをすべて破棄する
        /// </summary>
        private void ClearHandTiles()
        {
            foreach (var tileGameObject in _activeHandTiles)
            {
                Destroy(tileGameObject);
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
            rect.offsetMin += new Vector2(16.0f, BUTTON_PANEL_HEIGHT + HAND_PANEL_HEIGHT + 16.0f);
            rect.offsetMax -= new Vector2(16.0f, 16.0f);

            BuildButtonPanel(canvasGameObject.transform);
            BuildHandPanel(canvasGameObject.transform);
        }
        /// <summary>
        /// 手牌パネルのすぐ上に、選択肢ボタンを並べるパネルを組み立てる
        /// 横1行に並べるとカオス麻雀の取得元選択が画面幅に収まらないため、
        /// 幅が尽きたら次の行へ折り返す GridLayoutGroup を使う
        /// </summary>
        private void BuildButtonPanel(Transform canvasTransform)
        {
            var panelGameObject = new GameObject("ButtonPanel", typeof(RectTransform), typeof(GridLayoutGroup));
            panelGameObject.transform.SetParent(canvasTransform, false);

            _buttonPanel = panelGameObject.GetComponent<RectTransform>();
            _buttonPanel.anchorMin = new Vector2(0.0f, 0.0f);
            _buttonPanel.anchorMax = new Vector2(1.0f, 0.0f);
            _buttonPanel.pivot = new Vector2(0.5f, 0.0f);
            _buttonPanel.sizeDelta = new Vector2(0.0f, BUTTON_PANEL_HEIGHT);
            _buttonPanel.anchoredPosition = new Vector2(0.0f, HAND_PANEL_HEIGHT + 16.0f);

            var layout = panelGameObject.GetComponent<GridLayoutGroup>();
            layout.cellSize = ButtonCellSize;
            layout.spacing = ButtonSpacing;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.Flexible;
        }
        /// <summary>
        /// 画面最下部に、手牌アイコンを並べるパネルを組み立てる
        /// じゃん魂のように打牌は手牌アイコン自体のタップで行うため、他の選択肢ボタン（リーチ確認・鳴き選択）より
        /// 下・画面端に近い位置に置く
        /// </summary>
        private void BuildHandPanel(Transform canvasTransform)
        {
            // 牌の座標は LayoutHandTiles がパネルの左端から直接計算するため、HorizontalLayoutGroupは付けない
            // （レイアウトグループに任せると、枚数が変わったときに牌同士の間隔まで変わってしまう）
            // パネルの右端は牌の配置に影響しない（牌は左端から原寸で積み上げる）ため、画面右端まで広げておく
            var panelGameObject = new GameObject("HandPanel", typeof(RectTransform));
            panelGameObject.transform.SetParent(canvasTransform, false);

            _handPanel = panelGameObject.GetComponent<RectTransform>();
            _handPanel.anchorMin = new Vector2(HAND_PANEL_LEFT_FRACTION, 0.0f);
            _handPanel.anchorMax = new Vector2(1.0f, 0.0f);
            _handPanel.pivot = new Vector2(0.5f, 0.0f);
            _handPanel.sizeDelta = new Vector2(0.0f, HAND_PANEL_HEIGHT);
            _handPanel.anchoredPosition = new Vector2(0.0f, 8.0f);
        }
        /// <summary>
        /// 選択肢ボタンを1つ作る
        /// </summary>
        private void CreateButton(string label, Action onClick)
        {
            // 大きさは GridLayoutGroup が cellSize で決めるため、LayoutElement は付けない
            var buttonGameObject = new GameObject($"Button_{label}", typeof(Image), typeof(Button));
            buttonGameObject.transform.SetParent(_buttonPanel, false);

            var image = buttonGameObject.GetComponent<Image>();
            image.color = ButtonColor;

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
        /// RectTransformを親いっぱいに広げる
        /// </summary>
        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }


        // ========================================
        // 入れ子の型
        // ========================================
        /// <summary>
        /// 手牌アイコン1枚分の表示内容
        /// パネル幅に収める倍率を決めるには全体の幅を先に合計する必要があるため、
        /// いったんこの形にまとめてから配置する
        /// </summary>
        private readonly struct HandTileEntry
        {
            /// <summary>
            /// 表示する牌
            /// </summary>
            public TileView Tile { get; }
            /// <summary>
            /// ツモ牌かどうか（trueなら手前に隙間を空ける）
            /// </summary>
            public bool IsDrawnTile { get; }
            /// <summary>
            /// クリック時の処理。nullの場合はクリック不可の表示のみになる
            /// </summary>
            public Action OnClick { get; }

            /// <summary>
            /// 手牌アイコン1枚分の表示内容を生成する
            /// </summary>
            /// <param name="tile">表示する牌</param>
            /// <param name="isDrawnTile">ツモ牌かどうか</param>
            /// <param name="onClick">クリック時の処理（クリック不可の場合はnull）</param>
            public HandTileEntry(TileView tile, bool isDrawnTile, Action onClick)
            {
                Tile = tile;
                IsDrawnTile = isDrawnTile;
                OnClick = onClick;
            }
        }
    }
}
