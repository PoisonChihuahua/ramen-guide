import { Link } from 'react-router-dom';
import { StaticPage } from './StaticPage';

export function AboutPage() {
  return (
    <StaticPage title="このサイトについて" subtitle="About">
      <p>
        「ラーメン図鑑」は、全国のラーメン店を写真とともに紹介するガイドサイトです。きょうの気分とエリアから、あなたの一杯を見つけるお手伝いをします。
      </p>
      <h3>できること</h3>
      <ul>
        <li>店名・特徴のキーワード、ジャンル、エリアによる検索・絞り込み</li>
        <li>各店舗の写真・営業時間・価格帯などの詳細情報の閲覧</li>
        <li>気になるお店の「お気に入り」登録（この端末に保存されます）</li>
      </ul>
      <h3>掲載情報について</h3>
      <p>
        掲載しているデータはサンプルを含む参考情報です。営業時間や価格は変更される場合がありますので、お出かけの前に各店舗の最新情報をご確認ください。
      </p>
      <p>
        <Link to="/" className="back-link">
          店舗一覧を見る
        </Link>
      </p>
    </StaticPage>
  );
}
