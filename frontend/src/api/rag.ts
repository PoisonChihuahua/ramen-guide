import { apiFetch } from './client';
import type { AskResponse } from '../types';

/**
 * 自然文検索（RAG）。質問文をバックエンドへ送り、関連店舗と
 * それを根拠に生成された回答を受け取る。通信は必ず apiFetch 経由。
 */
export function askShops(question: string, topK?: number): Promise<AskResponse> {
  return apiFetch<AskResponse>('/api/shops/ask', {
    method: 'POST',
    body: { question, topK },
  });
}
