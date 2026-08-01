using System;
using System.Collections.Generic;
using Mahjong.Model.Hands;
using Mahjong.Model.Tiles;

namespace Mahjong.Model.Game
{
    /// <summary>
    /// カオス麻雀ルール（仕様書16章）専用の処理
    /// 通常ルールの状態遷移（ツモ → 打牌 → 他家の反応）はそのまま維持し、
    /// 「牌をどこから取るか」だけを差し替える
    /// 和了判定・シャンテン計算・役・符・点数の計算には一切手を加えない
    /// </summary>
    public partial class Round
    {
        // ========================================
        // パブリックメソッド（カオス麻雀：取得元）
        // ========================================
        /// <summary>
        /// 現在のプレイヤーがこのツモ番に選べる取得元をすべて列挙する
        /// 牌を奪われた側は山から補充するため、山の残り枚数が足りない取得元はここで除外する
        /// （実行時に補充できないと手牌枚数の不変条件が壊れ、シャンテン計算が黙って誤った値を返すため）
        /// </summary>
        /// <exception cref="InvalidOperationException">カオスルールが無効、AwaitingDraw フェーズでない、または途中流局が確定している場合</exception>
        public IReadOnlyList<ChaosDrawOption> GetChaosDrawOptions()
        {
            RequireChaosRules();
            RequireNoPendingAbortiveDraw();
            RequirePhase(TurnPhase.AwaitingDraw);

            var options = new List<ChaosDrawOption>();

            if (!_wall.IsEmpty)
            {
                options.Add(ChaosDrawOption.FromWall());
            }

            for (var playerIndex = 0; playerIndex < Settings.PlayerCount; playerIndex++)
            {
                var target = Players[playerIndex];

                // 河は自分のものも含めて取れる
                for (var discardIndex = 0; discardIndex < target.Discards.Count; discardIndex++)
                {
                    options.Add(ChaosDrawOption.FromDiscardPile(playerIndex, discardIndex));
                }

                if (playerIndex == CurrentPlayerIndex)
                {
                    AddOwnMeldOptions(options, target);
                    continue;
                }

                AddOpponentHandOptions(options, target, playerIndex);
                AddOpponentMeldOptions(options, target, playerIndex);
            }

            return options;
        }
        /// <summary>
        /// 選んだ取得元から牌を取り、打牌待ちに進める
        /// 自分の副露を戻した場合だけは例外で、チー・ポン（3枚）を戻すと手牌がちょうど正規枚数になるため
        /// 打牌せずに手番を終え、次のプレイヤーに移る（仕様書16.5）
        /// </summary>
        /// <param name="option">GetChaosDrawOptions が返した取得元</param>
        /// <returns>手牌に加わったツモ牌。打牌せずに手番が終わった場合は null</returns>
        /// <exception cref="ArgumentNullException">option が null の場合</exception>
        /// <exception cref="ArgumentException">option の指す対象が存在しない場合</exception>
        /// <exception cref="InvalidOperationException">カオスルールが無効、AwaitingDraw フェーズでない、または途中流局が確定している場合</exception>
        public Tile ExecuteChaosDraw(ChaosDrawOption option)
        {
            if (option == null)
            {
                throw new ArgumentNullException(nameof(option), "option が null です");
            }

            RequireChaosRules();
            RequireNoPendingAbortiveDraw();
            RequirePhase(TurnPhase.AwaitingDraw);

            return option.Source switch
            {
                ChaosDrawSource.Wall => DrawTile(),
                ChaosDrawSource.DiscardPile => DrawFromDiscardPile(option),
                ChaosDrawSource.OpponentHand => StealFromOpponentHand(option),
                ChaosDrawSource.OpponentMeld => StealFromOpponentMeld(option),
                ChaosDrawSource.OwnMeld => ReturnOwnMeld(option),
                _ => throw new ArgumentException($"未対応の ChaosDrawSource です: {option.Source}", nameof(option)),
            };
        }


        // ========================================
        // プライベートメソッド（カオス麻雀：取得元の列挙）
        // ========================================
        /// <summary>
        /// 自分の副露を戻す選択肢を追加する（補充が発生しないため山の残り枚数によらず常に選べる）
        /// </summary>
        private static void AddOwnMeldOptions(List<ChaosDrawOption> options, PlayerState player)
        {
            for (var meldIndex = 0; meldIndex < player.Hand.Melds.Count; meldIndex++)
            {
                options.Add(ChaosDrawOption.FromOwnMeld(meldIndex));
            }
        }
        /// <summary>
        /// 他家の手牌から奪う選択肢を追加する（奪われた側の補充に山を1枚使う）
        /// </summary>
        private void AddOpponentHandOptions(List<ChaosDrawOption> options, PlayerState target, int targetPlayerIndex)
        {
            if (_wall.RemainingCount < 1)
            {
                return;
            }

            for (var tileIndex = 0; tileIndex < target.Hand.Tiles.Count; tileIndex++)
            {
                options.Add(ChaosDrawOption.FromOpponentHand(targetPlayerIndex, tileIndex));
            }
        }
        /// <summary>
        /// 他家の副露から奪う選択肢を追加する
        /// 必要な補充枚数は副露の枚数で変わるため、副露ごとに山の残り枚数を確認する
        /// </summary>
        private void AddOpponentMeldOptions(List<ChaosDrawOption> options, PlayerState target, int targetPlayerIndex)
        {
            for (var meldIndex = 0; meldIndex < target.Hand.Melds.Count; meldIndex++)
            {
                var meld = target.Hand.Melds[meldIndex];

                if (_wall.RemainingCount < CountReplenishForMeldSteal(meld))
                {
                    continue;
                }

                for (var meldTileIndex = 0; meldTileIndex < meld.Tiles.Count; meldTileIndex++)
                {
                    options.Add(ChaosDrawOption.FromOpponentMeld(targetPlayerIndex, meldIndex, meldTileIndex));
                }
            }
        }
        /// <summary>
        /// 副露から1枚奪われたときに、奪われた側が山から補充する枚数を求める
        /// チー・ポン（3枚）は面子1組ぶんの枠に対して手牌に戻る牌が2枚しかないため1枚不足する
        /// カン（4枚）は3枚戻るため過不足なく、補充は要らない
        /// </summary>
        private static int CountReplenishForMeldSteal(Meld meld)
        {
            return meld.Tiles.Count > MELD_BLOCK_SIZE ? 0 : 1;
        }


        // ========================================
        // プライベートメソッド（カオス麻雀：取得元ごとの処理）
        // ========================================
        /// <summary>
        /// 誰かの河から1枚取る（山を消費しないため補充も発生しない）
        /// フリテン判定用の捨て牌履歴は残すため、河の表示からのみ牌を取り除く（仕様書16.8）
        /// </summary>
        private Tile DrawFromDiscardPile(ChaosDrawOption option)
        {
            var target = RequireTargetPlayer(option, allowSelf: true);

            if (option.TargetIndex < 0 || option.TargetIndex >= target.Discards.Count)
            {
                throw new ArgumentException($"河のインデックスが範囲外です: {option.TargetIndex}", nameof(option));
            }

            var tile = target.RemoveDiscardAt(option.TargetIndex);
            Players[CurrentPlayerIndex].Hand.Draw(tile);
            CompleteChaosDraw();

            return tile;
        }
        /// <summary>
        /// 他家の手牌から1枚奪い、奪われた側は山から補充する
        /// </summary>
        private Tile StealFromOpponentHand(ChaosDrawOption option)
        {
            var victim = RequireTargetPlayer(option, allowSelf: false);

            if (option.TargetIndex < 0 || option.TargetIndex >= victim.Hand.Tiles.Count)
            {
                throw new ArgumentException($"手牌のインデックスが範囲外です: {option.TargetIndex}", nameof(option));
            }

            var tile = victim.Hand.TakeTileAt(option.TargetIndex);
            Players[CurrentPlayerIndex].Hand.Draw(tile);
            ReplenishHand(victim);
            CompleteChaosDraw();

            return tile;
        }
        /// <summary>
        /// 他家の副露から1枚奪う
        /// 面子は成立しなくなるため副露ごと解体し、奪わなかった牌は相手の手牌に戻したうえで
        /// 不足分を山から補充する
        /// </summary>
        private Tile StealFromOpponentMeld(ChaosDrawOption option)
        {
            var victim = RequireTargetPlayer(option, allowSelf: false);

            if (option.TargetIndex < 0 || option.TargetIndex >= victim.Hand.Melds.Count)
            {
                throw new ArgumentException($"副露のインデックスが範囲外です: {option.TargetIndex}", nameof(option));
            }

            var meld = victim.Hand.Melds[option.TargetIndex];

            if (option.MeldTileIndex < 0 || option.MeldTileIndex >= meld.Tiles.Count)
            {
                throw new ArgumentException($"副露内の牌のインデックスが範囲外です: {option.MeldTileIndex}", nameof(option));
            }

            var stolenTile = meld.Tiles[option.MeldTileIndex];
            victim.Hand.RemoveMeld(meld);

            // RemoveMeld で戻された牌そのものを抜くため、種類ではなく参照で位置を特定する
            TakeTileByReference(victim.Hand, stolenTile);
            Players[CurrentPlayerIndex].Hand.Draw(stolenTile);
            ReplenishHand(victim);
            CompleteChaosDraw();

            return stolenTile;
        }
        /// <summary>
        /// 自分の副露を1組そのまま手牌に戻す（仕様書16.5）
        /// カン（4枚）は正規枚数より1枚多くなるため、1枚をツモ牌に見立ててそのまま打牌に進む
        /// チー・ポン（3枚）はちょうど正規枚数になり打牌できないため、打牌せずに手番を終える
        /// </summary>
        private Tile ReturnOwnMeld(ChaosDrawOption option)
        {
            var player = Players[CurrentPlayerIndex];

            if (option.TargetIndex < 0 || option.TargetIndex >= player.Hand.Melds.Count)
            {
                throw new ArgumentException($"副露のインデックスが範囲外です: {option.TargetIndex}", nameof(option));
            }

            var meld = player.Hand.Melds[option.TargetIndex];
            var returnedTiles = player.Hand.RemoveMeld(meld);
            MarkChaosActionTaken();

            if (returnedTiles.Count > MELD_BLOCK_SIZE)
            {
                var drawnTile = TakeTileByReference(player.Hand, returnedTiles[returnedTiles.Count - 1]);
                player.Hand.Draw(drawnTile);
                CompleteChaosDraw();

                return drawnTile;
            }

            _hasDrawnBefore[CurrentPlayerIndex] = true;
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Settings.PlayerCount;
            Phase = TurnPhase.AwaitingDraw;

            return null;
        }


        // ========================================
        // プライベートメソッド（カオス麻雀：共通処理）
        // ========================================
        /// <summary>
        /// 手牌が正規の枚数（13 - 副露数×3）に満ちるまで山から補充する
        /// 牌を奪われた側に対して呼ぶ
        /// </summary>
        /// <exception cref="InvalidOperationException">補充しきる前に山が尽きた場合</exception>
        private void ReplenishHand(PlayerState victim)
        {
            var expectedCount = INITIAL_HAND_SIZE - victim.Hand.Melds.Count * MELD_BLOCK_SIZE;

            while (victim.Hand.Tiles.Count < expectedCount)
            {
                if (_wall.IsEmpty)
                {
                    throw new InvalidOperationException("補充に必要な山牌が足りません。GetChaosDrawOptions で除外されているはずの取得元です");
                }

                victim.Hand.AddTile(_wall.Draw());
            }
        }
        /// <summary>
        /// 山以外から牌を取ったあとの状態を、通常のツモ直後と同じ形に整える
        /// 海底・嶺上・天和地和はいずれも山からのツモを前提とする役のため、すべて成立しない扱いにする
        /// </summary>
        private void CompleteChaosDraw()
        {
            MarkChaosActionTaken();

            _lastDrawWasHaitei = false;
            _lastDrawWasRinshan = false;
            _lastDrawWasFirstDrawForCurrentPlayer = false;
            _hasDrawnBefore[CurrentPlayerIndex] = true;
            TurnIndex++;

            Phase = TurnPhase.AwaitingDiscard;
        }
        /// <summary>
        /// 山以外からの取得・副露の巻き戻しを、副露の成立と同じ「場を乱す行為」として記録する
        /// 全員の一発を消し、天和・地和・九種九牌の成立条件も落とす
        /// 山からのツモを選んだ場合は呼ばないため、リーチ後も通常どおりツモれば一発は残る
        /// </summary>
        private void MarkChaosActionTaken()
        {
            _noCallsYet = false;
            CancelAllIppatsu();
        }
        /// <summary>
        /// 手牌から指定した牌そのもの（同種の別の牌ではなく）を抜き取る
        /// </summary>
        /// <exception cref="InvalidOperationException">その牌が手牌にない場合</exception>
        private static Tile TakeTileByReference(Hand hand, Tile tile)
        {
            for (var i = 0; i < hand.Tiles.Count; i++)
            {
                if (ReferenceEquals(hand.Tiles[i], tile))
                {
                    return hand.TakeTileAt(i);
                }
            }

            throw new InvalidOperationException($"抜き取ろうとした牌が手牌にありません: {tile}");
        }
        /// <summary>
        /// 取得元が指すプレイヤーを取り出し、席順の妥当性を検証する
        /// </summary>
        /// <param name="option">検証する取得元</param>
        /// <param name="allowSelf">自分自身を対象にできるかどうか</param>
        /// <exception cref="ArgumentException">席順が範囲外、または自分自身を対象にできない取得元で自分を指した場合</exception>
        private PlayerState RequireTargetPlayer(ChaosDrawOption option, bool allowSelf)
        {
            if (option.TargetPlayerIndex < 0 || option.TargetPlayerIndex >= Settings.PlayerCount)
            {
                throw new ArgumentException($"対象プレイヤーの席順が範囲外です: {option.TargetPlayerIndex}", nameof(option));
            }

            if (!allowSelf && option.TargetPlayerIndex == CurrentPlayerIndex)
            {
                throw new ArgumentException($"{option.Source} では自分自身を対象にできません", nameof(option));
            }

            return Players[option.TargetPlayerIndex];
        }
        /// <summary>
        /// カオス麻雀ルールが有効かどうかを検証する
        /// </summary>
        /// <exception cref="InvalidOperationException">カオス麻雀ルールが無効の場合</exception>
        private void RequireChaosRules()
        {
            if (!Settings.UseChaosRules)
            {
                throw new InvalidOperationException("カオス麻雀ルールが無効です。GameSettings.UseChaosRules を有効にしてください");
            }
        }
    }
}
