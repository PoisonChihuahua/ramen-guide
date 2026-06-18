import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { askShops } from '../api/rag';
import { ShopCard } from '../components/ShopCard';

const EXAMPLES = [
  'あっさりした塩ラーメンが食べたい',
  '濃厚な豚骨で替え玉できる店',
  '札幌で味噌ラーメン',
];

export function AskPage() {
  const [question, setQuestion] = useState('');

  const {
    mutate,
    data,
    isPending,
    isError,
  } = useMutation({
    mutationFn: (q: string) => askShops(q),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = question.trim();
    if (trimmed) {
      mutate(trimmed);
    }
  };

  return (
    <main className="ask-page">
      <section className="list-section">
        <div className="section-head">
          <div>
            <h2 className="section-title">AI 自然文検索</h2>
            <p className="section-sub">Ask in natural language</p>
          </div>
        </div>

        <p className="state-message">
          「{EXAMPLES[0]}」のように、ふだんの言葉で聞いてみてください。
        </p>

        <form className="ask-form" onSubmit={handleSubmit}>
          <label htmlFor="ask-question" className="visually-hidden">
            質問
          </label>
          <textarea
            id="ask-question"
            className="ask-input"
            rows={2}
            placeholder="例: あっさりした塩ラーメンが食べたい"
            value={question}
            onChange={(e) => setQuestion(e.target.value)}
          />
          <button
            className="nav-link nav-link--primary"
            type="submit"
            disabled={isPending || question.trim() === ''}
          >
            {isPending ? '検索中...' : '検索する'}
          </button>
        </form>

        <div className="ask-examples">
          {EXAMPLES.map((ex) => (
            <button
              key={ex}
              type="button"
              className="ask-example"
              onClick={() => setQuestion(ex)}
            >
              {ex}
            </button>
          ))}
        </div>

        {isError && (
          <p className="state-message state-error">検索に失敗しました。</p>
        )}

        {data && (
          <>
            <div className="ask-answer">
              <h3 className="section-title">回答</h3>
              <p>{data.answer}</p>
            </div>

            {data.matches.length > 0 && (
              <div className="shop-grid">
                {data.matches.map((m) => (
                  <ShopCard key={m.shop.id} shop={m.shop} />
                ))}
              </div>
            )}
          </>
        )}
      </section>
    </main>
  );
}
