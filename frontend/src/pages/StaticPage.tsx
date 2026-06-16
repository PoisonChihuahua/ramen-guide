import type { ReactNode } from 'react';

interface StaticPageProps {
  title: string;
  subtitle: string;
  children: ReactNode;
}

/** サイト情報系の静的ページ共通レイアウト。 */
export function StaticPage({ title, subtitle, children }: StaticPageProps) {
  return (
    <main className="shop-list-page">
      <section className="list-section">
        <div className="section-head">
          <div>
            <h2 className="section-title">{title}</h2>
            <p className="section-sub">{subtitle}</p>
          </div>
        </div>
        <div className="static-body">{children}</div>
      </section>
    </main>
  );
}
