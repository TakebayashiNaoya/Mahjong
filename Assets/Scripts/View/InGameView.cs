using Mahjong.Presenter;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Mahjong.View
{
    /// <summary>
    /// GamePresenter.DisplayText を購読して画面全体に表示する、テキストのみの最小View
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
        /// 表示文字のサイズ
        /// </summary>
        private const int FONT_SIZE = 20;
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
        }


        // ========================================
        // プライベートメソッド（UI構築）
        // ========================================
        /// <summary>
        /// Canvas・背景パネル・全画面テキストをコードのみで組み立てる
        /// TextMeshPro はフォントアセットのInspector設定が必要になるため、
        /// 組み込みフォントだけで完結するレガシーuGUI Textを使う
        /// </summary>
        private void BuildUi()
        {
            var canvasGameObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
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
            rect.offsetMin += new Vector2(16f, 16f);
            rect.offsetMax -= new Vector2(16f, 16f);
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
