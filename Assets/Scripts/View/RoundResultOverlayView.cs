using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mahjong.Presenter;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Mahjong.View
{
    /// <summary>
    /// GamePresenter.RoundResultDisplay を購読し、和了・流局の結果を全画面オーバーレイで表示する最小View
    /// InGameView と同じく、シーンに何も配置しなくても再生するだけで自分自身を組み立てる
    /// </summary>
    public sealed class RoundResultOverlayView : MonoBehaviour
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 他のCanvasより手前に表示するためのソート順
        /// </summary>
        private const int SORTING_ORDER = 10;
        /// <summary>
        /// 背景パネルの色（読みやすさのための半透明の黒）
        /// </summary>
        private static readonly Color BackgroundColor = new(0.0f, 0.0f, 0.0f, 0.85f);
        /// <summary>
        /// 内容パネルの背景色
        /// </summary>
        private static readonly Color PanelColor = new(0.12f, 0.12f, 0.14f, 0.95f);
        /// <summary>
        /// 表示文字の色
        /// </summary>
        private static readonly Color TextColor = Color.white;
        /// <summary>
        /// 見出し文字のサイズ
        /// </summary>
        private const int HEADER_FONT_SIZE = 28;
        /// <summary>
        /// 和了1件の見出し（席・ツモロン）の文字サイズ
        /// </summary>
        private const int WIN_HEADER_FONT_SIZE = 22;
        /// <summary>
        /// 本文（役・点数増減など）の文字サイズ
        /// </summary>
        private const int BODY_FONT_SIZE = 18;
        /// <summary>
        /// 内容パネルの幅
        /// </summary>
        private const float PANEL_WIDTH = 720.0f;
        /// <summary>
        /// 手牌アイコン1枚の高さ
        /// </summary>
        private const float TILE_ICON_HEIGHT = 56.0f;
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
        /// 表示のオン・オフを切り替える対象（Canvas本体）
        /// </summary>
        private GameObject _canvasGameObject;
        /// <summary>
        /// 内容（見出し・和了ブロック・点数増減）を積み上げるパネル
        /// </summary>
        private Transform _content;


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
            _presenter.RoundResultDisplay.Subscribe(Refresh).AddTo(this);
        }


        // ========================================
        // プライベートメソッド（表示切り替え）
        // ========================================
        /// <summary>
        /// 結果表示の内容を作り直す
        /// 値が null の間はCanvas自体を非表示にする
        /// </summary>
        private void Refresh(RoundResultView view)
        {
            ClearContent();

            if (view == null)
            {
                _canvasGameObject.SetActive(false);
                return;
            }

            _canvasGameObject.SetActive(true);

            CreateText(_content, $"{view.RoundLabel}　{view.ReasonLabel}", HEADER_FONT_SIZE, FontStyle.Bold);

            foreach (var win in view.Wins)
            {
                CreateWinBlock(win);
            }

            if (view.TenpaiStates.Count > 0)
            {
                CreateTenpaiBlock(view.TenpaiStates);
            }

            CreateText(_content, BuildScoreDeltaLine(view.ScoreDeltas), BODY_FONT_SIZE, FontStyle.Normal);
        }
        /// <summary>
        /// 前回の内容パネルの中身をすべて破棄する
        /// </summary>
        private void ClearContent()
        {
            for (var i = _content.childCount - 1; i >= 0; i--)
            {
                Destroy(_content.GetChild(i).gameObject);
            }
        }


        // ========================================
        // プライベートメソッド（内容の組み立て）
        // ========================================
        /// <summary>
        /// 和了1件分のブロック（席・手牌アイコン・役一覧・点数）を組み立てる
        /// </summary>
        private void CreateWinBlock(WinResultView win)
        {
            var blockGameObject = new GameObject("WinBlock", typeof(RectTransform), typeof(VerticalLayoutGroup));
            blockGameObject.transform.SetParent(_content, false);

            var layout = blockGameObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 4.0f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateText(blockGameObject.transform, $"{win.SeatLabel}　{win.SourceLabel}", WIN_HEADER_FONT_SIZE, FontStyle.Bold);
            CreateHandTileRow(blockGameObject.transform, win);
            CreateText(blockGameObject.transform, BuildYakuText(win), BODY_FONT_SIZE, FontStyle.Normal);

            var scoreLine = string.IsNullOrEmpty(win.LimitBandLabel)
                ? $"{win.Fu}符{win.Han}翻　{win.PointsLabel}"
                : $"{win.LimitBandLabel}　{win.PointsLabel}";
            CreateText(blockGameObject.transform, scoreLine, WIN_HEADER_FONT_SIZE, FontStyle.Bold);
        }
        /// <summary>
        /// 役・ドラの一覧を1つのテキストにまとめる
        /// 役満の場合、YakuLines の Han は倍率のため翻数を付けずに役名だけを並べる
        /// </summary>
        private static string BuildYakuText(WinResultView win)
        {
            var sb = new StringBuilder();

            foreach (var line in win.YakuLines)
            {
                if (win.IsYakuman)
                {
                    sb.AppendLine(line.Name);
                }
                else
                {
                    sb.AppendLine($"{line.Name}　{line.Han}翻");
                }
            }

            if (win.DoraHan > 0)
            {
                sb.AppendLine($"ドラ　{win.DoraHan}");
            }

            if (win.AkaDoraHan > 0)
            {
                sb.AppendLine($"赤ドラ　{win.AkaDoraHan}");
            }

            return sb.ToString().TrimEnd();
        }
        /// <summary>
        /// 荒牌平局時のテンパイ・ノーテン一覧を組み立てる
        /// </summary>
        private void CreateTenpaiBlock(IReadOnlyList<bool> tenpaiStates)
        {
            var sb = new StringBuilder();

            for (var i = 0; i < tenpaiStates.Count; i++)
            {
                sb.AppendLine($"P{i}　{(tenpaiStates[i] ? "テンパイ" : "ノーテン")}");
            }

            CreateText(_content, sb.ToString().TrimEnd(), BODY_FONT_SIZE, FontStyle.Normal);
        }
        /// <summary>
        /// 全員分の点数増減を1行にまとめる
        /// </summary>
        private static string BuildScoreDeltaLine(IReadOnlyList<int> scoreDeltas)
        {
            var parts = scoreDeltas.Select((delta, i) => $"P{i} {(delta >= 0 ? "+" : string.Empty)}{delta}");
            return "点数増減　" + string.Join("　", parts);
        }
        /// <summary>
        /// 和了時の手牌（門前牌＋和了牌）と副露をアイコンで並べる
        /// </summary>
        private void CreateHandTileRow(Transform parent, WinResultView win)
        {
            var rowGameObject = new GameObject("HandTiles", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGameObject.transform.SetParent(parent, false);

            var layout = rowGameObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 2.0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            foreach (var tile in win.HandTiles)
            {
                CreateTileIcon(rowGameObject.transform, tile);
            }

            foreach (var meld in win.Melds)
            {
                foreach (var tile in meld.Tiles)
                {
                    CreateTileIcon(rowGameObject.transform, tile);
                }
            }
        }
        /// <summary>
        /// 牌アイコンを1枚作る
        /// </summary>
        private static void CreateTileIcon(Transform parent, TileView tile)
        {
            var sprite = TileIconCache.GetSprite(tile);

            var tileGameObject = new GameObject("Tile", typeof(Image), typeof(LayoutElement));
            tileGameObject.transform.SetParent(parent, false);

            var image = tileGameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;

            // アイコン画像は牌の実際の縦横比（正方形ではない）のため、高さを固定して幅は比率から求める
            var aspect = sprite != null ? sprite.rect.width / sprite.rect.height : 1.0f;
            var layoutElement = tileGameObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = TILE_ICON_HEIGHT;
            layoutElement.preferredWidth = TILE_ICON_HEIGHT * aspect;
        }


        // ========================================
        // プライベートメソッド（UI構築）
        // ========================================
        /// <summary>
        /// Canvas・背景パネル・内容パネルをコードのみで組み立てる
        /// TextMeshPro はフォントアセットのInspector設定が必要になるため、組み込みフォントだけで完結するレガシーuGUI Textを使う
        /// </summary>
        private void BuildUi()
        {
            _canvasGameObject = new GameObject("RoundResultCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvasGameObject.transform.SetParent(transform, false);

            var canvas = _canvasGameObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SORTING_ORDER;

            var scaler = _canvasGameObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;

            var background = new GameObject("Background", typeof(Image));
            background.transform.SetParent(_canvasGameObject.transform, false);
            background.GetComponent<Image>().color = BackgroundColor;
            StretchToParent(background.GetComponent<RectTransform>());

            var panelGameObject = new GameObject(
                "Panel", typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panelGameObject.transform.SetParent(_canvasGameObject.transform, false);

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

            _content = panelGameObject.transform;
            _canvasGameObject.SetActive(false);
        }
        /// <summary>
        /// テキストを1つ作る
        /// </summary>
        private static Text CreateText(Transform parent, string content, int fontSize, FontStyle style)
        {
            var textGameObject = new GameObject("Text", typeof(Text));
            textGameObject.transform.SetParent(parent, false);

            var text = textGameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = TextColor;
            text.alignment = TextAnchor.UpperCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content;

            return text;
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
