import { StaticPage } from './StaticPage';

const CONTACT_EMAIL = 'support@ramen-zukan.example';

export function ContactPage() {
  return (
    <StaticPage title="お問い合わせ" subtitle="Contact">
      <p>
        掲載内容の誤り・追加のご要望・その他のお問い合わせは、下記メールアドレスまでご連絡ください。
      </p>
      <p>
        <a className="static-link" href={`mailto:${CONTACT_EMAIL}`}>
          {CONTACT_EMAIL}
        </a>
      </p>
      <h3>お問い合わせ時のお願い</h3>
      <ul>
        <li>店舗情報の修正依頼は、対象の店舗名をご記載ください。</li>
        <li>返信には数日いただく場合があります。</li>
      </ul>
    </StaticPage>
  );
}
